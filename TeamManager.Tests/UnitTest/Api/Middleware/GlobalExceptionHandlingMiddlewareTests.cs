using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TeamManager.Api.Middleware;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.PermissionExceptions;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Application.Common.Exceptions.RoleExceptions;
using TeamManager.Application.Common.Exceptions.UserExceptions;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlingMiddleware>> _logger = new();

    private async Task<HttpContext> InvokeAsync(Exception exception)
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var middleware = new GlobalExceptionHandlingMiddleware(_ => throw exception, _logger.Object);

        await middleware.InvokeAsync(context);

        return context;
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return JsonDocument.Parse(await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task InvokeAsync_WithDomainException_Returns400()
    {
        var context = await InvokeAsync(new DomainException("invalid state"));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_Returns401AndFlagsSecurityEvent()
    {
        var context = await InvokeAsync(new UnauthorizedAccessException("nope"));

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("unauthorized_access", context.Items["SecurityEventReason"]);
        Assert.Equal(nameof(UnauthorizedAccessException), context.Items["SecurityEventExceptionType"]);
    }

    [Fact]
    public async Task InvokeAsync_WithForbiddenException_Returns403AndFlagsSecurityEvent()
    {
        var context = await InvokeAsync(new ForbiddenException("no permission"));

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("permission_denied", context.Items["SecurityEventReason"]);
    }

    public static TheoryData<Type, object[]> NotFoundExceptions => new()
    {
    { typeof(UserNotFoundException), [Guid.Empty] },
    { typeof(ProjectNotFoundException), [Guid.Empty] },
    { typeof(PermissionNotFoundException), [0] }
    };

    [Theory]
    [MemberData(nameof(NotFoundExceptions))]
    public async Task InvokeAsync_WithNotFoundExceptions_Returns404(Type exceptionType, object[] arguments)
    {
        var exception = (Exception)exceptionType.GetConstructors()[0].Invoke(arguments);

        var context = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithConflictExceptions_Returns409()
    {
        var context = await InvokeAsync(new EmailAlreadyExistsException("x@example.com"));

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithRoleInUseException_Returns409()
    {
        var context = await InvokeAsync(new RoleInUseException(3));

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithConcurrencyException_Returns409()
    {
        var context = await InvokeAsync(new DbUpdateConcurrencyException());

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithAccountLockedException_Returns423()
    {
        var context = await InvokeAsync(new AccountLockedException("x@example.com", DateTime.UtcNow.AddMinutes(10)));

        Assert.Equal(StatusCodes.Status423Locked, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_Returns400WithProblemDetails()
    {
        var exception = new ValidationException([new ValidationFailure("Name", "Name is required.")]);
        var context = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var body = await ReadBodyAsync(context);
        Assert.Equal("Validation failed", body.RootElement.GetProperty("title").GetString());
        Assert.True(body.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Name", out _));
    }

    [Fact]
    public async Task InvokeAsync_WhenClientAbortsRequest_PropagatesCancellationWithoutWritingResponse()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        context.RequestAborted = new CancellationToken(canceled: true);
        var middleware = new GlobalExceptionHandlingMiddleware(_ => throw new OperationCanceledException(), _logger.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));

        Assert.Equal(0, context.Response.Body.Length);
        _logger.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithUnhandledException_Returns500RedactedAndLogs()
    {
        var context = await InvokeAsync(new InvalidOperationException("secret internal detail"));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var body = await ReadBodyAsync(context);
        Assert.Equal("An unhandled error occurred.", body.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("secret internal detail", body.RootElement.GetRawText());

        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
