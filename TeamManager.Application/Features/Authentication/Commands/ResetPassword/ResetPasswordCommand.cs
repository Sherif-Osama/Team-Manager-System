using MediatR;
namespace TeamManager.Application.Features.Authentication.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword, string ConfirmNewPassword) : IRequest;
}