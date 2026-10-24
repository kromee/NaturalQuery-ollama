namespace NaturalQuery.Application.Abstractions.Sql;

public interface ISqlQueryExecutor
{
    Task<SqlQueryResult> ExecuteAsync(string sql, CancellationToken cancellationToken = default);
}

public sealed record SqlQueryResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    bool Truncated);
