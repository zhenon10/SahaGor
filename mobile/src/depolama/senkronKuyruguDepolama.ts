import type { SQLiteDatabase } from "expo-sqlite";
import type { GorevIslemTuru, SenkronKuyrukOgesi } from "../tipler/senkron";

export const SenkronKuyruguDepolama = {
  async ekle(db: SQLiteDatabase, gorevId: string, islemTuru: GorevIslemTuru): Promise<void> {
    await db.runAsync(
      "INSERT INTO senkron_kuyrugu (gorevId, islemTuru, olusturulmaZamaniUtc) VALUES (?, ?, ?)",
      [gorevId, islemTuru, new Date().toISOString()],
    );
  },

  /** Kayit sirasi (id ASC), ekleme sirasiyla ayni oldugundan FIFO isleme garantisi verir. */
  async hepsiniGetir(db: SQLiteDatabase): Promise<SenkronKuyrukOgesi[]> {
    return db.getAllAsync<SenkronKuyrukOgesi>("SELECT * FROM senkron_kuyrugu ORDER BY id ASC");
  },

  async sil(db: SQLiteDatabase, id: number): Promise<void> {
    await db.runAsync("DELETE FROM senkron_kuyrugu WHERE id = ?", [id]);
  },

  async denemeBasarisizOlarakIsaretle(db: SQLiteDatabase, id: number, hataMesaji: string): Promise<void> {
    await db.runAsync(
      "UPDATE senkron_kuyrugu SET denemeSayisi = denemeSayisi + 1, sonHataMesaji = ? WHERE id = ?",
      [hataMesaji, id],
    );
  },
};
