#!/usr/bin/env bash
# MUFUTU — build Windows: instalador NSIS (.exe) — instala em Program Files (não portátil)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
CMMS="$(cd "${MUFUTU_CMMS_DIR:-$ROOT/../mufutu}" 2>/dev/null && pwd || true)"
if [[ -z "$CMMS" || ! -d "$CMMS/apps/web" ]]; then
  CMMS="$(cd "$ROOT/../mufutu" && pwd)"
fi
WEB="$CMMS/apps/web"
ELECTRON="$ROOT/apps/electron"
OUT="$ROOT/apps/desktop-win/artifacts/installer"
LOG="$ROOT/apps/desktop-win/artifacts/logs"
VERSION="${1:-1.0.0}"
SKIP_OBFUSCATE="${SKIP_OBFUSCATE:-0}"
BACKUP="$ELECTRON/.electron-obfuscate-backup"

if [[ ! -d "$WEB" ]]; then
  echo "❌ CMMS não encontrado: $WEB" >&2
  echo "   Execute a partir de mufutusoftware:" >&2
  echo "   cd /Users/fluadigital/Documents/GitHub/mufutusoftware" >&2
  echo "   bash apps/desktop-mac/scripts/package-win.sh $VERSION" >&2
  echo "   Ou: MUFUTU_CMMS_DIR=/caminho/mufutu bash ..." >&2
  exit 1
fi

if [[ "$(uname -s)" != "MINGW"* && "$(uname -s)" != "MSYS"* && "$(uname -s)" != "CYGWIN"* && "$(uname -s)" != "Windows_NT" ]]; then
  echo "❌ O instalador NSIS (.exe) só pode ser gerado em Windows ou GitHub Actions." >&2
  echo "   Mac/Linux: use package.sh para DMG." >&2
  echo "   GitHub: Actions → Windows Electron Installer (NSIS)" >&2
  exit 1
fi

restore_electron_sources() {
  if [[ -d "$BACKUP" ]]; then
    for f in electron-main.js preload.js; do
      [[ -f "$BACKUP/$f" ]] && cp -f "$BACKUP/$f" "$ELECTRON/$f"
    done
  fi
}
trap restore_electron_sources EXIT

mkdir -p "$OUT" "$LOG" "$ELECTRON/assets"

echo "=== npm install (mufutu CMMS) ==="
cd "$CMMS"
npm install --legacy-peer-deps 2>&1 | tee "$LOG/npm-install.log"

echo "=== Build workspace packages (@mufutu/core, @mufutu/ui) ==="
npm run build -w @mufutu/core -w @mufutu/ui 2>&1 | tee "$LOG/workspace-build.log"

echo "=== Next.js standalone ==="
cd "$WEB"
rm -f .next/lock 2>/dev/null || true
npm run icons 2>&1 | tee "$LOG/icons.log" || true
ELECTRON_BUILD=1 \
NEXT_PUBLIC_API_URL=/api \
NEXT_PUBLIC_APP_URL=http://127.0.0.1:3847 \
API_INTERNAL_URL="${API_INTERNAL_URL:-https://api.mufutu.ao}" \
npm run build 2>&1 | tee "$LOG/next-build.log"

echo "=== prepare electron-app ==="
export MUFUTU_WEB_DIR="$WEB"
node "$ELECTRON/scripts/prepare-electron.mjs" 2>&1 | tee "$LOG/prepare-electron.log"
cp -rf "$WEB/assets/"* "$ELECTRON/assets/" 2>/dev/null || true
[[ -d "$WEB/build" ]] && cp -rf "$WEB/build" "$ELECTRON/build" 2>/dev/null || true
node "$ROOT/scripts/generate-desktop-brand-assets.mjs" 2>&1 | tee -a "$LOG/prepare-electron.log"

if [[ "$SKIP_OBFUSCATE" != "1" ]]; then
  echo "=== Ofuscação ==="
  cd "$ROOT"
  npm install javascript-obfuscator@4.1.1 --no-save 2>&1 | tee -a "$LOG/obfuscate.log" || true
  node "$ROOT/apps/desktop-mac/scripts/obfuscate-electron.mjs" 2>&1 | tee -a "$LOG/obfuscate.log"
fi

echo "=== electron-builder (NSIS instalador) ==="
cd "$ELECTRON"
npm install --legacy-peer-deps 2>&1 | tee -a "$LOG/electron-npm.log"
npx electron-builder --win nsis --config electron-builder.json -p never \
  -c.extraMetadata.version="$VERSION" 2>&1 | tee "$LOG/electron-builder.log"

mkdir -p "$OUT"
shopt -s nullglob
SETUP=( "$ELECTRON"/dist-electron/MUFUTU-Setup-*.exe )
if [[ ${#SETUP[@]} -eq 0 ]]; then
  echo "❌ Instalador MUFUTU-Setup-*.exe não gerado em $ELECTRON/dist-electron" >&2
  exit 1
fi
cp -f "${SETUP[@]}" "$OUT/"
[[ -f "$ELECTRON/dist-electron/latest.yml" ]] && cp -f "$ELECTRON/dist-electron/latest.yml" "$OUT/"

cd "$OUT"
shasum -a 256 MUFUTU-Setup-*.exe > checksums.sha256 2>/dev/null || sha256sum MUFUTU-Setup-*.exe > checksums.sha256

echo "✅ Instalador Windows: $OUT"
ls -lh "$OUT"
