namespace NaturalQuery.Domain.Sales;

public sealed class SaleLine
{
    private SaleLine() { }

    internal SaleLine(Guid id, Guid productId, int quantity, decimal unitPrice)
    {
        Id = id;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Subtotal => Quantity * UnitPrice;
}
