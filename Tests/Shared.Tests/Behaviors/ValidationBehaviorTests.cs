using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using Shared.Application.Behaviors;
using Shared.Domain.Primitives;

namespace Shared.Tests.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record TestCommand(string Name) : IRequest<Result>;
    public sealed record TestQuery(string Name) : IRequest<Result<string>>;

    [Fact]
    public async Task Handle_NoValidatorsRegistered_CallsNext()
    {
        var sut = new ValidationBehavior<TestCommand, Result>([]);
        var nextCalled = false;
        Task<Result> Next(CancellationToken ct) { nextCalled = true; return Task.FromResult(Result.Success()); }

        var result = await sut.Handle(new TestCommand("x"), Next, default);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidatorPasses_CallsNext()
    {
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var sut = new ValidationBehavior<TestCommand, Result>([validator]);
        var nextCalled = false;
        Task<Result> Next(CancellationToken ct) { nextCalled = true; return Task.FromResult(Result.Success()); }

        var result = await sut.Handle(new TestCommand("x"), Next, default);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidatorFails_ReturnsFailureWithoutCallingNext_ForNonGenericResult()
    {
        var failure = new ValidationFailure("Name", "Name is required");
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([failure]));
        var sut = new ValidationBehavior<TestCommand, Result>([validator]);
        var nextCalled = false;
        Task<Result> Next(CancellationToken ct) { nextCalled = true; return Task.FromResult(Result.Success()); }

        var result = await sut.Handle(new TestCommand(""), Next, default);

        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
        Assert.Equal("Name", result.Error.Code);
        Assert.Equal("Name is required", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ValidatorFails_ReturnsFailureWithoutCallingNext_ForGenericResult()
    {
        var failure = new ValidationFailure("Name", "Name is required");
        var validator = Substitute.For<IValidator<TestQuery>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestQuery>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([failure]));
        var sut = new ValidationBehavior<TestQuery, Result<string>>([validator]);
        var nextCalled = false;
        Task<Result<string>> Next(CancellationToken ct) { nextCalled = true; return Task.FromResult(Result.Success("ok")); }

        var result = await sut.Handle(new TestQuery(""), Next, default);

        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
        Assert.Equal("Name", result.Error.Code);
    }

    [Fact]
    public async Task Handle_MultipleValidatorsWithFailures_AggregatesAndReturnsFirstError()
    {
        var validator1 = Substitute.For<IValidator<TestCommand>>();
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "First error")]));
        var validator2 = Substitute.For<IValidator<TestCommand>>();
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "Second error")]));
        var sut = new ValidationBehavior<TestCommand, Result>([validator1, validator2]);

        var result = await sut.Handle(new TestCommand(""), _ => Task.FromResult(Result.Success()), default);

        Assert.True(result.IsFailure);
        Assert.Equal("First error", result.Error.Description);
    }
}
