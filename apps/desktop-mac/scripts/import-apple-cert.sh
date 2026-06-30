#!/usr/bin/env bash
# Importa certificado Developer ID (.p12 base64) para keychain de build (CI ou local).
set -euo pipefail

if [[ -z "${APPLE_CERTIFICATE:-}" ]]; then
  echo "Sem APPLE_CERTIFICATE — DMG/ZIP sem assinatura Developer ID"
  if [[ -n "${GITHUB_ENV:-}" ]]; then
    echo "MUFUTU_MAC_SIGNED=false" >> "$GITHUB_ENV"
  fi
  exit 0
fi

P12="/tmp/mufutu-mac.p12"
echo "$APPLE_CERTIFICATE" | base64 --decode > "$P12"

KEYCHAIN="${BUILD_KEYCHAIN:-build.keychain}"
KEYCHAIN_PASS="${BUILD_KEYCHAIN_PASSWORD:-actions}"

security create-keychain -p "$KEYCHAIN_PASS" "$KEYCHAIN" 2>/dev/null || true
security default-keychain -s "$KEYCHAIN"
security unlock-keychain -p "$KEYCHAIN_PASS" "$KEYCHAIN"
security import "$P12" -k "$KEYCHAIN" -P "${APPLE_CERTIFICATE_PASSWORD:-}" -T /usr/bin/codesign -T /usr/bin/productsign
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k "$KEYCHAIN_PASS" "$KEYCHAIN"

if [[ -n "${GITHUB_ENV:-}" ]]; then
  echo "CSC_IDENTITY_AUTO_DISCOVERY=true" >> "$GITHUB_ENV"
  echo "MUFUTU_MAC_SIGNED=true" >> "$GITHUB_ENV"
fi

echo "Certificado Apple importado — assinatura activa"
