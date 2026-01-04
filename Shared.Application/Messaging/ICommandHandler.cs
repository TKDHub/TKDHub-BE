using MediatR;
using Shared.Domain.Primitives;

namespace Shared.Application.Messaging;

/// <summary>
/// Command handler interface (CQRS pattern)
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Generic command handler interface with response
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
