using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Authentication.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(IUserRepository userRepository, IRefreshTokenService refreshTokenService, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _refreshTokenService = refreshTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);

        var refreshToken = await _userRepository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
            return;

        refreshToken.Revoke();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}