using MediatR;
using Shared.Domain.Primitives;

namespace Shared.Application.Messaging;

/// <summary>
/// Query handler interface (CQRS pattern)
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
