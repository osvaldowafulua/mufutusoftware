# Distribuição de software (repositório público)

Instaladores e actualizações **não** vivem no repositório `mufutu` (código-fonte privado).

| Repositório | Conteúdo | GitHub |
|-------------|----------|--------|
| **mufutu** | Código, API, web, CI de build | Privado — commits leves (sem `.dmg`, `.exe`, `dist-electron/`) |
| **mufutusoftware** | Releases públicos, README, EULA | [osvaldowafulua/mufutusoftware](https://github.com/osvaldowafulua/mufutusoftware) |

## O que nunca entra no `mufutu` (git)

- `apps/web/dist-electron/` — builds Electron (~GB locais)
- `apps/desktop-win/artifacts/`, `apps/desktop-mac/artifacts/`
- `*.tsbuildinfo`, `*.blockmap`, `*.apk`, `*.ipa`
- Chaves `.pfx`, `.keystore`, `.env`

Tudo isto está em `.gitignore`.

## Publicar versão (CI)

1. Tag no repo **mufutu** (dispara build):
   - `desktop-mac/v1.0.6`
   - `desktop-win/v1.0.6`
   - `desktop/v1.0.6` (mac + win unificado)

2. O workflow compila, ofusca e publica em **mufutusoftware** como tag `v1.0.6`.

3. Secret obrigatório no repo **mufutu** → Settings → Secrets:
   - `MUFUTU_SOFTWARE_RELEASE_TOKEN` — PAT com `contents:write` só em `mufutusoftware`

## Publicar manualmente (Mac local)

```bash
# Depois de gerar em apps/web/dist-electron/
/Users/fluadigital/Documents/GitHub/mufutusoftware/scripts/publish-release.sh 1.0.6
```

Ou a partir do monorepo:

```bash
chmod +x scripts/publish-mufutusoftware-release.sh
GH_TOKEN=... scripts/publish-mufutusoftware-release.sh 1.0.6 apps/web/dist-electron
```

## Clientes (auto-update)

- **macOS Electron:** `electron-update.js` → `osvaldowafulua/mufutusoftware`, tags `v*`
- **Windows WPF:** `GitHubDesktopUpdateService` → mesmo repo
- **electron-builder** `publish.repo` → `mufutusoftware`

## Segurança

O repo público contém **apenas binários finais** + checksums + manifesto JSON.
Sem código-fonte, sem segredos de licenciamento, sem mapas de debug.
