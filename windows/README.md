# MUFUTU — Windows

## Caminho primário: Electron (NSIS + MSI + ZIP)

O cliente Windows é o **mesmo stack do macOS** (Electron + Next.js embutido + offline IndexedDB da web).

| Artefacto | Origem | Notas |
|-----------|--------|--------|
| `MUFUTU-Web-Setup-*.exe` | electron-builder **nsis-web** | **Recomendado.** < 1 MB; descarrega o pacote durante a instalação (SHA-512 verificado) |
| `mufutu-*-x64.nsis.7z` | electron-builder nsis-web | Pacote que o instalador web e a actualização automática descarregam |
| `MUFUTU-Setup-*-x64.exe` | electron-builder **NSIS** | Instalador completo, sem Internet |
| `MUFUTU-*-x64.msi` | electron-builder **MSI** | Instalação por GPO / Intune |
| `MUFUTU-*-win-x64.zip` | electron-builder zip | Portátil |
| `latest.yml` | electron-builder | Feed da actualização automática |

Todos são o **mesmo app**, só muda a forma de instalar. O instalador web e o completo são
construídos em **execuções separadas** do electron-builder: no mesmo comando partilham o
pacote em cache e o `latest.yml`, e o pacote de um saía com o conteúdo do outro.

> **A partir da 1.0.50.** Até à 1.0.49 o `.exe` e o `.msi` publicados aqui eram o
> cliente **WPF legado** — o `package.ps1` escrevia-o com os nomes do Electron e
> o upload (`--clobber`) substituía o app a sério na release. Nas 1.0.47/1.0.48 o
> `.exe` já era o app certo, mas morria ao arrancar instalado em Program Files
> (escrevia um ficheiro na pasta de instalação, onde um utilizador normal não
> tem permissão). Só o ZIP portátil funcionava. Ambas as causas estão
> corrigidas e o CI passou a instalar o `.exe` **e** o `.msi` e a arrancá-los com
> a pasta de instalação só-de-leitura antes de qualquer publicação.

Código-fonte do shell: repositório privado `mufutu` → `apps/electron/` (`electron-builder.json`: `nsis-web` por defeito; `nsis`, `msi` e `zip` na segunda execução do `package-win.sh`).

Auto-update: `electron-updater` (`electron-update.js`) com canal GitHub Releases `mufutusoftware`.

API Luachimo: `~/AppData/.../api-config.json` → `{ "apiOrigin": "https://sml.api.mufutu.ao" }` ou `MUFUTU_API_URL`.

## Legado: WPF (.NET 8)

O cliente **WPF** (`apps/desktop-win`) é uma aplicação **diferente** e não é o MUFUTU Desktop actual. Fica disponível apenas para instalações existentes e **não** recebe write-offline novo.

Os seus artefactos passaram a ter prefixo `MUFUTU-WPF-` e são publicados numa release própria (`desktop-win/v*`), nunca na release `v<versão>` do produto.

| Tipo | Ficheiro |
|------|----------|
| Setup legado | `MUFUTU-WPF-Setup-*-x64.exe` |
| MSI legado | `MUFUTU-WPF-*-x64.msi` |
| Portátil legado | `MUFUTU-WPF-*-win-x64.zip` |

## Instalação

1. Execute o instalador web (recomendado), o instalador completo, ou o MSI (parque gerido).
2. Aceite o [EULA](../EULA.md).
3. Login com credenciais do tenant.

### TI / silenciosa

```powershell
# Instalador web (precisa de Internet) ou completo
.\MUFUTU-Web-Setup-1.0.x.exe /S
.\MUFUTU-Setup-1.0.x-x64.exe /S

# MSI (GPO / Intune)
msiexec /i MUFUTU-1.0.x-x64.msi /qn /norestart
```

## Segurança

- Assinatura Authenticode quando certificados disponíveis no CI.
- TLS para a API do tenant.
- «MUFUTU 500» no desktop = erro da UI web embutida (não código nativo) — ver Error Boundaries no CMMS.

## Actualizações

Version gate + electron-updater; sem rede o bloqueio **não** activa (offline-first).
