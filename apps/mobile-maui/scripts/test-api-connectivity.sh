#!/usr/bin/env bash
# Testa conectividade HTTPS à API MUFUTU (CI + dev local).
set -euo pipefail

API_BASE="${MUFUTU_API_URL:-https://api.mufutu.ao/api}"
HEALTH_URL="${API_BASE%/}/health"
TIMEOUT="${MUFUTU_CONNECTIVITY_TIMEOUT:-20}"

echo "A testar ${HEALTH_URL} (timeout ${TIMEOUT}s)…"

if command -v curl >/dev/null 2>&1; then
  body=$(curl -fsS --max-time "$TIMEOUT" -H "Accept: application/json" "$HEALTH_URL")
  echo "OK — ${body}"
  exit 0
fi

echo "curl não encontrado — use dotnet test em Mufutu.Mobile.Tests" >&2
exit 1
