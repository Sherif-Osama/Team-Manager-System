using TeamManager.Application.Features.Projects.Project.Commands.DeleteProject;
using TeamManager.Domain.Enums;

namespace TeamManager.Tests.UnitTest.Application.Features.Projects.DeleteProjectTests;

public sealed class DeleteProjectCommandValidatorTests
{
    private readonly DeleteProjectCommandValidator _sut = new();

    [Fact]
    public void Validate_WithValidProjectId_Passes()
    {
        var result = _sut.Validate(new DeleteProjectCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyProjectId_Fails()
    {
        var result = _sut.Validate(new DeleteProjectCommand(Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Command_RequiresProjectOwnerRole()
    {
        var command = new DeleteProjectCommand(Guid.NewGuid());

        Assert.Contains(ProjectRole.Owner, command.RequiredRoles);
    }
}
