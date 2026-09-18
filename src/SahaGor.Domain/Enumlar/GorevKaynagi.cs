namespace SahaGor.Domain.Enumlar;

/// <summary>Bir GorevTalebi'nin sisteme hangi kanaldan girdigini belirtir (SG-401).</summary>
public enum GorevKaynagi
{
    /// <summary>Operator tarafindan komuta panelinden manuel giris.</summary>
    OperatorGirisi = 1,

    /// <summary>Saha personelinin mobil uygulama uzerinden kendi actigi gorev (ad-hoc tespit).</summary>
    MobilUygulama = 2,

    /// <summary>Vatandasin dogrudan basvurusu (web/mobil basvuru formu).</summary>
    VatandasBasvurusu = 3,

    /// <summary>153 Belediye Iletisim Merkezi hattindan otomatik entegrasyon.</summary>
    Hat153 = 4,

    /// <summary>CIMER uzerinden otomatik entegrasyon.</summary>
    Cimer = 5
}
