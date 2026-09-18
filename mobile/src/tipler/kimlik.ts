/**
 * Backend'deki SahaGor.Application.Kimlik.Dtolar.GirisIstegi/GirisYaniti ile
 * bire bir eslesir. Alan adlari kasitli olarak backend ile ayni (Turkce) tutuldu;
 * boylece API sozlesmesi ile istemci tipleri arasinda zihinsel ceviri gerekmez.
 */

export interface GirisIstegi {
  kullaniciAdi: string;
  sifre: string;
}

export interface GirisYaniti {
  erisimTokeni: string;
  erisimTokeniSonKullanmaZamaniUtc: string;
  yenilemeTokeni: string;
  yenilemeTokeniSonKullanmaZamaniUtc: string;
  personelId: string;
  adSoyad: string;
  rol: "Operator" | "SahaPersoneli" | "Amir" | "SistemYoneticisi";
}

export interface YenilemeIstegi {
  yenilemeTokeni: string;
}
