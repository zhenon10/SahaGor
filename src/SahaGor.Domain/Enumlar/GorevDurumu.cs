namespace SahaGor.Domain.Enumlar;

/// <summary>
/// Bir GorevTalebi'nin yasam dongusu boyunca gecebilecegi durumlar.
/// Gecerli gecisler GorevTalebi entity'si icinde bir durum makinesi (state machine) ile denetlenir.
/// </summary>
public enum GorevDurumu
{
    /// <summary>Gorev sisteme yeni girildi, henuz bir ekibe atanmadi.</summary>
    Acildi = 1,

    /// <summary>Otomatik atama algoritmasi uygun ekip bulamadi; Amir mudahalesi bekleniyor.</summary>
    Atanamadi = 2,

    /// <summary>Gorev bir ekibe/personele atandi.</summary>
    Atandi = 3,

    /// <summary>Personel gorev konumuna dogru yola cikti.</summary>
    YolaCikildi = 4,

    /// <summary>Personel sahada isleme basladi.</summary>
    Baslandi = 5,

    /// <summary>Personel isi sahada tamamladi, kanit fotograflari yuklendi.</summary>
    Tamamlandi = 6,

    /// <summary>Amir, tamamlanan isi inceleyip onayladi. Nihai/kapali durum.</summary>
    Dogrulandi = 7,

    /// <summary>Gorev iptal edildi (mukerrer kayit, yanlis ihbar vb.). Nihai/kapali durum.</summary>
    Iptal = 8
}
