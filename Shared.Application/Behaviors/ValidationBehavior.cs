using System.Reflection;
using FluentValidation;
using MediatR;
using Shared.Domain.Primitives;

namespace Shared.Application.Behaviors;

/// <summary>
/// Pipeline behavior for request validation using FluentValidation
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            var errors = failures
                .Select(f => new Error(f.PropertyName, f.ErrorMessage))
                .ToArray();

            return CreateValidationResult<TResponse>(errors);
        }

        return await next();
    }

    // Result<TValue>.Failure is declared on the base Result class, and Type.GetMethod on a
    // constructed generic type does NOT find inherited static members without
    // BindingFlags.FlattenHierarchy — so this resolves the open generic method straight off
    // Result itself instead of off the constructed Result<TValue> type.
    private static readonly MethodInfo GenericFailureMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Result.Failure) && m.IsGenericMethodDefinition);

    private static TResult CreateValidationResult<TResult>(Error[] errors)
        where TResult : Result
    {
        if (typeof(TResult) == typeof(Result))
        {
            return (Result.Failure(errors[0]) as TResult)!;
        }

        var closedFailureMethod = GenericFailureMethod.MakeGenericMethod(typeof(TResult).GenericTypeArguments[0]);
        var validationResult = closedFailureMethod.Invoke(null, [errors[0]])!;

        return (TResult)validationResult;
    }
}
