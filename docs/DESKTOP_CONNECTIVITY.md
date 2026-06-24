# Conectividade Desktop — Windows (WPF) e macOS (Electron)

Guia de rede, caminhos de pedidos e testes locais para clientes desktop MUFUTU.

## Resumo

| Plataforma | Stack | API produção | Proxy local |
|------------|-------|--------------|-------------|
| **Windows** | WPF + `HttpClient` | `https://api.mufutu.ao/api` | Não — pedidos directos HTTPS |
| **macOS** | Electron 42 + Next.js embebido | `https://api.mufutu.ao` (via proxy) | `launch-server.js` :3847 → `/api/*` |

Ambos usam **rede do sistema** (Wi-Fi/Ethernet) e **definições de proxy do SO** — sem `localhost` hardcoded em builds Release.

---

## Windows (WPF)

### Caminho do pedido

```
LoginWindow → LoginViewModel → IMufutuApiClient.LoginAsync()
  → HttpClient (BaseAddress: https://api.mufutu.ao/api/)
  → HttpClientHandler (proxy Windows por omissão)
  → DNS sistema → TLS 1.2/1.3 → api.mufutu.ao:443
  → POST /api/auth/login
```

Outros módulos (dashboard, OTs, activos) seguem o mesmo `HttpClient` com `Authorization: Bearer`.

### Configuração

| Variável | Release | Debug |
|----------|---------|-------|
| `MUFUTU_API_URL` | override opcional | override opcional |
| (default) | `https://api.mufutu.ao/api` | `http://localhost:6000/api` |
| `MUFUTU_SITE_CODE` | `MUA` | `MUA` |

### Logs de diagnóstico

- Ficheiro: `%LocalAppData%\MUFUTU\desktop.log`
- Erros de rede: `MufutuApiLoggingHandler` + mensagens em português no ecrã de login

### Testar localmente

**API produção (qualquer OS com PowerShell ou curl):**

```powershell
cd apps/desktop-win
./scripts/test-api-connectivity.ps1
# ou
curl -sS https://api.mufutu.ao/api/health
```

**Dev com API local:**

```powershell
$env:MUFUTU_API_URL = "http://localhost:6000/api"
dotnet run --project src/Mufutu.Desktop/Mufutu.Desktop.csproj
```

**Login produção (Release build):**

```powershell
dotnet run -c Release --project src/Mufutu.Desktop/Mufutu.Desktop.csproj
# Credenciais: admin@mufutu.ao / Admin@2025
```

### CI

O workflow `.github/workflows/desktop-win.yml` executa `test-api-connectivity.ps1` após os testes unitários.

---

## macOS (Electron)

### Caminho do pedido (produção, app empacotada)

```
BrowserWindow → axios/fetch (NEXT_PUBLIC_API_URL=/api)
  → http://127.0.0.1:3847/api/*
  → launch-server.js (proxy frontal)
  → resolve IPv4 de api.mufutu.ao (electron-ipv4.js)
  → HTTPS → api.mufutu.ao:443/api/*
```

UI Next.js corre em porta interna (`MUFUTU_INTERNAL_PORT`, default 3848); só o proxy frontal fala com a internet.

### Configuração

| Origem | Ficheiro / var |
|--------|----------------|
| Subsidiária LUC | `~/Library/Application Support/MUFUTU/api-config.json` → `{ "apiOrigin": "https://luc.api.mufutu.ao" }` |
| Ambiente | `MUFUTU_API_URL` ou `API_INTERNAL_URL` antes de abrir a app |
| Fallback | `https://api.mufutu.ao` (se API local :6000 não responder) |

### Logs

- `~/Library/Application Support/MUFUTU/desktop.log` — arranque, health probe IPv4, erros Next
- Consola: `[mufutu-launch]`, `[mufutu-labels]`, `[next]`

### Testar localmente

```bash
# Health API externa
curl -sS https://api.mufutu.ao/api/health

# Simular proxy IPv4 (dev)
cd apps/web
API_INTERNAL_URL=https://api.mufutu.ao node launch-server.js
# Noutro terminal:
curl -sS http://127.0.0.1:3847/api/health
```

**App Electron dev:**

```bash
npm run dev:web   # :3000
# noutro terminal, com API remota:
MUFUTU_API_URL=https://api.mufutu.ao npm run electron -w @mufutu/web
```

Reconstruir DMG: `apps/desktop-mac/scripts/package.sh`

---

## Base de dados local (IndexedDB) — Web e Electron

A UI Next.js (browser e app macOS Electron) usa **Dexie/IndexedDB** (`MufutuDB`) para cache e escrita offline. Não é necessário SQLite no processo principal — o renderer Chromium persiste dados em disco.

### Onde ficam os dados

| Plataforma | Armazenamento | Caminho típico |
|------------|---------------|----------------|
| **Browser** | IndexedDB por origem | DevTools → Application → IndexedDB → `MufutuDB` |
| **macOS Electron** | IndexedDB no perfil Chromium | `~/Library/Application Support/MUFUTU/IndexedDB/` (junto a `desktop.log`) |
| **Windows WPF** | SQLite próprio (`apps/desktop-win`) | `%LocalAppData%\MUFUTU\` — **não** partilha IndexedDB da web |

O Electron **não apaga** IndexedDB ao fechar; `userData` (`Application Support/MUFUTU`) persiste entre arranques.

### Fluxo offline → sync

```
Acção UI (criar OT / PT / activo)
  → offline-api.ts grava em IndexedDB (_syncStatus: pending)
  → syncQueue (prioridade: OT/PT = 1)
  → SyncProvider pill: «X alterações por sincronizar»
  → Ao ficar online: sync-engine.ts processa fila → API NestJS
  → IDs temporários (*-offline-*) substituídos pelos IDs do servidor
```

**Módulos com escrita offline ligada aos hooks principais (`lib/hooks.ts`):** activos, ordens de trabalho, pedidos (PT), movimentos de stock, combustível (compra/consumo).

**Leitura offline:** listagens acima caem para IndexedDB se a API falhar ou `navigator.onLine === false`.

**UI:** painel flutuante (canto inferior direito), `/settings/offline`, `/system/connectivity` — botão «Sincronizar Agora».

**Service Worker:** activo no browser PWA; **desactivado na app desktop** (IndexedDB + sync-engine bastam; evita conflito com `file://`/proxy local).

### Testar offline → sync

1. Login na app (web ou Electron).
2. DevTools → Network → **Offline** (ou desligar Wi-Fi).
3. Criar uma OT em `/work-orders` ou PT em `/maintenance/requests` (ou `/campo/avaria`).
4. Confirmar pill: «1 alteração por sincronizar».
5. Voltar **Online** → aguardar sync automático (30 s) ou clicar «Sincronizar Agora».
6. Verificar registo no servidor com número real (não `OT-OFF-*` / `PT-OFF-*`).

Sincronização completa (pull): `/settings/offline` → «Sincronização Completa» descarrega activos, OTs, PTs e peças.

---

## QR / Etiquetas — hardware real

### Browser vs desktop

| Acção | Browser | Desktop (Electron) |
|-------|---------|-------------------|
| Estado sincronizado | API `/labels/connection/*` | Bridge nativa + heartbeat API |
| Scan WiFi | Stub (lista vazia) | `airport -s` (macOS) / `netsh` (Win) |
| Ethernet host | Leitura API | `os.networkInterfaces()` |
| Impressora Zebra | Só guardar IP na API | TCP 9100 ZPL + Link-OS HTTP |
| NFC | Web NFC (Chrome Android) | Web NFC se disponível; paridade via API |

### Caminho bridge (desktop)

```
/qr/connection/* → hasLabelBridge() / window.labelsBridge (preload.js)
  → ipcMain → electron-labels-bridge.js
  → OS (WiFi/Ethernet) ou rede LAN (impressora)
  → labelsConnectionApi.heartbeat() → POST /api/labels/connection/heartbeat
```

O componente `LabelHardwareHint` oculta-se quando `canAccessLabelHardware()` detecta a bridge. `LabelHardwareSync` envia heartbeat a cada 30 s.

### Verificar bridge no desktop

1. Abrir `/qr/connection` na app empacotada
2. Confirmar ausência do aviso «requer app MUFUTU»
3. «Procurar redes» WiFi deve listar SSIDs reais (macOS: permissão Local Network pode ser pedida)
4. Logs: `[mufutu-labels] scan WiFi (darwin)`

---

## Checklist rápido

- [ ] `curl https://api.mufutu.ao/api/health` → `{"status":"ok",...}`
- [ ] Windows: `%LocalAppData%\MUFUTU\desktop.log` sem erros de rede após login
- [ ] macOS: `desktop.log` mostra `Health probe api.mufutu.ao → x.x.x.x:443`
- [ ] QR desktop: scan WiFi devolve redes; impressão exige IP Zebra na LAN

Ver também: [`WINDOWS_DESKTOP.md`](./WINDOWS_DESKTOP.md) · [`DESKTOP_MAC.md`](./DESKTOP_MAC.md)
