#!/usr/bin/env bash
# Remove quarentena Gatekeeper de MUFUTU.app (builds não notarizados descarregados do GitHub).
set -euo pipefail

APP="${1:-/Applications/MUFUTU.app}"

if [[ ! -d "$APP" ]]; then
  echo "❌ Aplicação não encontrada: $APP" >&2
  echo "   Uso: $0 [/Applications/MUFUTU.app]" >&2
  exit 1
fi

xattr -cr "$APP"

echo "✅ Quarentena removida de: $APP"
echo ""
echo "Abra MUFUTU normalmente (Launchpad, Spotlight ou duplo clique)."
echo "Alternativa na primeira vez: clique direito → Abrir."
