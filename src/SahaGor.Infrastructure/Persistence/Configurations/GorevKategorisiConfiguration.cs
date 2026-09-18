using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class GorevKategorisiConfiguration : IEntityTypeConfiguration<GorevKategorisi>
{
    public void Configure(EntityTypeBuilder<GorevKategorisi> builder)
    {
        builder.ToTable("gorev_kategorileri");
        TemelVarlikYapilandirmasi.Uygula(builder);

        builder.Property(k => k.Ad).IsRequired().HasMaxLength(200);
        builder.Property(k => k.Aciklama).HasMaxLength(1000);
        builder.Property(k => k.SlaYanitSuresiDakika).IsRequired();
        builder.Property(k => k.SlaCozumSuresiDakika).IsRequired();

        builder.HasIndex(k => k.Ad).IsUnique();

        // Belediyelerde en sik karsilasilan gorev turleri icin baslangic (referans) verisi.
        // Entity kurucusu yerine SABIT Id/zaman degerleri iceren anonim nesneler kullanilir
        // (bkz. BaslangicVerisiSabitleri) - aksi halde her migration yenilemesinde yeni
        // rastgele Id'ler uretilip gereksiz delete/insert farklari olusurdu.
        var suAn = BaslangicVerisiSabitleri.SabitOlusturmaZamaniUtc;

        builder.HasData(
            new
            {
                Id = BaslangicVerisiSabitleri.GorevKategorileri.KirikKaldirim,
                Ad = "Kırık Kaldırım / Yol Bozukluğu",
                Aciklama = "Kaldırım, yol veya bordür hasarları.",
                SlaYanitSuresiDakika = 60,
                SlaCozumSuresiDakika = 4320,
                AktifMi = true,
                OlusturmaZamaniUtc = suAn,
                SilindiMi = false,
            },
            new
            {
                Id = BaslangicVerisiSabitleri.GorevKategorileri.SokakLambasi,
                Ad = "Sokak Lambası Arızası",
                Aciklama = "Aydınlatma direği veya lamba arızaları.",
                SlaYanitSuresiDakika = 30,
                SlaCozumSuresiDakika = 1440,
                AktifMi = true,
                OlusturmaZamaniUtc = suAn,
                SilindiMi = false,
            },
            new
            {
                Id = BaslangicVerisiSabitleri.GorevKategorileri.CopToplama,
                Ad = "Çöp Toplama Aksaklığı",
                Aciklama = "Zamanında toplanmayan çöp/atık bildirimleri.",
                SlaYanitSuresiDakika = 30,
                SlaCozumSuresiDakika = 480,
                AktifMi = true,
                OlusturmaZamaniUtc = suAn,
                SilindiMi = false,
            },
            new
            {
                Id = BaslangicVerisiSabitleri.GorevKategorileri.BasibosHayvan,
                Ad = "Başıboş Hayvan İhbarı",
                Aciklama = "Sokak hayvanı ile ilgili şikayet/yardım talebi.",
                SlaYanitSuresiDakika = 30,
                SlaCozumSuresiDakika = 720,
                AktifMi = true,
                OlusturmaZamaniUtc = suAn,
                SilindiMi = false,
            });
    }
}
