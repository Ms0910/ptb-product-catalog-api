using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table =>
        {
            table.HasCheckConstraint("ck_products_stock_non_negative", "stock >= 0");
            table.HasCheckConstraint("ck_products_price_positive", "price > 0");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Stock).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(p => p.UpdatedBy).HasMaxLength(200).IsRequired();

        // uint + IsRowVersion es mapeado por Npgsql a la columna de sistema xmin.
        builder.Property(p => p.Version).IsRowVersion();

        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.CreatedAt);
    }
}
