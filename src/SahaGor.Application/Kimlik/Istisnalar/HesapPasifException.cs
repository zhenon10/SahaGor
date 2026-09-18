namespace SahaGor.Application.Kimlik.Istisnalar;

/// <summary>Pasif duruma alinmis (isten ayrilmis/askiya alinmis) bir personelin giris denemesinde firlatilir.</summary>
public sealed class HesapPasifException : Exception
{
    public HesapPasifException() : base("Bu kullanici hesabi pasif duruma alinmis. Yoneticinizle iletisime gecin.")
    {
    }
}
