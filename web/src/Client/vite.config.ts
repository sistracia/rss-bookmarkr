import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

const proxyPort = process.env.SERVER_PROXY_PORT || "5050";
const proxyTarget = "http://localhost:" + proxyPort;

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  clearScreen: false,
  server: {
    port: 8080,
    watch: {
      ignored: [
        "**/*.fs", // Don't watch F# files
      ],
    },
    proxy: {
      // redirect requests that start with /rpc/ to the server on port 5000
      "/rpc/": {
        target: proxyTarget,
        changeOrigin: true,
      },
    },
  },
});
