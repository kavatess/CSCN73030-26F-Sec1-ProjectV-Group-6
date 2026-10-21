import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vitest/config';
import plugin from '@vitejs/plugin-react';

// Kept separate from vite.config.js, which runs `dotnet dev-certs` and reads the HTTPS
// certificate when it loads. Tests don't need either, and shouldn't fail without them.
export default defineConfig({
    plugins: [plugin()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    test: {
        environment: 'jsdom',
        setupFiles: ['./src/test/setup.js'],
        include: ['src/**/*.{test,spec}.{js,jsx}']
    }
})
