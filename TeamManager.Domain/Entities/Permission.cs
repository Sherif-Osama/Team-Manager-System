using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class Permission : Entity<int>
{
    private readonly List<RolePermission> _rolePermissions = new();

    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Permission() { }

    public Permission(string code, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("A permission must have a code.");

        Code = code;
        Description = description;
    }
}
