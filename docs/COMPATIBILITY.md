# Compatibilidade clientes ↔ CMMS

| Clientes (mufutusoftware) | CMMS/API (mufutu) | Notas |
| --------------------------- | ----------------- | ----- |
| **Desktop 1.0.19** | **≥ 1.1.2** | Login holding multi-tenant, disponibilidade, i18n web |
| **Mobile MAUI 1.0.13** | **≥ 1.1.2** | Mesma API; configurar URL correcta no login |
| Desktop 1.0.16 | ≥ 1.1.0 | i18n desktop pt/en |
| Mobile 1.0.12 | ≥ 1.1.0 | i18n pt/en/de |

## Login multi-tenant (CMMS ≥ 1.1.2)

| Destino | URL API (clientes nativos) | Quem pode entrar |
| ------- | ------------------------ | ---------------- |
| **Holding** | `https://api.mufutu.ao/api` | Qualquer utilizador de tenant **activo** (MUA, LUC, MIL…) |
| **Subsidiária isolada** | `https://sml.api.mufutu.ao/api` (Luachimo/SML) | Só utilizadores desse tenant |
| **Web embebida (Electron)** | Proxy interno → `api.mufutu.ao` ou `api-config.json` | Igual à stack configurada no build |

O cliente **não** escolhe tenant no ecrã de login — a API resolve pelo `user.tenantId` após autenticação.

## URLs por plataforma

| Plataforma | Omissão produção | Override |
| ---------- | ---------------- | -------- |
| Windows WPF | `https://api.mufutu.ao/api` | `MUFUTU_API_URL` |
| macOS Electron | `https://api.mufutu.ao` (proxy) | `~/Library/Application Support/MUFUTU/api-config.json` |
| Mobile MAUI | Campo editável no login | Guardado em preferências |

Documentação detalhada de conectividade e do cliente mobile vive junto do
código-fonte no repositório privado `mufutu`.
