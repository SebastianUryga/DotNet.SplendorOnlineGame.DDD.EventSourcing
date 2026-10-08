using System.Diagnostics;
using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;

namespace Splendor.Application.Behaviors;

/// <summary>Stamps events written by this request with who caused them and a correlation id.</summary>
public class EventMetadataBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDocumentSession _session;

    public EventMetadataBehavior(IDocumentSession session)
    {
        _session = session;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _session.CorrelationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        _session.LastModifiedBy = request is IAuthorizedCommand command ? command.Caller.UserId.Value : "system";
        return next();
    }
}
