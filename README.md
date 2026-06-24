<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="assets/brand/logo-software-white.png" />
    <source media="(prefers-color-scheme: light)" srcset="assets/brand/logo-software-dark.png" />
    <img alt="MUFUTU Software" src="assets/brand/logo-software-banner.png" width="420" />
  </picture>
</p>

<p align="center">
  <strong>Cockpit de operações e gestão de ativos para mineração</strong><br/>
  <sub>CMMS/EAM · Angola · Offline-first · Multi-site MUA · LUC · MIL</sub>
</p>

<p align="center">
  <a href="https://github.com/osvaldowafulua/mufutusoftware/stargazers">
    <img src="https://img.shields.io/github/stars/osvaldowafulua/mufutusoftware?style=for-the-badge&logo=github&label=Stars&color=E8612D" alt="GitHub Stars" />
  </a>
  <a href="https://github.com/osvaldowafulua/mufutusoftware/releases">
    <img src="https://img.shields.io/github/downloads/osvaldowafulua/mufutusoftware/total?style=for-the-badge&logo=download&label=Downloads&color=1565C0" alt="Total Downloads" />
  </a>
  <a href="https://github.com/osvaldowafulua/mufutusoftware/releases/latest">
    <img src="https://img.shields.io/github/v/release/osvaldowafulua/mufutusoftware?style=for-the-badge&label=Release&color=E8612D" alt="Latest Release" />
  </a>
  <a href="https://github.com/osvaldowafulua/mufutusoftware/issues">
    <img src="https://img.shields.io/github/issues/osvaldowafulua/mufutusoftware?style=for-the-badge&color=1565C0" alt="Issues" />
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/badge/licen%C3%A7a-comercial-E8612D?style=for-the-badge" alt="Licença" />
  </a>
</p>

<p align="center">
  <a href="https://github.com/osvaldowafulua/mufutusoftware/releases/latest">
    <img src="https://img.shields.io/badge/⬇_Descarregar-agora-E8612D?style=for-the-badge&logo=github&logoColor=white" alt="Descarregar" />
  </a>
  &nbsp;
  <a href="https://app.mufutu.ao">
    <img src="https://img.shields.io/badge/Web-app.mufutu.ao-1565C0?style=for-the-badge" alt="Web App" />
  </a>
</p>

<p align="center">
  <a href="#descarregar-agora-v105">Downloads</a> ·
  <a href="#estatísticas--comunidade">Estatísticas</a> ·
  <a href="#licenças">Licenças</a> ·
  <a href="EULA.md">EULA</a> ·
  <a href="SECURITY.md">Segurança</a>
</p>

---

## Descarregar agora

| Plataforma | Instalador oficial | Notas |
|------------|-------------------|--------|
| **Windows** | [`MUFUTU-Setup-1.0.6-x64.exe`](https://github.com/osvaldowafulua/mufutusoftware/releases/download/v1.0.6/MUFUTU-Setup-1.0.6-x64.exe) | Assistente de instalação — **use este** |
| **macOS** | [`MUFUTU-1.0.5-arm64.dmg`](https://github.com/osvaldowafulua/mufutusoftware/releases/download/v1.0.5/MUFUTU-1.0.5-arm64.dmg) | Arrastar para Aplicações |
| **Web** | [app.mufutu.ao](https://app.mufutu.ao) | Sem instalador |

> **Não use o ZIP Windows** (`MUFUTU-*-win-x64.zip`) — é versão **portátil** para testes. O utilizador final deve instalar com **`MUFUTU-Setup-*.exe`**. Ver [windows/INSTALAR.md](windows/INSTALAR.md).

Verifique integridade com `checksums.sha256` em cada release.

---

Este repositório é o **código-fonte e distribuição** dos clientes nativos MUFUTU.

| Repositório | Conteúdo |
|-------------|----------|
| **mufutusoftware** (aqui) | Desktop Win/Mac, Electron, mobile, Releases |
| **mufutu** (privado) | CMMS web + API — sem instaladores |

> Binários (`.dmg`, `.exe`) **não** vão em commits — só em [Releases](https://github.com/osvaldowafulua/mufutusoftware/releases).

## Build local

Requer o CMMS ao lado desta pasta:

```
GitHub/
  mufutu/          ← privado (apps/web)
  mufutusoftware/  ← este repo
```

```bash
# macOS DMG
npm run build:mac -- 1.0.6
# ou
bash apps/desktop-mac/scripts/package.sh 1.0.6
```

---

## O que é a MUFUTU?

**MUFUTU** é um sistema **CMMS/EAM** (gestão de manutenção e activos) pensado para
operações mineiras em Angola. Integra num único ambiente:

- **Manutenção** — pedidos de trabalho (PT), ordens de trabalho (OT), PM e checklists
- **Activos** — frota pesada (CAT, Komatsu, Bell…), fichas técnicas e QR
- **Operações** — combustível, paragens, disponibilidade, logística
- **Modo Campo** — interface simplificada para técnicos (PWA e mobile)
- **Multi-site** — MUA, LUC, MIL e tenants dedicados por subsidiária
- **Offline-first** — trabalho em frente sem rede, sincronização automática

A aplicação web principal está disponível em **[app.mufutu.ao](https://app.mufutu.ao)**.
Este repositório é o **canal oficial de distribuição** dos instaladores nativos
(Windows, macOS, Android, iOS).

> **Nota:** Aqui publicamos apenas **produto final compilado**. Não há código-fonte,
> segredos de API nem materiais que facilitem engenharia reversa. Ver [SECURITY.md](SECURITY.md).

---

## Descarregar

Todos os instaladores são publicados em **[GitHub Releases](https://github.com/osvaldowafulua/mufutusoftware/releases)**.

| Plataforma | Ficheiro típico | Documentação |
|------------|-----------------|--------------|
| **Windows** | `MUFUTU-Setup.exe` ou `.msi` | [windows/README.md](windows/README.md) |
| **macOS** | `MUFUTU-*-arm64.dmg` / `.zip` | [macos/README.md](macos/README.md) |
| **Android** | `MUFUTU-*.apk` ou `.aab` (Play Store quando disponível) | [android/README.md](android/README.md) |
| **iOS** | TestFlight / App Store (IPA não sideload público) | [ios/README.md](ios/README.md) |
| **Web / PWA** | Navegador — sem instalador | [web/README.md](web/README.md) |

### Passos rápidos

1. Abra [Releases](https://github.com/osvaldowafulua/mufutusoftware/releases) e escolha a **versão mais recente** estável.
2. Descarregue o pacote da sua plataforma.
3. Verifique o **SHA-256** com o ficheiro `checksums.sha256` do release.
4. Instale e active a **licença** com o administrador da sua empresa (ver abaixo).

### Actualizações automáticas

- **Windows / macOS (desktop):** o cliente verifica novas versões nos Releases oficiais.
- **Android / iOS:** actualizações via loja ou canal empresarial acordado.
- **Web:** sempre a versão mais recente no servidor do seu tenant (sem acção manual).

---

## Licenças

A MUFUTU é software **comercial licenciado**. Cada organização (tenant) necessita de
uma chave no formato:

```
MUFUTU-LIC-xxxxxxxxxxxxxxxx
```

### Como obter

| Perfil | Acção |
|--------|--------|
| **Nova empresa** | Contacte a Muapi: **licenca@mufutu.ao** ou [mufutu.ao](https://mufutu.ao) |
| **Cliente existente** | O administrador recebe a chave no onboarding ou renovação |
| **Administrador** | Na app: **Definições → Licença** → colar a chave e activar |

### Tipos de plano

| Plano | Indicado para |
|-------|----------------|
| **Trial** | Avaliação (14–90 dias) |
| **Standard** | Operação mineira com módulos core |
| **Enterprise** | Multi-site, analytics, integrações |
| **Definitiva** | Licença perpétua + manutenção anual |

A chave activa **módulos** (manutenção, frota, combustível, etc.) conforme o contrato.
Chaves revogadas deixam de funcionar na próxima validação online.

### Renovação e suporte

- Renovação: gestor de conta Muapi ou **suporte@mufutu.ao**
- Tickets: formulário em **Definições → Suporte** na aplicação web
- Urgências operacionais: canal acordado no contrato SLA

---

## Vantagens dos clientes nativos

| Vantagem | Descrição |
|----------|-----------|
| **Offline robusto** | Base local encriptada; fila de sync quando a rede voltar |
| **Integração Windows** | Credenciais no Credential Locker; proxy corporativo |
| **Desktop macOS** | App dedicada com actualização automática |
| **Campo / Mobile** | OTs, avaria com foto, checklist — optimizado para touchscreen |
| **Segurança** | Binários assinados, ofuscados em produção, sem segredos no cliente |
| **Multi-tenant** | Ligação ao tenant correcto (`app.mufutu.ao` ou subdomínio da empresa) |

A versão **web** continua a ser a referência para gestão completa (relatórios, admin,
configuração). Os clientes nativos focam **execução em campo e offline**.

---

## Requisitos mínimos

| Plataforma | Requisitos |
|------------|------------|
| Windows | Windows 10/11 64-bit, .NET 8 runtime (incluído no instalador) |
| macOS | macOS 12+, Apple Silicon (arm64) ou Intel conforme release |
| Android | Android 10+ (API 29) |
| iOS | iOS 15+ |
| Rede | HTTPS para sync; VPN corporativa se exigida pela mina |

---

## Credenciais e primeiro acesso

As credenciais são **criadas pelo administrador** da sua empresa. Não existem
utilizadores genéricos públicos em produção.

1. Receba email de convite ou credenciais do IT da mina.
2. Abra o cliente ou `https://{seu-tenant}.app.mufutu.ao`.
3. Active a licença (admin) se ainda não estiver activa.
4. Técnicos podem ser redireccionados para **Modo Campo** (`/campo`) após login.

---

## Estrutura deste repositório

```
mufutusoftware/
├── README.md          ← está aqui
├── EULA.md            ← termos de utilização
├── SECURITY.md        ← integridade e reporte de vulnerabilidades
├── windows/           ← guia Windows
├── macos/             ← guia macOS
├── android/           ← guia Android
├── ios/               ← guia iOS
├── web/               ← PWA e acesso browser
├── releases/          ← exemplo de manifesto público (sem segredos)
└── checksums/         ← instruções de verificação
```

**Binários não vivem no Git** — apenas em [Releases](https://github.com/osvaldowafulua/mufutusoftware/releases).

---

## Estatísticas & comunidade

<p align="center">
  <a href="https://github.com/osvaldowafulua/mufutusoftware">
    <img src="https://img.shields.io/github/commit-activity/m/osvaldowafulua/mufutusoftware?style=flat-square&label=commits%2Fmês&color=1565C0" alt="Commits" />
  </a>
  <a href="https://github.com/osvaldowafulua/mufutusoftware/pulse">
    <img src="https://img.shields.io/github/last-commit/osvaldowafulua/mufutusoftware?style=flat-square&color=E8612D" alt="Último commit" />
  </a>
  <a href="https://github.com/osvaldowafulua/mufutusoftware/forks">
    <img src="https://img.shields.io/github/forks/osvaldowafulua/mufutusoftware?style=flat-square&color=1565C0" alt="Forks" />
  </a>
  <a href="https://github.com/osvaldowafulua/mufutusoftware/watchers">
    <img src="https://img.shields.io/github/watchers/osvaldowafulua/mufutusoftware?style=flat-square&color=E8612D" alt="Watchers" />
  </a>
</p>

### Star History

<p align="center">
  <a href="https://star-history.com/#osvaldowafulua/mufutusoftware&Date">
    <picture>
      <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=osvaldowafulua/mufutusoftware&type=Date&theme=dark" />
      <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=osvaldowafulua/mufutusoftware&type=Date" />
      <img alt="Star History — MUFUTU Software" src="https://api.star-history.com/svg?repos=osvaldowafulua/mufutusoftware&type=Date" width="620" />
    </picture>
  </a>
</p>

<p align="center">
  <sub>Gostou do projecto? Deixa uma <a href="https://github.com/osvaldowafulua/mufutusoftware/stargazers"><strong>⭐ star</strong></a> — ajuda outras equipas mineiras a encontrar o MUFUTU.</sub>
</p>

---

## Suporte

| Assunto | Contacto |
|---------|----------|
| Licenciamento | licenca@mufutu.ao |
| Suporte técnico | suporte@mufutu.ao |
| Segurança | seguranca@mufutu.ao |
| Comercial | [mufutu.ao](https://mufutu.ao) |

---

## Legal

- [EULA.md](EULA.md) — Acordo de Licença de Utilizador Final
- [LICENSE](LICENSE) — Aviso de copyright
- [SECURITY.md](SECURITY.md) — Política de segurança e divulgação responsável

**MUFUTU** é marca da **Muapi**. Angola · Lunda Norte · Luachimo · Lunda Sul.
