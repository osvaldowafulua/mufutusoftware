# MUFUTU — Windows

## Caminho primário: Electron (NSIS + MSIX)

O cliente Windows recomendado é o **mesmo stack do macOS** (Electron + Next.js embutido + offline IndexedDB da web).

| Artefacto | Origem | Notas |
|-----------|--------|--------|
| `MUFUTU-Setup-*-x64.exe` | electron-builder **NSIS** | Instalador completo |
| `MUFUTU-*-win-x64.appx` | electron-builder **appx/MSIX** | Empresas / Store sideload |
| `MUFUTU-*-win-x64.zip` | electron-builder zip | Portátil |

Código-fonte do shell: repositório privado `mufutu` → `apps/electron/` (`electron-builder.json` targets nsis + appx + zip).

Auto-update: `electron-updater` (`electron-update.js`) com canal GitHub Releases `mufutusoftware`.

API Luachimo: `~/AppData/.../api-config.json` → `{ "apiOrigin": "https://sml.api.mufutu.ao" }` ou `MUFUTU_API_URL`.

## Legado: WPF (.NET 8)

O cliente **WPF** (`apps/desktop-win`) permanece em Releases ZIP para instalações existentes. **Não** recebe write-offline novo — cutover para Electron após 2 releases estáveis.

| Tipo | Ficheiro |
|------|----------|
| Portátil legado | `MUFUTU-*-win-x64.zip` (WPF) |

## Instalação

1. Execute o instalador NSIS (recomendado) ou sideload MSIX.
2. Aceite o [EULA](../EULA.md).
3. Login com credenciais do tenant.

### TI / silenciosa (NSIS)

```powershell
.\MUFUTU-Setup-1.0.x-x64.exe /S
```

## Segurança

- Assinatura Authenticode quando certificados disponíveis no CI.
- TLS para a API do tenant.
- «MUFUTU 500» no desktop = erro da UI web embutida (não código nativo) — ver Error Boundaries no CMMS.

## Actualizações

Version gate + electron-updater; sem rede o bloqueio **não** activa (offline-first).
