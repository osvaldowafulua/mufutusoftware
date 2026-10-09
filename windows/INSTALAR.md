# Instalar MUFUTU no Windows (instalador oficial)

> **Use a 1.0.50 ou superior.** Nas versões anteriores o instalador do Windows
> não funcionava: na 1.0.49 o `.exe` e o `.msi` eram outra aplicação (o cliente
> WPF legado, publicado por engano com estes nomes) e nas 1.0.47/1.0.48 o `.exe`
> instalava o app certo mas ele fechava-se logo no arranque. Nessa altura só o
> ZIP portátil servia. Na 1.0.50 o `.exe` e o `.msi` estão corrigidos e
> verificados em CI.

## Qual ficheiro escolher

| Ficheiro | Tipo | Usar? |
|----------|------|-------|
| **`MUFUTU-Setup-1.0.50-x64.exe`** | Instalador NSIS | **Sim** — recomendado |
| `MUFUTU-1.0.50-x64.msi` | MSI | Sim — parque gerido (GPO/Intune) |
| `MUFUTU-1.0.50-win-x64.zip` | Portátil (extrair e correr) | Só para IT/testes |

O `.exe` e o `.msi` instalam exactamente o mesmo app.

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

O SmartScreen pode avisar («Editor desconhecido») enquanto a assinatura for
self-signed: **Mais informações → Executar na mesma**.

## Parque gerido (MSI)

```powershell
msiexec /i MUFUTU-1.0.50-x64.msi /qn /norestart
```

Para diagnosticar uma instalação que falha, acrescente um log:

```powershell
msiexec /i MUFUTU-1.0.50-x64.msi /qn /norestart /l*v "%TEMP%\mufutu-msi.log"
```

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
2. Correr o workflow **Windows Electron (NSIS + MSI + ZIP)**

Ou num **PC Windows**, dentro do clone do repositório privado `mufutu`:
```powershell
cd mufutu
bash apps/desktop-mac/scripts/package-win.sh 1.0.50
```

## O MUFUTU abre e fecha logo?

Era o sintoma das 1.0.47/1.0.48 instaladas em Program Files e está corrigido na
1.0.50 — actualize. Se acontecer na 1.0.50, o log do arranque está em
`%APPDATA%\MUFUTU\desktop.log` e ajuda a diagnosticar.
