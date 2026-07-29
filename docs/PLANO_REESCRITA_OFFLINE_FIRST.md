# MUFUTU — Plano de Reescrita Offline-First (v2)

> Estado: proposta · 25/07/2026 · repo de distribuição: `mufutusoftware`
> Alvos: **Windows**, **macOS**, **Web** e **App Modo Campo** (Android/iOS)
> Princípio único: **o offline é o modo normal de funcionamento**, não uma
> degradação. Na mina do Luachimo a rede é intermitente — o software tem de
> trabalhar igual com e sem ligação.

---

## 1. Porque recomeçar

O que existe hoje funciona, mas cresceu por camadas e paga juros técnicos:

| Problema | Custo actual |
| --- | --- |
| **4 clientes com código próprio** (Next.js web, WPF Windows, Electron macOS, .NET MAUI) | a mesma funcionalidade é escrita 2–4 vezes; correcções aplicam-se a uns e não a outros |
| **Offline "por cima"** (Dexie + fila no web, SQLite no MAUI; Electron partilha Dexie IndexedDB durable em `userData` — ver `mufutu/docs/DESKTOP_OFFLINE_TELEMETRY.md`) | regras de conflito diferentes por cliente; o que funciona no telemóvel falha no PC |
| **Estado em `localStorage`** (parametrizações, planos, overrides) | dados presos ao browser de um utilizador; perdem-se ao trocar de máquina |
| **Contrato de API implícito** | DTOs mudam e os clientes só falham em produção (ex.: `metadata` vs `specifications`, 400 silencioso) |
| **Sem testes de sincronização** | cada regressão de sync é descoberta pelo utilizador na mina |

A reescrita não deita fora o conhecimento do domínio (regimes de férias, D/A/R,
período 16→15, categorias de frota) — **reaproveita-o como pacote testado**.

---

## 2. Arquitectura alvo

```
mufutusoftware/
├── packages/
│   ├── domain/        # regras puras (TS, zero I/O): férias, disponibilidade,
│   │                  # MTBF/MTTR, categorias, período 16→15 · 100% testado
│   ├── contract/      # OpenAPI + tipos e clientes gerados (fonte da verdade)
│   ├── sync/          # motor offline-first partilhado (outbox, LWW, retry)
│   ├── db/            # esquema local (SQLite/WASM) + migrações
│   └── ui/            # design system: tokens, componentes, ícones Lucide
├── apps/
│   ├── web/           # PWA (browser + instalável)
│   ├── desktop/       # Tauri → Windows + macOS (1 código, 2 instaladores)
│   ├── field/         # App Modo Campo (Android/iOS)
│   └── api/           # NestJS + PostgreSQL (servidor)
└── docs/              # este plano, ADRs, runbooks
```

**Decisões-chave**

1. **Um só motor de sync** (`packages/sync`) usado por web, desktop e campo.
   Regras de conflito escritas uma vez e testadas uma vez.
2. **Uma só base local**: SQLite em todo o lado — nativo no desktop/telemóvel,
   WASM (OPFS) no browser. Fim de "Dexie aqui, SQLite ali".
3. **Desktop com Tauri**: substitui WPF + Electron. Um código, instaladores
   `.msi`/`.exe` (Windows) e `.dmg` (macOS), binários ~10 MB em vez de ~150 MB.
4. **Contrato primeiro**: OpenAPI no `packages/contract`; clientes gerados.
   Um campo que muda no servidor parte a compilação, não a produção.
5. **Domínio puro e partilhado**: as regras Luachimo deixam de estar copiadas
   entre web e MAUI.

---

## 3. Como funciona o offline-first

**Modelo: local-first com outbox.** A aplicação **lê e escreve sempre na base
local**. A rede é um detalhe de fundo.

```
Utilizador → escreve na SQLite local → UI actualiza (instantâneo)
                     ↓
                  outbox (fila persistente)
                     ↓ quando há rede
              POST /sync/push  →  servidor aplica e devolve deltas
                     ↓
              pull deltas → merge local → UI actualiza
```

- **Identificadores**: UUID v7 gerados no cliente — sem esperar pelo servidor,
  sem IDs temporários a reconciliar (o bug dos `asset-offline-*`).
- **Conflitos**: LWW por campo com relógio do servidor + registo do que foi
  sobreposto. Casos com significado operacional (ex.: dois estados D/A/R para
  o mesmo dia) vão para uma **fila de revisão** em vez de decidirem sozinhos.
- **Anexos** (fotos de guias, selfies de presença): guardados no disco local e
  enviados em segundo plano com retoma; o registo não espera pela foto.
- **Visibilidade**: barra de estado permanente — "12 alterações por enviar ·
  última sincronização há 4 min" — e nunca um ecrã em branco por falta de rede.

**Garantia testável:** suite de sincronização que corre em CI simulando avião
(sem rede), rede fraca, e dois dispositivos a editar o mesmo registo.

---

## 4. Fases

### Fase 0 — Fundação (2 semanas)
- Monorepo `mufutusoftware` (pnpm + Turborepo), CI com lint/testes/build.
- `packages/domain` com as regras já existentes **migradas com testes**.
- `packages/contract`: OpenAPI da API actual + geração de clientes.
- ADRs das 5 decisões-chave.
- *Saída:* pacotes publicados internamente; API actual continua a servir produção.

### Fase 1 — Motor de sync + base local (3 semanas)
- `packages/db` (SQLite + migrações) e `packages/sync` (outbox, LWW, retry).
- Endpoints `/sync/push` e `/sync/pull` v2 com cursores e deltas por tabela.
- Suite de testes de sincronização (avião, rede fraca, 2 dispositivos).
- *Saída:* motor com cobertura de testes, ainda sem UI.

### Fase 2 — App Modo Campo (4 semanas) ⭐ primeiro alvo
- É onde o offline mais dói → maior retorno imediato.
- Fluxos: OTs, avaria, checklist, presença (selfie + GPS), assiduidade, horímetro.
- Instalação lado a lado com a app actual; piloto com 5 técnicos, 2 semanas.
- *Saída:* app em produção para uma equipa, métricas de sync reais.

### Fase 3 — Desktop Tauri (3 semanas)
- Windows + macOS a partir do mesmo código, com a mesma base local.
- Actualização automática assinada; instaladores no repo de distribuição.
- *Saída:* substitui WPF e Electron.

### Fase 4 — Web PWA (3 semanas)
- Mesmos pacotes; SQLite WASM + OPFS; instalável no browser.
- Migração módulo a módulo (frota → trabalho → pessoal → materiais).
- *Saída:* paridade com o web actual, agora offline a sério.

### Fase 5 — Corte e desativação (2 semanas)
- Migração de dados presos em `localStorage` (parametrizações, planos) → API.
- Congelar o código antigo, redirecionar, arquivar.

**Total: ~17 semanas**, com valor entregue a partir da semana 9 (app de campo).

---

## 5. Regras de organização (para não repetir a história)

1. **Nada de estado de negócio em `localStorage`** — só preferências de UI.
2. **Uma regra, um sítio**: se aparece em dois clientes, vive em `packages/domain`.
3. **Sem mocks no código de produção** — dados vazios mostram estado vazio.
4. **PDF/Excel sempre vectorial** a partir dos dados (nunca captura de ecrã).
5. **pt-AO em toda a UI**; ícones Lucide, zero emojis.
6. **Toda a escrita passa pelo outbox** — sem chamadas directas à API na UI.
7. **Migrations registadas explicitamente** e testadas em CI contra BD limpa.
8. **Cada PR responde**: funciona sem rede? o que acontece se sincronizar duas vezes?

---

## 6. Riscos

| Risco | Mitigação |
| --- | --- |
| Reescrita "big bang" que nunca chega ao fim | fases entregáveis; app de campo em produção na semana 9 |
| Duas versões em paralelo a divergir | congelar features no antigo; só correcções críticas |
| SQLite WASM no browser (maturidade) | prova de conceito na Fase 1; alternativa: IndexedDB com a mesma interface |
| Curva do Tauri/Rust | só a casca é Rust; a aplicação é a mesma UI web |
| Perda de conhecimento do domínio | Fase 0 migra as regras **com testes** antes de qualquer UI nova |

---

## 7. Primeiro passo concreto

1. Criar `packages/domain` e mover para lá, **com testes**, quatro regras:
   regimes de férias (2/21 · 6/21 · 12/30), período de assiduidade 16→15,
   carry-forward D/A/R, categorias de frota Luachimo.
2. Gerar o OpenAPI da API actual e publicar `packages/contract`.
3. Escrever o ADR-001 (offline-first local-first) e o ADR-002 (Tauri).

Isto não parte nada em produção e já elimina a duplicação mais cara.
