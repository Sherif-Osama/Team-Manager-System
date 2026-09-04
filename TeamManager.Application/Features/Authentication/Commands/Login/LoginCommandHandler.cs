using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService, IRefreshTokenService refreshTokenService, IUnitOfWork unitOfWork,
        ICurrentUser currentUser, IOutbox outbox, ITeamRepository teamRepository) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");


        if (user.IsLockedOut)
            throw new AccountLockedException(user.Email, user.LockoutEndUtc);

        var isPasswordValid = passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            user.RecordFailedLoginAttempt();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var wasInactive = !user.IsActive;

        user.RecordSuccessfulLogin();

        var accessToken = accessTokenService.GenerateAccessToken(user);

        var refreshToken = refreshTokenService.GenerateToken();

        var refreshTokenHash = refreshTokenService.HashToken(refreshToken);

        var refreshTokenEntity = new Domain.Entities.RefreshToken(Guid.NewGuid(), user.Id,
            refreshTokenHash, refreshTokenService.GetExpiration(), currentUser.DeviceInfo, currentUser.IpAddress);

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await userRepository.AddRefreshTokenAsync(refreshTokenEntity, ct);
            if (wasInactive)
            {
                await teamRepository.ReactivateSuspendedMembershipsAsync(user.Id, ct);

                var payload = JsonSerializer.Serialize(new
                {
                    To = user.Email,
                    ActivatedAtUtc = DateTime.UtcNow,
                    DeviceInfo = currentUser.DeviceInfo
                });

                outbox.Add(OutboxMessageType.AccountActivationEmail, payload);
            }

        }, cancellationToken);

        return new LoginResponse(accessToken, refreshToken);
    }
}