using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Features.Authentication.Commands.Login;

namespace TeamManager.Application.Features.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenService refreshTokenService,
        IAccessTokenService accessTokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _refreshTokenService = refreshTokenService;
        _accessTokenService = accessTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);

        var refreshToken = await _userRepository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var newRefreshToken = _refreshTokenService.GenerateToken();

        var newRefreshTokenHash = _refreshTokenService.HashToken(newRefreshToken);

        var newRefreshTokenEntity = new Domain.Entities.RefreshToken(Guid.NewGuid(), user.Id, newRefreshTokenHash, DateTime.UtcNow.AddDays(7));

        await _userRepository.AddRefreshTokenAsync(newRefreshTokenEntity, cancellationToken);

        refreshToken.Revoke(newRefreshTokenEntity.Id);

        var accessToken = _accessTokenService.GenerateAccessToken(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, newRefreshToken);
    }
}