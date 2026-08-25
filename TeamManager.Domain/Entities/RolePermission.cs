using TeamManager.Domain.Common;

namespace TeamManager.Domain.Entities;

public class RolePermission : Entity<(int RoleId, int PermissionId)>
{
    public int RoleId { get; private set; }
    public Role Role { get; private set; } = null!;
    public int PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    private RolePermission()
    {
    }

    public RolePermission(int roleId, int permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        Id = (roleId, permissionId);
    }
}
