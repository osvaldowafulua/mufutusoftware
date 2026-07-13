# MUFUTU Software — identidade visual

Cor de marca: **#EB5E28** (laranja MUFUTU) · Cinza de apoio: **#8C8C8C**

## SVG oficiais (fonte de verdade — `svg/`)

| Ficheiro | Conteúdo | Uso recomendado |
|----------|----------|-----------------|
| `svg/logo-branco-fundo-laranja.svg` | Logótipo branco sobre banner laranja | README, cabeçalhos, social |
| `svg/logo-preto-fundo-laranja.svg` | Logótipo preto sobre banner laranja | Materiais impressos |
| `svg/logo-cor.svg` | Logótipo a cores (marca laranja + texto preto) | Fundos brancos/claros |
| `svg/logo-laranja.svg` | Logótipo todo laranja | Fundos brancos/claros |
| `svg/logo-preto.svg` | Logótipo todo preto | Fundos claros, monocromático |
| `svg/favicon.svg` | Círculo laranja + marca branca | Favicon, avatares, ícone redondo |
| `svg/icone-laranja.svg` | Marca "M" laranja isolada | Watermarks, fundos claros |

## PNG derivados (gerados a partir dos SVG)

| Ficheiro | Uso |
|----------|-----|
| `logo-software-horizontal.png` | Logótipo principal (README, cabeçalhos) |
| `icon-orange.png` | Ícone quadrado — stars, commits, favicon |
| `social-preview.png` | Pré-visualização social GitHub |

## Assets das aplicações (derivados)

O código-fonte das apps nativas (Electron, WPF, MAUI) vive no repositório
privado `mufutu`, com a sua própria cópia destes SVG oficiais em
`apps/electron/assets/svg/` — usada por `scripts/generate-desktop-brand-assets.mjs`
para gerar os ícones de cada plataforma. Ao actualizar o logo, replicar as
mudanças de `svg/` também nesse repositório.

**MUFUTU** é marca registada da **Smart Cloud, Lda**.

Legado (substituído): cor antiga `#E8612D`; `logo-software-banner.png`, `logo-software-white.png`, `logo-software-dark.png`
