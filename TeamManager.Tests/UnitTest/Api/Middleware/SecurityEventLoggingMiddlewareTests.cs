using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TeamManager.Api.Middleware;

namespace TeamManager.Tests.UnitTest.Api.Middleware;

public sealed class SecurityEventLoggingMiddlewareTests
{
    private readonly Mock<ILogger<SecurityEventLoggingMiddleware>> _logger = new();
    private readonly SecurityEventLoggingMiddleware _sut;

    public SecurityEventLoggingMiddlewareTests()
    {
        _sut = new(_ => Task.CompletedTask, _logger.Object);
    }

    private static HttpContext CreateContext(int statusCode)
    {
        var context = new DefaultHttpContext { Response = { StatusCode = statusCode } };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));
        return context;
    }

    [Fact]
    public async Task InvokeAsync_With401Response_LogsAuthenticationChallenge()
    {
        await _sut.InvokeAsync(CreateContext(StatusCodes.Status401Unauthorized));

        _logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_With403Response_LogsAuthorizationDenied()
    {
        await _sut.InvokeAsync(CreateContext(StatusCodes.Status403Forbidden));

        _logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithOtherStatusCodes_DoesNotLog()
    {
        await _sut.InvokeAsync(CreateContext(StatusCodes.Status200OK));
        await _sut.InvokeAsync(CreateContext(StatusCodes.Status500InternalServerError));

        _logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }
}
