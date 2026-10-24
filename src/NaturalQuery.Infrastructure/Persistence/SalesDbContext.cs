using Microsoft.EntityFrameworkCore;
using NaturalQuery.Domain.Sales;

namespace NaturalQuery.Infrastructure.Persistence;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Producto");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasColumnName("Nombre").HasMaxLength(160).IsRequired();
            entity.Property(product => product.UnitPrice).HasColumnName("PrecioUnitario").HasPrecision(18, 2);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Cliente");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Name).HasColumnName("Nombre").HasMaxLength(160).IsRequired();
            entity.Property(customer => customer.City).HasColumnName("Ciudad").HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Venta");
            entity.HasKey(sale => sale.Id);
            entity.Property(sale => sale.OccurredAt).HasColumnName("Fecha").IsRequired();
            entity.Ignore(sale => sale.Total);
            entity.Navigation(sale => sale.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(sale => sale.CustomerId)
                .HasConstraintName("FK_Venta_Cliente_ClienteId")
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(sale => sale.CustomerId).HasColumnName("ClienteId");
        });

        modelBuilder.Entity<SaleLine>(entity =>
        {
            entity.ToTable("DetalleVenta");
            entity.HasKey(line => line.Id);
            entity.Property(line => line.ProductId).HasColumnName("ProductoId");
            entity.Property(line => line.Quantity).HasColumnName("Cantidad").IsRequired();
            entity.Property(line => line.UnitPrice).HasColumnName("PrecioUnitario").HasPrecision(18, 2);
            entity.Ignore(line => line.Subtotal);

            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(line => line.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Sale>()
                .WithMany(sale => sale.Lines)
                .HasForeignKey("SaleId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property<Guid>("SaleId").HasColumnName("VentaId");
        });
    }
}
