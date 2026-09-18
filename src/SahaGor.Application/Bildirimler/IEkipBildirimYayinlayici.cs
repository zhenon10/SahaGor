namespace SahaGor.Application.Bildirimler;

/// <summary>IGorevBildirimYayinlayici ile ayni gerekceyle: SignalR'in kendisi Api katmaninda kalir.</summary>
public interface IEkipBildirimYayinlayici
{
    Task YayinlaAsync(EkipKonumBildirimi bildirim, CancellationToken iptalToken = default);
}
