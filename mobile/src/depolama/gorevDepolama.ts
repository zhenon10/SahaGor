import type { SQLiteDatabase } from "expo-sqlite";
import type { GorevOzeti } from "../tipler/gorev";

/** SQLite satirinin ham (INTEGER bool iceren) hali. */
interface GorevSatiri {
  id: string;
  baslik: string;
  kategoriAdi: string;
  durum: string;
  oncelik: string;
  enlem: number;
  boylam: number;
  bolgeAdi: string | null;
  atananEkipAdi: string | null;
  olusturulmaZamaniUtc: string;
  slaHedefZamaniUtc: string;
  slaIhlalEdildiMi: number;
}

function satiridanGorevOlustur(satir: GorevSatiri): GorevOzeti {
  return { ...satir, slaIhlalEdildiMi: satir.slaIhlalEdildiMi === 1 };
}

export const GorevDepolama = {
  /**
   * Yerel onbellegi sunucudan gelen en guncel liste ile tamamen degistirir
   * (delta senkronizasyon yerine basit "tam yenileme" stratejisi - Sprint 2'nin
   * bu asamasi icin yeterli; kismi/artimli senkron ileride gerekirse eklenir).
   */
  async hepsiniDegistir(db: SQLiteDatabase, gorevler: GorevOzeti[]): Promise<void> {
    await db.runAsync("DELETE FROM gorevler");

    for (const gorev of gorevler) {
      await db.runAsync(
        `INSERT INTO gorevler
          (id, baslik, kategoriAdi, durum, oncelik, enlem, boylam, bolgeAdi, atananEkipAdi,
           olusturulmaZamaniUtc, slaHedefZamaniUtc, slaIhlalEdildiMi)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
        [
          gorev.id,
          gorev.baslik,
          gorev.kategoriAdi,
          gorev.durum,
          gorev.oncelik,
          gorev.enlem,
          gorev.boylam,
          gorev.bolgeAdi,
          gorev.atananEkipAdi,
          gorev.olusturulmaZamaniUtc,
          gorev.slaHedefZamaniUtc,
          gorev.slaIhlalEdildiMi ? 1 : 0,
        ],
      );
    }
  },

  async hepsiniGetir(db: SQLiteDatabase): Promise<GorevOzeti[]> {
    const satirlar = await db.getAllAsync<GorevSatiri>(
      "SELECT * FROM gorevler ORDER BY olusturulmaZamaniUtc DESC",
    );
    return satirlar.map(satiridanGorevOlustur);
  },

  async idIleGetir(db: SQLiteDatabase, gorevId: string): Promise<GorevOzeti | null> {
    const satir = await db.getFirstAsync<GorevSatiri>("SELECT * FROM gorevler WHERE id = ?", [gorevId]);
    return satir ? satiridanGorevOlustur(satir) : null;
  },

  /**
   * Tek bir gorevin durumunu yerel onbellekte gunceller. Iki senaryoda kullanilir:
   * (1) SG-202 - kullanici offline'ken bir islem yaptiginda anlik (optimistic) yansitma,
   * (2) SG-203 - senkron sirasinda sunucudan donen kesin/cakisma durumunun yazilmasi.
   */
  async durumGuncelle(db: SQLiteDatabase, gorevId: string, yeniDurum: string): Promise<void> {
    await db.runAsync("UPDATE gorevler SET durum = ? WHERE id = ?", [yeniDurum, gorevId]);
  },
};
