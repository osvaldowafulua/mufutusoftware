# MUFUTU — Android

Cliente **.NET MAUI** para técnicos e supervisores em campo. Código-fonte no
repositório privado `mufutu`.

## Descarregar

| Canal | Quando usar |
|-------|-------------|
| [GitHub Releases](https://github.com/osvaldowafulua/mufutusoftware/releases) | APK empresarial (sideload controlado pelo IT) |
| Google Play (futuro) | Distribuição pública — anunciado nos Releases |
| MDM / Intune | Upload do `.apk` ou `.aab` pelo administrador |

Ficheiros típicos: **`MUFUTU-*-release.apk`** ou **`MUFUTU-*.aab`** (Play Store).

## Instalação (APK empresarial)

1. O IT distribui o APK via MDM ou pasta interna segura.
2. No dispositivo: permitir «fontes desconhecidas» **apenas** para o gestor de instalação corporativo.
3. Verifique o hash SHA-256 publicado no release.
4. Abra a app → login → Modo Campo.

> **Não** instale APKs de terceiros que claim ser «MUFUTU cracked» ou «mod» — são inseguros e violam o [EULA](../EULA.md).

## Segurança

- APK assinado com chave de release Smart Cloud, Lda (v2/v3 signing).
- ProGuard/R8 e ofuscação em builds de produção.
- Comunicação TLS com a API do tenant; certificados pinning em roadmap.

## Requisitos

- Android 10+ (API 29)
- 3 GB RAM recomendado
- Câmara (checklist, avaria com foto)
- GPS opcional (registo de local em campo)

## Actualizações

- **MDM:** push da nova versão pelo administrador.
- **Play Store:** actualização automática quando disponível.

Suporte: **suporte@mufutu.ao**
