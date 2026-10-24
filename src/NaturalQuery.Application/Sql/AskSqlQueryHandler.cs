using NaturalQuery.Application.Abstractions.Ai;
using NaturalQuery.Application.Abstractions.Sql;

namespace NaturalQuery.Application.Sql;

public sealed class AskSqlQueryHandler(
    IQuestionSqlGenerator sqlGenerator,
    IReadOnlySqlValidator sqlValidator,
    ISqlQueryExecutor sqlQueryExecutor,
    IQueryAnswerGenerator answerGenerator)
{
    public async Task<SqlPreviewResult> PreviewAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        ValidateQuestion(question);
        var generatedSql = await sqlGenerator.GenerateSqlAsync(question, cancellationToken);
        var safeSql = sqlValidator.ValidateAndNormalize(generatedSql);
        return new SqlPreviewResult(question, safeSql);
    }

    public async Task<SqlExecutionResult> ExecuteAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var preview = await PreviewAsync(question, cancellationToken);
        var queryResult = await sqlQueryExecutor.ExecuteAsync(preview.Sql, cancellationToken);
        var answer = await answerGenerator.GenerateAnswerAsync(question, queryResult, cancellationToken);

        return new SqlExecutionResult(
            preview.Question,
            preview.Sql,
            answer,
            queryResult.Columns,
            queryResult.Rows,
            queryResult.Truncated);
    }

    private static void ValidateQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Length > 500)
        {
            throw new SqlQuestionValidationException("La pregunta es obligatoria y debe tener 500 caracteres o menos.");
        }
    }
}

public sealed record SqlPreviewResult(string Question, string Sql);

public sealed record SqlExecutionResult(
    string Question,
    string Sql,
    string Answer,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    bool Truncated);

public sealed class SqlQuestionValidationException(string message) : Exception(message);
