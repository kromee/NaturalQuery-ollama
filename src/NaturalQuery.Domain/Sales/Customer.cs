namespace NaturalQuery.Domain.Sales;

public sealed class Customer
{
    private Customer() { }

    public Customer(Guid id, string name, string city)
    {
        if (id == Guid.Empty) throw new ArgumentException("El identificador es obligatorio.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("La ciudad es obligatoria.", nameof(city));

        Id = id;
        Name = name.Trim();
        City = city.Trim();
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
}
