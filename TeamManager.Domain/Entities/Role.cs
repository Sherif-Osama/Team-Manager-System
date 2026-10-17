using TeamManager.Domain.Common;
using TeamManager.Domain.Common.Events.Roles;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class Role : AggregateRoot<int>
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

    public void AddPermission(int permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
            throw new DomainException("Role already has this permission.");

        _rolePermissions.Add(new RolePermission(Id, permissionId));

        AddDomainEvent(new RolePermissionAddedDomainEvent(Id, permissionId));
    }

    public void RemovePermission(int permissionId)
    {
        var rolePermission = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);

        if (rolePermission is null)
            throw new DomainException("Role does not have this permission.");

        _rolePermissions.Remove(rolePermission);

        AddDomainEvent(new RolePermissionRemovedDomainEvent(Id, permissionId));
    }
}
