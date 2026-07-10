# Instalar MUFUTU no Windows (instalador oficial)

## Use o instalador — não o ZIP portátil

| Ficheiro | Tipo | Usar? |
|----------|------|-------|
| **`MUFUTU-Setup-1.0.6-x64.exe`** | Instalador NSIS | **Sim** — recomendado |
| `MUFUTU-*-win-x64.zip` | Portátil (extrair e correr) | **Não** — só para IT/testes |

O instalador `.exe`:
- Instala em `C:\Program Files\MUFUTU\` (ou pasta que escolher)
- Cria atalhos no **Menu Iniciar** e **Ambiente de trabalho**
- Aparece em **Definições → Aplicações** para desinstalar
- Abre o assistente de instalação (como o DMG no Mac)

## Passos (instalador oficial `.exe`)

1. Descarregue **`MUFUTU-Setup-*-x64.exe`** em [Releases](https://github.com/osvaldowafulua/mufutusoftware/releases/latest)
2. Duplo clique → assistente → escolher pasta (ex. `C:\Program Files\MUFUTU`)
3. Atalhos criados automaticamente
4. Desinstalar em **Definições → Aplicações → MUFUTU**

## Solução temporária (só se ainda não houver Setup.exe)

Se só tiver o ZIP portátil:

1. Extraia o ZIP e o ficheiro `install-mufutu.ps1` para a **mesma pasta**
2. Clique direito em **PowerShell (Administrador)**
3. `cd` para essa pasta e execute:
   ```powershell
   Set-ExecutionPolicy Bypass -Scope Process -Force
   .\install-mufutu.ps1
   ```
4. Isto instala em **Program Files** e cria atalhos (não é o instalador NSIS final)

## Desinstalar

**Definições → Aplicações → MUFUTU → Desinstalar**

Ou **Menu Iniciar → MUFUTU → Desinstalar**

## Ainda não vê o Setup.exe no GitHub?

O instalador é gerado no **GitHub Actions** (runner Windows). Peça ao administrador para:
1. Configurar secret `MUFUTU_CMMS_CHECKOUT_TOKEN` no repo
2. Correr workflow **Windows Electron Installer (NSIS)** versão `1.0.6`

Ou num **PC Windows**, dentro do clone do repositório privado `mufutu`:
```powershell
cd mufutu
bash apps/desktop-mac/scripts/package-win.sh 1.0.6
```
