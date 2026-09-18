namespace SahaGor.Application.Bildirimler;

/// <summary>
/// Vatandasa SMS gonderme islemini soyutlar (SG-402). Gercek saglayici (Netgsm, Twilio vb.)
/// entegrasyonu Infrastructure katmaninda degisebilir; is kurallari bu detaydan bagimsizdir.
/// </summary>
public interface ISmsGonderimServisi
{
    Task GonderAsync(string telefonNumarasi, string mesaj, CancellationToken iptalToken = default);
}
