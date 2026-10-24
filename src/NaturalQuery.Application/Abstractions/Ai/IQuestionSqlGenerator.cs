namespace NaturalQuery.Application.Abstractions.Ai;

public interface IQuestionSqlGenerator
{
    Task<string> GenerateSqlAsync(string question, CancellationToken cancellationToken = default);
}
