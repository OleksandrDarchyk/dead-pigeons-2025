/// <reference types="vitest" />

import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import * as path from "node:path";

export default defineConfig({
    plugins: [react()],
    test: {
        // Use browser-like DOM implementation
        environment: "happy-dom",
        // Allow using describe/it/expect without imports
        globals: true,

        // Enable code coverage with V8 provider (@vitest/coverage-v8)
        coverage: {
            provider: "v8", // <-- required when using @vitest/coverage-v8
            // Reporters for local + CI usage
            reporter: ["text", "json-summary", "json"],
            // Generate coverage report even if some tests fail
            reportOnFailure: true,
        },
    },
    resolve: {
        alias: {
            "@core": path.resolve(__dirname, "./src/core"),
            "@utilities": path.resolve(__dirname, "./src/utilities"),
            "@components": path.resolve(__dirname, "./src/components"),
            "@atoms": path.resolve(__dirname, "./src/atoms"),
        },
    },
});
