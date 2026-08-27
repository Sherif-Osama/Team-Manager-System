using System.Security.Claims;
using TeamManager.Application.Abstractions.Authentication;

namespace TeamManager.Api.Authentication
{
    public sealed class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

                return Guid.TryParse(value, out var userId) ? userId : null;
            }
        }

        public string? Email => _httpContextAccessor.HttpContext?.User.FindFirstValue("email");

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

        public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public string? DeviceInfo => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
    }
}