import { ApiIstemcisi } from "./apiIstemcisi";
import type { BirimYaniti } from "@/tipler/birim";

export const BirimlerApi = {
  listele: () => ApiIstemcisi.get<BirimYaniti[]>("/api/birimler"),
};
