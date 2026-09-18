using SahaGor.Application.Bildirimler;

namespace SahaGor.Api.Hublar;

/// <summary>
/// SignalR istemcilerinin (komuta paneli) sunucudan alacagi metotlarin guclu tipli sozlesmesi.
/// Bu sayede istemci tarafinda metot adi/imza hatalari derleme zamaninda yakalanir.
/// </summary>
public interface IGorevTalebiHubClient
{
    /// <summary>Bir gorev olusturuldugunda veya durumu degistiginde tetiklenir (SG-140).</summary>
    Task GorevGuncellendi(GorevBildirimi bildirim, CancellationToken iptalToken = default);

    /// <summary>Bir ekibin canli konumu guncellendiginde tetiklenir (SG-301, SG-302).</summary>
    Task EkipKonumuGuncellendi(EkipKonumBildirimi bildirim, CancellationToken iptalToken = default);

    /// <summary>Bir gorev SLA esigine yaklastiginda veya SLA'yi ihlal ettiginde tetiklenir (SG-410, SG-411).</summary>
    Task SlaAlarmiTetiklendi(SlaAlarmBildirimi bildirim, CancellationToken iptalToken = default);
}
