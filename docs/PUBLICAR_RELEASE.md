# Publicar um release (equipa Muapi)

Guia interno para publicar **binários finais** neste repositório público.
O código-fonte e pipelines completos permanecem no repositório privado `mufutu`.

## Princípios de segurança

- Publicar **apenas** artefactos de produção (ofuscados, sem símbolos de debug).
- **Nunca** incluir: `.env`, chaves privadas, PFX, mapas de origem, código-fonte,
  documentação de API interna, seeds, credenciais de exemplo de produção.
- Cada ficheiro deve constar em `checksums.sha256` e `manifest.json`.
- Assinatura de código activa quando disponível (Windows/macOS).

## Fluxo recomendado

```
Repositório privado mufutu (CI)
    → build + ofuscação + testes SAST
    → artefactos em GitHub Actions
    → upload manual ou automatizado para mufutusoftware Releases
```

### 1. Gerar builds (repo `mufutu`)

| Plataforma | Tag / workflow |
|------------|----------------|
| Windows | `desktop-win/v*` → `desktop-win.yml` |
| macOS + Win unificado | `desktop/v*` → `desktop-release.yml` |
| macOS só | `desktop-mac/v*` → `desktop-mac.yml` |
| Android / iOS | Pipeline mobile (quando activo) |

### 2. Validar artefactos

- [ ] Versão semver correcta
- [ ] Obfuscação / release configuration (não Debug)
- [ ] Sem ficheiros `.pdb`, `.map`, `appsettings.Development.json`
- [ ] Checksums gerados
- [ ] Teste de instalação em VM limpa

### 3. Criar release em `mufutusoftware`

```bash
# Exemplo — ajustar versão e ficheiros
VERSION=1.0.0
gh release create "v${VERSION}" \
  --repo osvaldowafulua/mufutusoftware \
  --title "MUFUTU ${VERSION}" \
  --notes-file release-notes.md \
  ./staging/MUFUTU-Setup.exe \
  ./staging/MUFUTU-${VERSION}-arm64.dmg \
  ./staging/checksums.sha256 \
  ./staging/manifest.json
```

### 4. Notas de release (template público)

- Novidades visíveis ao utilizador
- Correções de bugs
- Requisitos de sistema alterados
- **Sem** detalhes de vulnerabilidades não corrigidas (ver SECURITY.md)

### 5. Comunicação

- Email clientes com link do release (opcional)
- Banner em `app.mufutu.ao` se breaking change
- Actualização documentação se novas plataformas

## O que não vai para este repo

| Item | Onde fica |
|------|-----------|
| Código-fonte | `github.com/osvaldowafulua/mufutu` (privado) |
| Chaves Ed25519 licenças | Control Plane / secrets API |
| Config Obfuscar detalhada | `apps/desktop-win/scripts/` (privado) |
| Credenciais Dokploy | Servidor / vault |

## Checklist rápido

- [ ] `checksums.sha256` no release
- [ ] `manifest.json` sem campos extra (validar contra `manifest.schema.json`)
- [ ] EULA e SECURITY.md actualizados se mudou termos
- [ ] Tag `vX.Y.Z` em mufutusoftware alinhada com versão do produto
