#!/usr/bin/env bash
# MUFUTU — build instaladores desktop (Mac + Windows + ZIP)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

PLATFORM="${1:-all}"

echo "🚀 MUFUTU — Build Desktop"
echo "============================"
echo ""

if ! command -v node >/dev/null 2>&1; then
  echo "❌ Node.js não encontrado"
  exit 1
fi

echo "✅ Node $(node --version)"
echo ""

if [ ! -d "node_modules" ]; then
  echo "📦 npm install (raiz)..."
  npm install
fi

echo "📦 Dependências apps/web..."
npm install -w @ayomant/web

cd apps/web

case "$PLATFORM" in
  mac)
    echo "🍎 Build Mac (DMG + ZIP)..."
    npm run electron:dist:mac
    ;;
  win|windows)
    echo "🪟 Build Windows (NSIS + ZIP)..."
    npm run electron:dist:win
    ;;
  zip)
    echo "📦 Build ZIP (Mac + Windows)..."
    npm run electron:dist:zip
    ;;
  all)
    echo "🍎 Build Mac..."
    npm run electron:dist:mac
    echo ""
    echo "🪟 Build Windows..."
    npm run electron:dist:win || {
      echo "⚠️  Build Windows falhou (normal sem Wine). ZIP Mac disponível."
    }
    ;;
  *)
    echo "Uso: $0 [all|mac|win|zip]"
    exit 1
    ;;
esac

OUT="$ROOT/apps/web/dist-electron"
echo ""
echo "🎉 Build concluído!"
echo "📁 Saída: $OUT"
echo ""
if [ -d "$OUT" ]; then
  ls -lh "$OUT" 2>/dev/null || true
fi
echo ""
echo "Instalação:"
echo "  Mac:     abrir MUFUTU-*.dmg → arrastar para Aplicações"
echo "  Mac ZIP: descompactar MUFUTU-*.zip → abrir MUFUTU.app"
echo "  Windows: executar MUFUTU-*-setup.exe ou descompactar MUFUTU-*-win-x64.zip"
