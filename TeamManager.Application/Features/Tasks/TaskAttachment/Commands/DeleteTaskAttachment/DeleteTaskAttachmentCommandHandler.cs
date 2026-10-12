using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Storage;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Commands.DeleteTaskAttachment
{
    public sealed class DeleteTaskAttachmentCommandHandler(ITaskRepository taskRepository, IFileStorage fileStorage,
        IUnitOfWork unitOfWork) : IRequestHandler<DeleteTaskAttachmentCommand>
    {
        public async Task Handle(DeleteTaskAttachmentCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdWithAttachmentAsync(request.TaskId, request.AttachmentId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var attachment = task.RemoveAttachment(request.AttachmentId);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await fileStorage.DeleteAsync(attachment.StorageKey, cancellationToken);
        }
    }
}