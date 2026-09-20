using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;

namespace TeamManager.Application.Features.Admin.Commands.BootstrapAdmin
{
    public sealed record BootstrapAdminCommand(string Email, string Secret) : IRequest, IRequiresConfirmedEmail;
    //User must be registered and confirmed his account to get first admin role.
}
