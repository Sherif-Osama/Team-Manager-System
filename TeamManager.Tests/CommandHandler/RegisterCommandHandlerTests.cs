using Moq;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;
using TeamManager.Application.Features.Authentication.Commands.Register;
using TeamManager.Domain.Entities;
namespace TeamManager.Tests.CommandHandler
{
    public sealed class RegisterCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly Mock<IEmailConfirmationTokenService> _tokenService = new();
        private readonly Mock<IRoleRepository> _roleRepository = new();
        private readonly Mock<IOutbox> _outbox = new();
        private readonly RegisterCommandHandler _sut;
        private readonly Role role = new Role("User");

        public RegisterCommandHandlerTests()
        {
            _sut = new(_userRepository.Object, _passwordHasher.Object, _unitOfWork.Object, _teamRepository.Object,
                _tokenService.Object, _roleRepository.Object, _outbox.Object);

            //Happy path
            _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed-password");
            _tokenService.Setup(t => t.HashToken(It.IsAny<string>())).Returns("hashed-token");
            _roleRepository.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(role);
            _tokenService.Setup(t => t.GenerateToken()).Returns("token");
            _tokenService.Setup(t => t.HashToken(It.IsAny<string>())).Returns("hashed-token");
        }

        private readonly RegisterCommand Request = new("user@example.com", "Test User", "Password123!");

        [Fact]
        public async Task Handle_WhenEmailAlreadyExists_ThrowsWithoutStartingTransaction()
        {
            _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => _sut.Handle(Request, CancellationToken.None));

            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task Handle_WhenEmailDoesNotExist_CreatesUserWithRoleAndConfirmationData()
        {
            User NewUser = null!;

            _userRepository.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((user, _) => { NewUser = user; }).Returns(Task.CompletedTask);

            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>())).Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var id = await _sut.Handle(Request, CancellationToken.None);
            Assert.NotNull(NewUser);
            Assert.Equal(id, NewUser.Id);
            Assert.Equal(Request.Email, NewUser.Email);
            Assert.Equal(Request.DisplayName, NewUser.DisplayName);
            Assert.Equal("hashed-password", NewUser.PasswordHash);
            Assert.Single(NewUser.UserRoles, x => x.RoleId == role.Id);
            Assert.Equal("hashed-token", NewUser.EmailConfirmationTokenHash);
            Assert.True(NewUser.EmailConfirmationTokenExpiresAtUtc > DateTime.UtcNow);

            _userRepository.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _teamRepository.Verify(t =>
            t.LinkPendingInvitationsToUserAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
            _outbox.Verify(o => o.Add(It.IsAny<OutboxMessageType>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenDefaultRoleIsMissing_ThrowsBeforeTransaction()
        {
            _roleRepository.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

            await Assert.ThrowsAsync<DefaultRoleNotFoundException>(() => _sut.Handle(Request, CancellationToken.None));

            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task Handle_SavesBeforeLinksAndConfirmationOutboxMessage()
        {
            var events = new List<int>();

            string? outboxPayload = null;

            _userRepository.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((_, _) =>
                {
                    events.Add(1);
                }).Returns(Task.CompletedTask);

            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>())).Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

            _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => events.Add(2)).ReturnsAsync(1);

            _teamRepository.Setup(t => t.LinkPendingInvitationsToUserAsync(Request.Email, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback(() => events.Add(3)).Returns(Task.CompletedTask);

            _outbox.Setup(o => o.Add(OutboxMessageType.EmailConfirmationEmail, It.IsAny<string>()))
                .Callback<OutboxMessageType, string>((_, payload) =>
                {
                    events.Add(4);
                    outboxPayload = payload;
                });

            await _sut.Handle(Request, CancellationToken.None);

            Assert.Equal(new[] { 1, 2, 3, 4 }, events);

            Assert.NotNull(outboxPayload);

            using var payload = JsonDocument.Parse(outboxPayload!);

            Assert.Equal(Request.Email, payload.RootElement.GetProperty("To").GetString());

            Assert.Equal("token", payload.RootElement.GetProperty("Token").GetString());

            _userRepository.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);

            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _teamRepository.Verify(t => t.LinkPendingInvitationsToUserAsync(Request.Email, It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _outbox.Verify(o => o.Add(OutboxMessageType.EmailConfirmationEmail, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenAddingUserFails_RollsBack()
        {
            _userRepository.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            bool rolledBack = false;

            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>())).Returns<
                    Func<CancellationToken, Task>, CancellationToken>(async (action, ct) =>
                    {
                        try
                        {

                            await action(ct);
                        }
                        catch (InvalidOperationException)
                        {
                            rolledBack = true;
                            throw;
                        }
                    });

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(Request, CancellationToken.None));
            Assert.True(rolledBack);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _teamRepository.Verify(t => t.LinkPendingInvitationsToUserAsync(It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _outbox.Verify(o => o.Add(It.IsAny<OutboxMessageType>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSavingUserFails_RollsBackBeforeLinking()
        {
            _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            bool rolledBack = false;

            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>())).Returns<
                    Func<CancellationToken, Task>, CancellationToken>(async (action, ct) =>
                    {
                        try
                        {

                            await action(ct);
                        }
                        catch (InvalidOperationException)
                        {
                            rolledBack = true;
                            throw;
                        }
                    });

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(Request, CancellationToken.None));
            _userRepository.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _teamRepository.Verify(t =>
            t.LinkPendingInvitationsToUserAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _outbox.Verify(o => o.Add(It.IsAny<OutboxMessageType>(), It.IsAny<string>()), Times.Never);
            Assert.True(rolledBack);
        }

        [Fact]
        public async Task Handle_WhenLinkingInvitationsFails_RollsBackBeforeOutbox()
        {
            _teamRepository.Setup(t => t.LinkPendingInvitationsToUserAsync(It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            bool rolledBack = false;

            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>())).Returns<
                    Func<CancellationToken, Task>, CancellationToken>(async (action, ct) =>
                    {
                        try
                        {

                            await action(ct);
                        }
                        catch (InvalidOperationException)
                        {
                            rolledBack = true;
                            throw;
                        }
                    });

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(Request, CancellationToken.None));
            _userRepository.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _teamRepository.Verify(t =>
            t.LinkPendingInvitationsToUserAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
            _outbox.Verify(o => o.Add(It.IsAny<OutboxMessageType>(), It.IsAny<string>()), Times.Never);
            Assert.True(rolledBack);
        }

        [Fact]
        public async Task Handle_WhenOutboxFails_RollsBackAfterPreviousWork()
        {
            bool rolledBack = false;

            _outbox.Setup(o => o.Add(It.IsAny<OutboxMessageType>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException());

            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>())).Returns<
                    Func<CancellationToken, Task>, CancellationToken>(async (action, ct) =>
                    {
                        try
                        {

                            await action(ct);
                        }
                        catch (InvalidOperationException)
                        {
                            rolledBack = true;
                            throw;
                        }
                    });

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(Request, CancellationToken.None));
            _userRepository.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _teamRepository.Verify(t =>
            t.LinkPendingInvitationsToUserAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
            _outbox.Verify(o => o.Add(It.IsAny<OutboxMessageType>(), It.IsAny<string>()), Times.Once);
            Assert.True(rolledBack);
        }
    }
}