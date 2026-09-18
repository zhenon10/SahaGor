using Microsoft.Extensions.Options;
using SahaGor.Application.Gorevler;
using SahaGor.Infrastructure.Gorevler;

namespace SahaGor.Api.ArkaPlanIsleri;

/// <summary>
/// Uygulama boyunca calisan, periyodik olarak acik gorevleri SLA esiklerine gore tarayan
/// arka plan servisi (SG-410). Is mantigi (hangi gorev alarm uretir) ISlaAlarmTarayiciServisi'nde;
/// bu sinifin tek sorumlulugu periyodu yonetmek ve DbContext gibi "scoped" bagimliliklar icin
/// her turda yeni bir DI kapsami (scope) acmaktir (BackgroundService'in kendisi singleton'dir).
/// </summary>
public sealed class SlaAlarmArkaPlanServisi : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SlaAlarmAyarlari _ayarlar;
    private readonly ILogger<SlaAlarmArkaPlanServisi> _logger;

    public SlaAlarmArkaPlanServisi(IServiceScopeFactory scopeFactory, IOptions<SlaAlarmAyarlari> ayarlar,
        ILogger<SlaAlarmArkaPlanServisi> logger)
    {
        _scopeFactory = scopeFactory;
        _ayarlar = ayarlar.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken durdurmaTokeni)
    {
        _logger.LogInformation("SLA alarm arka plan servisi baslatildi. Tarama araligi: {Saniye} saniye.",
            _ayarlar.TaramaAraligiSaniye);

        using var zamanlayici = new PeriodicTimer(TimeSpan.FromSeconds(_ayarlar.TaramaAraligiSaniye));

        // Uygulama ayaga kalkar kalkmaz ilk tarama, ilk periyodu beklemeden hemen yapilir.
        await TekTaramaCalistirAsync(durdurmaTokeni);

        try
        {
            while (await zamanlayici.WaitForNextTickAsync(durdurmaTokeni))
            {
                await TekTaramaCalistirAsync(durdurmaTokeni);
            }
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapatiliyor; beklenen bir durumdur, hata olarak loglanmaz.
        }
    }

    private async Task TekTaramaCalistirAsync(CancellationToken iptalToken)
    {
        try
        {
            // SahaGorDbContext ve ISlaAlarmTarayiciServisi "scoped" servislerdir; BackgroundService
            // singleton oldugundan, her tarama icin ayri bir DI kapsami acilmasi zorunludur.
            using var kapsam = _scopeFactory.CreateScope();
            var tarayici = kapsam.ServiceProvider.GetRequiredService<ISlaAlarmTarayiciServisi>();

            var gonderilenAlarmSayisi = await tarayici.TaraVeAlarmlariGonderAsync(iptalToken);

            if (gonderilenAlarmSayisi > 0)
            {
                _logger.LogInformation("SLA taramasi tamamlandi: {Sayi} yeni alarm gonderildi.", gonderilenAlarmSayisi);
            }
        }
        catch (OperationCanceledException) when (iptalToken.IsCancellationRequested)
        {
            // Uygulama kapatiliyor; bu beklenen bir durumdur, hata olarak loglanmaz.
        }
        catch (Exception hata)
        {
            // Tek bir tarama basarisiz olsa dahi (orn. gecici veritabani baglanti sorunu),
            // arka plan servisinin tamamen durmasi yerine bir sonraki periyotta tekrar denenir.
            _logger.LogError(hata, "SLA alarm taramasi sirasinda beklenmeyen bir hata olustu.");
        }
    }
}
