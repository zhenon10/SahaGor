using Microsoft.Extensions.Options;
using SahaGor.Application.Gorevler;

namespace SahaGor.Infrastructure.Dosyalar;

/// <summary>
/// Kanit fotograflarini sunucunun yerel diskinde saklayan basit implementasyon. Production'a
/// gecerken (Sprint 4) bu sinif, ayni IFotografDepolamaServisi sozlesmesini karsilayan bir
/// S3/Blob Storage implementasyonuyla degistirilebilir; cagiran kod (GorevTalebiServisi)
/// hicbir sekilde etkilenmez.
/// </summary>
public sealed class YerelDosyaFotografDepolamaServisi : IFotografDepolamaServisi
{
    private readonly string _kokDizin;

    public YerelDosyaFotografDepolamaServisi(IOptions<DosyaDepolamaAyarlari> ayarlar)
    {
        _kokDizin = Path.GetFullPath(ayarlar.Value.KokDizin);
        Directory.CreateDirectory(_kokDizin);
    }

    public async Task<string> KaydetAsync(Stream icerik, string dosyaUzantisi, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(icerik);

        // Depolama anahtari olarak rastgele bir Guid kullanilir; istemcinin gonderdigi
        // orijinal dosya adina guvenilmez (path traversal / cakisma riski).
        var dosyaAdi = $"{Guid.NewGuid()}{dosyaUzantisi}";
        var tamYol = Path.Combine(_kokDizin, dosyaAdi);

        await using var hedefDosya = File.Create(tamYol);
        await icerik.CopyToAsync(hedefDosya, iptalToken);

        return dosyaAdi;
    }
}
