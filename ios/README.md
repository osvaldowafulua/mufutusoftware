# MUFUTU — iOS

Aplicação para iPhone e iPad (técnicos em campo).

## Descarregar

A Apple **não** permite distribuição pública de IPA fora da App Store ou canais empresariais.

| Canal | Descrição |
|-------|-----------|
| **App Store** | Disponível quando publicado — link no site [mufutu.ao](https://mufutu.ao) |
| **TestFlight** | Beta fechado — convite pelo administrador Muapi |
| **Apple Business Manager** | Empresas com ABM — distribuição privada via MDM |

Não procure ficheiros `.ipa` neste repositório para instalação manual em dispositivos pessoais.

## Instalação (utilizador final)

1. Receba convite TestFlight **ou** instale pela App Store quando disponível.
2. Login com credenciais da empresa.
3. Permita notificações se quiser alertas de OT (opcional).

## Segurança

- Binários compilados em pipeline CI; sem código-fonte exposto.
- Keychain iOS para tokens.
- [EULA](../EULA.md) e política de privacidade no ecrã de onboarding.

## Requisitos

- iOS 15+
- iPhone 8 ou superior / iPad compatível
- Ligação periódica à Internet para sync

## Empresas (MDM)

O administrador Muapi fornece:

- Bundle ID da app
- Link TestFlight ou pacote ABM
- Perfil de configuração (URL do tenant) quando aplicável

Suporte: **suporte@mufutu.ao**
