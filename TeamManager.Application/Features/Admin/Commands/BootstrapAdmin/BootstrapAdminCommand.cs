using MediatR;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Admin.Commands.BootstrapAdmin
{
    public sealed record BootstrapAdminCommand(string Email, string Secret) : IRequest, IRequiresConfirmedEmail;
}