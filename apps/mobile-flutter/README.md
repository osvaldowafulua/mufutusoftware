# MUFUTU Campo — Flutter (caminho primário mobile)

Cliente **Android / iOS** offline-first para técnicos. Substitui gradualmente o .NET MAUI (`apps/mobile-maui` = **legado**).

| Item | Valor |
|------|--------|
| Package | `mufutu_campo` |
| Bundle / applicationId | `com.mufutu.mufutu_campo` |
| API default (SML) | `https://sml.api.mufutu.ao/api` |
| Auth | PIN (`POST /auth/login-pin`) + email/password |
| Local DB | sqflite `mufutu_campo.db` |
| Sync | write-local-first → `sync_queue` → batch `POST /sync/push` · backoff expo · LWW |

## Arranque

```bash
cd apps/mobile-flutter
flutter pub get
flutter run
```

## Build APK

```bash
flutter build apk --release
# → build/app/outputs/flutter-apk/app-release.apk
```

## Fluxos Campo

- Login (PIN ou email)
- Hub + estado Offline / A sincronizar / Sincronizado / N pendentes
- OTs (lista + começar/terminar offline)
- Avaria (enfileirada)
- Checklist (local)
- Presença (enfileirada)
- Sync manual

## Relação com MAUI

| | Flutter (novo) | MAUI (legado) |
|--|----------------|---------------|
| Repo path | `apps/mobile-flutter` | `apps/mobile-maui` |
| Offline-first | Completo (este app) | Parcial |
| Login PIN | Sim | Não |
| Releases futuras | Preferir Flutter | Manutenção até cutover |
