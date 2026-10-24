using NaturalQuery.Application.Sql;

namespace NaturalQuery.Application.Tests;

public sealed class ReadOnlySqlValidatorTests
{
    private readonly ReadOnlySqlValidator _validator = new();

    [Theory]
    [InlineData("SELECT TOP 1 Nombre FROM Producto")]
    [InlineData("SELECT Cliente.Nombre FROM dbo.Cliente")]
    [InlineData("SELECT Venta.Id FROM Venta INNER JOIN Cliente ON Venta.ClienteId = Cliente.Id")]
    public void ValidateAndNormalize_AcceptsSelectFromAllowedTables(string sql)
    {
        var normalized = _validator.ValidateAndNormalize(sql);

        Assert.Equal(sql, normalized);
    }

    [Fact]
    public void ValidateAndNormalize_TrimsWhitespaceAndTrailingSemicolon()
    {
        var normalized = _validator.ValidateAndNormalize("  SELECT Id FROM Producto;  ");

        Assert.Equal("SELECT Id FROM Producto", normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndNormalize_RejectsEmptyQueries(string sql)
    {
        Assert.Throws<SqlValidationException>(() => _validator.ValidateAndNormalize(sql));
    }

    [Theory]
    [InlineData("DELETE FROM Producto")]
    [InlineData("SELECT * FROM sys.tables")]
    [InlineData("SELECT * FROM sales.Producto")]
    [InlineData("SELECT * FROM Producto; DELETE FROM Producto")]
    [InlineData("SELECT * FROM Producto -- comentario")]
    [InlineData("SELECT * FROM (SELECT Id FROM Producto) AS p")]
    [InlineData("SELECT * FROM Producto OPTION (RECOMPILE)")]
    public void ValidateAndNormalize_RejectsUnsafeOrUnsupportedQueries(string sql)
    {
        Assert.Throws<SqlValidationException>(() => _validator.ValidateAndNormalize(sql));
    }
}
