namespace SahaGor.Application.Bildirimler;

/// <summary>
/// SLA alarmlarini gercek zamanli istemcilere yayinlamayi soyutlar. Application/Infrastructure
/// katmanlari SignalR'in kendisini bilmez; somut yayin mekanizmasi (Api katmanindaki SignalR Hub)
/// bu arayuzu DI konteynerinde bagimlilik tersine cevirme (dependency inversion) ile karsilar.
/// </summary>
public interface ISlaAlarmYayinlayici
{
    Task YayinlaAsync(SlaAlarmBildirimi bildirim, CancellationToken iptalToken = default);
}
