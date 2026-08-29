using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Features.Authentication.Commands.Login;

namespace TeamManager.Application.Features.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(IUserRepository userRepository, IRefreshTokenService refreshTokenService,
        IAccessTokenService accessTokenService, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenService.HashToken(request.RefreshToken);

        var refreshToken = await userRepository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var user = await userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var newRefreshToken = refreshTokenService.GenerateToken();

        var newRefreshTokenHash = refreshTokenService.HashToken(newRefreshToken);

        var newRefreshTokenEntity = new Domain.Entities.RefreshToken(Guid.NewGuid(), user.Id, newRefreshTokenHash, refreshTokenService.GetExpiration()
            , currentUser.DeviceInfo, currentUser.IpAddress);

        await userRepository.AddRefreshTokenAsync(newRefreshTokenEntity, cancellationToken);

        refreshToken.Revoke(newRefreshTokenEntity.Id);

        var accessToken = accessTokenService.GenerateAccessToken(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, newRefreshToken);
    }
}