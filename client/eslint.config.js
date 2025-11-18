import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
    // Ignore build output and dependencies
    globalIgnores(['dist', 'node_modules']),

    {
        files: ['src/**/*.{ts,tsx}'],

        languageOptions: {
            ecmaVersion: 2020,
            sourceType: 'module',
            globals: globals.browser,
        },

        // Plugins must be OBJECTS in flat config
        plugins: {
            'react-hooks': reactHooks,
            'react-refresh': reactRefresh,
        },

        // Base recommended configs for JS + TypeScript
        extends: [
            js.configs.recommended,
            ...tseslint.configs.recommended,
            // IMPORTANT: do NOT include reactHooks.configs[...] or reactRefresh.configs.vite here
        ],

        rules: {
            // Let TypeScript handle undefined variables
            'no-undef': 'off',

            // Disable base rule and use TS-aware version
            'no-unused-vars': 'off',
            '@typescript-eslint/no-unused-vars': [
                'warn',
                {
                    argsIgnorePattern: '^_', // ignore arguments starting with "_"
                    varsIgnorePattern: '^_', // ignore variables starting with "_"
                },
            ],

            // React hooks rules
            'react-hooks/rules-of-hooks': 'error',
            'react-hooks/exhaustive-deps': 'warn',

            // Not required in modern React (React 17+ / Vite)
            'react/react-in-jsx-scope': 'off',
        },
    },
])
