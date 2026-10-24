namespace NaturalQuery.Domain.Sales;

public sealed class Product
{
    private Product() { }

    public Product(Guid id, string name, decimal unitPrice)
    {
        if (id == Guid.Empty) throw new ArgumentException("El identificador es obligatorio.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));

        Id = id;
        Name = name.Trim();
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
}
