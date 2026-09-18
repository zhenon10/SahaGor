import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Production Docker imajinin sadece gerekli dosyalari (node_modules'un tamamini degil)
  // icermesi icin (bkz. web/Dockerfile, SG-422).
  output: "standalone",
};

export default nextConfig;
