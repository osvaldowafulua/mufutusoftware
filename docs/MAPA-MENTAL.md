# Mapa Mental — MUFUTU Software

Visão global do produto, plataformas, licenciamento e suporte. O GitHub renderiza
o diagrama abaixo automaticamente (Mermaid).

```mermaid
mindmap
  root((MUFUTU Software))
    Visão Geral
      Sistema CMMS/EAM
      Operações mineiras em Angola
      Smart Cloud, Lda
      Estratégia offline-first
    Módulos Core
      Manutenção
        Pedidos e Ordens de Trabalho PT/OT
        Checklists
        Planeamento PM
      Activos
        Frota pesada CAT e Komatsu
        Identificação por QR Code
      Operações
        Controlo de combustível
        Disponibilidade e paragens
        Logística
    Plataformas Suportadas
      Windows
        Cliente WPF
        Setup instalável e ZIP portátil
      macOS
        Apple Silicon arm64
        Instalador DMG
      Mobile
        APK Android
        iOS App Store
        Interface Modo Campo
      Web
        Gestão completa
        app.mufutu.ao
    Licenciamento e Planos
      Trial 14 a 90 dias
      Standard Mining
      Enterprise multi-site
      Licença perpétua
    Detalhes Técnicos
      Linguagens
        C# 64.1%
        JavaScript 21%
        Shell e PowerShell
      Estrutura do Repositório
        Monorepo de apps
        Workflows GitHub Actions
        Release assets
    Legal e Suporte
      EULA e política de segurança
      Smart Cloud Lda Luachimo
      Canais de suporte técnico
```

## Ligações rápidas

| Ramo | Onde está no repositório |
|------|--------------------------|
| Cliente Windows (WPF) | [`apps/desktop-win/`](../apps/desktop-win/) |
| Cliente macOS (Electron) | [`apps/electron/`](../apps/electron/) + [`apps/desktop-mac/`](../apps/desktop-mac/) |
| Mobile Android/iOS (MAUI) | [`apps/mobile-maui/`](../apps/mobile-maui/) |
| Workflows CI/CD | [`.github/workflows/`](../.github/workflows/) |
| Identidade visual | [`assets/brand/`](../assets/brand/) |
| Guias por plataforma | [`windows/`](../windows/) · [`macos/`](../macos/) · [`android/`](../android/) · [`ios/`](../ios/) · [`web/`](../web/) |
| Licenciamento e EULA | [`EULA.md`](../EULA.md) · [`LICENSE`](../LICENSE) |
| Segurança | [`SECURITY.md`](../SECURITY.md) |
| Plano de melhoria | [`PLANO-DE-MELHORIA.md`](PLANO-DE-MELHORIA.md) |
