using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NaturalQuery.Application.Abstractions.Ai;

namespace NaturalQuery.Infrastructure.Ai;

internal sealed class OllamaSqlGenerator(HttpClient httpClient, IConfiguration configuration)
    : IQuestionSqlGenerator
{
    private readonly string _model = configuration["Ollama:Model"] ?? "qwen3:4b";

    public async Task<string> GenerateSqlAsync(string question, CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest(
            _model,
            Stream: false,
            Think: false,
            Format: new
            {
                type = "object",
                properties = new { sql = new { type = "string" } },
                required = new[] { "sql" },
                additionalProperties = false
            },
            Messages:
            [
                new OllamaMessage("system", """
                    Eres un generador de consultas SQL Server para una base de datos de demostración.
                    Devuelve un objeto JSON con una única propiedad llamada sql cuyo valor sea una consulta SELECT.
                    No incluyas explicaciones ni razonamiento en el valor de sql.
                    Solo puedes consultar estas tablas y columnas:
                    - Producto(Id, Nombre, PrecioUnitario)
                    - Cliente(Id, Nombre, Ciudad)
                    - Venta(Id, ClienteId, Fecha)
                    - DetalleVenta(Id, VentaId, ProductoId, Cantidad, PrecioUnitario)
                    Relaciones: DetalleVenta.ProductoId = Producto.Id; DetalleVenta.VentaId = Venta.Id;
                    Venta.ClienteId = Cliente.Id.
                    Todas las expresiones calculadas deben llevar un alias descriptivo con AS;
                    por ejemplo, COUNT(*) AS TotalProductos y SUM(Cantidad) AS CantidadTotal.
                    Cuando la pregunta pida el más vendido, los totales o una cantidad agregada,
                    devuelve también la suma solicitada como una columna con alias claro.
                    Para "producto más vendido por cantidad", devuelve un solo registro con
                    SELECT TOP 1, Producto.Nombre y SUM(DetalleVenta.Cantidad) AS CantidadTotal,
                    ordenados de mayor a menor por CantidadTotal.
                    Para ingresos por producto, calcula siempre
                    SUM(DetalleVenta.Cantidad * DetalleVenta.PrecioUnitario) AS IngresosTotal;
                    no sumes únicamente PrecioUnitario. Si se pregunta cuál generó más ingresos,
                    usa SELECT TOP 1 y ordena de mayor a menor por IngresosTotal.
                    Para el importe promedio por venta en cada mes, calcula
                    SUM(DetalleVenta.Cantidad * DetalleVenta.PrecioUnitario) /
                    COUNT(DISTINCT Venta.Id), agrupa por YEAR(Venta.Fecha) y MONTH(Venta.Fecha),
                    y devuelve ambos como columnas Año y Mes. SUM solo debe recibir expresiones
                    numéricas; nunca sumes nombres, ciudades, identificadores de texto ni fechas.
                    Esta es SQL Server: nunca uses LIMIT, OFFSET ni sintaxis de MySQL o PostgreSQL.
                    Para devolver pocas filas usa SELECT TOP n.
                    Usa nombres de tablas y columnas tal como aparecen arriba. No propongas escrituras,
                    procedimientos, múltiples sentencias ni acceso a otras tablas.
                    Si la pregunta no se puede contestar con este esquema, devuelve SELECT 1 WHERE 1 = 0.
                    """),
                new OllamaMessage("user", question)
            ]);

        using var response = await httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
        var sql = result?.Message?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException("Ollama devolvió una respuesta vacía.");
        }

        sql = RemoveThinking(sql);
        var jsonStart = sql.IndexOf('{');
        var jsonEnd = sql.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd < jsonStart)
        {
            throw new InvalidOperationException("Ollama no devolvió el objeto JSON esperado.");
        }

        try
        {
            using var document = JsonDocument.Parse(sql[jsonStart..(jsonEnd + 1)]);
            if (!document.RootElement.TryGetProperty("sql", out var sqlProperty)
                || sqlProperty.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException("La respuesta JSON de Ollama no contiene una propiedad sql válida.");
            }

            var query = sqlProperty.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(query)
                || !query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Ollama no devolvió una consulta SELECT.");
            }

            return query;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Ollama devolvió un objeto JSON inválido.", exception);
        }
    }

    private static string RemoveThinking(string response)
    {
        var start = response.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        while (start >= 0)
        {
            var end = response.IndexOf("</think>", start, StringComparison.OrdinalIgnoreCase);
            if (end < 0)
            {
                return response[..start].Trim();
            }

            response = response.Remove(start, end + "</think>".Length - start);
            start = response.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        }

        return response.Trim();
    }

    private sealed record OllamaChatRequest(
        string Model,
        bool Stream,
        bool Think,
        object Format,
        IReadOnlyList<OllamaMessage> Messages);

    private sealed record OllamaMessage(string Role, string Content);

    private sealed record OllamaChatResponse(OllamaMessage? Message);
}
