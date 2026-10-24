using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NaturalQuery.Infrastructure.Persistence;

public sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__LocalAiDemo")
            ?? "Server=localhost;Database=LocalAiDemo;Integrated Security=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SalesDbContext(options);
    }
}
