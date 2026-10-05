using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Commands.AddTaskAttachment
{
    public sealed class AddTaskAttachmentCommandValidator : AbstractValidator<AddTaskAttachmentCommand>
    {
        private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx"];

        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public AddTaskAttachmentCommandValidator()
        {
            RuleFor(x => x.TaskId).GreaterThan(0);

            RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);

            RuleFor(x => x.ContentType).NotEmpty().MaximumLength(150);

            RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("File size must not exceed 10 MB.");

            RuleFor(x => x.FileName).Must(fileName => AllowedExtensions.Contains(Path.GetExtension(fileName)
                .ToLowerInvariant())).WithMessage("This file type is not allowed.");

            RuleFor(x => x.Content).NotNull();
        }
    }
}