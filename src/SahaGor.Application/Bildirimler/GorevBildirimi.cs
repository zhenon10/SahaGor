namespace SahaGor.Application.Bildirimler;

/// <summary>
/// Bir GorevTalebi olay (olusturma/durum degisikligi) bildirimi. SignalR uzerinden
/// komuta panelindeki Amir istemcilerine canli olarak yayinlanir (SG-140).
/// </summary>
public sealed record GorevBildirimi(Guid GorevId, string Baslik, string Durum, DateTime ZamanUtc);
