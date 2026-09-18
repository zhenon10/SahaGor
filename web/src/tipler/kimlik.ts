/**
 * Backend'deki SahaGor.Application.Kimlik.Dtolar.GirisIstegi/GirisYaniti ile bire bir
 * eslesir (bkz. SahaGor.Api projesi, AuthController).
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

/**
 * Web panelinin tarayici tarafina (React Context) tasidigi oturum bilgisi. Yenileme
 * tokeni bilerek burada YOK: o sadece httpOnly cookie icinde, sunucu tarafinda tutulur
 * (XSS ile calinabilecek bir JS degiskeninde asla yer almaz).
 */
export interface OturumBilgisi {
  erisimTokeni: string;
  erisimTokeniSonKullanmaZamaniUtc: string;
  personelId: string;
  adSoyad: string;
  rol: GirisYaniti["rol"];
}
