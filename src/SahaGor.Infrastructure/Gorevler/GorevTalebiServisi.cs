using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SahaGor.Application.Atama;
using SahaGor.Application.Bildirimler;
using SahaGor.Application.Ekipler.Istisnalar;
using SahaGor.Application.Gorevler;
using SahaGor.Application.Gorevler.Dtolar;
using SahaGor.Application.Gorevler.Istisnalar;
using SahaGor.Application.Personeller.Istisnalar;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Gorevler;

/// <summary>
/// GorevTalebi CRUD ve coğrafi sorgularin EF Core/PostGIS implementasyonu. Yakin konum ve
/// bolge eslestirme sorgulari, NetTopologySuite'in Npgsql eklentisi araciligiyla dogrudan
/// ST_DWithin/ST_Distance/ST_Covers fonksiyonlarina cevrilir.
/// </summary>
public sealed class GorevTalebiServisi : IGorevTalebiServisi
{
    private readonly SahaGorDbContext _dbContext;
    private readonly IGorevBildirimYayinlayici _bildirimYayinlayici;
    private readonly IFotografDepolamaServisi _fotografDepolamaServisi;
    private readonly IAtamaMotoru _atamaMotoru;
    private readonly ISmsGonderimServisi _smsGonderimServisi;
    private readonly ILogger<GorevTalebiServisi> _logger;

    public GorevTalebiServisi(SahaGorDbContext dbContext, IGorevBildirimYayinlayici bildirimYayinlayici,
        IFotografDepolamaServisi fotografDepolamaServisi, IAtamaMotoru atamaMotoru,
        ISmsGonderimServisi smsGonderimServisi, ILogger<GorevTalebiServisi> logger)
    {
        _dbContext = dbContext;
        _bildirimYayinlayici = bildirimYayinlayici;
        _fotografDepolamaServisi = fotografDepolamaServisi;
        _atamaMotoru = atamaMotoru;
        _smsGonderimServisi = smsGonderimServisi;
        _logger = logger;
    }

    public async Task<GorevTalebiDetayYaniti> OlusturAsync(GorevTalebiOlusturIstegi istek, Guid? olusturanPersonelId,
        CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var kategori = await _dbContext.GorevKategorileri
            .FirstOrDefaultAsync(k => k.Id == istek.KategoriId, iptalToken)
            ?? throw new GorevKategorisiBulunamadiException(istek.KategoriId);

        if (!kategori.AktifMi)
        {
            throw new GorevKategorisiPasifException(kategori.Ad);
        }

        var konum = KonumFabrikasi.NoktaOlustur(istek.Enlem, istek.Boylam);

        var gorev = new GorevTalebi(
            istek.Baslik,
            kategori,
            konum,
            istek.Oncelik,
            istek.Kaynak,
            istek.Aciklama,
            istek.BildirenTelefonNumarasi,
            istek.DisKaynakReferansNo);

        if (olusturanPersonelId.HasValue)
        {
            gorev.OlusturanKullaniciyiAyarla(olusturanPersonelId.Value);
        }

        // Otomatik bolgelendirme (SG-115): konumu kapsayan ilk bolge bulunursa atanir;
        // hicbir bolge eslesmezse gorev "bolge disi" olarak (BolgeId = null) acik kalir.
        // ST_Contains DEGIL ST_Covers kullanilir; SinirPolygonu "geography" oldugundan ve
        // PostGIS'te ST_Contains geography icin tanimli olmadigindan bu bilinctli bir secimdir
        // (bkz. Bolge.NoktayiKapsiyorMu ustundeki aciklama, SG-420).
        var kapsayanBolge = await _dbContext.Bolgeler
            .FirstOrDefaultAsync(b => b.SinirPolygonu.Covers(konum), iptalToken);

        if (kapsayanBolge is not null)
        {
            gorev.BolgeAta(kapsayanBolge);
        }

        _dbContext.GorevTalepleri.Add(gorev);
        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Yeni gorev talebi olusturuldu: {GorevId} - {Baslik}.", gorev.Id, gorev.Baslik);

        // SG-310/SG-311: sistem hicbir zaman otomatik ATAMAZ, sadece oneri hesaplar (loglanir).
        // Tek istisna: hic uygun ekip yoksa, gorev "Atanamadi" olarak isaretlenip Amir'e
        // haber verilir (AC: "Uygun ekip yoksa gorev 'Atanamadi' kuyruguna duser").
        var oneri = await _atamaMotoru.OneriHesaplaAsync(gorev.Id, iptalToken);
        if (oneri.OnerilenEkip is null)
        {
            gorev.AtanamadiOlarakIsaretle("Aktif ve en az bir aktif uyesi olan uygun bir ekip bulunamadi.");
            await _dbContext.SaveChangesAsync(iptalToken);
        }

        await _bildirimYayinlayici.YayinlaAsync(
            new GorevBildirimi(gorev.Id, gorev.Baslik, gorev.Durum.ToString(), DateTime.UtcNow), iptalToken);

        return await DetayGetirAsync(gorev.Id, iptalToken);
    }

    public async Task<SayfalanmisSonuc<GorevTalebiOzetYaniti>> ListeleAsync(GorevTalebiFiltre filtre,
        CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtre);

        var sorgu = _dbContext.GorevTalepleri
            .Include(g => g.Kategori)
            .Include(g => g.Bolge)
            .Include(g => g.AtananEkip)
            .AsQueryable();

        if (filtre.Durum.HasValue)
        {
            sorgu = sorgu.Where(g => g.Durum == filtre.Durum.Value);
        }

        if (filtre.KategoriId.HasValue)
        {
            sorgu = sorgu.Where(g => g.KategoriId == filtre.KategoriId.Value);
        }

        if (filtre.BolgeId.HasValue)
        {
            sorgu = sorgu.Where(g => g.BolgeId == filtre.BolgeId.Value);
        }

        if (filtre.AtananEkipId.HasValue)
        {
            sorgu = sorgu.Where(g => g.AtananEkipId == filtre.AtananEkipId.Value);
        }

        if (filtre.AtananPersonelId.HasValue)
        {
            sorgu = sorgu.Where(g => g.AtananPersonelId == filtre.AtananPersonelId.Value);
        }

        if (filtre.BaslangicTarihiUtc.HasValue)
        {
            sorgu = sorgu.Where(g => g.OlusturulmaZamaniUtc >= filtre.BaslangicTarihiUtc.Value);
        }

        if (filtre.BitisTarihiUtc.HasValue)
        {
            sorgu = sorgu.Where(g => g.OlusturulmaZamaniUtc <= filtre.BitisTarihiUtc.Value);
        }

        var toplamKayitSayisi = await sorgu.CountAsync(iptalToken);

        var kayitlar = await sorgu
            .OrderByDescending(g => g.OlusturulmaZamaniUtc)
            .Skip((filtre.Sayfa - 1) * filtre.SayfaBoyutu)
            .Take(filtre.SayfaBoyutu)
            .Select(g => new GorevTalebiOzetYaniti(
                g.Id,
                g.Baslik,
                g.Kategori!.Ad,
                g.Durum.ToString(),
                g.Oncelik.ToString(),
                g.Konum.Y,
                g.Konum.X,
                g.Bolge != null ? g.Bolge.Ad : null,
                g.AtananEkip != null ? g.AtananEkip.Ad : null,
                g.OlusturulmaZamaniUtc,
                g.SlaHedefZamaniUtc,
                g.Durum != GorevDurumu.Tamamlandi &&
                    g.Durum != GorevDurumu.Dogrulandi &&
                    DateTime.UtcNow > g.SlaHedefZamaniUtc))
            .ToListAsync(iptalToken);

        return new SayfalanmisSonuc<GorevTalebiOzetYaniti>(kayitlar, toplamKayitSayisi, filtre.Sayfa, filtre.SayfaBoyutu);
    }

    public async Task<GorevTalebiDetayYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default)
    {
        var gorev = await _dbContext.GorevTalepleri
            .Include(g => g.Kategori)
            .Include(g => g.Bolge)
            .Include(g => g.AtananEkip)
            .Include(g => g.AtananPersonel)
            .Include(g => g.Fotograflar)
            .Include(g => g.DurumGecmisi)
            .FirstOrDefaultAsync(g => g.Id == id, iptalToken)
            ?? throw new GorevTalebiBulunamadiException(id);

        return new GorevTalebiDetayYaniti(
            gorev.Id,
            gorev.Baslik,
            gorev.Aciklama,
            gorev.Kategori!.Ad,
            gorev.Durum.ToString(),
            gorev.Oncelik.ToString(),
            gorev.Kaynak.ToString(),
            gorev.Konum.Y,
            gorev.Konum.X,
            gorev.Bolge?.Ad,
            gorev.AtananEkip?.Ad,
            gorev.AtananPersonel?.AdSoyad,
            gorev.BildirenTelefonNumarasi,
            gorev.OlusturulmaZamaniUtc,
            gorev.SlaHedefZamaniUtc,
            gorev.SlaIhlalEdildiMi(),
            gorev.DurumGecmisi
                .OrderBy(h => h.DegisiklikZamaniUtc)
                .Select(h => new GorevDurumGecmisiYaniti(h.OncekiDurum.ToString(), h.YeniDurum.ToString(), h.DegisiklikZamaniUtc, h.Not))
                .ToList(),
            gorev.Fotograflar
                .OrderBy(f => f.CekilmeZamaniUtc)
                .Select(f => new GorevFotografiYaniti(f.Id, f.DosyaYolu, f.Asama.ToString(), f.CekilmeZamaniUtc))
                .ToList());
    }

    public async Task<IReadOnlyList<YakinimdakiGorevYaniti>> YakinimdakiGetirAsync(double enlem, double boylam,
        double yaricapMetre, int maksimumSonucSayisi, CancellationToken iptalToken = default)
    {
        if (yaricapMetre <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(yaricapMetre), yaricapMetre, "Yaricap pozitif olmalidir.");
        }

        var merkezNokta = KonumFabrikasi.NoktaOlustur(enlem, boylam);

        var acikOlmayanDurumlar = new[]
        {
            GorevDurumu.Tamamlandi,
            GorevDurumu.Dogrulandi,
            GorevDurumu.Iptal,
        };

        // Geography kolonunda IsWithinDistance -> ST_DWithin(...), Distance -> ST_Distance(...)
        // olarak Npgsql.NetTopologySuite eklentisi tarafindan cevrilir; GiST index sayesinde
        // (bkz. GorevTalebiConfiguration) bu sorgu tum tabloyu taramadan calisir.
        //
        // ONEMLI (SG-420'de gercek PostGIS'e karsi ortaya cikan hata): ST_X/ST_Y, "geography"
        // kolon tipi icin PostGIS'te TANIMLI DEGILDIR (sadece "geometry" icin vardir). Bu yuzden
        // g.Konum.X/g.Konum.Y ifadeleri IQueryable Select() icinde (yani SQL'e cevrilecek sekilde)
        // KULLANILAMAZ. Cozum: Konum'un tamamini (ham deger, fonksiyon cagrisi gerektirmez) SQL
        // seviyesinde secip, X/Y degerlerini ENTITY'LER BELLEGE ALINDIKTAN SONRA, materyalize
        // edilmis NetTopologySuite Point nesnesinin duz C# ozellikleri olarak okuruz.
        var sorguSonuclari = await _dbContext.GorevTalepleri
            .Where(g => !acikOlmayanDurumlar.Contains(g.Durum))
            .Where(g => g.Konum.IsWithinDistance(merkezNokta, yaricapMetre))
            .OrderBy(g => g.Konum.Distance(merkezNokta))
            .Take(maksimumSonucSayisi)
            .Select(g => new
            {
                g.Id,
                g.Baslik,
                g.Durum,
                g.Oncelik,
                MesafeMetre = g.Konum.Distance(merkezNokta),
                g.Konum,
            })
            .ToListAsync(iptalToken);

        return sorguSonuclari
            .Select(g => new YakinimdakiGorevYaniti(
                g.Id,
                g.Baslik,
                g.Durum.ToString(),
                g.Oncelik.ToString(),
                g.MesafeMetre,
                g.Konum.Y,
                g.Konum.X))
            .ToList();
    }

    public async Task IptalEtAsync(Guid id, string neden, Guid iptalEdenPersonelId, CancellationToken iptalToken = default)
    {
        var gorev = await _dbContext.GorevTalepleri.FirstOrDefaultAsync(g => g.Id == id, iptalToken)
            ?? throw new GorevTalebiBulunamadiException(id);

        gorev.IptalEt(iptalEdenPersonelId, neden);
        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Gorev talebi iptal edildi: {GorevId}, neden: {Neden}.", id, neden);

        await _bildirimYayinlayici.YayinlaAsync(
            new GorevBildirimi(gorev.Id, gorev.Baslik, gorev.Durum.ToString(), DateTime.UtcNow), iptalToken);
    }

    public async Task<GorevTalebiDetayYaniti> AtaAsync(Guid gorevId, Guid ekipId, Guid? sorumluPersonelId,
        Guid atayanPersonelId, CancellationToken iptalToken = default)
    {
        var gorev = await _dbContext.GorevTalepleri.FirstOrDefaultAsync(g => g.Id == gorevId, iptalToken)
            ?? throw new GorevTalebiBulunamadiException(gorevId);

        var ekip = await _dbContext.Ekipler.FirstOrDefaultAsync(e => e.Id == ekipId, iptalToken)
            ?? throw new EkipBulunamadiException(ekipId);

        Personel? sorumluPersonel = null;
        if (sorumluPersonelId.HasValue)
        {
            sorumluPersonel = await _dbContext.Personeller.FirstOrDefaultAsync(p => p.Id == sorumluPersonelId.Value, iptalToken)
                ?? throw new PersonelBulunamadiException(sorumluPersonelId.Value);
        }

        try
        {
            gorev.EkibeAta(ekip, atayanPersonelId, sorumluPersonel);
        }
        catch (InvalidOperationException)
        {
            throw new GorevDurumCakismasiException(gorev.Id, gorev.Durum.ToString());
        }

        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Gorev {GorevId} manuel/onayli olarak '{EkipAdi}' ekibine atandi (atayan: {AtayanPersonelId}).",
            gorevId, ekip.Ad, atayanPersonelId);

        await _bildirimYayinlayici.YayinlaAsync(
            new GorevBildirimi(gorev.Id, gorev.Baslik, gorev.Durum.ToString(), DateTime.UtcNow), iptalToken);

        return await DetayGetirAsync(gorev.Id, iptalToken);
    }

    public async Task<GorevTalebiDetayYaniti> YolaCikAsync(Guid gorevId, Guid personelId, CancellationToken iptalToken = default)
    {
        var gorev = await EkipUyeligiDogrulaVeGorevGetirAsync(gorevId, personelId, iptalToken);

        try
        {
            gorev.YolaCik(personelId);
        }
        catch (InvalidOperationException)
        {
            throw new GorevDurumCakismasiException(gorev.Id, gorev.Durum.ToString());
        }

        await _dbContext.SaveChangesAsync(iptalToken);
        await _bildirimYayinlayici.YayinlaAsync(
            new GorevBildirimi(gorev.Id, gorev.Baslik, gorev.Durum.ToString(), DateTime.UtcNow), iptalToken);

        return await DetayGetirAsync(gorev.Id, iptalToken);
    }

    public async Task<GorevTalebiDetayYaniti> BaslatAsync(Guid gorevId, Guid personelId, CancellationToken iptalToken = default)
    {
        var gorev = await EkipUyeligiDogrulaVeGorevGetirAsync(gorevId, personelId, iptalToken);

        try
        {
            gorev.Baslat(personelId);
        }
        catch (InvalidOperationException)
        {
            throw new GorevDurumCakismasiException(gorev.Id, gorev.Durum.ToString());
        }

        await _dbContext.SaveChangesAsync(iptalToken);
        await _bildirimYayinlayici.YayinlaAsync(
            new GorevBildirimi(gorev.Id, gorev.Baslik, gorev.Durum.ToString(), DateTime.UtcNow), iptalToken);

        return await DetayGetirAsync(gorev.Id, iptalToken);
    }

    public async Task<GorevFotografiYaniti> FotografEkleAsync(Guid gorevId, Guid personelId, Stream fotografIcerigi,
        string dosyaUzantisi, GorevFotografAsamasi asama, double enlem, double boylam, DateTime cekilmeZamaniUtc,
        CancellationToken iptalToken = default)
    {
        var gorev = await EkipUyeligiDogrulaVeGorevGetirAsync(gorevId, personelId, iptalToken);

        // Once dosyayi kaydet, sonra domain kuralini uygula: boylece dosya kaydetme basarisiz
        // olursa yarim kalmis (dosyasiz) bir GorevFotografi kaydi asla olusmaz.
        var depolamaAnahtari = await _fotografDepolamaServisi.KaydetAsync(fotografIcerigi, dosyaUzantisi, iptalToken);
        var konum = KonumFabrikasi.NoktaOlustur(enlem, boylam);

        var fotograf = gorev.FotografEkle(depolamaAnahtari, asama, konum, cekilmeZamaniUtc, personelId);
        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Gorev icin kanit fotografi eklendi: {GorevId}, fotografId: {FotografId}.",
            gorevId, fotograf.Id);

        return new GorevFotografiYaniti(fotograf.Id, fotograf.DosyaYolu, fotograf.Asama.ToString(), fotograf.CekilmeZamaniUtc);
    }

    public async Task<GorevIstatistikleriYaniti> IstatistikleriGetirAsync(int gunSayisi, CancellationToken iptalToken = default)
    {
        if (gunSayisi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gunSayisi), gunSayisi, "Gun sayisi pozitif olmalidir.");
        }

        var baslangicZamani = DateTime.UtcNow.AddDays(-gunSayisi);
        var suAn = DateTime.UtcNow;

        var acikDurumlar = new[]
        {
            GorevDurumu.Acildi, GorevDurumu.Atanamadi, GorevDurumu.Atandi, GorevDurumu.YolaCikildi, GorevDurumu.Baslandi,
        };

        var acikGorevSayisi = await _dbContext.GorevTalepleri.CountAsync(g => acikDurumlar.Contains(g.Durum), iptalToken);

        var slaIhlalSayisi = await _dbContext.GorevTalepleri.CountAsync(
            g => acikDurumlar.Contains(g.Durum) && g.SlaHedefZamaniUtc < suAn, iptalToken);

        var pencereIcindeOlusturulan = await _dbContext.GorevTalepleri
            .CountAsync(g => g.OlusturulmaZamaniUtc >= baslangicZamani, iptalToken);

        // Ortalama cozum suresi hesabi (Tamamlanma - Olusturulma) icin ilgili iki zaman
        // damgasi cekilir; kucuk bir veri kumesi oldugundan bellekte Average almak sorun
        // degildir (buyuk veri hacminde bu, DB tarafinda AVG(EXTRACT(EPOCH FROM ...)) ile
        // optimize edilebilir).
        var tamamlananZamanCiftleri = await _dbContext.GorevTalepleri
            .Where(g => g.TamamlanmaZamaniUtc != null && g.TamamlanmaZamaniUtc >= baslangicZamani)
            .Select(g => new { g.OlusturulmaZamaniUtc, g.TamamlanmaZamaniUtc })
            .ToListAsync(iptalToken);

        double? ortalamaCozumSuresiDakika = tamamlananZamanCiftleri.Count > 0
            ? tamamlananZamanCiftleri.Average(g => (g.TamamlanmaZamaniUtc!.Value - g.OlusturulmaZamaniUtc).TotalMinutes)
            : null;

        var slaIhlalOrani = acikGorevSayisi > 0 ? (double)slaIhlalSayisi / acikGorevSayisi : 0;

        return new GorevIstatistikleriYaniti(
            acikGorevSayisi,
            slaIhlalSayisi,
            Math.Round(slaIhlalOrani, 4),
            ortalamaCozumSuresiDakika.HasValue ? Math.Round(ortalamaCozumSuresiDakika.Value, 1) : null,
            pencereIcindeOlusturulan,
            tamamlananZamanCiftleri.Count,
            gunSayisi);
    }

    public async Task<GorevTalebiDetayYaniti> DogrulaAsync(Guid gorevId, Guid dogrulayanPersonelId,
        CancellationToken iptalToken = default)
    {
        var gorev = await _dbContext.GorevTalepleri.FirstOrDefaultAsync(g => g.Id == gorevId, iptalToken)
            ?? throw new GorevTalebiBulunamadiException(gorevId);

        try
        {
            gorev.Dogrula(dogrulayanPersonelId);
        }
        catch (InvalidOperationException)
        {
            throw new GorevDurumCakismasiException(gorev.Id, gorev.Durum.ToString());
        }

        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Gorev dogrulandi: {GorevId}.", gorevId);

        await _bildirimYayinlayici.YayinlaAsync(
            new GorevBildirimi(gorev.Id, gorev.Baslik, gorev.Durum.ToString(), DateTime.UtcNow), iptalToken);

        // SG-402: is nihai olarak dogrulandiginda (Tamamlandi asamasinda DEGIL, cunku o asama
        // Amir tarafindan sahaya geri gonderilebilir) vatandasa SMS bildirimi gonderilir.
        if (!string.IsNullOrWhiteSpace(gorev.BildirenTelefonNumarasi))
        {
            var mesaj = $"Sayin vatandasimiz, \"{gorev.Baslik}\" basligiyla bildirdiginiz talebiniz tamamlanmistir. " +
                "Ilginiz icin tesekkur ederiz. - SahaGor";

            // SMS gonderimindeki bir hata, gorevin dogrulanmasini geri almamali (is fiilen
            // tamamlandi); bu yuzden hata burada yutulup loglanir, exception fırlatılmaz.
            try
            {
                await _smsGonderimServisi.GonderAsync(gorev.BildirenTelefonNumarasi, mesaj, iptalToken);
            }
            catch (Exception hata)
            {
                _logger.LogWarning(hata, "Gorev {GorevId} dogrulandi ancak SMS bildirimi gonderilemedi.", gorevId);
            }
        }

        return await DetayGetirAsync(gorev.Id, iptalToken);
    }

    public async Task<GorevTalebiDetayYaniti> SahayaGeriGonderAsync(Guid gorevId, Guid amirId, string neden,
        CancellationToken iptalToken = default)
    {
        var gorev = await _dbContext.GorevTalepleri.FirstOrDefaultAsync(g => g.Id == gorevId, iptalToken)
            ?? throw new GorevTalebiBulunamadiException(gorevId);

        try
        {
            gorev.SahayaGeriGonder(amirId, neden);
        }
        catch (InvalidOperationException)
        {
            throw new GorevDurumCakismasiException(gorev.Id, gorev.Durum.ToString());
        }

        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Gorev sahaya geri gonderildi: {GorevId}, neden: {Neden}.", gorevId, neden);

        await _bildirimYayinlayici.YayinlaAsync(
            new GorevBildirimi(gorev.Id, gorev.Baslik, gorev.Durum.ToString(), DateTime.UtcNow), iptalToken);

        return await DetayGetirAsync(gorev.Id, iptalToken);
    }

    /// <summary>
    /// Bir personelin, sadece kendi ekibine atanmis gorevler uzerinde durum degistirebilmesini
    /// garanti eder; aksi halde baska bir ekibin gorevini "yola ciktim" diye isaretleyebilirdi.
    /// </summary>
    private async Task<GorevTalebi> EkipUyeligiDogrulaVeGorevGetirAsync(Guid gorevId, Guid personelId, CancellationToken iptalToken)
    {
        var gorev = await _dbContext.GorevTalepleri.FirstOrDefaultAsync(g => g.Id == gorevId, iptalToken)
            ?? throw new GorevTalebiBulunamadiException(gorevId);

        var personel = await _dbContext.Personeller.FirstOrDefaultAsync(p => p.Id == personelId, iptalToken)
            ?? throw new PersonelBulunamadiException(personelId);

        if (gorev.AtananEkipId is null || personel.EkipId != gorev.AtananEkipId)
        {
            throw new GorevErisimYetkisiYokException(gorevId);
        }

        return gorev;
    }
}
