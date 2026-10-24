using NaturalQuery.Application.Abstractions.Sql;

namespace NaturalQuery.Application.Abstractions.Ai;

public interface IQueryAnswerGenerator
{
    Task<string> GenerateAnswerAsync(
        string question,
        SqlQueryResult result,
        CancellationToken cancellationToken = default);
}
