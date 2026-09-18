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

        builder.Property(j => j.Token).IsRequired().HasMaxLength(200);
        builder.Property(j => j.SonKullanmaZamaniUtc).IsRequired();

        // Ayni jeton degeriyle iki kayit olusmasi (cakisma/tekrar uretim) engellenir.
        builder.HasIndex(j => j.Token).IsUnique();

        builder.HasOne(j => j.Personel)
            .WithMany()
            .HasForeignKey(j => j.PersonelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
