using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class GorevTalebiConfiguration : IEntityTypeConfiguration<GorevTalebi>
{
    public void Configure(EntityTypeBuilder<GorevTalebi> builder)
    {
        builder.ToTable("gorev_talepleri");
        TemelVarlikYapilandirmasi.Uygula(builder);

        builder.Property(g => g.Baslik).IsRequired().HasMaxLength(300);
        builder.Property(g => g.Aciklama).HasMaxLength(2000);
        builder.Property(g => g.DisKaynakReferansNo).HasMaxLength(100);
        builder.Property(g => g.BildirenTelefonNumarasi).HasMaxLength(20);

        builder.Property(g => g.Konum).IsRequired().HasColumnType("geography (Point,4326)");

        builder.Property(g => g.Durum).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(g => g.Oncelik).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(g => g.Kaynak).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasOne(g => g.Kategori)
            .WithMany()
            .HasForeignKey(g => g.KategoriId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Bolge)
            .WithMany()
            .HasForeignKey(g => g.BolgeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(g => g.AtananEkip)
            .WithMany()
            .HasForeignKey(g => g.AtananEkipId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(g => g.AtananPersonel)
            .WithMany()
            .HasForeignKey(g => g.AtananPersonelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(g => g.Fotograflar)
            .WithOne(f => f.GorevTalebi!)
            .HasForeignKey(f => f.GorevTalebiId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.Fotograflar).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(g => g.DurumGecmisi)
            .WithOne(h => h.GorevTalebi!)
            .HasForeignKey(h => h.GorevTalebiId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.DurumGecmisi).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Komuta panelinin durum filtreli listelemesi ve SLA alarm job'unun taramasi icin.
        builder.HasIndex(g => g.Durum);
        builder.HasIndex(g => g.SlaHedefZamaniUtc);

        // "Yakinimdaki gorevler" (ST_DWithin) sorgusu icin mekansal index.
        builder.HasIndex(g => g.Konum).HasMethod("GIST");

        // 153/CIMER gibi dis kaynaklardan ayni basvurunun (webhook tekrar denemesi veya
        // sinyal tekrar oynatma/replay saldirisi sonucu) birden fazla kez islenip mukerrer
        // gorev olusturmasini veritabani seviyesinde de engeller (SG-423, OWASP A04/A08).
        // Uygulama katmanindaki "once kontrol et" mantigi (DisKaynakBasvuruServisi) yarisma
        // durumuna (race condition) karsi tek basina yeterli degildir; bu index son savunma
        // hattidir. Sadece NULL OLMAYAN degerler icin benzersizlik uygulanir (cogu gorevin
        // dis kaynak referansi yoktur).
        builder.HasIndex(g => g.DisKaynakReferansNo)
            .IsUnique()
            .HasFilter("\"DisKaynakReferansNo\" IS NOT NULL");
    }
}
