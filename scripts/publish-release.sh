#!/usr/bin/env bash
# Publica release no repositório mufutusoftware a partir de builds locais ou CI.
# Aceita conjuntos macOS (DMG+ZIP+latest-mac.yml), Windows Electron
# (Setup NSIS + MSI + latest.yml [+ zip, blockmap, .cer]) ou ambos.
# Uso: ./scripts/publish-release.sh 1.0.45 /caminho/para/dist
#
# O .exe e o .msi têm de ser o app Electron. Até à 1.0.49 vinham do cliente
# WPF legado (apps/desktop-win), que saía com estes mesmos nomes — daí a
# verificação de NSIS mais abaixo antes de publicar o que for.

set -euo pipefail

VERSION="${1:?Versão semver obrigatória (ex: 1.0.45)}"
DIST_DIR="${2:-$HOME/Documents/GitHub/mufutu/apps/web/dist-electron}"
REPO="osvaldowafulua/mufutusoftware"
TAG="v${VERSION}"
STAGING="$(cd "$(dirname "$0")/.." && pwd)/staging"

sha() {
  if command -v shasum &>/dev/null; then shasum -a 256 "$@"; else sha256sum "$@"; fi
}
fsize() {
  stat -f%z "$1" 2>/dev/null || stat -c%s "$1"
}

DMG="${DIST_DIR}/MUFUTU-${VERSION}-arm64.dmg"
MAC_ZIP="${DIST_DIR}/MUFUTU-${VERSION}-arm64.zip"
MAC_YML="${DIST_DIR}/latest-mac.yml"
WIN_SETUP="${DIST_DIR}/MUFUTU-Setup-${VERSION}-x64.exe"
WIN_MSI="${DIST_DIR}/MUFUTU-${VERSION}-x64.msi"
WIN_YML="${DIST_DIR}/latest.yml"

HAS_MAC=0
[[ -f "$DMG" && -f "$MAC_ZIP" && -f "$MAC_YML" ]] && HAS_MAC=1
HAS_WIN=0
[[ -f "$WIN_SETUP" && -f "$WIN_MSI" && -f "$WIN_YML" ]] && HAS_WIN=1

if [[ "$HAS_MAC" -eq 0 && "$HAS_WIN" -eq 0 ]]; then
  echo "❌ Nenhum conjunto completo em $DIST_DIR:" >&2
  echo "   macOS precisa de: $(basename "$DMG"), $(basename "$MAC_ZIP"), latest-mac.yml" >&2
  echo "   Windows precisa de: $(basename "$WIN_SETUP"), $(basename "$WIN_MSI"), latest.yml" >&2
  exit 1
fi

if [[ "$HAS_WIN" -eq 1 ]]; then
  # Guardrail: recusar publicar como Windows qualquer coisa que não seja o
  # instalador NSIS do app Electron.
  if grep -qa '\.wixburn' "$WIN_SETUP"; then
    echo "❌ $(basename "$WIN_SETUP") é um bootstrapper WiX Burn (cliente WPF legado), não o app Electron." >&2
    exit 1
  fi
  if ! grep -qa 'Nullsoft' "$WIN_SETUP"; then
    echo "❌ $(basename "$WIN_SETUP") não tem assinatura de NSIS — não é o instalador do app Electron." >&2
    exit 1
  fi
fi

mkdir -p "$STAGING"
UPLOAD_FILES=()

if [[ "$HAS_MAC" -eq 1 ]]; then
  cp "$DMG" "$MAC_ZIP" "$MAC_YML" "$STAGING/"
  UPLOAD_FILES+=("MUFUTU-${VERSION}-arm64.dmg" "MUFUTU-${VERSION}-arm64.zip" latest-mac.yml)
fi

if [[ "$HAS_WIN" -eq 1 ]]; then
  cp "$WIN_SETUP" "$WIN_MSI" "$WIN_YML" "$STAGING/"
  UPLOAD_FILES+=("MUFUTU-Setup-${VERSION}-x64.exe" "MUFUTU-${VERSION}-x64.msi" latest.yml)
  for extra in \
    "${DIST_DIR}/MUFUTU-Setup-${VERSION}-x64.exe.blockmap" \
    "${DIST_DIR}/MUFUTU-${VERSION}-win-x64.zip" \
    "${DIST_DIR}/MUFUTU-${VERSION}-win-x64.appx" \
    "${DIST_DIR}/MUFUTU-CodeSign.cer"; do
    if [[ -f "$extra" ]]; then
      cp "$extra" "$STAGING/"
      UPLOAD_FILES+=("$(basename "$extra")")
    fi
  done
fi

# signed=true/false vindo do build (signing.env) — parsing restrito.
SIGNED="false"
if [[ -f "${DIST_DIR}/signing.env" ]]; then
  if grep -qx 'signed=true' "${DIST_DIR}/signing.env"; then SIGNED="true"; fi
fi

cd "$STAGING"
sha "${UPLOAD_FILES[@]}" > checksums.sha256

platforms_json=""
if [[ "$HAS_MAC" -eq 1 ]]; then
  DMG_HASH=$(sha "MUFUTU-${VERSION}-arm64.dmg" | awk '{print $1}')
  ZIP_HASH=$(sha "MUFUTU-${VERSION}-arm64.zip" | awk '{print $1}')
  platforms_json+="$(cat <<EOF
    {
      "id": "macos",
      "artifacts": [
        { "filename": "MUFUTU-${VERSION}-arm64.dmg", "sha256": "${DMG_HASH}", "sizeBytes": $(fsize "MUFUTU-${VERSION}-arm64.dmg"), "signed": ${SIGNED} },
        { "filename": "MUFUTU-${VERSION}-arm64.zip", "sha256": "${ZIP_HASH}", "sizeBytes": $(fsize "MUFUTU-${VERSION}-arm64.zip"), "signed": ${SIGNED} }
      ]
    }
EOF
)"
fi
if [[ "$HAS_WIN" -eq 1 ]]; then
  [[ -n "$platforms_json" ]] && platforms_json+=","
  SETUP_HASH=$(sha "MUFUTU-Setup-${VERSION}-x64.exe" | awk '{print $1}')
  MSI_HASH=$(sha "MUFUTU-${VERSION}-x64.msi" | awk '{print $1}')
  platforms_json+="$(cat <<EOF
    {
      "id": "windows",
      "artifacts": [
        { "filename": "MUFUTU-Setup-${VERSION}-x64.exe", "sha256": "${SETUP_HASH}", "sizeBytes": $(fsize "MUFUTU-Setup-${VERSION}-x64.exe"), "signed": ${SIGNED} },
        { "filename": "MUFUTU-${VERSION}-x64.msi", "sha256": "${MSI_HASH}", "sizeBytes": $(fsize "MUFUTU-${VERSION}-x64.msi"), "signed": ${SIGNED} }
      ]
    }
EOF
)"
fi

cat > manifest.json <<EOF
{
  "product": "MUFUTU",
  "version": "${VERSION}",
  "releasedAt": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "channel": "stable",
  "platforms": [
${platforms_json}
  ],
  "releaseNotesUrl": "https://github.com/${REPO}/releases/tag/${TAG}",
  "eula": "https://github.com/${REPO}/blob/main/EULA.md"
}
EOF
UPLOAD_FILES+=(manifest.json checksums.sha256)

if gh release view "$TAG" --repo "$REPO" &>/dev/null; then
  echo "→ Actualizar release ${TAG}..."
  gh release upload "$TAG" --repo "$REPO" --clobber "${UPLOAD_FILES[@]}"
else
  echo "→ Criar release ${TAG}..."
  gh release create "$TAG" \
    --repo "$REPO" \
    --title "MUFUTU ${VERSION}" \
    --notes "## MUFUTU ${VERSION}

### macOS
- DMG e ZIP (Apple Silicon) + \`latest-mac.yml\` para actualização automática

### Windows
- \`MUFUTU-Setup-${VERSION}-x64.exe\` (NSIS — mesmo app Electron do macOS) + \`latest.yml\`
- \`MUFUTU-${VERSION}-x64.msi\` (MSI do mesmo app — para instalação por GPO/Intune)

Verifique \`checksums.sha256\` antes de instalar." \
    "${UPLOAD_FILES[@]}"
fi

echo "✓ https://github.com/${REPO}/releases/tag/${TAG}"
