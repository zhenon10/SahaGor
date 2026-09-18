namespace SahaGor.Domain.Enumlar;

/// <summary>Bir kanit fotografinin is akisinin hangi asamasinda cekildigini belirtir.</summary>
public enum GorevFotografAsamasi
{
    /// <summary>Isleme baslamadan once mevcut durumun tespiti (oncesi).</summary>
    Once = 1,

    /// <summary>Islem sirasinda cekilen ilerleme fotografi.</summary>
    Sirasinda = 2,

    /// <summary>Is tamamlandiktan sonra sonucu gosteren kanit fotografi.</summary>
    Sonra = 3
}
