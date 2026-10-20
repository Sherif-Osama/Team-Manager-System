using Moq;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Application.Features.Projects.Project.Commands.DeleteProject;
using TeamManager.Domain.Entities;

namespace TeamManager.Tests.UnitTest.Application.Features.Projects.DeleteProjectTests;

public sealed class DeleteProjectCommandHandlerTests
{
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly DeleteProjectCommandHandler _sut;

    public DeleteProjectCommandHandlerTests()
    {
        _sut = new(_projectRepository.Object, _taskRepository.Object, _unitOfWork.Object, _cacheService.Object);
        _unitOfWork.Setup(x => x.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
    }

    private Project CreateProject() =>
        new(_projectId, Guid.NewGuid(), "Test Project", Guid.NewGuid());

    private void SetupProject(Project? project) =>
        _projectRepository.Setup(x => x.GetByIdAsync(_projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);

    [Fact]
    public async Task Handle_WhenProjectDoesNotExist_Throws404WithoutCheckingTasks()
    {
        SetupProject(null);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            _sut.Handle(new DeleteProjectCommand(_projectId), CancellationToken.None));

        _taskRepository.Verify(x => x.HasActiveTasksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(
            It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectHasActiveTasks_Throws409AndDoesNotDelete()
    {
        var project = CreateProject();
        SetupProject(project);
        _taskRepository.Setup(x => x.HasActiveTasksAsync(_projectId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ProjectHasActiveTasksException>(() =>
            _sut.Handle(new DeleteProjectCommand(_projectId), CancellationToken.None));

        Assert.Null(project.DeletedAtUtc);
        _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(
            It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(x => x.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_SoftDeletesAndInvalidatesAuthorizationCache()
    {
        var project = CreateProject();
        SetupProject(project);
        _taskRepository.Setup(x => x.HasActiveTasksAsync(_projectId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _sut.Handle(new DeleteProjectCommand(_projectId), CancellationToken.None);

        Assert.NotNull(project.DeletedAtUtc);
        _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(
            It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(x => x.RemoveByTagAsync($"teammanager:auth:project:{_projectId:N}",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
