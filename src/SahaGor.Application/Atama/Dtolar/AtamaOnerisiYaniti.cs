namespace SahaGor.Application.Atama.Dtolar;

/// <summary>
/// Bir gorev icin atama motorunun urettigi tam oneri: en iyi aday (varsa) ve tum
/// degerlendirilen adaylarin skor dokumu. Amir, onerilen ekibi tek tikla onaylayabilir
/// veya listeden baska birini secebilir (SG-311).
/// </summary>
public sealed record AtamaOnerisiYaniti(
    Guid GorevId,
    AtamaAdayiYaniti? OnerilenEkip,
    IReadOnlyList<AtamaAdayiYaniti> TumAdaylar);
