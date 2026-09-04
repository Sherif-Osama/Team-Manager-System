using MediatR;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.ChangePassword
{
    public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        : IRequest;
}