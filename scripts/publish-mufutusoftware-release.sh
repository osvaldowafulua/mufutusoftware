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
SIGNED=false
# Parsing explícito, nunca "source": o signing.env vem de um artefacto de CI —
# executá-lo como shell daria execução de código arbitrário com o token de
# release a quem conseguisse influenciar o artefacto.
if [[ -f "$ASSET_DIR/signing.env" ]] && grep -qx 'signed=true' "$ASSET_DIR/signing.env"; then
  SIGNED=true
fi
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
        "signed": ${SIGNED}
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
  # Notas geradas para ficheiro via heredoc com delimitador entre plicas
  # ('NOTES_EOF') — o corpo fica 100% literal, sem o bash a interpretar
  # crases/$/aspas. Passar isto inline como argumento de --notes já
  # partiu a release (várias crases escapadas na mesma string confundiam
  # o parser do bash e cortavam "MUFUTU" para fora, passando-o ao gh como
  # se fosse um ficheiro — "no matches found for `MUFUTU`").
  NOTES_FILE="release-notes.md"
  sed "s|__VERSION__|${VERSION}|g; s|__REPO__|${REPO}|g" > "$NOTES_FILE" <<'NOTES_EOF'
## MUFUTU __VERSION__

Distribuição oficial — [mufutusoftware](https://github.com/__REPO__).

Verifique `checksums.sha256` antes de instalar.
Licença: `MUFUTU-LIC-*` — licenca@mufutu.ao

### macOS — «App danificada» / Gatekeeper

Se o macOS disser que **MUFUTU está danificado**, o ficheiro **não está corrompido** — é o Gatekeeper a bloquear builds ainda **sem notarização Apple**. Clique-direito → Abrir **não** contorna esta mensagem específica.

**Solução imediata — sem Terminal:** dentro do `.dmg`, depois de arrastar o MUFUTU para Aplicações, dê duplo clique em **"Instalar MUFUTU — clique aqui"** (o macOS pede confirmação só na primeira vez — escolha Abrir).

**Alternativa via Terminal:**

```bash
xattr -cr /Applications/MUFUTU.app
```

Script incluído no repositório: `scripts/macos-unquarantine.sh`
NOTES_EOF

  gh release create "$TAG" \
    --repo "$REPO" \
    --title "MUFUTU ${VERSION}" \
    --notes-file "$NOTES_FILE" \
    "${UPLOAD[@]}"
fi

echo "✓ Publicado em https://github.com/${REPO}/releases/tag/${TAG}"
