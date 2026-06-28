#!/usr/bin/env bash
# MUFUTU — build macOS (repo mufutusoftware). UI Next.js vem do repo privado mufutu.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
CMMS="${MUFUTU_CMMS_DIR:-$ROOT/../mufutu}"
WEB="$CMMS/apps/web"
ELECTRON="$ROOT/apps/electron"
OUT="$ROOT/apps/desktop-mac/artifacts"
LOG="$OUT/logs"
VERSION="${1:-1.0.0}"
SKIP_OBFUSCATE="${SKIP_OBFUSCATE:-0}"
SKIP_SIGN="${SKIP_SIGN:-0}"
BACKUP="$ELECTRON/.electron-obfuscate-backup"

if [[ ! -d "$WEB" ]]; then
  echo "❌ CMMS não encontrado: $WEB" >&2
  echo "   Defina MUFUTU_CMMS_DIR ou clone mufutu ao lado de mufutusoftware" >&2
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

echo "::group::npm install (mufutu CMMS)"
cd "$CMMS"
npm install --legacy-peer-deps 2>&1 | tee "$LOG/npm-install.log"
echo "::endgroup::"

echo "::group::Build workspace packages"
npm run build -w @mufutu/core -w @mufutu/ui 2>&1 | tee "$LOG/workspace-build.log"
echo "::endgroup::"

echo "::group::Next.js standalone"
cd "$WEB"
rm -f .next/lock 2>/dev/null || true
npm run icons 2>&1 | tee "$LOG/icons.log" || true
ELECTRON_BUILD=1 \
NEXT_PUBLIC_API_URL=/api \
NEXT_PUBLIC_APP_URL=http://127.0.0.1:3847 \
API_INTERNAL_URL="${API_INTERNAL_URL:-https://api.mufutu.ao}" \
npm run build 2>&1 | tee "$LOG/next-build.log"
echo "::endgroup::"

echo "::group::prepare electron-app"
export MUFUTU_WEB_DIR="$WEB"
node "$ELECTRON/scripts/prepare-electron.mjs" 2>&1 | tee "$LOG/prepare-electron.log"
cp -rf "$WEB/assets/"* "$ELECTRON/assets/" 2>/dev/null || true
[[ -d "$WEB/build" ]] && cp -rf "$WEB/build" "$ELECTRON/build" 2>/dev/null || true
node "$ROOT/scripts/generate-desktop-brand-assets.mjs" 2>&1 | tee -a "$LOG/prepare-electron.log"
echo "::endgroup::"

if [[ "$SKIP_OBFUSCATE" != "1" ]]; then
  echo "::group::Ofuscação"
  cd "$ROOT"
  npm install javascript-obfuscator@4.1.1 --no-save 2>&1 | tee -a "$LOG/obfuscate.log" || true
  node "$ROOT/apps/desktop-mac/scripts/obfuscate-electron.mjs" 2>&1 | tee -a "$LOG/obfuscate.log"
  echo "::endgroup::"
fi

echo "::group::electron-builder"
cd "$ELECTRON"
npm install --legacy-peer-deps 2>&1 | tee -a "$LOG/electron-npm.log"
BUILD_ARGS=(-c.extraMetadata.version="$VERSION")
if [[ -n "${APPLE_CERTIFICATE:-}" ]] && [[ "$SKIP_SIGN" != "1" ]]; then
  export CSC_LINK="$APPLE_CERTIFICATE"
  export CSC_KEY_PASSWORD="${APPLE_CERTIFICATE_PASSWORD:-}"
  export CSC_IDENTITY_AUTO_DISCOVERY="${CSC_IDENTITY_AUTO_DISCOVERY:-true}"
else
  export CSC_IDENTITY_AUTO_DISCOVERY=false
  BUILD_ARGS+=(-c.mac.identity=null -c.mac.hardenedRuntime=false)
fi
npx electron-builder --mac dmg zip --config electron-builder.json -p never \
  "${BUILD_ARGS[@]}" 2>&1 | tee "$LOG/electron-builder.log"
echo "::endgroup::"

mkdir -p "$OUT/dist"
shopt -s nullglob
if compgen -G "$ELECTRON/dist-electron/*.dmg" > /dev/null; then
  cp -f "$ELECTRON"/dist-electron/*.dmg "$ELECTRON"/dist-electron/*.zip "$OUT/dist/" 2>/dev/null || true
  cp -f "$ELECTRON"/dist-electron/latest-mac.yml "$ELECTRON"/dist-electron/*.blockmap "$OUT/dist/" 2>/dev/null || true
  bash "$ROOT/apps/desktop-mac/scripts/verify-artifact.sh" "$OUT/dist" 2>&1 | tee "$LOG/verify.log"
else
  echo "❌ Sem DMG/ZIP em $ELECTRON/dist-electron" | tee "$LOG/verify.log"
  exit 1
fi

echo "✅ Artefactos: $OUT/dist"
ls -lh "$OUT/dist"
