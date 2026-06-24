# MUFUTU Desktop macOS

Cliente **Electron** (CMMS completo) — compila em **macOS**, gera **`.dmg` + `.zip`**, splash animado com logótipo MUFUTU.

> **WPF** (`apps/desktop-win`) só compila em Windows. **macOS** usa este pipeline Electron.

## Distribuição

| Canal | Usar? |
|-------|-------|
| GitHub Releases `desktop-mac/v*` | Sim |
| Dokploy | **Não** |

## Build local (Mac)

```bash
chmod +x apps/desktop-mac/scripts/*.sh
apps/desktop-mac/scripts/package.sh 1.0.0
# → apps/desktop-mac/artifacts/dist/*.dmg e *.zip
```

## Release

```bash
git tag desktop-mac/v1.0.0 && git push origin desktop-mac/v1.0.0
```

## Assinatura Apple (opcional)

Secrets: `APPLE_CERTIFICATE` (p12 base64), `APPLE_CERTIFICATE_PASSWORD`

Sem certificado Apple, o build gera DMG/ZIP não notarizados (Gatekeeper pode avisar).

Ver [`docs/DESKTOP_MAC.md`](../../docs/DESKTOP_MAC.md).
