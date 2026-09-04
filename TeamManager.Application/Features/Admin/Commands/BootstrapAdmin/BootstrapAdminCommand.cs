using MediatR;

namespace TeamManager.Application.Features.Admin.Commands.BootstrapAdmin
{
    public sealed record BootstrapAdminCommand(string Email, string Secret) : IRequest;
}