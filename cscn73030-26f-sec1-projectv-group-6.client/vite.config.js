import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "cscn73030-26f-sec1-projectv-group-6.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7038';

// Top-level paths served by the Staff/HR API (see API.md).
const apiPrefixes = [
    'staff', 'departments', 'assignments', 'rosters', 'requisitions', 'credentials',
    'headcounts', 'audit', 'health', 'openapi', 'weatherforecast'
];

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        // Forward API calls to the ASP.NET server. A browser navigation (Accept: text/html) to a
        // path such as /staff is left to the React app, so client-side routes can share API paths.
        proxy: {
            [`^/(${apiPrefixes.join('|')})(/|\\?|$)`]: {
                target,
                secure: false,
                bypass: (req) => (req.headers.accept?.includes('text/html') ? '/index.html' : undefined)
            }
        },
        port: parseInt(env.DEV_SERVER_PORT || '54552'),
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    }
})
