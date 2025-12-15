import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
    // Ігноруємо зібраний код і залежності
    globalIgnores(['dist', 'node_modules']),

    {
        files: ['src/**/*.{ts,tsx}'],

        languageOptions: {
            ecmaVersion: 2020,
            sourceType: 'module',
            globals: globals.browser,
            parserOptions: {

                project: [
                    './tsconfig.node.json',
                    './src/tsconfig.json'
                ],

                tsconfigRootDir: import.meta.dirname,
            },
        },

        plugins: {
            'react-hooks': reactHooks,
            'react-refresh': reactRefresh,
        },

        extends: [
            js.configs.recommended,

            ...tseslint.configs.recommendedTypeChecked,

            // ...tseslint.configs.strictTypeChecked,
            // ...tseslint.configs.stylisticTypeChecked,
        ],

        rules: {
            'no-undef': 'off',

            'no-unused-vars': 'off',
            '@typescript-eslint/no-unused-vars': [
                'warn',
                {
                    argsIgnorePattern: '^_',
                    varsIgnorePattern: '^_',
                },
            ],

            'react-hooks/rules-of-hooks': 'error',
            'react-hooks/exhaustive-deps': 'warn',

            'react/react-in-jsx-scope': 'off',
        },
    },
])
