using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class KurumConfiguration : IEntityTypeConfiguration<Kurum>
{
    public void Configure(EntityTypeBuilder<Kurum> builder)
    {
        builder.ToTable("kurumlar");
        TemelVarlikYapilandirmasi.Uygula(builder);

        builder.Property(k => k.Ad).IsRequired().HasMaxLength(200);
        builder.Property(k => k.Adres).HasMaxLength(500);
        builder.Property(k => k.IletisimTelefonu).HasMaxLength(20);

        builder.HasIndex(k => k.Ad).IsUnique();

        builder.HasMany(k => k.Birimler)
            .WithOne(b => b.Kurum)
            .HasForeignKey(b => b.KurumId)
            .OnDelete(DeleteBehavior.Restrict);

        // Birimler koleksiyonu disaridan salt-okunur (IReadOnlyCollection) sunuldugu icin
        // EF Core'un ekleme/cikarma islemlerini "_birimler" alani uzerinden yapmasi gerekir.
        builder.Navigation(k => k.Birimler).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Ilk kurulumda SistemYoneticisi girisi yapabilmesi icin baslangic kurumu (bkz. BirimConfiguration,
        // PersonelConfiguration'daki iliskili seed kayitlari - sabit Id kullanim gerekcesi orada aciklanmistir).
        builder.HasData(new
        {
            Id = BaslangicVerisiSabitleri.Organizasyon.OrnekKurum,
            Ad = "Örnek Belediyesi",
            Adres = (string?)"Örnek Mah. Belediye Cad. No:1",
            IletisimTelefonu = (string?)"0212 000 00 00",
            AktifMi = true,
            OlusturmaZamaniUtc = BaslangicVerisiSabitleri.SabitOlusturmaZamaniUtc,
            SilindiMi = false,
        });
    }
}
