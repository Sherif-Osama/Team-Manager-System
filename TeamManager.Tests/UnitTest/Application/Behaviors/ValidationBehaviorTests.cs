using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using TeamManager.Application.Common.Behaviors;

namespace TeamManager.Tests.UnitTest.Application.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_CallsNextAndReturnsItsResult()
    {
        var sut = new ValidationBehavior<TestRequest, string>([]);
        var nextCalled = false;

        var result = await sut.Handle(new TestRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task Handle_WhenValidationSucceeds_CallsNextAndReturnsItsResult()
    {
        var validator = new Mock<IValidator<TestRequest>>();
        validator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        var sut = new ValidationBehavior<TestRequest, string>([validator.Object]);
        var nextCalled = false;

        var result = await sut.Handle(new TestRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationExceptionAndDoesNotCallNext()
    {
        var validator = new Mock<IValidator<TestRequest>>();
        validator.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Name", "Name is required.")]));
        var sut = new ValidationBehavior<TestRequest, string>([validator.Object]);
        var nextCalled = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.Handle(new TestRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
        Assert.Single(exception.Errors);
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_RunsAllOfThem()
    {
        var first = new Mock<IValidator<TestRequest>>();
        var second = new Mock<IValidator<TestRequest>>();
        first.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        second.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        var sut = new ValidationBehavior<TestRequest, string>([first.Object, second.Object]);
        var nextCalled = false;

        await sut.Handle(new TestRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        first.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
        second.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public sealed record TestRequest : IRequest<string>;
}
