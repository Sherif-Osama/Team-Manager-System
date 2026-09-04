using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.DeleteMyAccount
{
    public sealed class DeleteMyAccountCommandHandler(ICurrentUser currentUser, IUserRepository userRepository,
        ITeamRepository teamRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, IOutbox outbox) : IRequestHandler<DeleteMyAccountCommand>
    {
        public async Task Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(userId);

            if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw new ForbiddenException("Invalid password.");

            var hasActiveOwnedTeams = await teamRepository.HasActiveOwnedTeamsAsync(userId, cancellationToken);

            if (hasActiveOwnedTeams)
                throw new UserOwnsActiveTeamException(userId);

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var isLastSystemAdmin = await userRepository.IsLastSystemAdminAsync(user.Id, ct);

                if (isLastSystemAdmin)
                    throw new ForbiddenException("The last system administrator cannot delete their account.");

                user.SoftDelete();

                await teamRepository.DeactivateActiveMembershipsAsync(userId, ct);

                await userRepository.RevokeAllRefreshTokensAsync(userId, ct);

                var payload = JsonSerializer.Serialize(new
                {
                    To = user.Email,
                    DeletedAtUtc = DateTime.UtcNow,
                    DeviceInfo = currentUser.DeviceInfo
                });

                outbox.Add(OutboxMessageType.AccountDeletedEmail, payload);

            }, cancellationToken);
        }
    }
}
