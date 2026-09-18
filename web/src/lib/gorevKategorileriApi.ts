import { ApiIstemcisi } from "./apiIstemcisi";
import type { GorevKategorisiYaniti } from "@/tipler/gorevKategorisi";

export const GorevKategorileriApi = {
  listele: () => ApiIstemcisi.get<GorevKategorisiYaniti[]>("/api/gorev-kategorileri"),
};
