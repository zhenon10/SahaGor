using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence.Configurations;

public class YenilemeJetonuConfiguration : IEntityTypeConfiguration<YenilemeJetonu>
{
    public void Configure(EntityTypeBuilder<YenilemeJetonu> builder)
    {
        builder.ToTable("yenileme_jetonlari");
        TemelVarlikYapilandirmasi.Uygula(builder);

        // SHA-256 hex cikti sabit 64 karakterdir (bkz. YenilemeJetonu.TokenHash).
        builder.Property(j => j.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(j => j.SonKullanmaZamaniUtc).IsRequired();

        // Ayni jeton hash'iyle iki kayit olusmasi (cakisma/tekrar uretim) engellenir.
        builder.HasIndex(j => j.TokenHash).IsUnique();

        builder.HasOne(j => j.Personel)
            .WithMany()
            .HasForeignKey(j => j.PersonelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
