# MUFUTU — macOS

Cliente **Electron** para macOS (Apple Silicon arm64; releases Intel quando indicado).

## Descarregar

1. [GitHub Releases](https://github.com/osvaldowafulua/mufutusoftware/releases)
2. Ficheiros:
   - **`MUFUTU-*-arm64.dmg`** — imagem de disco (recomendado)
   - **`MUFUTU-*-mac.zip`** — arquivo alternativo
   - **`latest-mac.yml`** — metadados para actualização automática

## Instalação

1. Abra o `.dmg` e arraste **MUFUTU** para **Aplicações**.
2. Na primeira execução: **Preferências do Sistema → Privacidade e Segurança** → «Abrir mesmo assim» se o Gatekeeper alertar (apenas para builds notarizados oficiais).
3. Login no tenant da sua empresa.

## Segurança

- Preferir builds **notarizados** publicados nos Releases.
- Verifique SHA-256 com `checksums.sha256`:

```bash
shasum -a 256 MUFUTU-1.0.0-arm64.dmg
```

- Não instale `.app` de fontes desconhecidas.

## Actualização automática

O cliente usa `electron-updater` com metadados `latest-mac.yml` do release oficial.
Desactivar actualizações automáticas: política MDM da empresa.

## Requisitos

- macOS 12 Monterey ou superior
- Apple Silicon (M1/M2/M3) ou Intel conforme artefacto
- 4 GB RAM · 600 MB disco

## Problemas comuns

| Sintoma | Solução |
|---------|---------|
| «App danificada» | Re-descarregue do Release; verifique checksum |
| Ecrã em branco | Confirme URL do tenant e rede |

Suporte: **suporte@mufutu.ao**
