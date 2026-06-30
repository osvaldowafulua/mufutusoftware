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

Secrets GitHub (Settings → Secrets → Actions):

| Secret | Descrição |
|--------|-----------|
| `APPLE_CERTIFICATE` | Developer ID Application (.p12) em base64 |
| `APPLE_CERTIFICATE_PASSWORD` | Password do .p12 |
| `APPLE_ID` | Apple ID developer |
| `APPLE_APP_SPECIFIC_PASSWORD` | App-specific password |
| `APPLE_TEAM_ID` | Team ID (10 chars) |

Sem certificado, o build gera DMG/ZIP **válidos** mas **não notarizados** — o macOS mostra «app danificada» (Gatekeeper). Workaround para utilizadores:

```bash
xattr -cr /Applications/MUFUTU.app
```

Ver [`macos/README.md`](../../macos/README.md) e `scripts/macos-unquarantine.sh`.
