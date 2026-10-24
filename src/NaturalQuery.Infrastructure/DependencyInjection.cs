using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NaturalQuery.Application.Abstractions.Persistence;
using NaturalQuery.Application.Abstractions.Ai;
using NaturalQuery.Application.Abstractions.Sql;
using NaturalQuery.Infrastructure.Ai;
using NaturalQuery.Infrastructure.Persistence;

namespace NaturalQuery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LocalAiDemo")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:LocalAiDemo'.");
        services.AddDbContext<SalesDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<ISaleRepository, EfSaleRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ISqlQueryExecutor, EfSqlQueryExecutor>();

        return services;
    }

    public static IServiceCollection AddOllama(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute),
            Timeout = TimeSpan.FromMinutes(3)
        });
        services.AddScoped<IQuestionSqlGenerator, OllamaSqlGenerator>();
        services.AddScoped<IQueryAnswerGenerator, OllamaQueryAnswerGenerator>();

        return services;
    }
}
