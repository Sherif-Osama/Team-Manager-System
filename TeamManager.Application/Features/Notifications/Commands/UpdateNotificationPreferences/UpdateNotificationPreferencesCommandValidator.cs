using FluentValidation;
namespace TeamManager.Application.Features.Notifications.Commands.UpdateNotificationPreferences
{
    public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
    {
        public UpdateNotificationPreferencesCommandValidator()
        {
            RuleFor(x => x.Preferences).NotEmpty();

            RuleForEach(x => x.Preferences).ChildRules(preference => preference.RuleFor(x => x.NotificationType).IsInEnum());

            RuleFor(x => x.Preferences).Must(preferences => preferences.Select(x => x.NotificationType)
            .Distinct().Count() == preferences.Count).WithMessage("Duplicate notification types are not allowed.");
        }
    }
}
