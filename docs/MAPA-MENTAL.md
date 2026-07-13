# Mapa Mental — MUFUTU Software

Visão global do produto, plataformas, licenciamento e suporte. O GitHub renderiza
o diagrama abaixo automaticamente (Mermaid — `flowchart`, suportado de forma
estável em qualquer repositório; o tipo `mindmap` é experimental e nem sempre
renderiza no GitHub).

```mermaid
flowchart LR
    ROOT((MUFUTU Software))

    ROOT --> OV[Visão Geral]
    OV --> OV1[Sistema CMMS/EAM]
    OV --> OV2[Operações mineiras em Angola]
    OV --> OV3[Smart Cloud, Lda]
    OV --> OV4[Estratégia offline-first]

    ROOT --> CM[Módulos Core]
    CM --> CM1[Manutenção]
    CM1 --> CM1a[Pedidos e Ordens de Trabalho PT/OT]
    CM1 --> CM1b[Checklists]
    CM1 --> CM1c[Planeamento PM]
    CM --> CM2[Activos]
    CM2 --> CM2a[Frota pesada CAT e Komatsu]
    CM2 --> CM2b[Identificação por QR Code]
    CM --> CM3[Operações]
    CM3 --> CM3a[Controlo de combustível]
    CM3 --> CM3b[Disponibilidade e paragens]
    CM3 --> CM3c[Logística]

    ROOT --> SP[Plataformas Suportadas]
    SP --> SP1[Windows]
    SP1 --> SP1a[Cliente WPF]
    SP1 --> SP1b[Setup instalável e ZIP portátil]
    SP --> SP2[macOS]
    SP2 --> SP2a[Apple Silicon arm64]
    SP2 --> SP2b[Instalador DMG]
    SP --> SP3[Mobile]
    SP3 --> SP3a[APK Android]
    SP3 --> SP3b[iOS App Store]
    SP3 --> SP3c[Interface Modo Campo]
    SP --> SP4[Web]
    SP4 --> SP4a[Gestão completa]
    SP4 --> SP4b[app.mufutu.ao]

    ROOT --> LP[Licenciamento e Planos]
    LP --> LP1[Trial 14 a 90 dias]
    LP --> LP2[Standard Mining]
    LP --> LP3[Enterprise multi-site]
    LP --> LP4[Licença perpétua]

    ROOT --> TEC[Detalhes Técnicos]
    TEC --> TD1[Linguagens]
    TD1 --> TD1a["C# 64.1%"]
    TD1 --> TD1b["JavaScript 21%"]
    TD1 --> TD1c[Shell e PowerShell]
    TEC --> TD2[Estrutura do Repositório]
    TD2 --> TD2a[Monorepo de apps]
    TD2 --> TD2b[Workflows GitHub Actions]
    TD2 --> TD2c[Release assets]

    ROOT --> LS[Legal e Suporte]
    LS --> LS1[EULA e política de segurança]
    LS --> LS2[Smart Cloud Lda Luachimo]
    LS --> LS3[Canais de suporte técnico]

    classDef root fill:#EB5E28,color:#fff,stroke:#EB5E28,stroke-width:2px;
    classDef branch fill:#1F2A24,color:#fff,stroke:#3a4a3f;
    classDef leaf fill:#2E7D57,color:#fff,stroke:#3a4a3f;

    class ROOT root;
    class OV,CM,SP,LP,TEC,LS branch;
    class OV1,OV2,OV3,OV4,CM1,CM2,CM3,SP1,SP2,SP3,SP4,LP1,LP2,LP3,LP4,TD1,TD2,LS1,LS2,LS3 branch;
    class CM1a,CM1b,CM1c,CM2a,CM2b,CM3a,CM3b,CM3c,SP1a,SP1b,SP2a,SP2b,SP3a,SP3b,SP3c,SP4a,SP4b,TD1a,TD1b,TD1c,TD2a,TD2b,TD2c leaf;
```

## Ligações rápidas

| Ramo | Onde está |
|------|-----------|
| Cliente Windows (WPF), macOS (Electron), Mobile (MAUI) — código-fonte | `apps/desktop-win/`, `apps/electron/`, `apps/mobile-maui/` no repositório privado `mufutu` |
| Workflows CI/CD (fazem checkout do `mufutu`) | [`.github/workflows/`](../.github/workflows/) |
| Identidade visual | [`assets/brand/`](../assets/brand/) |
| Guias de instalação por plataforma | [`windows/`](../windows/) · [`macos/`](../macos/) · [`android/`](../android/) · [`ios/`](../ios/) · [`web/`](../web/) |
| Licenciamento e EULA | [`EULA.md`](../EULA.md) · [`LICENSE`](../LICENSE) |
| Segurança | [`SECURITY.md`](../SECURITY.md) |
| Política de repositórios (público vs privado) | [`POLITICA_REPOSITORIOS.md`](POLITICA_REPOSITORIOS.md) |
