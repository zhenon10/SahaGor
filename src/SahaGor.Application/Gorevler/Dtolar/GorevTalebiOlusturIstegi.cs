using System.ComponentModel.DataAnnotations;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Application.Gorevler.Dtolar;

/// <summary>
/// POST /api/gorevler istegi govdesi. Dogrulama nitelikleri BILEREK "property:" hedefi
/// OLMADAN, dogrudan constructor parametrelerine uygulanir: ASP.NET Core'un record'lara
/// ozel model dogrulamasi (constructor'dan once dogrulama yapabilmek icin) nitelikleri
/// parametrede bekler; "property:" ile property'ye tasinirlarsa calisma zamaninda
/// "validation metadata... will be ignored" InvalidOperationException'i firlatilir
/// (SG-421'de gercek bir HTTP istegiyle ortaya cikan production hatasi).
/// </summary>
public sealed record GorevTalebiOlusturIstegi(
    [Required, MaxLength(300)] string Baslik,
    [MaxLength(2000)] string? Aciklama,
    [Required] Guid KategoriId,
    [Range(-90, 90)] double Enlem,
    [Range(-180, 180)] double Boylam,
    [Required] GorevOnceligi Oncelik,
    [Required] GorevKaynagi Kaynak,
    [MaxLength(20)] string? BildirenTelefonNumarasi,
    [MaxLength(100)] string? DisKaynakReferansNo);
