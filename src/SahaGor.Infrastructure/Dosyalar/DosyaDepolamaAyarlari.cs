namespace SahaGor.Infrastructure.Dosyalar;

/// <summary>appsettings.json / ortam degiskenlerinden baglanan dosya depolama yapilandirmasi.</summary>
public sealed class DosyaDepolamaAyarlari
{
    public const string BolumAdi = "DosyaDepolama";

    /// <summary>
    /// Kanit fotograflarinin yazilacagi kok dizin. Goreceli verilirse calisma dizinine (Environment.CurrentDirectory)
    /// gore cozumlenir. Production'da bu yolun kalici bir disk/volume'e (bkz. docker-compose) isaret etmesi gerekir;
    /// aksi halde konteyner yeniden olusturuldugunda tum kanit fotograflari kaybolur.
    /// </summary>
    public string KokDizin { get; set; } = "App_Data/gorev-fotograflari";
}
