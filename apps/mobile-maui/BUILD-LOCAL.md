# Build local — MUFUTU Campo (Android)

## Repositório correcto

Este app está em **`mufutusoftware`**, **não** em `mufutu` (CMMS privado).

```bash
# Caminho no Mac (ajusta se a tua pasta for diferente)
cd /Users/fluadigital/Documents/GitHub/mufutusoftware
```

**Não uses** `cd mufutusoftware/...` a partir da pasta `mufutu` — essa subpasta não existe.

---

## Testes (sem workload MAUI)

```bash
cd /Users/fluadigital/Documents/GitHub/mufutusoftware
npm run test:mobile-maui
# ou:
dotnet test apps/mobile-maui/tests/Mufutu.Mobile.Tests/Mufutu.Mobile.Tests.csproj -c Release
```

---

## APK no Mac — SDK .NET com MAUI

O `dotnet` do **Homebrew** (`brew install dotnet@8`) **não inclui** workloads MAUI.

### Opção A — .NET Microsoft (recomendado no Mac)

Se tens o instalador em `/usr/local/share/dotnet/x64/dotnet` (SDK 9+):

```bash
export DOTNET_ROOT=/usr/local/share/dotnet/x64
export PATH="$DOTNET_ROOT:$PATH"

dotnet workload update
sudo dotnet workload install maui-android   # pede password uma vez

cd /Users/fluadigital/Documents/GitHub/mufutusoftware/apps/mobile-maui
bash scripts/package-android.sh 1.0.0
```

APK em: `apps/mobile-maui/artifacts/android/`

### Opção B — CI GitHub (sem instalar MAUI no Mac)

Na pasta **`mufutusoftware`** (não `mufutu`):

```bash
cd /Users/fluadigital/Documents/GitHub/mufutusoftware
git checkout main && git pull
git tag mobile-maui/v1.0.0
git push origin mobile-maui/v1.0.0
```

O workflow `.github/workflows/mobile-maui.yml` gera o APK na [Release](https://github.com/osvaldowafulua/mufutusoftware/releases).

---

## Erros comuns

| Erro | Causa | Solução |
|------|--------|---------|
| `cd: mufutusoftware/apps/mobile-maui` | Estás em `mufutu` | `cd ../mufutusoftware` ou caminho absoluto |
| `Workload ID maui-android is not recognized` | SDK Homebrew sem MAUI | Usar SDK Microsoft + `sudo dotnet workload install maui-android` |
| `npm … --workspace=mufutusoftware` | Comando na pasta errada | `cd mufutusoftware` e `npm run test:mobile-maui` (sem `-w`) |
| Tag push em `mufutu` | Repo errado | Tag só em `github.com/osvaldowafulua/mufutusoftware` |
