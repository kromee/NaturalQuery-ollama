namespace NaturalQuery.Domain.Sales;

public sealed class Sale
{
    private readonly List<SaleLine> _lines = [];

    private Sale() { }

    public Sale(Guid id, Guid customerId, DateTimeOffset occurredAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("El identificador es obligatorio.", nameof(id));
        if (customerId == Guid.Empty) throw new ArgumentException("El cliente es obligatorio.", nameof(customerId));
        Id = id;
        CustomerId = customerId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public IReadOnlyCollection<SaleLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(line => line.Subtotal);

    public void AddLine(Guid productId, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty) throw new ArgumentException("El producto es obligatorio.", nameof(productId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));

        _lines.Add(new SaleLine(Guid.NewGuid(), productId, quantity, unitPrice));
    }
}
