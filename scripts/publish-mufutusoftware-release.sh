#!/usr/bin/env bash
# Publica artefactos de build no repositório público mufutusoftware (sem código-fonte).
# Uso CI: scripts/publish-mufutusoftware-release.sh 1.0.5 /caminho/para/artefactos/*
# Requer: gh CLI + secret MUFUTU_SOFTWARE_RELEASE_TOKEN (ou GH_TOKEN com write no repo público)

set -euo pipefail

VERSION="${1:?Versão semver (ex: 1.0.5)}"
shift
ASSET_DIR="${1:?Directório ou ficheiros de artefactos}"
shift || true

REPO="osvaldowafulua/mufutusoftware"
TAG="v${VERSION}"
STAGING="${RUNNER_TEMP:-/tmp}/mufutu-release-${VERSION}"
mkdir -p "$STAGING"

collect_assets() {
  if [[ -d "$ASSET_DIR" ]]; then
    find "$ASSET_DIR" -type f \( \
      -name '*.dmg' -o -name '*.zip' -o -name '*.exe' -o -name '*.msi' \
      -o -name 'latest-mac.yml' -o -name 'latest.yml' \
    \) -exec cp {} "$STAGING/" \;
  else
    cp "$ASSET_DIR" "$STAGING/" 2>/dev/null || true
    for f in "$@"; do
      [[ -f "$f" ]] && cp "$f" "$STAGING/"
    done
  fi
}

collect_assets

cd "$STAGING"
shopt -s nullglob
mapfile -t FILES < <(find . -maxdepth 1 -type f \( \
  -name '*.dmg' -o -name '*.zip' -o -name '*.exe' -o -name '*.msi' \
  -o -name 'latest-mac.yml' -o -name 'latest.yml' \
\) -printf '%f\n' | sort -u)
if [[ ${#FILES[@]} -eq 0 ]]; then
  echo "Nenhum artefacto encontrado em $ASSET_DIR" >&2
  exit 1
fi

shasum -a 256 "${FILES[@]}" > checksums.sha256 2>/dev/null || true

PLATFORMS_JSON="[]"
if ls MUFUTU-*-arm64.dmg &>/dev/null; then
  DMG=$(ls MUFUTU-*-arm64.dmg | head -1)
  DMG_HASH=$(shasum -a 256 "$DMG" | awk '{print $1}')
  DMG_SIZE=$(stat -f%z "$DMG" 2>/dev/null || stat -c%s "$DMG")
  PLATFORMS_JSON=$(cat <<EOF
[
  {
    "id": "macos",
    "artifacts": [
      {
        "filename": "$(basename "$DMG")",
        "sha256": "${DMG_HASH}",
        "sizeBytes": ${DMG_SIZE},
        "signed": true
      }
    ]
  }
]
EOF
)
fi

cat > manifest.json <<EOF
{
  "product": "MUFUTU",
  "version": "${VERSION}",
  "releasedAt": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "channel": "stable",
  "platforms": ${PLATFORMS_JSON},
  "releaseNotesUrl": "https://github.com/${REPO}/releases/tag/${TAG}",
  "eula": "https://github.com/${REPO}/blob/main/EULA.md"
}
EOF

UPLOAD=( "${FILES[@]}" checksums.sha256 manifest.json )

if gh release view "$TAG" --repo "$REPO" &>/dev/null; then
  gh release upload "$TAG" --repo "$REPO" --clobber "${UPLOAD[@]}"
else
  gh release create "$TAG" \
    --repo "$REPO" \
    --title "MUFUTU ${VERSION}" \
    --notes "## MUFUTU ${VERSION}

Distribuição oficial — [mufutusoftware](https://github.com/${REPO}).

Verifique \`checksums.sha256\` antes de instalar.
Licença: \`MUFUTU-LIC-*\` — licenca@mufutu.ao" \
    "${UPLOAD[@]}"
fi

echo "✓ Publicado em https://github.com/${REPO}/releases/tag/${TAG}"
