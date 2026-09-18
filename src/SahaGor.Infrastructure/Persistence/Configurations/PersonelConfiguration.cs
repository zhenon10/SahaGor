using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class PersonelConfiguration : IEntityTypeConfiguration<Personel>
{
    public void Configure(EntityTypeBuilder<Personel> builder)
    {
        builder.ToTable("personeller");
        TemelVarlikYapilandirmasi.Uygula(builder);

        builder.Property(p => p.AdSoyad).IsRequired().HasMaxLength(200);
        builder.Property(p => p.KullaniciAdi).IsRequired().HasMaxLength(100);
        builder.Property(p => p.SifreHash).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Telefon).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Eposta).HasMaxLength(200);

        // Enum degerleri veritabaninda okunabilir metin olarak saklanir; sayisal degerlerin
        // migration sirasi degisince anlam kaymasi (orn. 2 = Amir yerine SahaPersoneli) onlenir.
        builder.Property(p => p.Rol).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(p => p.KullaniciAdi).IsUnique();

        builder.HasOne(p => p.Ekip)
            .WithMany(e => e.Uyeler)
            .HasForeignKey(p => p.EkipId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ilk kurulumda sisteme giris yapabilecek tek hesap: SistemYoneticisi.
        // Sifre "DegistirilmeliSifre#2026" - bu hash'in ustunde calisildigi icin
        // ILK GIRISTEN HEMEN SONRA degistirilmesi ZORUNLU tutulmalidir (bkz. README).
        builder.HasData(new
        {
            Id = BaslangicVerisiSabitleri.Organizasyon.SistemYoneticisiPersoneli,
            BirimId = BaslangicVerisiSabitleri.Organizasyon.SistemYonetimiBirimi,
            EkipId = (Guid?)null,
            AdSoyad = "Sistem Yöneticisi",
            KullaniciAdi = "sistem.yoneticisi",
            SifreHash = "$2a$12$nAwKPKrugSehsxpFuJIIMuhqRs9dlar/YXWPs5twVFGfXBUkRYjIq",
            Telefon = "0000000000",
            Eposta = (string?)null,
            Rol = PersonelRolu.SistemYoneticisi,
            AktifMi = true,
            MusaitMi = true,
            BasarisizGirisSayisi = 0,
            KilitlenmeBitisZamaniUtc = (DateTime?)null,
            OlusturmaZamaniUtc = BaslangicVerisiSabitleri.SabitOlusturmaZamaniUtc,
            SilindiMi = false,
        });
    }
}
