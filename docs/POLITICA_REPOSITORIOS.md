# Política de repositórios

## mufutusoftware (este repo — público)

| Incluir | Não incluir |
|---------|-------------|
| Instaladores via **GitHub Releases** | Código-fonte |
| README, EULA, SECURITY | Chaves `.pfx`, `.env`, tokens |
| `checksums.sha256`, `manifest.json` | Mapas de debug (`.pdb`, source maps) |
| `latest-mac.yml` (auto-update) | Credenciais seed, URLs internas admin |
| Guias de instalação por plataforma | Config Obfuscar / licenciamento privado |

## mufutu (privado)

Código-fonte, API, web CMMS, pipelines CI. Builds pesados ficam **locais ou em Actions** — nunca em `git push`.

## Fluxo

```
mufutu (privado) — tag desktop-*/v* → CI build → mufutusoftware Releases (v*)
```

Utilizadores finais descarregam apenas deste repositório.
