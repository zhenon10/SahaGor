using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class BirimConfiguration : IEntityTypeConfiguration<Birim>
{
    public void Configure(EntityTypeBuilder<Birim> builder)
    {
        builder.ToTable("birimler");
        TemelVarlikYapilandirmasi.Uygula(builder);

        builder.Property(b => b.Ad).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Aciklama).HasMaxLength(1000);

        // Ayni kurum icinde iki farkli birimin ayni adi tasimasini engeller.
        builder.HasIndex(b => new { b.KurumId, b.Ad }).IsUnique();

        builder.HasMany(b => b.Personeller)
            .WithOne(p => p.Birim)
            .HasForeignKey(p => p.BirimId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(b => b.Personeller).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(b => b.Ekipler)
            .WithOne(e => e.Birim)
            .HasForeignKey(e => e.BirimId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(b => b.Ekipler).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Ilk kurulumda SistemYoneticisi personelinin baglanacagi baslangic birimi.
        builder.HasData(new
        {
            Id = BaslangicVerisiSabitleri.Organizasyon.SistemYonetimiBirimi,
            KurumId = BaslangicVerisiSabitleri.Organizasyon.OrnekKurum,
            Ad = "Sistem Yönetimi",
            Aciklama = (string?)"Kurum, birim ve kullanici yonetimi.",
            AktifMi = true,
            OlusturmaZamaniUtc = BaslangicVerisiSabitleri.SabitOlusturmaZamaniUtc,
            SilindiMi = false,
        });
    }
}
