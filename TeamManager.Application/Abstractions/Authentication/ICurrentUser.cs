namespace TeamManager.Application.Abstractions.Authentication
{
    public interface ICurrentUser
    {
        Guid? UserId { get; }

        string? Email { get; }

        bool IsAuthenticated { get; }

        string? IpAddress { get; }

        string? DeviceInfo { get; }
    }
}
