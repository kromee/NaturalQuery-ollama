using Microsoft.EntityFrameworkCore;
using NaturalQuery.Application.Abstractions.Sql;
using NaturalQuery.Application.Sql;
using NaturalQuery.Infrastructure;
using NaturalQuery.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddOllama(builder.Configuration);
builder.Services.AddSingleton<IReadOnlySqlValidator, ReadOnlySqlValidator>();
builder.Services.AddScoped<AskSqlQueryHandler>();

var connectionString = builder.Configuration.GetConnectionString("LocalAiDemo");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddInfrastructure(builder.Configuration);
}
else
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new
    {
        status = "ok",
        databaseConfigured = !string.IsNullOrWhiteSpace(connectionString),
        ollamaModel = builder.Configuration["Ollama:Model"]
    }))
    .WithName("Health");

app.MapPost("/api/ai/sql/preview", async (
    SqlPreviewRequest request,
    AskSqlQueryHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await handler.PreviewAsync(request.Question, cancellationToken);
        return Results.Ok(new
        {
            result.Question,
            model = builder.Configuration["Ollama:Model"] ?? "qwen3:4b",
            result.Sql,
            executed = false
        });
    }
    catch (SqlQuestionValidationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (HttpRequestException exception)
    {
        app.Logger.LogError(exception, "Falló la llamada a Ollama al generar la vista previa SQL.");
        return Results.Problem(
            title: "No se pudo comunicar con Ollama",
            detail: exception.StatusCode is { } statusCode
                ? $"Ollama respondió con HTTP {(int)statusCode}."
                : "Verifica que Ollama esté activo y que el modelo configurado esté disponible.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (InvalidOperationException)
    {
        return Results.Problem(
            title: "Ollama devolvió una respuesta fuera del formato esperado",
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (SqlValidationException exception)
    {
        return Results.Problem(
            title: "La consulta generada no pasó la validación",
            detail: exception.Message,
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }
})
    .WithName("PreviewSql");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    app.MapPost("/api/ai/sql", async (
        SqlPreviewRequest request,
        AskSqlQueryHandler handler,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await handler.ExecuteAsync(request.Question, cancellationToken);
            return Results.Ok(new
            {
                result.Question,
                model = builder.Configuration["Ollama:Model"] ?? "qwen3:4b",
                result.Sql,
                result.Answer,
                result.Columns,
                result.Rows,
                result.Truncated,
                executed = true
            });
        }
        catch (SqlQuestionValidationException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
        catch (HttpRequestException exception)
        {
            app.Logger.LogError(exception, "Falló la llamada a Ollama durante la consulta.");
            return Results.Problem(
                title: "No se pudo comunicar con Ollama",
                detail: exception.StatusCode is { } statusCode
                    ? $"Ollama respondió con HTTP {(int)statusCode}."
                    : "Verifica que Ollama esté activo y que el modelo configurado esté disponible.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (InvalidOperationException)
        {
            return Results.Problem(
                title: "Ollama devolvió una respuesta fuera del formato esperado",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (SqlValidationException exception)
        {
            return Results.Problem(
                title: "La consulta generada no pasó la validación",
                detail: exception.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }
        catch (SqlException exception)
        {
            app.Logger.LogError(exception, "No se pudo ejecutar la consulta validada en LocalAiDemo.");
            return Results.Problem(
                title: "No se pudo ejecutar la consulta en LocalAiDemo",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    })
        .WithName("AskSql");
}

if (!string.IsNullOrWhiteSpace(connectionString))
{
    app.MapGet("/health/database", async (
        SalesDbContext dbContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {
        try
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            await dbContext.Database.CloseConnectionAsync();
            return Results.Ok(new { status = "ok", database = "LocalAiDemo" });
        }
        catch (SqlException exception)
        {
            logger.LogError(exception, "No se pudo conectar a LocalAiDemo.");
            return Results.Problem(
                title: "No se pudo conectar a LocalAiDemo",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    })
        .WithName("DatabaseHealth");
}

app.Run();

public sealed record SqlPreviewRequest(string Question);
