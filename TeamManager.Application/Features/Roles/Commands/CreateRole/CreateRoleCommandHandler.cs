using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.RoleExceptions;
using TeamManager.Domain.Entities;

namespace TeamManager.Application.Features.Roles.Commands.CreateRole
{
    public sealed class CreateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateRoleCommand, int>
    {
        public async Task<int> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            var name = request.Name.Trim();

            var existingRole = await roleRepository.GetByNameAsync(name, cancellationToken);

            if (existingRole is not null)
                throw new RoleNameAlreadyExistsException(name);

            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

            var role = new Role(name, description);

            await roleRepository.AddAsync(role, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return role.Id;
        }
    }
}