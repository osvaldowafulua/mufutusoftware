# MUFUTU — Web e PWA

Acesso via navegador — **não requer instalador** deste repositório.

## URLs oficiais

| Ambiente | URL |
|----------|-----|
| Holding MUA | [https://app.mufutu.ao](https://app.mufutu.ao) |
| Subsidiária | `https://{slug}.app.mufutu.ao` (ex.: `luc.app.mufutu.ao`) |

## PWA — Modo Campo

Técnicos podem instalar a PWA dedicada ao campo:

1. Login em `/campo` no browser (Chrome, Safari, Edge).
2. **Adicionar ao ecrã inicial** / **Instalar aplicação**.
3. Manifest: modo portrait, offline parcial, ícone MUFUTU laranja.

Ficheiro de referência (no produto web, não aqui): `manifest-campo.json`.

## Vantagens da web

- Sempre actualizada (servidor)
- Sem gestão de instaladores
- Ideal para gestão, relatórios e administração

## Segurança

- Autenticação com cookies httpOnly (sessão segura)
- HTTPS obrigatório em produção
- Não partilhe links de login com credenciais na URL

Para clientes **offline pesado** em Windows/macOS/mobile, use os instaladores em [Releases](https://github.com/osvaldowafulua/mufutusoftware/releases).

Suporte: **suporte@mufutu.ao**
