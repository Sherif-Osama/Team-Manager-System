using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher,
        IAccessTokenService accessTokenService, IRefreshTokenService refreshTokenService, IUnitOfWork unitOfWork,
        ICurrentUser currentUser) : IRequestHandler<LoginCommand, LoginResponse>
{


    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !user.IsActive)
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

        var accessToken = accessTokenService.GenerateAccessToken(user);

        var refreshToken = refreshTokenService.GenerateToken();

        var refreshTokenHash = refreshTokenService.HashToken(refreshToken);

        var refreshTokenEntity = new Domain.Entities.RefreshToken(Guid.NewGuid(), user.Id,
            refreshTokenHash, refreshTokenService.GetExpiration(), currentUser.DeviceInfo, currentUser.IpAddress);

        await userRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);

        user.RecordSuccessfulLogin();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, refreshToken);
    }
}