namespace SahaGor.Domain.Enumlar;

/// <summary>
/// Kurum bunyesindeki (belediye) personelin sistem uzerindeki rolu.
/// Vatandas bir Personel kaydi degildir; GorevTalebi uzerinde iletisim bilgisi olarak tutulur.
/// </summary>
public enum PersonelRolu
{
    /// <summary>153/CIMER veya telefon basvurularini sisteme giren, gorev acan personel.</summary>
    Operator = 1,

    /// <summary>Sahada fiilen isi yapan, mobil uygulamayi kullanan personel.</summary>
    SahaPersoneli = 2,

    /// <summary>Ekipleri yoneten, atama onaylayan, komuta panelini kullanan sorumlu.</summary>
    Amir = 3,

    /// <summary>Kurum, birim, kullanici ve sistem parametrelerini yoneten teknik yetkili.</summary>
    SistemYoneticisi = 4
}
