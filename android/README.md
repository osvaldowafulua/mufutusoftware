# MUFUTU — Android

## Caminho primário: Flutter

O cliente Campo novo vive em [`apps/mobile-flutter`](../apps/mobile-flutter) (**Flutter**).

- Offline-first (sqflite + fila + backoff + LWW)
- Login **PIN** + email/password
- API default SML: `https://sml.api.mufutu.ao/api`

```bash
cd apps/mobile-flutter
flutter pub get
flutter run
flutter build apk --release
```

Ver [apps/mobile-flutter/README.md](../apps/mobile-flutter/README.md).

## Legado: .NET MAUI

[`apps/mobile-maui`](../apps/mobile-maui) permanece disponível para APKs já publicados, mas **não** é o caminho de novas features. Preferir Flutter.

## Descarregar

| Canal | Quando usar |
|-------|-------------|
| [GitHub Releases](https://github.com/osvaldowafulua/mufutusoftware/releases) | APK empresarial (sideload) |
| Build Flutter local | Desenvolvimento / piloto SML |

## Requisitos

- Android 10+ (API 29)
- Câmara (avaria / presença)
- GPS opcional

## Actualizações

Version gate via [`releases/latest.json`](../releases/latest.json) (nunca bloqueia offline).
