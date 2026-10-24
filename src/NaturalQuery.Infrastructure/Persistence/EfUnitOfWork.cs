using NaturalQuery.Application.Abstractions.Persistence;

namespace NaturalQuery.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(SalesDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
