using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class Role : Entity<int>
{
    private readonly List<RolePermission> _rolePermissions = new();
    private readonly List<UserRole> _userRoles = new();

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role()
    {
    }

    public Role(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A role must have a name.");

        Name = name;
        Description = description;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A role must have a name.");

        Name = name;
    }
}
