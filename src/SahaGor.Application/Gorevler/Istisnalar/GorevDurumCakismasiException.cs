namespace SahaGor.Application.Gorevler.Istisnalar;

/// <summary>
/// Bir durum gecis istegi, gorev baska bir islem tarafindan (orn. baska bir cihazdan
/// gonderilmis senkron kuyrugu ogesi, baska bir personel) farkli bir duruma tasindigi
/// icin gecersiz hale geldiginde firlatilir (SG-203 - offline senkron cakisma tespiti).
/// </summary>
public sealed class GorevDurumCakismasiException : Exception
{
    public Guid GorevId { get; }

    public string MevcutDurum { get; }

    public GorevDurumCakismasiException(Guid gorevId, string mevcutDurum)
        : base($"Gorev baska bir islem tarafindan '{mevcutDurum}' durumuna getirilmis. Bu istek uygulanamadi.")
    {
        GorevId = gorevId;
        MevcutDurum = mevcutDurum;
    }
}
