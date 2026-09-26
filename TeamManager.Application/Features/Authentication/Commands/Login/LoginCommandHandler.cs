using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.UserExceptions;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService, IRefreshTokenService refreshTokenService, IUnitOfWork unitOfWork,
        ICurrentUser currentUser, IOutbox outbox) : IRequestHandler<LoginCommand, LoginResponse>
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

        var accessToken = accessTokenService.GenerateAccessToken(user);

        var refreshToken = refreshTokenService.GenerateToken();

        var refreshTokenHash = refreshTokenService.HashToken(refreshToken);


        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            user.RecordSuccessfulLogin();

            var refreshTokenEntity = new Domain.Entities.RefreshToken(Guid.NewGuid(), user.Id, refreshTokenHash,
                refreshTokenService.GetExpiration(), currentUser.DeviceInfo, currentUser.IpAddress);

            await userRepository.AddRefreshTokenAsync(refreshTokenEntity, ct);

            if (wasInactive)
            {
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