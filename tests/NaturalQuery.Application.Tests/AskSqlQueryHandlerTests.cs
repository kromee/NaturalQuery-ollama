using NaturalQuery.Application.Abstractions.Ai;
using NaturalQuery.Application.Abstractions.Sql;
using NaturalQuery.Application.Sql;

namespace NaturalQuery.Application.Tests;

public sealed class AskSqlQueryHandlerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_RejectsEmptyQuestionBeforeCallingAi(string question)
    {
        var sqlGenerator = new FakeSqlGenerator("SELECT Id FROM Producto");
        var executor = new FakeSqlQueryExecutor(EmptyResult);
        var answerGenerator = new FakeQueryAnswerGenerator("Respuesta");
        var handler = CreateHandler(sqlGenerator, executor, answerGenerator);

        await Assert.ThrowsAsync<SqlQuestionValidationException>(() => handler.ExecuteAsync(question));

        Assert.Equal(0, sqlGenerator.CallCount);
        Assert.Equal(0, executor.CallCount);
        Assert.Equal(0, answerGenerator.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ValidatesExecutesAndBuildsAnswerFromQueryResult()
    {
        const string question = "¿Cuántos productos hay?";
        const string generatedSql = " SELECT COUNT(*) AS TotalProductos FROM Producto; ";
        const string expectedSql = "SELECT COUNT(*) AS TotalProductos FROM Producto";
        var queryResult = new SqlQueryResult(
            ["TotalProductos"],
            [[5]],
            false);
        var sqlGenerator = new FakeSqlGenerator(generatedSql);
        var executor = new FakeSqlQueryExecutor(queryResult);
        var answerGenerator = new FakeQueryAnswerGenerator("Hay 5 productos.");
        var handler = CreateHandler(sqlGenerator, executor, answerGenerator);

        var result = await handler.ExecuteAsync(question);

        Assert.Equal(question, result.Question);
        Assert.Equal(expectedSql, result.Sql);
        Assert.Equal("Hay 5 productos.", result.Answer);
        Assert.Equal(["TotalProductos"], result.Columns);
        Assert.Equal([[5]], result.Rows);
        Assert.False(result.Truncated);
        Assert.Equal(expectedSql, executor.LastSql);
        Assert.Same(queryResult, answerGenerator.LastResult);
        Assert.Equal(1, sqlGenerator.CallCount);
        Assert.Equal(1, executor.CallCount);
        Assert.Equal(1, answerGenerator.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsUnsafeGeneratedSqlBeforeExecution()
    {
        var sqlGenerator = new FakeSqlGenerator("DELETE FROM Producto");
        var executor = new FakeSqlQueryExecutor(EmptyResult);
        var answerGenerator = new FakeQueryAnswerGenerator("Respuesta");
        var handler = CreateHandler(sqlGenerator, executor, answerGenerator);

        await Assert.ThrowsAsync<SqlValidationException>(() => handler.ExecuteAsync("¿Cuántos productos hay?"));

        Assert.Equal(1, sqlGenerator.CallCount);
        Assert.Equal(0, executor.CallCount);
        Assert.Equal(0, answerGenerator.CallCount);
    }

    private static AskSqlQueryHandler CreateHandler(
        FakeSqlGenerator sqlGenerator,
        FakeSqlQueryExecutor executor,
        FakeQueryAnswerGenerator answerGenerator) =>
        new(sqlGenerator, new ReadOnlySqlValidator(), executor, answerGenerator);

    private static SqlQueryResult EmptyResult => new([], [], false);

    private sealed class FakeSqlGenerator(string sql) : IQuestionSqlGenerator
    {
        public int CallCount { get; private set; }

        public Task<string> GenerateSqlAsync(string question, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(sql);
        }
    }

    private sealed class FakeSqlQueryExecutor(SqlQueryResult result) : ISqlQueryExecutor
    {
        public int CallCount { get; private set; }
        public string? LastSql { get; private set; }

        public Task<SqlQueryResult> ExecuteAsync(string sql, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastSql = sql;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeQueryAnswerGenerator(string answer) : IQueryAnswerGenerator
    {
        public int CallCount { get; private set; }
        public SqlQueryResult? LastResult { get; private set; }

        public Task<string> GenerateAnswerAsync(
            string question,
            SqlQueryResult result,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastResult = result;
            return Task.FromResult(answer);
        }
    }
}
