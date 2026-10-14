using System.Security.Claims;

namespace TeamManager.Api.Middleware
{
    public sealed class SecurityEventLoggingMiddleware(RequestDelegate next, ILogger<SecurityEventLoggingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            await next(context);

            var statusCode = context.Response.StatusCode;


            if (statusCode is not StatusCodes.Status401Unauthorized and not StatusCodes.Status403Forbidden)
                return;


            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value;

            var eventName = statusCode switch
            {
                StatusCodes.Status401Unauthorized => "security.authentication.challenge",

                StatusCodes.Status403Forbidden => "security.authorization.denied",

                _ => throw new InvalidOperationException()
            };

            var reason = context.Items.TryGetValue("SecurityEventReason", out var reasonValue) ? reasonValue?.ToString() : null;

            var exceptionType = context.Items.TryGetValue("SecurityEventExceptionType", out var exceptionTypeValue)
                ? exceptionTypeValue?.ToString() : null;

            logger.LogWarning("Security event {EventName}. Reason: {Reason}. " +
                "ExceptionType: {ExceptionType}. Method: {HttpMethod}. " + "Path: {Path}. StatusCode: {StatusCode}. " +
                "UserId: {UserId}. ClientIp: {ClientIp}. TraceId: {TraceId}", eventName, reason ?? "unspecified", exceptionType,
                context.Request.Method, context.Request.Path.Value, statusCode, userId,
                context.Connection.RemoteIpAddress?.ToString(), context.TraceIdentifier);
        }
    }
}