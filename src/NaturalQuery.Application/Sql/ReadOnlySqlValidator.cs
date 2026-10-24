using System.Text.RegularExpressions;
using NaturalQuery.Application.Abstractions.Sql;

namespace NaturalQuery.Application.Sql;

public sealed class ReadOnlySqlValidator : IReadOnlySqlValidator
{
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cliente",
        "Producto",
        "Venta",
        "DetalleVenta"
    };

    private static readonly Regex TableReference = new(
        @"\b(?:FROM|JOIN)\s+(?<table>(?:\[[^\]]+\]|[A-Za-z_][\w$]*)(?:\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][\w$]*)){0,2})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ForbiddenTokens = new(
        @"\b(INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP|EXEC|EXECUTE|TRUNCATE|GRANT|REVOKE|DENY|BACKUP|RESTORE|DBCC|USE|WAITFOR|BULK|INTO|OPENROWSET|OPENDATASOURCE|OPENQUERY|OPENXML|DECLARE|SET|CALL|OPTION)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string ValidateAndNormalize(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new SqlValidationException("La consulta está vacía.");
        }

        var normalized = sql.Trim();
        if (normalized.EndsWith(';'))
        {
            normalized = normalized[..^1].TrimEnd();
        }

        if (normalized.Contains(';')
            || normalized.Contains("--", StringComparison.Ordinal)
            || normalized.Contains("/*", StringComparison.Ordinal)
            || normalized.Contains("*/", StringComparison.Ordinal))
        {
            throw new SqlValidationException("Solo se permite una consulta SELECT sin comentarios.");
        }

        if (!normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || (normalized.Length > 6 && !char.IsWhiteSpace(normalized[6])))
        {
            throw new SqlValidationException("Solo se permiten consultas SELECT.");
        }

        if (ForbiddenTokens.IsMatch(normalized)
            || Regex.IsMatch(normalized, @"\b(?:FROM|JOIN)\s*\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            || Regex.IsMatch(normalized, @"\b(?:CROSS|OUTER)\s+APPLY\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            throw new SqlValidationException("La consulta contiene una operación o construcción no permitida.");
        }

        var references = TableReference.Matches(normalized);
        if (references.Count == 0)
        {
            throw new SqlValidationException("La consulta debe leer al menos una tabla permitida.");
        }

        foreach (Match reference in references)
        {
            var parts = reference.Groups["table"].Value
                .Replace("[", string.Empty, StringComparison.Ordinal)
                .Replace("]", string.Empty, StringComparison.Ordinal)
                .Split('.', StringSplitOptions.TrimEntries);

            if (parts.Length is < 1 or > 2
                || (parts.Length == 2 && !string.Equals(parts[0], "dbo", StringComparison.OrdinalIgnoreCase))
                || !AllowedTables.Contains(parts[^1]))
            {
                throw new SqlValidationException("La consulta solo puede usar Cliente, Producto, Venta y DetalleVenta en dbo.");
            }
        }

        return normalized;
    }
}

public sealed class SqlValidationException(string message) : Exception(message);
