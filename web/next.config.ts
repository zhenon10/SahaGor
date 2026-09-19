import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Production Docker imajinin sadece gerekli dosyalari (node_modules'un tamamini degil)
  // icermesi icin (bkz. web/Dockerfile, SG-422).
  output: "standalone",

  // SG-423 (OWASP A05 - Security Misconfiguration): Content-Security-Policy bilerek
  // eklenmedi - Tailwind/Leaflet/SignalR ile dogru bir CSP kurmak dikkatli nonce/hash
  // yonetimi gerektirir ve canli tarayici testi olmadan yanlislikla uygulamayi
  // kirabilir; ileride ayri bir is olarak ele alinmalidir.
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-Frame-Options", value: "DENY" },
          { key: "Referrer-Policy", value: "no-referrer" },
        ],
      },
    ];
  },
};

export default nextConfig;
