using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class BolgeConfiguration : IEntityTypeConfiguration<Bolge>
{
    public void Configure(EntityTypeBuilder<Bolge> builder)
    {
        builder.ToTable("bolgeler");
        TemelVarlikYapilandirmasi.Uygula(builder);

        builder.Property(b => b.Ad).IsRequired().HasMaxLength(200);
        builder.Property(b => b.SinirPolygonu).IsRequired().HasColumnType("geography (Polygon,4326)");

        builder.HasOne(b => b.Kurum)
            .WithMany()
            .HasForeignKey(b => b.KurumId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.SorumluBirim)
            .WithMany()
            .HasForeignKey(b => b.SorumluBirimId)
            .OnDelete(DeleteBehavior.SetNull);

        // Gelen gorevin hangi bolgeye dustugunu bulan ST_Contains sorgusu icin mekansal index.
        builder.HasIndex(b => b.SinirPolygonu).HasMethod("GIST");
    }
}
