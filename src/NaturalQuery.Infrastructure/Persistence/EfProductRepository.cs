using Microsoft.EntityFrameworkCore;
using NaturalQuery.Application.Abstractions.Persistence;
using NaturalQuery.Domain.Sales;

namespace NaturalQuery.Infrastructure.Persistence;

internal sealed class EfProductRepository(SalesDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        dbContext.Products.AddAsync(product, cancellationToken).AsTask();
}
