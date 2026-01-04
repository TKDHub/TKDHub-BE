using MediatR;
using Shared.Domain.Primitives;

namespace Shared.Application.Messaging;

/// <summary>
/// Query interface (CQRS pattern)
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
