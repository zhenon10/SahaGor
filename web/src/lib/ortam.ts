const apiTabanAdresi = process.env.NEXT_PUBLIC_API_BASE_URL;

if (!apiTabanAdresi) {
  throw new Error(
    "NEXT_PUBLIC_API_BASE_URL tanimli degil. Proje kokunde '.env.local' dosyasi olusturup " +
      "bu degeri tanimlayin (bkz. .env.local.example).",
  );
}

export const Ortam = {
  apiTabanAdresi,
} as const;
