using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class GorevDurumGecmisiConfiguration : IEntityTypeConfiguration<GorevDurumGecmisi>
{
    public void Configure(EntityTypeBuilder<GorevDurumGecmisi> builder)
    {
        builder.ToTable("gorev_durum_gecmisleri");
        TemelVarlikYapilandirmasi.Uygula(builder);

        builder.Property(h => h.OncekiDurum).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(h => h.YeniDurum).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(h => h.Not).HasMaxLength(1000);
        builder.Property(h => h.DegisiklikZamaniUtc).IsRequired();

        // Islemi yapan personel silinmis olsa dahi denetim izi kaybolmamali; bu yuzden
        // navigasyon eklenmedi, sadece FK kolonu tutulur (Personel silinirse alan null'a duser).
        builder.HasOne<Personel>()
            .WithMany()
            .HasForeignKey(h => h.IslemiYapanPersonelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => h.GorevTalebiId);
    }
}
