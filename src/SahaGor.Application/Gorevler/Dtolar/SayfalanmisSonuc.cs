namespace SahaGor.Application.Gorevler.Dtolar;

/// <summary>Herhangi bir liste uc noktasi icin genel sayfalama zarfi.</summary>
public sealed record SayfalanmisSonuc<T>(IReadOnlyList<T> Kayitlar, int ToplamKayitSayisi, int Sayfa, int SayfaBoyutu);
