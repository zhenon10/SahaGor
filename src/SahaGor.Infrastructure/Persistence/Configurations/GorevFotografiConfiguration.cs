using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class GorevFotografiConfiguration : IEntityTypeConfiguration<GorevFotografi>
{
    public void Configure(EntityTypeBuilder<GorevFotografi> builder)
    {
        builder.ToTable("gorev_fotograflari");
        TemelVarlikYapilandirmasi.Uygula(builder);

        builder.Property(f => f.DosyaYolu).IsRequired().HasMaxLength(500);
        builder.Property(f => f.Asama).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(f => f.CekildigiKonum).IsRequired().HasColumnType("geography (Point,4326)");
        builder.Property(f => f.CekilmeZamaniUtc).IsRequired();
        builder.Property(f => f.SunucuyaUlasmaZamaniUtc).IsRequired();

        builder.HasOne(f => f.CekenPersonel)
            .WithMany()
            .HasForeignKey(f => f.CekenPersonelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.GorevTalebiId);
    }
}
