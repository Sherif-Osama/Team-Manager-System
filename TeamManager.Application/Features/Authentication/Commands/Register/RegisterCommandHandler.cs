using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Entities;

namespace TeamManager.Application.Features.Authentication.Commands.Register
{
    public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork UnitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = UnitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var exists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);

            if (exists)
                throw new EmailAlreadyExistsException(request.Email);

            var passwordHash = _passwordHasher.Hash(request.Password);

            var user = new User(Guid.NewGuid(), request.Email, request.DisplayName, passwordHash);

            await _userRepository.AddAsync(user, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}