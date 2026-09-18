import type { SQLiteDatabase } from "expo-sqlite";
import type { YerelFotograf } from "../tipler/fotograf";

interface YerelFotografSatiri {
  id: number;
  gorevId: string;
  dosyaYolu: string;
  asama: string;
  enlem: number;
  boylam: number;
  cekilmeZamaniUtc: string;
  yuklendiMi: number;
}

function satiridanFotografOlustur(satir: YerelFotografSatiri): YerelFotograf {
  return { ...satir, asama: satir.asama as YerelFotograf["asama"], yuklendiMi: satir.yuklendiMi === 1 };
}

export const FotografDepolama = {
  async ekle(
    db: SQLiteDatabase,
    fotograf: Omit<YerelFotograf, "id" | "yuklendiMi">,
  ): Promise<void> {
    await db.runAsync(
      "INSERT INTO yerel_fotograflar (gorevId, dosyaYolu, asama, enlem, boylam, cekilmeZamaniUtc) VALUES (?, ?, ?, ?, ?, ?)",
      [fotograf.gorevId, fotograf.dosyaYolu, fotograf.asama, fotograf.enlem, fotograf.boylam, fotograf.cekilmeZamaniUtc],
    );
  },

  async gorevIcinGetir(db: SQLiteDatabase, gorevId: string): Promise<YerelFotograf[]> {
    const satirlar = await db.getAllAsync<YerelFotografSatiri>(
      "SELECT * FROM yerel_fotograflar WHERE gorevId = ? ORDER BY cekilmeZamaniUtc ASC",
      [gorevId],
    );
    return satirlar.map(satiridanFotografOlustur);
  },

  /** Henuz sunucuya yuklenmemis fotograflari, cekilme sirasina (FIFO) gore doner (SG-212). */
  async yuklenmemisleriGetir(db: SQLiteDatabase): Promise<YerelFotograf[]> {
    const satirlar = await db.getAllAsync<YerelFotografSatiri>(
      "SELECT * FROM yerel_fotograflar WHERE yuklendiMi = 0 ORDER BY id ASC",
    );
    return satirlar.map(satiridanFotografOlustur);
  },

  async yuklendiOlarakIsaretle(db: SQLiteDatabase, id: number): Promise<void> {
    await db.runAsync("UPDATE yerel_fotograflar SET yuklendiMi = 1 WHERE id = ?", [id]);
  },
};
