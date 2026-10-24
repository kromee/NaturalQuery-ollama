using Microsoft.EntityFrameworkCore;
using NaturalQuery.Application.Abstractions.Sql;

namespace NaturalQuery.Infrastructure.Persistence;

internal sealed class EfSqlQueryExecutor(SalesDbContext dbContext) : ISqlQueryExecutor
{
    private const int MaxRows = 100;
    private const int CommandTimeoutSeconds = 15;

    public async Task<SqlQueryResult> ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = CommandTimeoutSeconds;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(ordinal =>
                {
                    var name = reader.GetName(ordinal);
                    return string.IsNullOrWhiteSpace(name) ? $"Columna{ordinal + 1}" : name;
                })
                .ToArray();
            var rows = new List<IReadOnlyList<object?>>();
            var truncated = false;

            while (await reader.ReadAsync(cancellationToken))
            {
                if (rows.Count == MaxRows)
                {
                    truncated = true;
                    break;
                }

                var values = new object?[reader.FieldCount];
                for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                {
                    values[ordinal] = await reader.IsDBNullAsync(ordinal, cancellationToken)
                        ? null
                        : reader.GetValue(ordinal);
                }

                rows.Add(values);
            }

            return new SqlQueryResult(columns, rows, truncated);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
