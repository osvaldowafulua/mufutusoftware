# Releases — manifesto público

Este directório contém **apenas exemplos** de metadados públicos.
Os binários ficam em [GitHub Releases](https://github.com/osvaldowafulua/mufutusoftware/releases).

## Ficheiros por release (típicos)

| Ficheiro | Conteúdo |
|----------|----------|
| `checksums.sha256` | Hashes SHA-256 de todos os artefactos |
| `manifest.json` | Versão, plataformas, datas (sem URLs internas sensíveis) |
| `latest-mac.yml` | Actualização Electron macOS |
| Instaladores | `.exe`, `.msi`, `.dmg`, `.apk`, etc. |

Ver `manifest.example.json` para o esquema público aceite.

## Versão «latest»

A versão estável mais recente está sempre na página principal de
[Releases](https://github.com/osvaldowafulua/mufutusoftware/releases) (tag semver).

Não publicamos URLs de API internas, chaves de licença nem endpoints de administração
nestes manifestos.
