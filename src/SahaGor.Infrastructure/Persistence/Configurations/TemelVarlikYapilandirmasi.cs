using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Ortak;

namespace SahaGor.Infrastructure.Persistence.Configurations;

/// <summary>
/// Tum entity konfigurasyonlarinda tekrar eden TemelVarlik alanlarini (Id, audit, soft-delete)
/// tek noktadan uygulayan yardimci sinif (DRY). Her IEntityTypeConfiguration bunu cagirir.
/// </summary>
internal static class TemelVarlikYapilandirmasi
{
    public static void Uygula<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : TemelVarlik
    {
        builder.HasKey(e => e.Id);

        // Id degeri veritabani tarafindan degil, Domain katmaninda TemelVarlik kurucusunda
        // (Guid.NewGuid()) uretilir; EF Core'un kendi degeri uretmeye calismasi engellenir.
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.OlusturmaZamaniUtc).IsRequired();
        builder.Property(e => e.OlusturanKullaniciId);
        builder.Property(e => e.GuncellemeZamaniUtc);
        builder.Property(e => e.GuncelleyenKullaniciId);

        builder.Property(e => e.SilindiMi).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.SilinmeZamaniUtc);

        // Yumusak silinmis kayitlarin sikca filtrelenmesi (global query filter) beklenir.
        builder.HasIndex(e => e.SilindiMi);
    }
}
