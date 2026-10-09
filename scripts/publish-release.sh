#!/usr/bin/env bash
# Publica release no repositório mufutusoftware a partir de builds locais ou CI.
# Aceita conjuntos macOS (DMG+ZIP+latest-mac.yml), Windows Electron ou ambos.
#
# Conjunto Windows (todos obrigatórios):
#   MUFUTU-Web-Setup-<v>.exe        instalador web (~0,6 MB) — o que o cliente descarrega primeiro
#   mufutu-<v>-x64.nsis.7z          payload que o stub e o auto-update descarregam
#   latest.yml                      feed do electron-updater (com `packages`)
#   MUFUTU-Setup-<v>-x64.exe        instalador completo, offline
#   MUFUTU-<v>-x64.msi              MSI completo (GPO / Intune)
#   (+ opcionais: .zip portátil, .exe.blockmap, MUFUTU-CodeSign.cer)
#
# Uso: ./scripts/publish-release.sh 1.0.50 /caminho/para/dist
#
# O .exe e o .msi têm de ser o app Electron. Até à 1.0.49 vinham do cliente
# WPF legado (apps/desktop-win), que saía com estes mesmos nomes — daí as
# verificações abaixo antes de publicar o que for.

set -euo pipefail

VERSION="${1:?Versão semver obrigatória (ex: 1.0.50)}"
DIST_DIR="${2:-$HOME/Documents/GitHub/mufutu/apps/web/dist-electron}"
REPO="osvaldowafulua/mufutusoftware"
TAG="v${VERSION}"
STAGING="$(cd "$(dirname "$0")/.." && pwd)/staging"
STUB_MAX_BYTES=$((10 * 1024 * 1024))

sha() {
  if command -v shasum &>/dev/null; then shasum -a 256 "$@"; else sha256sum "$@"; fi
}
fsize() {
  stat -f%z "$1" 2>/dev/null || stat -c%s "$1"
}
die() { echo "❌ $*" >&2; exit 1; }

DMG="${DIST_DIR}/MUFUTU-${VERSION}-arm64.dmg"
MAC_ZIP="${DIST_DIR}/MUFUTU-${VERSION}-arm64.zip"
MAC_YML="${DIST_DIR}/latest-mac.yml"
WIN_STUB="${DIST_DIR}/MUFUTU-Web-Setup-${VERSION}.exe"
WIN_PAYLOAD="${DIST_DIR}/mufutu-${VERSION}-x64.nsis.7z"
WIN_SETUP="${DIST_DIR}/MUFUTU-Setup-${VERSION}-x64.exe"
WIN_MSI="${DIST_DIR}/MUFUTU-${VERSION}-x64.msi"
WIN_YML="${DIST_DIR}/latest.yml"

HAS_MAC=0
[[ -f "$DMG" && -f "$MAC_ZIP" && -f "$MAC_YML" ]] && HAS_MAC=1
HAS_WIN=0
[[ -f "$WIN_STUB" && -f "$WIN_PAYLOAD" && -f "$WIN_SETUP" && -f "$WIN_MSI" && -f "$WIN_YML" ]] && HAS_WIN=1

if [[ "$HAS_MAC" -eq 0 && "$HAS_WIN" -eq 0 ]]; then
  echo "❌ Nenhum conjunto completo em $DIST_DIR:" >&2
  echo "   macOS precisa de: $(basename "$DMG"), $(basename "$MAC_ZIP"), latest-mac.yml" >&2
  echo "   Windows precisa de: $(basename "$WIN_STUB"), $(basename "$WIN_PAYLOAD"), $(basename "$WIN_SETUP")," >&2
  echo "                       $(basename "$WIN_MSI"), latest.yml" >&2
  exit 1
fi

if [[ "$HAS_WIN" -eq 1 ]]; then
  # 1) Os dois Setup são instaladores NSIS do app Electron, nunca o bootstrapper
  #    WiX Burn do cliente WPF legado.
  for f in "$WIN_STUB" "$WIN_SETUP"; do
    grep -qa '\.wixburn' "$f" && die "$(basename "$f") é um bootstrapper WiX Burn (cliente WPF legado), não o app Electron."
    grep -qa 'Nullsoft' "$f" || die "$(basename "$f") não tem assinatura de NSIS — não é o instalador do app Electron."
  done

  # 2) O requisito «instalador até ~10 MB» é do stub.
  STUB_BYTES="$(fsize "$WIN_STUB")"
  (( STUB_BYTES <= STUB_MAX_BYTES )) || die "$(basename "$WIN_STUB") tem $((STUB_BYTES / 1024 / 1024)) MB — o limite é 10 MB."

  # 3) O nome do payload tem de sobreviver ao upload: o GitHub renomeia
  #    caracteres fora de [A-Za-z0-9._-] (um «@» no nome do pacote já deixou o
  #    stub e o latest.yml a apontar para um ficheiro que não existe).
  [[ "$(basename "$WIN_PAYLOAD")" =~ ^[A-Za-z0-9._-]+$ ]] \
    || die "o nome do payload «$(basename "$WIN_PAYLOAD")» tem caracteres que o GitHub renomeia no upload."

  # 4) latest.yml coerente com o payload — sem `packages` o auto-update
  #    descarregava o stub inteiro; com o hash errado recusava o payload.
  node - "$WIN_YML" "$WIN_PAYLOAD" <<'NODE' || exit 1
const fs = require('fs');
const crypto = require('crypto');
const path = require('path');
const [yml, payload] = process.argv.slice(2);
const text = fs.readFileSync(yml, 'utf8');
const fail = (m) => { console.error('❌ ' + m); process.exit(1); };
// O latest.yml é pequeno e de formato fixo; evita-se uma dependência de YAML.
const block = /^packages:\s*\n\s+x64:\s*\n((?:\s{4,}.*\n?)+)/m.exec(text);
if (!block) fail('latest.yml sem `packages.x64` — o auto-update descarregaria o stub inteiro.');
const field = (k) => new RegExp(`^\\s+${k}:\\s*(.+?)\\s*$`, 'm').exec(block[1])?.[1];
const base = path.basename(payload);
if (field('path') !== base) fail(`packages.x64.path = «${field('path')}», mas o payload é «${base}».`);
if (Number(field('size')) !== fs.statSync(payload).size) fail('packages.x64.size não corresponde ao payload.');
const sha = crypto.createHash('sha512').update(fs.readFileSync(payload)).digest('base64');
if (field('sha512') !== sha) fail('packages.x64.sha512 não corresponde ao payload.');
console.log(`✓ latest.yml coerente com ${base}`);
NODE
fi

mkdir -p "$STAGING"
UPLOAD_FILES=()

if [[ "$HAS_MAC" -eq 1 ]]; then
  cp "$DMG" "$MAC_ZIP" "$MAC_YML" "$STAGING/"
  UPLOAD_FILES+=("MUFUTU-${VERSION}-arm64.dmg" "MUFUTU-${VERSION}-arm64.zip" latest-mac.yml)
fi

if [[ "$HAS_WIN" -eq 1 ]]; then
  cp "$WIN_STUB" "$WIN_PAYLOAD" "$WIN_YML" "$WIN_SETUP" "$WIN_MSI" "$STAGING/"
  UPLOAD_FILES+=(
    "MUFUTU-Web-Setup-${VERSION}.exe"
    "mufutu-${VERSION}-x64.nsis.7z"
    latest.yml
    "MUFUTU-Setup-${VERSION}-x64.exe"
    "MUFUTU-${VERSION}-x64.msi"
  )
  for extra in \
    "${DIST_DIR}/MUFUTU-Setup-${VERSION}-x64.exe.blockmap" \
    "${DIST_DIR}/MUFUTU-${VERSION}-win-x64.zip" \
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

artifact_json() { # <ficheiro>
  printf '{ "filename": "%s", "sha256": "%s", "sizeBytes": %s, "signed": %s }' \
    "$1" "$(sha "$1" | awk '{print $1}')" "$(fsize "$1")" "$SIGNED"
}

platforms_json=""
if [[ "$HAS_MAC" -eq 1 ]]; then
  platforms_json+="    { \"id\": \"macos\", \"artifacts\": [
      $(artifact_json "MUFUTU-${VERSION}-arm64.dmg"),
      $(artifact_json "MUFUTU-${VERSION}-arm64.zip")
    ] }"
fi
if [[ "$HAS_WIN" -eq 1 ]]; then
  [[ -n "$platforms_json" ]] && platforms_json+=","$'\n'
  platforms_json+="    { \"id\": \"windows\", \"artifacts\": [
      $(artifact_json "MUFUTU-Web-Setup-${VERSION}.exe"),
      $(artifact_json "mufutu-${VERSION}-x64.nsis.7z"),
      $(artifact_json "MUFUTU-Setup-${VERSION}-x64.exe"),
      $(artifact_json "MUFUTU-${VERSION}-x64.msi")
    ] }"
fi

cat > manifest.json <<MANIFEST
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
MANIFEST
UPLOAD_FILES+=(manifest.json checksums.sha256)

# Notas só com as secções do que esta release contém.
NOTES_FILE="release-notes.md"
{
  echo "## MUFUTU ${VERSION}"
  echo
  if [[ "$HAS_MAC" -eq 1 ]]; then
    echo "### macOS"
    echo "- DMG e ZIP (Apple Silicon) + \`latest-mac.yml\` para actualização automática"
    echo
  fi
  if [[ "$HAS_WIN" -eq 1 ]]; then
    echo "### Windows"
    echo "- **\`MUFUTU-Web-Setup-${VERSION}.exe\`** — instalador web (menos de 1 MB). Descarrega o resto durante a instalação, com verificação SHA-512. **Recomendado.**"
    echo "- \`MUFUTU-Setup-${VERSION}-x64.exe\` — instalador completo, sem necessidade de Internet."
    echo "- \`MUFUTU-${VERSION}-x64.msi\` — MSI completo para GPO / Intune."
    echo "- \`mufutu-${VERSION}-x64.nsis.7z\` + \`latest.yml\` — usados pelo instalador web e pela actualização automática (não descarregar à mão)."
    echo
  fi
  echo "Verifique \`checksums.sha256\` antes de instalar."
  echo "Licença: \`MUFUTU-LIC-*\` — licenca@mufutu.ao"
} > "$NOTES_FILE"

if gh release view "$TAG" --repo "$REPO" &>/dev/null; then
  echo "→ Actualizar release ${TAG}..."
  gh release upload "$TAG" --repo "$REPO" --clobber "${UPLOAD_FILES[@]}"
else
  echo "→ Criar release ${TAG}..."
  gh release create "$TAG" \
    --repo "$REPO" \
    --title "MUFUTU ${VERSION}" \
    --notes-file "$NOTES_FILE" \
    "${UPLOAD_FILES[@]}"
fi

echo "✓ https://github.com/${REPO}/releases/tag/${TAG}"
