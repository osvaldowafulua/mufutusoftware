#!/usr/bin/env node
/**
 * Prepara electron-app/ a partir do Next.js standalone (repo mufutu CMMS).
 * MUFUTU_WEB_DIR — caminho para apps/web do monorepo mufutu
 */
import { cpSync, existsSync, mkdirSync, rmSync, readdirSync, statSync, writeFileSync } from 'fs';
import { dirname, join, relative, resolve, sep } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const electronRoot = resolve(__dirname, '..');
const webRoot = resolve(process.env.MUFUTU_WEB_DIR || join(electronRoot, '../../mufutu/apps/web'));
const outDir = join(electronRoot, 'electron-app');

const standaloneRoot = join(webRoot, '.next', 'standalone');
if (!existsSync(standaloneRoot)) {
  console.error(`❌ ${standaloneRoot} não encontrado. Corra ELECTRON_BUILD=1 next build em mufutu/apps/web`);
  process.exit(1);
}

function findServerJs(dir, depth = 0) {
  if (depth > 12) return null;
  const direct = join(dir, 'server.js');
  if (existsSync(direct) && !dir.includes(`${sep}node_modules${sep}`)) {
    if (dir.endsWith(`${sep}apps${sep}web`) || dir.endsWith('apps/web')) return direct;
  }
  for (const name of readdirSync(dir)) {
    if (name === 'node_modules') continue;
    const full = join(dir, name);
    if (statSync(full).isDirectory()) {
      const found = findServerJs(full, depth + 1);
      if (found) return found;
    }
  }
  return null;
}

function resolveServerJs(standaloneCopy) {
  for (const candidate of [join(standaloneCopy, 'apps', 'web', 'server.js'), join(standaloneCopy, 'server.js')]) {
    if (existsSync(candidate)) return candidate;
  }
  return findServerJs(standaloneCopy);
}

rmSync(outDir, { recursive: true, force: true });
mkdirSync(outDir, { recursive: true });
cpSync(standaloneRoot, join(outDir, 'standalone'), { recursive: true });

const standaloneCopy = join(outDir, 'standalone');
const serverJs = resolveServerJs(standaloneCopy);
if (!serverJs) {
  console.error('❌ server.js não encontrado no bundle standalone');
  process.exit(1);
}

const serverDir = dirname(serverJs);
const staticSrc = join(webRoot, '.next', 'static');
const publicSrc = join(webRoot, 'public');
const staticDest = join(serverDir, '.next', 'static');
const publicDest = join(serverDir, 'public');

mkdirSync(dirname(staticDest), { recursive: true });
if (existsSync(staticSrc)) cpSync(staticSrc, staticDest, { recursive: true });
if (existsSync(publicSrc)) cpSync(publicSrc, publicDest, { recursive: true });

for (const file of ['launch-server.js', 'electron-ipv4.js']) {
  const src = join(electronRoot, file);
  if (existsSync(src)) cpSync(src, join(outDir, file));
}

writeFileSync(
  join(outDir, 'server-manifest.json'),
  JSON.stringify(
    {
      serverJs: relative(outDir, serverJs).split(sep).join('/'),
      serverDir: relative(outDir, serverDir).split(sep).join('/'),
      launchServer: 'launch-server.js',
      port: 3847,
    },
    null,
    2,
  ),
);

console.log('✅ Electron bundle em', outDir);
