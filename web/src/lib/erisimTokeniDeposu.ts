/**
 * Erisim tokenini React state'i DISINDA, sade bir modul-seviyesi degiskende tutar.
 * Boylece apiIstemcisi.ts gibi React bilesen agacinin disindaki kod (fetch cagrilari,
 * ileride SignalR baglanti kurulumu) tokene React context'e bagli olmadan erisebilir.
 * Kaynagi hala KimlikBaglami'dir; bu sadece onun "en guncel deger" onbellegidir.
 */
let mevcutErisimTokeni: string | null = null;

export const ErisimTokeniDeposu = {
  ayarla(token: string | null): void {
    mevcutErisimTokeni = token;
  },

  getir(): string | null {
    return mevcutErisimTokeni;
  },
};
