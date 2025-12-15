import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "path";

export default defineConfig({
    plugins: [react(), tailwindcss()],
    resolve: {
        alias: {
            "@core": path.resolve(__dirname, "./src/core"),
            "@hooks": path.resolve(__dirname, "./src/utils/hooks"),
            "@pages": path.resolve(__dirname, "./src/ui/pages"),
            "@layouts": path.resolve(__dirname, "./src/ui/layouts"),
            "@app": path.resolve(__dirname, "./src/ui/app"),
        },
    },
});
