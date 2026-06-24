#!/usr/bin/env bash
# Build Android Release (APK/AAB) — requer workload MAUI no SDK.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="${1:-1.0.0}"
OUT="$ROOT/artifacts/android"
mkdir -p "$OUT"

cd "$ROOT"
dotnet restore Mufutu.Mobile.sln
dotnet build src/Mufutu.Mobile/Mufutu.Mobile.csproj \
  -f net8.0-android \
  -c Release \
  -p:ApplicationDisplayVersion="$VERSION"

dotnet publish src/Mufutu.Mobile/Mufutu.Mobile.csproj \
  -f net8.0-android \
  -c Release \
  -p:ApplicationDisplayVersion="$VERSION" \
  -p:AndroidPackageFormat=apk \
  -o "$OUT"

echo "✅ Android: $OUT"
ls -lh "$OUT" || true
