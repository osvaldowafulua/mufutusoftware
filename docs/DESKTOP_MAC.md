# MUFUTU Desktop macOS

Pipeline **macOS** (Electron) — DMG, ZIP, splash animado, ofuscação, criptografia local, GitHub Releases.

> O cliente **WPF** (`apps/desktop-win`) continua no workflow `desktop-win.yml` (runner Windows). Este documento cobre **só macOS**.

## Arquitectura

| Componente | Caminho |
|------------|---------|
| Electron main | `apps/web/electron-main.js` |
| Splash animado | `apps/web/electron-splash.html` |
| Empacotamento | `apps/web/electron-builder.json` |
| Scripts CI | `apps/desktop-mac/scripts/` |
| Workflow | `.github/workflows/desktop-mac.yml` |

## Segurança

| Camada | Implementação |
|--------|---------------|
| Trânsito | HTTPS para API em produção |
| Repouso | AES-256-GCM + Keychain (`safeStorage`) em `localData` |
| Pacote | ASAR (`electron-builder`) |
| Ofuscação | `javascript-obfuscator` em main + preload |
| SAST | `npm audit`, CodeQL |
| Pentest | OWASP ZAP baseline (API) |
| Integridade | `checksums.sha256` + `manifest.json` |

## Splash ao abrir

Janela transparente com logótipo MUFUTU (laranja `#E8612D`), anéis animados e barra de progresso — fecha quando a app principal está pronta.

## Release

### Tags GitHub

| Formato | Plataforma | Workflow |
| -------- | ---------- | -------- |
| `desktop-mac/v1.0.0` | só macOS | `desktop-mac.yml` |
| `desktop-win/v1.0.0` | só Windows | `desktop-win.yml` |
| `desktop/v1.0.0` | macOS + Windows (release unificada) | `desktop-release.yml` |

```bash
# Só macOS
git tag desktop-mac/v1.0.0
git push origin desktop-mac/v1.0.0

# macOS + Windows na mesma release
git tag desktop/v1.0.0
git push origin desktop/v1.0.0
```

Artefactos: `MUFUTU-{version}-arm64.dmg`, `MUFUTU-{version}-arm64.zip`, `latest-mac.yml` (auto-update).

## Actualização automática (electron-updater)

| Comportamento | Detalhe |
| ------------- | ------- |
| Verificação | ~8 s após abrir a app (silenciosa) |
| Manual | Menu **Ajuda → Verificar actualizações** |
| Download | Prompt «Actualização disponível» → «A descarregar…» |
| Instalação | «Reiniciar para instalar» (`quitAndInstall`) |
| Feed | Releases GitHub com prefixo `desktop-mac/v*` + `latest-mac.yml` |

Sem assinatura Apple (dev/CI sem secrets `APPLE_*`), o Gatekeeper bloqueia com a mensagem **«app danificada»** — o ficheiro não está corrompido.

**Workaround imediato (pt-AO):**

```bash
xattr -cr /Applications/MUFUTU.app
```

Ou clique direito → **Abrir** na primeira execução. Script: `scripts/macos-unquarantine.sh`.

Com **Developer ID + notarização** (`APPLE_CERTIFICATE`, `APPLE_ID`, `APPLE_APP_SPECIFIC_PASSWORD`, `APPLE_TEAM_ID` em CI), a app abre normalmente e o auto-update funciona sem aviso.

Código: `apps/web/electron-update.js` · config publish em `electron-builder.json`.

## API remota (sync online)

O cliente desktop **não** inclui a API NestJS local. O servidor Next.js embebido faz proxy `/api` → API em produção (por omissão `https://api.mufutu.ao`).

| Configuração | Onde |
|--------------|------|
| URL API (subsidiária) | `~/Library/Application Support/MUFUTU/api-config.json` → `{ "apiOrigin": "https://luc.api.mufutu.ao" }` |
| Variável ambiente | `MUFUTU_API_URL=https://luc.api.mufutu.ao` antes de abrir a app |
| Logs | `~/Library/Application Support/MUFUTU/desktop.log` |

Reconstruir DMG após alterações ao pipeline: `apps/desktop-mac/scripts/package.sh`.

## Conectividade e testes

Caminho completo UI → proxy IPv4 → API, logs e comandos `curl`: **[`DESKTOP_CONNECTIVITY.md`](./DESKTOP_CONNECTIVITY.md)**.

## Dokploy

**Não** — distribuição apenas via GitHub HTTPS (igual ao desktop Windows).
