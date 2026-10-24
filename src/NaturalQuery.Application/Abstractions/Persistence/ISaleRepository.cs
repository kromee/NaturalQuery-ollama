using NaturalQuery.Domain.Sales;

namespace NaturalQuery.Application.Abstractions.Persistence;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);
}
