#!/usr/bin/env bash
# Publica release no repositório mufutusoftware a partir de builds locais.
# Uso: ./scripts/publish-release.sh 1.0.5 /caminho/para/dist-electron

set -euo pipefail

VERSION="${1:?Versão semver obrigatória (ex: 1.0.5)}"
DIST_DIR="${2:-$HOME/Documents/GitHub/mufutu/apps/web/dist-electron}"
REPO="osvaldowafulua/mufutusoftware"
TAG="v${VERSION}"
STAGING="$(cd "$(dirname "$0")/.." && pwd)/staging"

DMG="${DIST_DIR}/MUFUTU-${VERSION}-arm64.dmg"
ZIP="${DIST_DIR}/MUFUTU-${VERSION}-arm64.zip"
MAC_YML="${DIST_DIR}/latest-mac.yml"

for f in "$DMG" "$ZIP" "$MAC_YML"; do
  if [[ ! -f "$f" ]]; then
    echo "Falta ficheiro: $f" >&2
    exit 1
  fi
done

mkdir -p "$STAGING"
cp "$DMG" "$ZIP" "$MAC_YML" "$STAGING/"

WIN_ZIP=$(ls "${DIST_DIR}"/MUFUTU-*-win-x64.zip 2>/dev/null | tail -1 || true)
if [[ -n "$WIN_ZIP" && -f "$WIN_ZIP" ]]; then
  cp "$WIN_ZIP" "$STAGING/"
fi

cd "$STAGING"
shasum -a 256 MUFUTU-${VERSION}-arm64.dmg MUFUTU-${VERSION}-arm64.zip > checksums.sha256
[[ -f "$(basename "$WIN_ZIP")" ]] && shasum -a 256 "$(basename "$WIN_ZIP")" >> checksums.sha256

DMG_HASH=$(shasum -a 256 "MUFUTU-${VERSION}-arm64.dmg" | awk '{print $1}')
ZIP_HASH=$(shasum -a 256 "MUFUTU-${VERSION}-arm64.zip" | awk '{print $1}')
DMG_SIZE=$(stat -f%z "MUFUTU-${VERSION}-arm64.dmg")
ZIP_SIZE=$(stat -f%z "MUFUTU-${VERSION}-arm64.zip")

cat > manifest.json <<EOF
{
  "product": "MUFUTU",
  "version": "${VERSION}",
  "releasedAt": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "channel": "stable",
  "platforms": [
    {
      "id": "macos",
      "artifacts": [
        {
          "filename": "MUFUTU-${VERSION}-arm64.dmg",
          "sha256": "${DMG_HASH}",
          "sizeBytes": ${DMG_SIZE},
          "signed": true
        },
        {
          "filename": "MUFUTU-${VERSION}-arm64.zip",
          "sha256": "${ZIP_HASH}",
          "sizeBytes": ${ZIP_SIZE},
          "signed": true
        }
      ]
    }
  ],
  "releaseNotesUrl": "https://github.com/${REPO}/releases/tag/${TAG}",
  "eula": "https://github.com/${REPO}/blob/main/EULA.md"
}
EOF

UPLOAD_FILES=(
  "MUFUTU-${VERSION}-arm64.dmg"
  "MUFUTU-${VERSION}-arm64.zip"
  latest-mac.yml
  manifest.json
  checksums.sha256
)
if [[ -n "$WIN_ZIP" && -f "$(basename "$WIN_ZIP")" ]]; then
  UPLOAD_FILES+=("$(basename "$WIN_ZIP")")
fi

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
- DMG e ZIP (Apple Silicon)
- \`latest-mac.yml\` para actualização automática

### Windows (se incluído)
- ZIP portátil x64

Verifique \`checksums.sha256\` antes de instalar." \
    "${UPLOAD_FILES[@]}"
fi

echo "✓ https://github.com/${REPO}/releases/tag/${TAG}"
