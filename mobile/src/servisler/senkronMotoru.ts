import type { SQLiteDatabase } from "expo-sqlite";
import { GorevlerApi } from "../api/gorevlerApi";
import { GorevDepolama } from "../depolama/gorevDepolama";
import { SenkronKuyruguDepolama } from "../depolama/senkronKuyruguDepolama";
import { ApiHatasi } from "../tipler/api";
import type { GorevIslemTuru, SenkronKuyrukOgesi } from "../tipler/senkron";

function islemiCalistir(oge: SenkronKuyrukOgesi) {
  switch (oge.islemTuru) {
    case "YOLA_CIK":
      return GorevlerApi.yolaCik(oge.gorevId);
    case "BASLA":
      return GorevlerApi.basla(oge.gorevId);
  }
}

/**
 * Senkron kuyrugundaki bekleyen islemleri, eklenme sirasiyla (FIFO) tek tek sunucuya
 * gonderir (SG-203). Bir istek AG HATASI ile basarisiz olursa (offline), sirayi
 * bozmamak icin isleme burada durdurulur; kalan ogeler bir sonraki cagrida denenir.
 * Sunucu 409 (cakisma) donerse, o oge cozulmus sayilir: yerel durum sunucununkiyle
 * degistirilir ve kuyruktan atilir (tekrar denemek anlamsizdir, gecis artik gecersizdir).
 */
export async function kuyruguIsle(db: SQLiteDatabase): Promise<void> {
  const ogeler = await SenkronKuyruguDepolama.hepsiniGetir(db);

  for (const oge of ogeler) {
    try {
      const sonuc = await islemiCalistir(oge);
      await GorevDepolama.durumGuncelle(db, oge.gorevId, sonuc.durum);
      await SenkronKuyruguDepolama.sil(db, oge.id);
    } catch (hata) {
      if (hata instanceof ApiHatasi && hata.durumKodu === 409) {
        const mevcutDurum = hata.detaylar?.mevcutDurum;
        if (typeof mevcutDurum === "string") {
          await GorevDepolama.durumGuncelle(db, oge.gorevId, mevcutDurum);
        }
        await SenkronKuyruguDepolama.sil(db, oge.id);
        continue;
      }

      const mesaj = hata instanceof Error ? hata.message : "Bilinmeyen bir senkron hatasi olustu.";
      await SenkronKuyruguDepolama.denemeBasarisizOlarakIsaretle(db, oge.id, mesaj);
      break;
    }
  }
}

/**
 * Bir durum degisikligini hem aninda yerel onbellege yansitir (optimistic UI, SG-220)
 * hem de senkron kuyruguna ekler; ardindan kuyugu hemen islemeyi dener (cihaz online ise
 * kullanici bekleme hissetmeden degisiklik sunucuya gitmis olur).
 */
export async function gorevIslemiBaslat(db: SQLiteDatabase, gorevId: string, islemTuru: GorevIslemTuru): Promise<void> {
  const iyimserDurum: Record<GorevIslemTuru, string> = {
    YOLA_CIK: "YolaCikildi",
    BASLA: "Baslandi",
  };

  await GorevDepolama.durumGuncelle(db, gorevId, iyimserDurum[islemTuru]);
  await SenkronKuyruguDepolama.ekle(db, gorevId, islemTuru);
  await kuyruguIsle(db);
}
