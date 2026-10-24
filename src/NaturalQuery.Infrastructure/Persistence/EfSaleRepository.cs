using Microsoft.EntityFrameworkCore;
using NaturalQuery.Application.Abstractions.Persistence;
using NaturalQuery.Domain.Sales;

namespace NaturalQuery.Infrastructure.Persistence;

internal sealed class EfSaleRepository(SalesDbContext dbContext) : ISaleRepository
{
    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Sales.Include(sale => sale.Lines)
            .SingleOrDefaultAsync(sale => sale.Id == id, cancellationToken);

    public Task AddAsync(Sale sale, CancellationToken cancellationToken = default) =>
        dbContext.Sales.AddAsync(sale, cancellationToken).AsTask();
}
