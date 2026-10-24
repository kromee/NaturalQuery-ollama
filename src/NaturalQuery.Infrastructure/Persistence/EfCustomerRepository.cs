using Microsoft.EntityFrameworkCore;
using NaturalQuery.Application.Abstractions.Persistence;
using NaturalQuery.Domain.Sales;

namespace NaturalQuery.Infrastructure.Persistence;

internal sealed class EfCustomerRepository(SalesDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Customers.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        dbContext.Customers.AddAsync(customer, cancellationToken).AsTask();
}
