# MUFUTU — macOS

Cliente **Electron** para macOS (Apple Silicon arm64).

## Descarregar

**Última versão:** [GitHub Releases](https://github.com/osvaldowafulua/mufutusoftware/releases/latest)

- `MUFUTU-{version}-arm64.dmg` — recomendado (arrastar para Aplicações)
- `MUFUTU-{version}-arm64.zip` — alternativa
- `latest-mac.yml` — actualização automática (`electron-updater`)

## Instalação

1. Abra o `.dmg` e arraste **MUFUTU** para **Aplicações**.
2. Na primeira execução, o macOS pode bloquear — ver secção **Gatekeeper** abaixo.
3. Login no tenant da sua empresa.

## Gatekeeper — «App danificada»

Mensagem típica:

> *Não é possível abrir MUFUTU, porque está danificado. Este elemento deveria ser movido para o Lixo.*

**Isto não significa ficheiro corrompido.** O macOS bloqueia apps descarregadas da Internet que ainda **não têm assinatura Developer ID + notarização Apple** (releases actuais do GitHub).

### Solução imediata

Após instalar em `/Applications/MUFUTU.app`:

```bash
xattr -cr /Applications/MUFUTU.app
```

Ou use o script do repositório:

```bash
curl -fsSL https://raw.githubusercontent.com/osvaldowafulua/mufutusoftware/main/scripts/macos-unquarantine.sh | bash
# ou, com clone local:
chmod +x scripts/macos-unquarantine.sh
./scripts/macos-unquarantine.sh
```

**Alternativa:** clique direito em MUFUTU → **Abrir** (apenas na primeira vez).

### ZIP vs DMG

Ambos recebem o atributo de quarentena ao descarregar do browser/GitHub. O `.dmg` é preferível para instalação; o workaround `xattr -cr` aplica-se ao `.app` instalado, independentemente do formato de download.

## Segurança

- Verifique SHA-256 com `checksums.sha256` em cada release.
- Não instale `.app` de fontes desconhecidas.
- Builds futuros com notarização Apple abrirão sem este passo extra.

## Actualização automática

O cliente usa `electron-updater` com metadados `latest-mac.yml` do release oficial.

## Requisitos

- macOS 12 Monterey ou superior
- Apple Silicon (M1/M2/M3/M4)
- 4 GB RAM · 600 MB disco

## Problemas comuns

| Sintoma | Solução |
|---------|---------|
| «App danificada» | `xattr -cr /Applications/MUFUTU.app` ou clique direito → Abrir |
| Ecrã em branco | Confirme URL do tenant e rede |
| Checksum diferente | Re-descarregue do [Release oficial](https://github.com/osvaldowafulua/mufutusoftware/releases) |

Suporte: **suporte@mufutu.ao**

## CI — assinatura Apple (equipa MUFUTU)

Secrets GitHub necessários para builds assinados e notarizados:

| Secret | Descrição |
|--------|-----------|
| `APPLE_CERTIFICATE` | Certificado Developer ID (.p12) em base64 |
| `APPLE_CERTIFICATE_PASSWORD` | Password do .p12 |
| `APPLE_ID` | Apple ID da conta developer |
| `APPLE_APP_SPECIFIC_PASSWORD` | App-specific password (appleid.apple.com) |
| `APPLE_TEAM_ID` | Team ID (10 caracteres) |

Sem estes secrets, o CI gera builds válidos mas o Gatekeeper exige o workaround acima.

Ver [`docs/DESKTOP_MAC.md`](../docs/DESKTOP_MAC.md).
