namespace SahaGor.Application.Gorevler.Dtolar;

/// <summary>GET /api/gorevler/yakinimdaki sonuc satiri; mesafeye gore artan sirada doner (SG-131).</summary>
public sealed record YakinimdakiGorevYaniti(
    Guid Id,
    string Baslik,
    string Durum,
    string Oncelik,
    double MesafeMetre,
    double Enlem,
    double Boylam);
