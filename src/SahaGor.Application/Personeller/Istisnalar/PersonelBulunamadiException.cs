namespace SahaGor.Application.Personeller.Istisnalar;

public sealed class PersonelBulunamadiException : Exception
{
    public PersonelBulunamadiException(Guid id) : base($"'{id}' numarali personel bulunamadi.")
    {
    }
}
