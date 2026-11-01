using FluentValidation;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Commands.AddTaskAttachment
{
    public sealed class AddTaskAttachmentCommandValidator : AbstractValidator<AddTaskAttachmentCommand>
    {
        private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx"];

        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public AddTaskAttachmentCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .GreaterThan(0)
                .WithMessage("Task ID must be greater than 0.");

            RuleFor(x => x.FileName)
                .NotEmpty()
                .WithMessage("File name is required.")
                .MaximumLength(260)
                .WithMessage("File name must not exceed 260 characters.");

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .WithMessage("Content type is required.")
                .MaximumLength(150)
                .WithMessage("Content type must not exceed 150 characters.");

            RuleFor(x => x.SizeBytes)
                .GreaterThan(0)
                .WithMessage("File size must be greater than 0 bytes.")
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage("File size must not exceed 10 MB.");

            RuleFor(x => x.FileName)
                .Must(fileName => AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
                .WithMessage("This file type is not allowed.");

            RuleFor(x => x.Content)
                .NotNull()
                .WithMessage("File content is required.");
        }
    }
}
