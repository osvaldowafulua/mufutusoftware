# Super Plano de Melhoria — MUFUTU Software

> Plano de estabilização e evolução dos clientes nativos MUFUTU (Windows, macOS,
> Android/iOS) e da distribuição via GitHub. Actualizado a **2026-07-09**.
> Mapa geral do produto: [MAPA-MENTAL.md](MAPA-MENTAL.md).

## Sumário executivo

| # | Problema | Causa raiz identificada | Estado |
|---|----------|------------------------|--------|
| 1 | **APK Android nunca abre** (trava na splash) | Arranque constrói as 7 páginas de uma vez + AOT/trimming em Release + zero captura de erros (impossível diagnosticar no terreno) | Correcções aplicadas — validar no dispositivo |
| 2 | **Windows perdeu o Setup instalável** (só ZIP) | Build WiX falhava sempre em CI: `ComponentGroup AppFiles` nunca foi definido, o projecto MSI compilava também o `Bundle.wxs` (globbing), faltava a extensão BAL — e o fallback silencioso publicava só o ZIP | Corrigido — validar no CI |
| 3 | **macOS abre "segunda app"** além da principal | O servidor Next.js embebido é lançado com `spawn` do próprio binário; se `ELECTRON_RUN_AS_NODE` se perde, nasce uma 2.ª instância GUI | Guarda aplicada — validar no dispositivo |
| 4 | **Sem armazenamento geral local** (dados repetidos, offline fraco) | Clientes puxam listas completas sempre; não há sync delta ("só o que mudou") | Arquitectura definida — Fase 2 |
| 5 | **Logos desactualizados** | — | ✅ Feito (SVG novos + cor `#EB5E28` em todas as apps) |
| 6 | **GitHub desorganizado / sem mapa mental** | — | ✅ Feito (docs + mapa mental Mermaid) |

---

## Fase 0 — Aplicado já (esta PR)

### Rebrand completo
- Novos SVG oficiais em [`assets/brand/svg/`](../assets/brand/README.md) (7 variantes).
- Cor de marca actualizada `#E8612D` → **`#EB5E28`** em: MAUI (ícone, splash, XAML), Electron (splash, janelas), WPF (assets regenerados).
- Ícones/splash regenerados: `icon.png/.icns/.ico`, `splash-logo.png`, assets WPF e MAUI.
- README com logo novo; `assets/brand/README.md` documenta cada variante.

### Android (correcções de fiabilidade no arranque)
- **`CrashLog`**: handlers globais (`AndroidEnvironment`, `AppDomain`, `TaskScheduler`) gravam qualquer excepção em ficheiro; no arranque seguinte a app mostra o erro da sessão anterior — fim dos crashes "cegos" no terreno.
- **AppShell lazy**: as 7 páginas passaram a `ContentTemplate` (DataTemplate) — nada é construído na splash; um erro numa página deixa de matar a app inteira no arranque.
- **AOT perfilado desligado** em Release Android (JIT puro é mais fiável; custo: arranque um pouco mais lento).
- **Build de diagnóstico**: `dotnet publish -p:MufutuDiag=true` (ou input `diagnostico` no workflow) desliga o trimming para isolar crashes de linker.

### Windows (Setup instalável de volta)
- `Package.wxs`: definido o `ComponentGroup AppFiles` (harvest automático do publish via `<Files>`), **atalhos** Menu Iniciar + Ambiente de trabalho, versão vinda do build.
- `Mufutu.Package.wixproj` / `Mufutu.Bundle.wixproj`: globbing desligado (o MSI já não tenta compilar o Bundle), extensão BAL adicionada, constantes `Version`/`PublishDir`.
- `package.ps1`: publish **self-contained** (runtime .NET incluído — minas sem internet), artefactos com nome versionado (`MUFUTU-Setup-{v}-x64.exe`, `MUFUTU-{v}-x64.msi`, ZIP), e **falha ruidosa** se o WiX não produzir o Setup (fim do fallback silencioso).

### macOS
- Guarda no `electron-main.js`: se o processo filho do servidor arrancar como GUI (env `MUFUTU_SERVER_CHILD` sem `ELECTRON_RUN_AS_NODE`), sai imediatamente — sem segunda app no Dock.

### GitHub
- [`docs/MAPA-MENTAL.md`](MAPA-MENTAL.md) (Mermaid, renderizado pelo GitHub), este plano, índice de docs e README reorganizado.

---

## Fase 1 — Estabilizar (P0, 1–2 semanas)

### 1.1 Android: confirmar e fechar o crash de arranque
1. Correr o workflow **Mobile MAUI** com `diagnostico=true` → instalar o APK `-diag` num dispositivo real (Android 10+).
2. Se o diag abrir e o normal não → é trimming: adicionar `TrimmerRootAssembly` para `SocketIOClient`/`System.Text.Json` em vez de desligar o trimming globalmente.
3. Se nenhum abrir → ler `mufutu-crash.log` (agora existe) via ecrã de arranque ou `adb pull /data/data/com.mufutu.mobile/files/.local/share/mufutu-crash.log`; com `adb logcat -s DOTNET AndroidRuntime` confirmar stack.
4. Suspeitos já identificados no código: `Microsoft.Data.Sqlite` (provider nativo e_sqlite3 no arranque), `SocketIOClient` (reflexão JSON), versões `Microsoft.Extensions.*` 8.0.1 vs MAUI 8.0.100.
5. Assinar o APK com keystore próprio (hoje sai com assinatura de debug do CI) e guardar o keystore em `secrets`.
6. **Critério de aceitação**: APK Release abre em < 3 s num Android 10 low-end, login offline e online, 0 crashes em 20 arranques.

### 1.2 Windows: validar o novo Setup em CI
1. Disparar o workflow **Windows Desktop (WPF)** manualmente → confirmar que saem `MUFUTU-Setup-{v}-x64.exe` + `.msi` + ZIP.
2. Testar em VM limpa (sem .NET): instalar → atalhos → abrir → desinstalar em Definições→Aplicações.
3. Publicar release `desktop-win/v1.0.20` e actualizar a tabela de downloads do README + `windows/INSTALAR.md`.
4. Médio prazo: certificado de assinatura de código real (EV ou OV) — remove o aviso SmartScreen; considerar publicação `winget`.
5. **Critério de aceitação**: instalar/actualizar/desinstalar limpo em Windows 10 e 11 sem .NET pré-instalado.

### 1.3 macOS: fechar a "segunda app"
1. Reconstruir o DMG com a guarda e testar: `open /Applications/MUFUTU.app` → só 1 ícone no Dock.
2. Se ainda aparecer um segundo processo visível, identificar o nome exacto em Monitor de Actividade (provável "MUFUTU Helper" do Chromium — cosmético e normal) e registar em `docs/DESKTOP_MAC.md`.
3. Verificar `~/Library/Application Support/MUFUTU/desktop.log` — o arranque do servidor deve registar "spawn oculto".
4. **Critério de aceitação**: 1 ícone no Dock, servidor local activo na porta 3847, app funcional offline→online.

---

## Fase 2 — Armazenamento Geral Local + Sync Delta (P1, 3–6 semanas)

Objectivo: **todas as apps descarregam o conjunto completo de dados do tenant uma
vez, e depois só pedem o que mudou** — funcionam igual online e offline.

### 2.1 Contrato de API (repo `mufutu`, CMMS ≥ 1.2)
- Cada entidade sync-ável expõe `GET /api/{entidade}?updatedSince={cursor}&limit={n}`:
  - resposta: `{ items: [...], deleted: [ids], serverTime, nextCursor }`;
  - `updated_at` indexado + soft-delete (`deleted_at`) em todas as tabelas sync-áveis;
  - ETag/`If-None-Match` nos endpoints de catálogo (304 = nada mudou, zero payload).
- Entidades da 1.ª vaga: ordens de trabalho (OT/PT), activos, checklists, planos PM,
  utilizadores/atribuições, tabelas de referência (sites, categorias, prioridades).
- Endpoint de bootstrap: `GET /api/sync/snapshot` (dump comprimido por tenant) para o
  primeiro download completo em redes lentas.

### 2.2 Motor local comum (conceito "MufutuStore")
Uma base SQLite local por dispositivo com o mesmo esquema lógico em todas as apps:

```
tables:
  entities(id, type, json, updated_at, deleted, synced_at)
  sync_cursors(entity_type, cursor, last_full_sync)
  outbox(id, entity_type, operation, payload, retry_count, created_at)
```

Ciclo de sincronização (igual em todas as plataformas):
1. **Push** primeiro: esvaziar `outbox` (já existe fila no MAUI e WPF — reutilizar).
2. **Pull delta**: por entidade, pedir `?updatedSince=cursor`; upsert local; aplicar `deleted`.
3. Actualizar cursor com `nextCursor`/`serverTime` (nunca o relógio do dispositivo).
4. Conflitos: *server-wins* por omissão; operações de campo críticas (fecho de OT,
   avaria) preservam a versão local em `outbox` até o servidor aceitar.
5. Gatilhos: arranque, reconexão (já monitorizada), timer 30 s (mobile) / 5 min (desktop), acção manual "Sincronizar".

### 2.3 Aplicação por plataforma
| Plataforma | O que mudar |
|------------|-------------|
| **MAUI (mobile)** | Evoluir `CampoOfflineStore`/`CampoSyncEngine`: adicionar tabela `sync_cursors`, generalizar `entities` (hoje só OT/notificações), pull delta em vez de `GetMyWorkOrdersAsync` completo |
| **WPF (Windows)** | `OfflineStore` já tem SQLite+AES — adicionar as mesmas tabelas e um `SyncService` com o ciclo acima |
| **Electron/Web (macOS + browser)** | No CMMS: Service Worker + IndexedDB (Dexie) com o mesmo protocolo delta; o vault local do Electron continua para credenciais |
| **iOS (MAUI)** | Herda o motor do MAUI Core sem trabalho extra |

### 2.4 Critérios de aceitação
- 2.º arranque online transfere **< 5%** dos bytes do 1.º (só deltas/304).
- Criar avaria offline → reconectar → visível no web em < 1 min.
- Perda de rede a meio do sync não corrompe dados (transacções SQLite).
- Modo avião: todas as listas da 1.ª vaga consultáveis.

---

## Fase 3 — Distribuição e auto-update (P2, 2–3 semanas)

- **Windows**: auto-update do WPF via `GitHubDesktopUpdateService` a apontar para releases `desktop-win/v*`; instalador silencioso `/quiet` para IT.
- **macOS**: manter electron-updater; notarização Apple obrigatória em todos os releases (remove o passo `xattr -cr` do README).
- **Android**: keystore de produção + trilha interna na Play Store (ou canal empresarial MDM); versão mínima validada (API 29).
- **Checksums**: `checksums.sha256` gerado no CI para todos os artefactos (hoje é manual).
- **Compatibilidade**: gate no CI que valida a matriz de `docs/COMPATIBILITY.md` contra a versão da API de produção.

## Fase 4 — Qualidade contínua (P2/P3, contínuo)

- Testes de fumo de arranque no CI: Android (emulador headless `maui-android`), Windows (lançar exe + esperar janela), macOS (abrir app + probe porta 3847).
- Crash reporting opt-in (Sentry self-hosted ou similar) alimentado pelo `CrashLog` já criado.
- Telemetria mínima de sync (duração, bytes, erros) para afinar o delta.
- Actualizar MAUI 8 → 9 quando o CMMS 1.2 fechar (ganhos grandes de estabilidade Android).
- Consolidar os 2 pipelines Windows (WPF actual vs Electron legado) — arquivar `desktop-electron-win.yml` quando o Setup WPF estiver publicado.

---

## Cronograma sugerido

| Semana | Entrega |
|--------|---------|
| 1 | Fase 1 completa: APK a abrir + Setup Windows publicado + DMG validado |
| 2–3 | API delta no CMMS (`updatedSince`, soft-delete, snapshot) |
| 3–5 | MufutuStore no MAUI + WPF; release mobile 1.1.0 e desktop 1.1.0 |
| 5–6 | Service Worker/IndexedDB no web/Electron; release 1.1.x |
| 7+ | Fase 3 (distribuição) e Fase 4 (qualidade contínua) |

## Riscos e mitigação

| Risco | Mitigação |
|-------|-----------|
| Crash Android tiver causa diferente (device-specific) | `CrashLog` + build diag dão o stack em minutos; matriz de dispositivos de teste (1 low-end + 1 recente) |
| WiX falhar no runner Windows por detalhe de validação (ICE) | Validação ICE suprimida no projecto; `verify-artifact.ps1` valida o resultado; testar via `workflow_dispatch` antes do tag |
| API delta atrasar (repo mufutu) | O motor local funciona com fallback "pull completo + upsert" até a API 1.2 chegar — já reduz repetição via cache |
| Trimming voltar a partir Release móvel | Build diag permanente no workflow + smoke test de arranque no emulador (Fase 4) |
