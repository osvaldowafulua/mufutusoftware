# Política de Segurança — MUFUTU Software

## Canais oficiais

| Canal | Uso |
|-------|-----|
| [GitHub Releases](https://github.com/osvaldowafulua/mufutusoftware/releases) | Instaladores finais (EXE, MSI, DMG, APK, IPA) |
| [mufutu.ao](https://mufutu.ao) | Informação comercial e contacto |
| Aplicação web do seu tenant | `https://app.mufutu.ao` ou `https://{empresa}.app.mufutu.ao` |

**Não** instale pacotes de mirrors não autorizados, links em fóruns ou ficheiros sem
checksum publicado no release correspondente.

## Integridade dos instaladores

Cada release oficial inclui, quando aplicável:

- `checksums.sha256` — hashes SHA-256 dos ficheiros
- `manifest.json` — metadados de versão e plataforma (sem segredos)
- Assinatura de código (Windows Authenticode, Apple notarization quando disponível)

### Verificar checksum (exemplo)

```bash
# macOS / Linux
shasum -a 256 -c checksums.sha256

# Windows (PowerShell)
Get-FileHash .\MUFUTU-Setup.exe -Algorithm SHA256
# Compare com o valor publicado no release
```

## Protecção do produto

Os binários distribuídos neste repositório são **builds de produção**:

- Compilados e empacotados em pipeline CI isolado
- Ofuscação e endurecimento aplicados antes da publicação (cliente Windows)
- Sem código-fonte, símbolos de debug ou mapas expostos publicamente
- Sem chaves privadas, tokens ou credenciais de ambiente

Este repositório **não** contém informação que permita replicar a infraestrutura
interna, chaves de licenciamento ou lógica proprietária desprotegida.

## Reportar vulnerabilidades

Envie relatórios **privados** para:

**seguranca@mufutu.ao**

Inclua: produto e versão, plataforma, passos para reproduzir, impacto estimado.
**Não** abra issues públicas com detalhes de exploit antes da correcção.

Compromisso:

- Confirmação de recepção em até 5 dias úteis
- Actualização de estado quando existir correcção ou mitigação
- Crédito ao investigador (se desejado) após divulgação coordenada

## O que não reportar aqui

- Pedidos de licença ou suporte funcional → ver README (secção Suporte)
- Issues de conta ou password → administrador da sua empresa
- Engenharia reversa ou pedidos de código-fonte → fora do âmbito deste canal

## Divulgação responsável

Pedimos que não divulgue publicamente vulnerabilidades até disponibilizarmos
uma versão corrigida nos Releases, excepto se a lei o exigir.
