# MUFUTU Desktop Windows

Cliente **WPF .NET 8** — **não faz parte do deploy Dokploy** (web/api/control). O código vive no monorepo; o instalador é gerado e distribuído **apenas via GitHub** (Actions + Releases).

## Distribuição

| Canal                                   | Usar?                                                               |
| --------------------------------------- | ------------------------------------------------------------------- |
| **GitHub Releases** (`desktop-win/v*`)  | Sim — HTTPS + checksums                                             |
| **GitHub Actions** (artefactos 90 dias) | Sim — builds manuais / tags                                         |
| **Dokploy**                             | **Não** — pasta excluída de `.dockerignore`; sem serviço no compose |

Documentação:

- [`docs/WINDOWS_DESKTOP.md`](../../docs/WINDOWS_DESKTOP.md) — arquitectura e desenvolvimento
- [`docs/WINDOWS_DESKTOP_PIPELINE.md`](../../docs/WINDOWS_DESKTOP_PIPELINE.md) — CI/CD, assinatura, release GitHub

## Comandos rápidos

```bash
# Testes (Mac/Linux/Windows)
dotnet test tests/Mufutu.Desktop.Tests -c Release

# Build completo + instalador (só Windows)
.\scripts\package.ps1 -Version 1.0.0
```

## Release

```bash
git tag desktop-win/v1.0.0 && git push origin desktop-win/v1.0.0
```
