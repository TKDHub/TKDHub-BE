using MediatR;
using Shared.Domain.Primitives;

namespace Shared.Application.Messaging;

/// <summary>
/// Marker interface for commands (CQRS pattern)
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Generic command interface with response
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
