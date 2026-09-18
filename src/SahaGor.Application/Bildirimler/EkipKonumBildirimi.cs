namespace SahaGor.Application.Bildirimler;

/// <summary>Bir ekibin canli konum guncellemesi; SignalR uzerinden komuta paneline yayinlanir (SG-302).</summary>
public sealed record EkipKonumBildirimi(Guid EkipId, string EkipAdi, double Enlem, double Boylam, DateTime ZamanUtc);
