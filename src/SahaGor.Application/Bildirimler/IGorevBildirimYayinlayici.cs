namespace SahaGor.Application.Bildirimler;

/// <summary>
/// Gorev olaylarini gercek zamanli istemcilere yayinlamayi soyutlar. Application/Infrastructure
/// katmanlari SignalR'in kendisini bilmez; somut yayin mekanizmasi (Api katmanindaki SignalR Hub)
/// bu arayuzu DI konteynerinde bagimlilik tersine cevirme (dependency inversion) ile karsilar.
/// </summary>
public interface IGorevBildirimYayinlayici
{
    Task YayinlaAsync(GorevBildirimi bildirim, CancellationToken iptalToken = default);
}
