using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SahaGor.Application.Bildirimler;
using SahaGor.Application.Gorevler;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Gorevler;

/// <summary>
/// Acik gorevleri periyodik olarak tarayip SLA esik asimi/ihlal alarmlarini yayinlayan servis
/// (SG-410, SG-411). Ayni alarmin tekrar tekrar gonderilmesini onlemek icin (SG-412), her
/// gorevde alarm turu basina bir kez isaretlenen zaman damgalari kullanilir.
/// </summary>
public sealed class SlaAlarmTarayiciServisi : ISlaAlarmTarayiciServisi
{
    /// <summary>SLA'nin hala isledigi (henuz kapanmamis) durumlar; sadece bunlar taranir.</summary>
    private static readonly GorevDurumu[] AcikDurumlar =
    {
        GorevDurumu.Acildi,
        GorevDurumu.Atanamadi,
        GorevDurumu.Atandi,
        GorevDurumu.YolaCikildi,
        GorevDurumu.Baslandi,
    };

    private readonly SahaGorDbContext _dbContext;
    private readonly ISlaAlarmYayinlayici _slaAlarmYayinlayici;
    private readonly SlaAlarmAyarlari _ayarlar;
    private readonly ILogger<SlaAlarmTarayiciServisi> _logger;

    public SlaAlarmTarayiciServisi(SahaGorDbContext dbContext, ISlaAlarmYayinlayici slaAlarmYayinlayici,
        IOptions<SlaAlarmAyarlari> ayarlar, ILogger<SlaAlarmTarayiciServisi> logger)
    {
        _dbContext = dbContext;
        _slaAlarmYayinlayici = slaAlarmYayinlayici;
        _ayarlar = ayarlar.Value;
        _logger = logger;
    }

    public async Task<int> TaraVeAlarmlariGonderAsync(CancellationToken iptalToken = default)
    {
        var suAn = DateTime.UtcNow;

        // Iki alarm turunden EN AZ biri henuz gonderilmemis acik gorevler getirilir; her ikisi de
        // gonderilmis gorevler (artik alarm uretemeyecekleri icin) taramadan tamamen elenir.
        var adaylar = await _dbContext.GorevTalepleri
            .Include(g => g.Kategori)
            .Where(g => AcikDurumlar.Contains(g.Durum) &&
                (g.SlaYaklasmaAlarmiZamaniUtc == null || g.SlaIhlalAlarmiZamaniUtc == null))
            .ToListAsync(iptalToken);

        var gonderilenAlarmSayisi = 0;

        foreach (var gorev in adaylar)
        {
            if (gorev.SlaIhlalAlarmiZamaniUtc is null && gorev.SlaIhlalEdildiMi(suAn))
            {
                gorev.SlaIhlalAlarmiGonderildiOlarakIsaretle(suAn);
                await _slaAlarmYayinlayici.YayinlaAsync(BildirimOlustur(gorev, SlaAlarmTuru.Ihlal, suAn), iptalToken);
                gonderilenAlarmSayisi++;

                _logger.LogWarning("SLA IHLAL alarmi gonderildi: {GorevId} - {Baslik}.", gorev.Id, gorev.Baslik);
                continue;
            }

            if (gorev.SlaYaklasmaAlarmiZamaniUtc is null &&
                gorev.SlaTuketimYuzdesi(suAn) >= _ayarlar.YaklasmaEsikYuzdesi)
            {
                gorev.SlaYaklasmaAlarmiGonderildiOlarakIsaretle(suAn);
                await _slaAlarmYayinlayici.YayinlaAsync(BildirimOlustur(gorev, SlaAlarmTuru.Yaklasiyor, suAn), iptalToken);
                gonderilenAlarmSayisi++;

                _logger.LogInformation("SLA YAKLASMA alarmi gonderildi: {GorevId} - {Baslik}.", gorev.Id, gorev.Baslik);
            }
        }

        if (gonderilenAlarmSayisi > 0)
        {
            await _dbContext.SaveChangesAsync(iptalToken);
        }

        return gonderilenAlarmSayisi;
    }

    private static SlaAlarmBildirimi BildirimOlustur(GorevTalebi gorev, SlaAlarmTuru tur, DateTime suAn) =>
        new(gorev.Id, gorev.Baslik, gorev.Kategori?.Ad ?? "Bilinmeyen kategori", tur, gorev.SlaHedefZamaniUtc,
            gorev.SlaTuketimYuzdesi(suAn), suAn);
}
