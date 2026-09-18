import type { SQLiteDatabase } from "expo-sqlite";
import { GorevlerApi } from "../api/gorevlerApi";
import { GorevDepolama } from "../depolama/gorevDepolama";
import type { GorevOzeti } from "../tipler/gorev";

export interface GorevListesiSonucu {
  gorevler: GorevOzeti[];
  /** true: liste sunucudan taze geldi. false: ag hatasi nedeniyle yerel onbellekten okundu (SG-201). */
  kaynakCevrimici: boolean;
}

/**
 * Offline-first okuma stratejisi: once sunucudan guncel listeyi cekmeyi dener ve basarili
 * olursa yerel onbellegi gunceller; herhangi bir ag/sunucu hatasinda (SG-201 kabul kriteri)
 * sessizce yerel SQLite onbellegine duser. Boylece saha personeli, internet olmasa dahi
 * en son bilinen atanmis gorev listesini gorebilir.
 */
export async function atanmisGorevleriYukle(db: SQLiteDatabase, ekipId: string): Promise<GorevListesiSonucu> {
  try {
    const sonuc = await GorevlerApi.listele({ atananEkipId: ekipId, sayfaBoyutu: 100 });
    await GorevDepolama.hepsiniDegistir(db, sonuc.kayitlar);
    return { gorevler: sonuc.kayitlar, kaynakCevrimici: true };
  } catch {
    const yerelGorevler = await GorevDepolama.hepsiniGetir(db);
    return { gorevler: yerelGorevler, kaynakCevrimici: false };
  }
}
