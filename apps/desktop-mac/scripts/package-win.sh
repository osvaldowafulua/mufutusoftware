#!/usr/bin/env bash
# MUFUTU — build Windows: instalador NSIS (.exe) + ZIP portátil
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
  if ! command -v wine64 >/dev/null 2>&1 && ! command -v wine >/dev/null 2>&1; then
    echo "⚠️  Build NSIS (.exe) no Mac requer Wine ou GitHub Actions (Windows)." >&2
    echo "   Alternativa: bash apps/desktop-mac/scripts/package-win.sh no PC Windows" >&2
    echo "   Ou: GitHub → Actions → Windows Electron Installer (NSIS)" >&2
  fi
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

if [[ "$SKIP_OBFUSCATE" != "1" ]]; then
  echo "=== Ofuscação ==="
  cd "$ROOT"
  npm install javascript-obfuscator@4.1.1 --no-save 2>&1 | tee -a "$LOG/obfuscate.log" || true
  node "$ROOT/apps/desktop-mac/scripts/obfuscate-electron.mjs" 2>&1 | tee -a "$LOG/obfuscate.log"
fi

echo "=== electron-builder (NSIS + ZIP) ==="
cd "$ELECTRON"
npm install --legacy-peer-deps 2>&1 | tee -a "$LOG/electron-npm.log"
npx electron-builder --win nsis zip --config electron-builder.json -p never \
  -c.extraMetadata.version="$VERSION" 2>&1 | tee "$LOG/electron-builder.log"

mkdir -p "$OUT"
shopt -s nullglob
copied=0
for f in "$ELECTRON"/dist-electron/*.{exe,zip}; do
  cp -f "$f" "$OUT/"
  copied=1
done
[[ -f "$ELECTRON/dist-electron/latest.yml" ]] && cp -f "$ELECTRON/dist-electron/latest.yml" "$OUT/"

if [[ "$copied" != "1" ]]; then
  echo "❌ Sem .exe/.zip em $ELECTRON/dist-electron" >&2
  exit 1
fi

cd "$OUT"
shasum -a 256 MUFUTU-* > checksums.sha256 2>/dev/null || sha256sum MUFUTU-* > checksums.sha256

echo "✅ Instalador Windows: $OUT"
ls -lh "$OUT"
