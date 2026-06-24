# MUFUTU — Windows

Cliente nativo **WPF / .NET 8** para Windows 10 e 11 (64-bit).

## Descarregar

1. [GitHub Releases](https://github.com/osvaldowafulua/mufutusoftware/releases)
2. Ficheiro: **`MUFUTU-Setup.exe`** (bootstrapper) ou **`MUFUTU-*.msi`** (instalação silenciosa IT)

## Instalação

### Utilizador

1. Execute o instalador como utilizador com permissões de instalação.
2. Aceite o [EULA](../EULA.md).
3. Inicie **MUFUTU** no menu Iniciar.
4. Introduza URL do tenant (ex.: `https://app.mufutu.ao` ou o subdomínio da sua empresa).
5. Faça login com as credenciais fornecidas pelo administrador.

### TI / instalação silenciosa

```powershell
# Exemplo MSI — ajuste o nome do ficheiro à versão do release
msiexec /i MUFUTU-1.0.0-x64.msi /qn
```

Variáveis de ambiente opcionais (definidas pelo administrador, não sensíveis):

| Variável | Descrição |
|----------|-----------|
| `MUFUTU_API_URL` | URL base da API do tenant |
| `MUFUTU_SITE_CODE` | Site operacional (MUA, LUC, MIL) |

## Segurança

- Instalador assinado digitalmente (Authenticode) em releases de produção.
- Binário ofuscado — **não** inclui símbolos de debug públicos.
- Tokens em repouso: encriptação AES + Credential Locker.
- Verifique `checksums.sha256` no release antes de distribuir na rede interna.

## Actualização

O cliente pode verificar automaticamente novos releases neste repositório.
Política de actualização pode ser gerida pelo GPO / Intune da sua empresa.

## Requisitos

- Windows 10 22H2+ ou Windows 11
- 4 GB RAM (8 GB recomendado)
- 500 MB disco
- Ligação HTTPS à API do tenant (ou modo offline limitado)

## Problemas comuns

| Sintoma | Solução |
|---------|---------|
| «Não confia no publicador» | Instale apenas releases oficiais; contacte TI para certificado |
| Erro de rede | Verifique proxy Windows e firewall para `*.mufutu.ao` |
| Licença inválida | Administrador deve activar chave em Definições → Licença |

Suporte: **suporte@mufutu.ao**
