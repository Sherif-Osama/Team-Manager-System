using FluentValidation;

namespace TeamManager.Application.Features.Notifications.Commands.MarkAsRead
{
    public sealed class MarkAsReadCommandValidator : AbstractValidator<MarkAsReadCommand>
    {
        public MarkAsReadCommandValidator()
        {
            RuleFor(x => x.NotificationId).GreaterThan(0);
        }
    }
}