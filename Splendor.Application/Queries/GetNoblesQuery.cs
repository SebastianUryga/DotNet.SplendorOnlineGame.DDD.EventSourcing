using MediatR;
using Splendor.Domain;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Queries;

public record GetNoblesQuery() : IRequest<IReadOnlyList<Noble>>;

public class GetNoblesQueryHandler : IRequestHandler<GetNoblesQuery, IReadOnlyList<Noble>>
{
    public Task<IReadOnlyList<Noble>> Handle(GetNoblesQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(NobleDefinitions.AllNobles);
    }
}
