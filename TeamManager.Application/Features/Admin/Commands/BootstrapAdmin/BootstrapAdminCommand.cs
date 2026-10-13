using MediatR;
using TeamManager.Application.Abstractions.Scopes;

namespace TeamManager.Application.Features.Admin.Commands.BootstrapAdmin
{
    public sealed record BootstrapAdminCommand(string Email, string Secret) : IRequest, IRequiresConfirmedEmail;
    //User must be registered and confirmed his account to get first admin role.
}
