#!/usr/bin/env node
/**
 * Gera ícones e splash para Electron + WPF a partir dos SVG oficiais MUFUTU.
 * Fonte: apps/electron/assets/svg/ (alinhado com MAUI splash v1.0.11)
 */
import { execSync } from 'child_process';
import { copyFileSync, existsSync, mkdirSync, rmSync } from 'fs';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, '..');
const svgDir = join(root, 'apps/electron/assets/svg');
const electronAssets = join(root, 'apps/electron/assets');
const winAssets = join(root, 'apps/desktop-win/src/Mufutu.Desktop/Assets/brand');

const appIconSvg = join(svgDir, 'app-icon.svg');
const splashMarkSvg = join(svgDir, 'splash-mark.svg');

function has(cmd) {
  try {
    execSync(`which ${cmd}`, { stdio: 'ignore' });
    return true;
  } catch {
    return false;
  }
}

function renderSvg(input, output, size) {
  if (!has('rsvg-convert')) {
    throw new Error('rsvg-convert não encontrado — instale librsvg');
  }
  execSync(`rsvg-convert -w ${size} -h ${size} "${input}" -o "${output}"`);
}

function writeIcoFromPng(pngPath, icoPath) {
  if (process.platform === 'darwin') {
    console.log('ℹ️  Skipping .ico on macOS — Electron mac usa icon.icns');
    return;
  }
  if (!has('python3')) {
    throw new Error('python3 não encontrado — necessário para gerar .ico');
  }
  try {
    execSync('python3 -c "import PIL"', { stdio: 'ignore' });
  } catch {
    execSync('python3 -m pip install --user pillow --break-system-packages', { stdio: 'inherit' });
  }
  execSync(
    `python3 - <<'PY'\nfrom PIL import Image\nimg = Image.open("${pngPath}").convert("RGBA")\nsizes = [(256,256),(128,128),(64,64),(48,48),(32,32),(16,16)]\nimg.save("${icoPath}", format="ICO", sizes=[(s[0], s[1]) for s in sizes])\nPY`,
    { stdio: 'inherit', shell: '/bin/bash' },
  );
}

function buildIcns(png512, icnsPath) {
  if (process.platform !== 'darwin' || !has('iconutil')) return;
  const iconset = join(electronAssets, 'icon.iconset');
  rmSync(iconset, { recursive: true, force: true });
  mkdirSync(iconset, { recursive: true });
  const sizes = [16, 32, 64, 128, 256, 512];
  for (const s of sizes) {
    renderSvg(appIconSvg, join(iconset, `icon_${s}x${s}.png`), s);
    if (s <= 256) {
      renderSvg(appIconSvg, join(iconset, `icon_${s}x${s}@2x.png`), s * 2);
    }
  }
  execSync(`iconutil -c icns "${iconset}" -o "${icnsPath}"`);
  rmSync(iconset, { recursive: true, force: true });
}

if (!existsSync(appIconSvg) || !existsSync(splashMarkSvg)) {
  console.error('❌ SVG fonte em falta em apps/electron/assets/svg/');
  process.exit(1);
}

mkdirSync(electronAssets, { recursive: true });
mkdirSync(winAssets, { recursive: true });

const iconPng = join(electronAssets, 'icon.png');
const splashLogo = join(electronAssets, 'splash-logo.png');
const splashFull = join(electronAssets, 'splash-full.png');

renderSvg(appIconSvg, iconPng, 512);
renderSvg(splashMarkSvg, splashLogo, 256);
renderSvg(appIconSvg, splashFull, 512);
renderSvg(appIconSvg, join(winAssets, 'splash.png'), 512);
renderSvg(splashMarkSvg, join(winAssets, 'logo-white.png'), 256);

buildIcns(iconPng, join(electronAssets, 'icon.icns'));
writeIcoFromPng(iconPng, join(electronAssets, 'icon.ico'));
if (existsSync(join(electronAssets, 'icon.ico'))) {
  copyFileSync(join(electronAssets, 'icon.ico'), join(winAssets, 'app-icon.ico'));
}

console.log('✅ Desktop brand assets gerados');
console.log('   Electron → apps/electron/assets/{icon.png,icon.ico,icon.icns,splash-logo.png}');
console.log('   WPF      → apps/desktop-win/src/Mufutu.Desktop/Assets/brand/');
