namespace SahaGor.Application.Gorevler;

/// <summary>
/// Acik gorevleri SLA hedeflerine gore tarayip esik asimi/ihlal alarmlarini yayinlayan servis
/// (SG-410, SG-411). Tarama periyodu (arka plan servisi) Api katmaninda, bu servisin cagirdigi
/// is mantigi ise burada, Infrastructure'da uygulanir.
/// </summary>
public interface ISlaAlarmTarayiciServisi
{
    /// <summary>
    /// Tum acik gorevleri tek seferde tarar; daha once gonderilmemis "yaklasiyor"/"ihlal"
    /// alarmlarini yayinlar ve ilgili gorevleri tekrar alarm gonderilmeyecek sekilde isaretler.
    /// </summary>
    /// <returns>Bu taramada gonderilen toplam alarm sayisi (izleme/loglama amacli).</returns>
    Task<int> TaraVeAlarmlariGonderAsync(CancellationToken iptalToken = default);
}
