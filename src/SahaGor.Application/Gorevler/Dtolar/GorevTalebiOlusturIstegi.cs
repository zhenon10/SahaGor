using System.ComponentModel.DataAnnotations;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Application.Gorevler.Dtolar;

/// <summary>POST /api/gorevler istegi govdesi.</summary>
public sealed record GorevTalebiOlusturIstegi(
    [property: Required, MaxLength(300)] string Baslik,
    [property: MaxLength(2000)] string? Aciklama,
    [property: Required] Guid KategoriId,
    [property: Range(-90, 90)] double Enlem,
    [property: Range(-180, 180)] double Boylam,
    [property: Required] GorevOnceligi Oncelik,
    [property: Required] GorevKaynagi Kaynak,
    [property: MaxLength(20)] string? BildirenTelefonNumarasi,
    [property: MaxLength(100)] string? DisKaynakReferansNo);
