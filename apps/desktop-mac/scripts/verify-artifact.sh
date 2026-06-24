#!/usr/bin/env bash
# Valida SHA-256/512 e gera manifest.json + checksums.sha256
set -euo pipefail

DIR="${1:?Diretório de artefactos}"
MANIFEST="$DIR/manifest.json"
CHECKSUMS="$DIR/checksums.sha256"

artifacts=()
while IFS= read -r -d '' f; do
  artifacts+=("$f")
done < <(find "$DIR" -maxdepth 1 \( -name '*.dmg' -o -name '*.zip' \) -print0)

if [[ ${#artifacts[@]} -eq 0 ]]; then
  echo "❌ Nenhum .dmg ou .zip em $DIR"
  exit 1
fi

generated="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
echo "{" > "$MANIFEST"
echo "  \"generatedAt\": \"$generated\"," >> "$MANIFEST"
echo "  \"artifacts\": [" >> "$MANIFEST"

> "$CHECKSUMS"
first=1
for f in "${artifacts[@]}"; do
  name="$(basename "$f")"
  sha256="$(shasum -a 256 "$f" | awk '{print $1}')"
  sha512="$(shasum -a 512 "$f" | awk '{print $1}')"
  echo "$sha256  $name" >> "$CHECKSUMS"
  [[ $first -eq 1 ]] || echo "," >> "$MANIFEST"
  first=0
  cat >> "$MANIFEST" <<EOF
    {
      "file": "$name",
      "sha256": "$sha256",
      "sha512": "$sha512"
    }
EOF
  echo "✓ $name"
  echo "  SHA-256: $sha256"
done

echo "" >> "$MANIFEST"
echo "  ]" >> "$MANIFEST"
echo "}" >> "$MANIFEST"

echo "Manifest: $MANIFEST"
echo "Checksums: $CHECKSUMS"
