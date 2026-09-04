using TeamManager.Domain.Common;

namespace TeamManager.Domain.Entities;

public class UserRole : Entity<(Guid UserId, int RoleId)>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public int RoleId { get; private set; }
    public Role Role { get; private set; } = null!;
    public DateTime AssignedAtUtc { get; private set; }

    private UserRole() { }

    public UserRole(Guid userId, int roleId)
    {
        UserId = userId;
        RoleId = roleId;
        Id = (userId, roleId);
        AssignedAtUtc = DateTime.UtcNow;
    }
}