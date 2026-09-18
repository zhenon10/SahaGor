namespace SahaGor.Application.Gorevler;

/// <summary>
/// Kanit fotograflarinin ham dosya icerigini kalici depolamaya yazma/okuma islemini soyutlar.
/// Bu sayede GorevTalebiServisi, fotograflarin diskte mi, S3'te mi yoksa baska bir bulut
/// depolamada mi tutuldugunu bilmez (SG-212 - ileride bulut depolamaya gecis kolaylasir).
/// </summary>
public interface IFotografDepolamaServisi
{
    /// <summary>Icerigi kalici depolamaya yazar ve daha sonra okumak icin kullanilacak depolama anahtarini dondurur.</summary>
    Task<string> KaydetAsync(Stream icerik, string dosyaUzantisi, CancellationToken iptalToken = default);
}
