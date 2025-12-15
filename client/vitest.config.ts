/// <reference types="vitest" />

import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import * as path from "node:path";

export default defineConfig({
    plugins: [react()],
    test: {
        environment: "happy-dom",
        globals: true,
        coverage: {
            provider: "v8",
            reporter: ["text", "json-summary", "json"],
            reportOnFailure: true,
        },
    },
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
