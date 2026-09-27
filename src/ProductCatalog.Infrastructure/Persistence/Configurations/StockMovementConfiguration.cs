using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements", table =>
            table.HasCheckConstraint("ck_stock_movements_resulting_stock_non_negative", "resulting_stock >= 0"));

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Quantity).IsRequired();
        builder.Property(m => m.ResultingStock).IsRequired();
        builder.Property(m => m.Reason).HasMaxLength(StockMovement.ReasonMaxLength);
        builder.Property(m => m.IdempotencyKey).HasMaxLength(StockMovement.IdempotencyKeyMaxLength);
        builder.Property(m => m.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(m => m.OccurredAt).IsRequired();
        builder.Ignore(m => m.Type);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ProductId, m.OccurredAt });

        // Garantiza que una clave de idempotencia se aplique una sola vez, incluso entre productos distintos.
        builder.HasIndex(m => m.IdempotencyKey)
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
    }
}
