using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class EkipConfiguration : IEntityTypeConfiguration<Ekip>
{
    public void Configure(EntityTypeBuilder<Ekip> builder)
    {
        builder.ToTable("ekipler");
        TemelVarlikYapilandirmasi.Uygula(builder);

        builder.Property(e => e.Ad).IsRequired().HasMaxLength(200);

        // "geography" tipi, kucuk mesafe hesaplamalarinda (ST_DWithin/ST_Distance) dunyanin
        // kureselligini dikkate alarak metre cinsinden doğru sonuc verir.
        builder.Property(e => e.GuncelKonum).HasColumnType("geography (Point,4326)");

        builder.HasIndex(e => new { e.BirimId, e.Ad }).IsUnique();

        // Canli haritada "yakinimdaki ekipler" sorgusu icin mekansal (GiST) index sarttir;
        // aksi halde PostGIS her satiri tek tek tarayarak O(n) mesafe hesaplar.
        builder.HasIndex(e => e.GuncelKonum).HasMethod("GIST");

        builder.Navigation(e => e.Uyeler).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.UzmanlikAlanlari)
            .WithMany()
            .UsingEntity(j => j.ToTable("ekip_uzmanlik_alanlari"));
        builder.Navigation(e => e.UzmanlikAlanlari).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
