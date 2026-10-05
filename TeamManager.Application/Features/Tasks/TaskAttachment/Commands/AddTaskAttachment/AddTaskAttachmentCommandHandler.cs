using MediatR;
using System.Security.Cryptography;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Storage;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskAttachment.Commands.AddTaskAttachment
{
    public sealed class AddTaskAttachmentCommandHandler(ITaskRepository taskRepository, IFileStorage fileStorage,
        ICurrentUser currentUser, IUnitOfWork unitOfWork) : IRequestHandler<AddTaskAttachmentCommand, long>
    {
        public async Task<long> Handle(AddTaskAttachmentCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var hashBytes = await SHA256.HashDataAsync(request.Content, cancellationToken);

            var fileHash = Convert.ToHexString(hashBytes);

            var exists = await taskRepository.AttachmentExistsByHashAsync(request.TaskId, fileHash, cancellationToken);

            if (exists)
                throw new TaskAttachmentAlreadyExistsException();

            var extension = Path.GetExtension(request.FileName);

            var storageKey = $"{Guid.NewGuid()}{extension}";

            await fileStorage.SaveAsync(request.Content, storageKey, cancellationToken);

            var attachment = task.AddAttachment(request.FileName, storageKey, request.ContentType, request.SizeBytes,
                currentUser.UserId.Value, fileHash);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return attachment.Id;
        }
    }
}