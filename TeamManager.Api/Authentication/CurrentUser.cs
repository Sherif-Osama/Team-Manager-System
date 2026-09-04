using System.Security.Claims;
using TeamManager.Application.Abstractions.Authentication;

namespace TeamManager.Api.Authentication
{
    public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public Guid? UserId
        {
            get
            {
                var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                return Guid.TryParse(value, out var userId) ? userId : null;
            }
        }

        public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

        public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

        public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public string? DeviceInfo => httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
    }
}