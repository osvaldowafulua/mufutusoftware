# MUFUTU Desktop Windows (WPF)

Cliente Windows nativo em **C# / .NET 8 LTS** que consome a API NestJS existente.

> **Dokploy:** esta app **não** entra no deploy Docker (web, api, control). A pasta `apps/desktop-win/` está em `.dockerignore`. Instaladores ficam no **GitHub** — Releases e artefactos do workflow `desktop-win.yml`.

## Estrutura

```
apps/desktop-win/
├── Mufutu.sln
├── src/
│   ├── Mufutu.Desktop/           # WPF — UI MVVM
│   ├── Mufutu.Desktop.Core/      # API client, crypto, offline SQLite
│   └── Mufutu.Desktop.Licensing/ # Validação MUFUTU-LIC-* (Ed25519)
├── tests/Mufutu.Desktop.Tests/
├── installer/wix/                # MSI + Burn bootstrapper
└── scripts/                      # package.ps1, obfuscar, assinatura
```

## Stack

| Camada   | Tecnologia                                              |
| -------- | ------------------------------------------------------- |
| UI       | WPF + CommunityToolkit.Mvvm                             |
| Runtime  | .NET 8 (`net8.0-windows`)                               |
| API      | `HttpClient` + Bearer JWT                               |
| Repouso  | AES-256-GCM + DPAPI + Credential Locker (Windows)       |
| Offline  | SQLite (`Microsoft.Data.Sqlite`) + fila sync encriptada |
| Licenças | Port de `packages/licensing` (Ed25519/JWS)              |

## Endpoints consumidos (MVP)

| Módulo    | Endpoint                                         |
| --------- | ------------------------------------------------ |
| Auth      | `POST /api/auth/login`, `POST /api/auth/refresh` |
| Dashboard | `GET /api/reports/overview`                      |
| OTs       | `GET /api/work-orders`                           |
| Activos   | `GET /api/assets`                                |

Headers: `Authorization: Bearer`, `X-Site-Id` (default `MUA`).

## Variáveis de ambiente

| Var                | Default Release             | Default Debug               | Descrição        |
| ------------------ | --------------------------- | --------------------------- | ---------------- |
| `MUFUTU_API_URL`   | `https://api.mufutu.ao/api` | `http://localhost:6000/api` | Base URL da API  |
| `MUFUTU_SITE_CODE` | `MUA`                       | `MUA`                       | Site operacional |

> **Rede:** `HttpClient` usa proxy e credenciais por omissão do Windows. Logs em `%LocalAppData%\MUFUTU\desktop.log`. Ver [`DESKTOP_CONNECTIVITY.md`](./DESKTOP_CONNECTIVITY.md).

## Desenvolvimento local (Windows)

```powershell
cd apps/desktop-win
dotnet restore Mufutu.sln
dotnet run --project src/Mufutu.Desktop/Mufutu.Desktop.csproj
```

Credenciais seed: `admin@mufutu.ao` / `Admin@2025` (API local na porta 6000).

## Modelo de ameaças (resumo STRIDE)

| Ameaça                 | Mitigação                                             |
| ---------------------- | ----------------------------------------------------- |
| Spoofing               | JWT Bearer + refresh rotation                         |
| Tampering              | HTTPS TLS 1.2+, checksums no release                  |
| Repudiation            | Audit logs na API                                     |
| Information disclosure | AES-256-GCM em repouso, Credential Locker para tokens |
| Denial of service      | Timeouts HttpClient, retry refresh                    |
| Elevation              | Instalador per-machine, execução asInvoker            |

## Release GitHub

| Formato tag | Workflow |
| ----------- | -------- |
| `desktop-win/v1.0.0` | `desktop-win.yml` (MSI/EXE + testes SAST) |
| `desktop/v1.0.0` | `desktop-release.yml` (macOS + Windows) |

```powershell
git tag desktop-win/v1.0.0
git push origin desktop-win/v1.0.0
```

Artefactos em `apps/desktop-win/artifacts/installer/` após `scripts/package.ps1`.

## Actualização automática

O cliente WPF **não** usa `electron-updater`. Verifica a API GitHub Releases:

| Comportamento | Detalhe |
| ------------- | ------- |
| Verificação | ~6 s após arranque (silenciosa) |
| Manual | Barra lateral → **Verificar actualizações** |
| Comparação | Versão do assembly vs tag `desktop-win/v*` |
| Download | MSI/EXE para `%TEMP%\MUFUTU-update\` |
| Instalação | Abre `msiexec` ou `.exe` — utilizador conclui o wizard |

**Limitação:** não há actualização silenciosa em background (Squirrel/MSIX). O instalador WiX/Burn requer interacção do utilizador. Para silent enterprise deploy, usar MSI via GPO/Intune.

Código: `Mufutu.Desktop.Core/Updates/GitHubDesktopUpdateService.cs` · UI em `Mufutu.Desktop/Updates/DesktopUpdateUi.cs`.

Repositório: `github.com/osvaldowafulua/mufutu`

## Roadmap paridade web

1. MVP — login, dashboard, OTs, activos (leitura) ✅
2. Modo Campo — OTs técnicas, PT express
3. Offline sync completo — fila → API
4. Impressão térmica / QR

Ver também: [`WINDOWS_DESKTOP_PIPELINE.md`](./WINDOWS_DESKTOP_PIPELINE.md).
