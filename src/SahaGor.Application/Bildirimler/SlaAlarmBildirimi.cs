using SahaGor.Domain.Enumlar;

namespace SahaGor.Application.Bildirimler;

/// <summary>
/// SLA arka plan tarama servisinin urettigi bir alarm bildirimi; SignalR uzerinden komuta
/// panelindeki Amir istemcilerine canli olarak yayinlanir (SG-410, SG-411).
/// </summary>
public sealed record SlaAlarmBildirimi(
    Guid GorevId,
    string Baslik,
    string KategoriAdi,
    SlaAlarmTuru AlarmTuru,
    DateTime SlaHedefZamaniUtc,
    double TuketimYuzdesi,
    DateTime ZamanUtc);
