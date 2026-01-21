using MediatR;
using Splendor.Domain;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Queries;

public record GetCardsQuery() : IRequest<IReadOnlyList<Card>>;

public class GetCardsQueryHandler : IRequestHandler<GetCardsQuery, IReadOnlyList<Card>>
{
    public Task<IReadOnlyList<Card>> Handle(GetCardsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CardDefinitions.AllCards);
    }
}
