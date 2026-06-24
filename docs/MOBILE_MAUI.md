# MUFUTU Mobile — .NET MAUI

Cliente **Android e iOS** para técnicos em campo (Modo Campo). Substitui o stub React Native em `apps/mobile/`.

| Item | Valor |
|------|--------|
| **Solution** | `apps/mobile-maui/Mufutu.Mobile.sln` |
| **Framework** | .NET 8 + MAUI 8 |
| **Bundle ID** | `com.mufutu.mobile` |
| **API** | `https://api.mufutu.ao/api` (Debug: `MUFUTU_API_URL`) |

---

## 1. Visual Studio 2022 (Windows — Android + emulador)

### Pré-requisitos

1. [Visual Studio 2022](https://visualstudio.microsoft.com/) 17.10+ com workload **«Desenvolvimento de .NET Multiplataforma com .NET MAUI»**
2. **SDK .NET 8** + workload MAUI (`maui-android`, `maui-ios` no Mac)
3. Android SDK API 29+ (instalado pelo VS Installer)
4. Para iOS: **Mac com Xcode 15+** + pairing VS → Mac (ou build só no CI macOS)

### Abrir o projecto

```
Ficheiro → Abrir → Projecto/Solução
→ mufutusoftware/apps/mobile-maui/Mufutu.Mobile.sln
```

| Projecto | Função |
|----------|--------|
| `Mufutu.Mobile` | App MAUI (UI + plataforma) |
| `Mufutu.Mobile.Core` | API HTTP, probe `/health`, opções |
| `Mufutu.Mobile.Tests` | Testes de conectividade (xUnit) |

### Executar

1. Barra superior: destino **Android Emulator** ou dispositivo USB
2. `F5` (Debug) ou `Ctrl+F5`
3. Ecrã inicial: **login** → hub **MUFUTU Campo** (OTs, avaria, checklist, sync)

### Modo Campo (paridade web `/campo`)

| Ecrã | Função |
|------|--------|
| **Login** | JWT Bearer + site MUA/LUC/MIL (`SecureStorage`) |
| **Hub** | Atalhos + badge notificações + estado offline/pendentes |
| **Minhas OTs** | API + **cache SQLite** · Começar / Terminei **offline** (fila sync) |
| **Avaria** | Câmara + motivo → envio imediato ou **fila offline** |
| **Checklist** | Templates locais (bulldozer, escavadora, …) |
| **Notificações** | REST + **Socket.IO** `/notifications` · cache local · alerta no telemóvel |
| **Enviar** | Sincronizar fila + probe `/health` + sair |

### Offline-first (SQLite `mufutu_campo.db`)

- OTs em cache — lista disponível sem rede
- Fila `sync_queue` — mudanças de estado OT + PTs com foto
- Reconexão automática (30s + evento `ConnectivityChanged`) envia pendentes
- Refresh token JWT em pedidos 401

### Notificações

- `GET /notifications` — lista + cache local (lidas offline)
- WebSocket `wss://…/notifications` com Bearer token
- Notificações locais nativas (Android NotificationCompat + iOS UNUserNotificationCenter) — alerta quando chega OT nova
- Ecrã **Notificações** no hub (ícone 🔔 / contador)

### iPhone — notch / Dynamic Island

Todos os ecrãs Campo usam `FieldCampoPage` com **`SafeAreaEdges="All"`**: o header e a barra inferior ficam **abaixo da câmara frontal** e **acima do home indicator**, sem conteúdo cortado.


### Variáveis de debug

| Variável | Exemplo | Uso |
|----------|---------|-----|
| `MUFUTU_API_URL` | `http://10.0.2.2:6000/api` | API local no emulador Android |
| `MUFUTU_API_URL` | `http://localhost:6000/api` | API local no simulador iOS |

---

## 2. Visual Studio for Mac / `dotnet` CLI

> **Repo:** `mufutusoftware` (software para download) — **não** `mufutu` (CMMS).  
> Erros `cd` / workload: **`apps/mobile-maui/BUILD-LOCAL.md`**

```bash
cd /Users/fluadigital/Documents/GitHub/mufutusoftware/apps/mobile-maui

# Workloads (uma vez) — SDK Microsoft, não Homebrew; ver BUILD-LOCAL.md
sudo dotnet workload install maui-android maui-ios

dotnet restore Mufutu.Mobile.sln
dotnet test tests/Mufutu.Mobile.Tests/Mufutu.Mobile.Tests.csproj -c Release

# Android APK
bash scripts/package-android.sh 1.0.0

# iOS (só macOS + Xcode)
dotnet build src/Mufutu.Mobile/Mufutu.Mobile.csproj -f net8.0-ios -c Release
```

---

## 3. Rede — Wi‑Fi e Internet

### Permissões Android (`AndroidManifest.xml`)

- `INTERNET`
- `ACCESS_NETWORK_STATE`
- `ACCESS_WIFI_STATE`
- `CHANGE_WIFI_STATE`
- `CAMERA` (avaria com foto)

`network_security_config.xml`: HTTPS obrigatório em produção; HTTP permitido só para `localhost`, `127.0.0.1`, `10.0.2.2` (emulador).

### iOS (`Info.plist`)

- **ATS** activo (`NSAllowsArbitraryLoads = false`)
- Excepção HTTP apenas para `localhost` (dev)
- `NSCameraUsageDescription` — fotografar avarias

### Código

- `MauiNetworkStatusProvider` — `Connectivity.Current` (Wi‑Fi vs cellular vs offline)
- `ConnectivityProbeService` — `GET {ApiBaseUrl}/health` com timeout 20s e header `X-Site-Id`

---

## 4. Testes de conectividade (automatizados)

| Camada | Comando | O que valida |
|--------|---------|--------------|
| **Unitários** | `dotnet test tests/Mufutu.Mobile.Tests` | Offline, HTTP 200, perfil Wi‑Fi/cellular (mock) |
| **API produção** | `bash scripts/test-api-connectivity.sh` | `curl` → `https://api.mufutu.ao/api/health` |
| **Windows CI** | `scripts/test-api-connectivity.ps1` | Mesmo probe em runners Windows |
| **App** | Hub Campo → Enviar → Testar ligação | Rede real do dispositivo |

---

## 5. Pipeline CI (GitHub Actions)

Workflow: **`.github/workflows/mobile-maui.yml`**

| Job | Runner | Resultado |
|-----|--------|-----------|
| `test-connectivity` | `ubuntu-latest` | xUnit + health API |
| `build-android` | `windows-latest` | APK Release |
| `build-ios` | `macos-latest` | Build `net8.0-ios` + health API |
| `release` | tag `mobile-maui/v*` | Upload APK no GitHub Release |

### Disparar manualmente

Actions → **Mobile MAUI (Android + iOS)** → Run workflow

### Tag de release

No repositório **`mufutusoftware`** (não `mufutu`):

```bash
cd /Users/fluadigital/Documents/GitHub/mufutusoftware
git checkout main && git pull
git tag mobile-maui/v1.0.0
git push origin mobile-maui/v1.0.0
```

---

## 6. Distribuição

| Plataforma | Artefacto CI | Canal |
|------------|--------------|--------|
| **Android** | `MUFUTU.apk` | GitHub Releases · MDM · sideload IT |
| **iOS** | Build validado no CI | TestFlight / App Store (assinatura Apple fora do CI) |

Assinatura release Android/iOS: configurar keystore Apple/Google nos secrets do repo (fase seguinte).

---

## 7. Estrutura

```
apps/mobile-maui/
├── Mufutu.Mobile.sln
├── src/
│   ├── Mufutu.Mobile/           # MAUI UI
│   └── Mufutu.Mobile.Core/      # HTTP + conectividade
├── tests/Mufutu.Mobile.Tests/
├── scripts/
│   ├── package-android.sh
│   ├── test-api-connectivity.sh
│   └── test-api-connectivity.ps1
└── docs/MOBILE_MAUI.md          # este ficheiro
```

---

## 8. Resolução de problemas

| Sintoma | Solução |
|---------|---------|
| `workload maui not recognized` | Actualizar SDK .NET 8.0.400+; `dotnet workload update` |
| Emulador sem rede | Reiniciar emulador; Wi‑Fi do host activo |
| API local no Android | Usar `http://10.0.2.2:6000/api` (não `localhost`) |
| iOS build CI falha assinatura | Normal — CI usa `CodesignKey=-`; assinar no Mac com perfil dev |
| Probe timeout | VPN/firewall; confirmar `curl https://api.mufutu.ao/api/health` |

---

**MUFUTU** é marca da **Smart Cloud, Lda** · Suporte: suporte@mufutu.ao
