namespace SahaGor.Application.Kimlik.Istisnalar;

/// <summary>Art arda basarisiz giris denemesi sonucu gecici olarak kilitlenmis hesaba giris denendiginde firlatilir.</summary>
public sealed class HesapKilitliException : Exception
{
    public DateTime KilitAcilmaZamaniUtc { get; }

    public HesapKilitliException(DateTime kilitAcilmaZamaniUtc)
        : base($"Hesap cok sayida basarisiz giris denemesi nedeniyle kilitlendi. " +
               $"{kilitAcilmaZamaniUtc:HH:mm} (UTC) itibariyla tekrar deneyebilirsiniz.")
    {
        KilitAcilmaZamaniUtc = kilitAcilmaZamaniUtc;
    }
}
