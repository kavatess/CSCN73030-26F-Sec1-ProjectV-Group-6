import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import fs from 'node:fs';
import path from 'node:path';
import childProcess from 'node:child_process';
import { env, platform } from 'node:process';

// Visual Studio uses HTTPS for its local development proxy. Builds, tests, and
// standalone Vite development do not need a .NET development certificate.
export default defineConfig(({ command }) => {
  const useHttps = command === 'serve' && (platform === 'win32' || Boolean(env.ASPNETCORE_HTTPS_PORT));
  let https;
  if (useHttps) {
    const folder = env.APPDATA
      ? path.join(env.APPDATA, 'ASP.NET', 'https')
      : path.join(env.HOME, '.aspnet', 'https');
    const name = 'cscn73030-26f-sec1-projectv-group-6.client';
    const cert = path.join(folder, name + '.pem');
    const key = path.join(folder, name + '.key');
    fs.mkdirSync(folder, { recursive: true });
    if (!fs.existsSync(cert) || !fs.existsSync(key)) {
      const result = childProcess.spawnSync('dotnet', ['dev-certs','https','--export-path',cert,'--format','Pem','--no-password'], { stdio:'inherit' });
      if (result.status !== 0) throw new Error('Could not create certificate.');
    }
    https = { key:fs.readFileSync(key), cert:fs.readFileSync(cert) };
  }
  const target = env.ASPNETCORE_HTTPS_PORT
    ? 'https://localhost:' + env.ASPNETCORE_HTTPS_PORT
    : env.ASPNETCORE_URLS?.split(';')[0] || 'https://localhost:7038';
  return {
    plugins: [react()],
    resolve: { alias: { '@':fileURLToPath(new URL('./src', import.meta.url)) } },
    server: { proxy: { '^/weatherforecast': { target, secure:false } }, port:Number(env.DEV_SERVER_PORT || 54552), ...(https ? { https } : {}) },
  };
});
