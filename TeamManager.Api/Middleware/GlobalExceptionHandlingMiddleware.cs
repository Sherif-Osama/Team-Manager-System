using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Api.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (exception is ValidationException validationException)
            {
                var errors = validationException.Errors.GroupBy(x => x.PropertyName)
                    .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).ToArray());

                var ValidationProblemDetails = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(ValidationProblemDetails);
                return;
            }

            var statusCode = exception switch
            {
                EmailAlreadyExistsException => StatusCodes.Status409Conflict,
                TeamNameAlreadyExistsException => StatusCodes.Status409Conflict,
                AccountLockedException => StatusCodes.Status423Locked,
                UserNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                DomainException => StatusCodes.Status400BadRequest,
                TeamNotFoundException => StatusCodes.Status404NotFound,
                UserNotMemberOfTeamException => StatusCodes.Status404NotFound,
                InvitationNotFoundException => StatusCodes.Status404NotFound,
                ForbiddenException => StatusCodes.Status403Forbidden,
                TeamMemberNotFoundException => StatusCodes.Status404NotFound,
                DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = exception.Message,
                Instance = context.Request.Path
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails);
        }

        private static string GetTitle(int statusCode) =>
            statusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status423Locked => "Account Locked",
                StatusCodes.Status403Forbidden => "Forbidden",
                _ => "Internal Server Error"
            };
    }
}