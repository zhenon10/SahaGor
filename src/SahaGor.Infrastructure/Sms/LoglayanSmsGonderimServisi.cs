using Microsoft.Extensions.Logging;
using SahaGor.Application.Bildirimler;

namespace SahaGor.Infrastructure.Sms;

/// <summary>
/// Gercek bir SMS saglayicisi (orn. belediyenin sozlesmeli oldugu toplu SMS servisi)
/// entegre edilene kadar kullanilan gelistirme/yer tutucu implementasyon. Gonderimi
/// GERCEKTEN yapmaz; sadece yapilandirilmis (structured) log olarak kaydeder - boylece
/// is akisi (SG-402) gercek saglayici olmadan da uctan uca test edilebilir.
///
/// SG-403'teki retry/dead-letter kuyrugu, GERCEK bir saglayicinin gercek hata modlarina
/// (aginin cevap vermemesi, hatali numara, saglayici kotasi vb.) karsi anlam tasir; bu
/// yuzden kalici bir "gonderim kuyrugu" tablosu, gercek saglayici baglanildiginda
/// (production'a gecis oncesi) eklenecektir - simdiden bos bir altyapi kurmak, hicbir
/// gercek hatayi yakalayamayacagi icin yaniltici olurdu.
/// </summary>
public sealed class LoglayanSmsGonderimServisi : ISmsGonderimServisi
{
    private readonly ILogger<LoglayanSmsGonderimServisi> _logger;

    public LoglayanSmsGonderimServisi(ILogger<LoglayanSmsGonderimServisi> logger)
    {
        _logger = logger;
    }

    public Task GonderAsync(string telefonNumarasi, string mesaj, CancellationToken iptalToken = default)
    {
        if (string.IsNullOrWhiteSpace(telefonNumarasi))
        {
            throw new ArgumentException("Telefon numarasi bos olamaz.", nameof(telefonNumarasi));
        }

        _logger.LogInformation("[SIMULASYON] SMS gonderildi -> {TelefonNumarasiMaskeli}: {Mesaj}",
            TelefonNumarasiniMaskele(telefonNumarasi), mesaj);

        return Task.CompletedTask;
    }

    /// <summary>Loglarda telefon numarasinin tamaminin gorunmemesi icin son 4 hane disini maskeler.</summary>
    private static string TelefonNumarasiniMaskele(string telefonNumarasi)
    {
        if (telefonNumarasi.Length <= 4)
        {
            return new string('*', telefonNumarasi.Length);
        }

        return new string('*', telefonNumarasi.Length - 4) + telefonNumarasi[^4..];
    }
}
