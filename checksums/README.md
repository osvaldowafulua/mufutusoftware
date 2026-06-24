# Verificação de integridade

Cada [GitHub Release](https://github.com/osvaldowafulua/mufutusoftware/releases) oficial
pode incluir um ficheiro **`checksums.sha256`** com hashes dos instaladores.

## macOS / Linux

```bash
# Descarregar release + checksums para a mesma pasta
shasum -a 256 -c checksums.sha256
```

Saída esperada: `OK` para cada ficheiro.

## Windows (PowerShell)

```powershell
$expected = "COLE_AQUI_O_HASH_DO_RELEASE"
$actual = (Get-FileHash .\MUFUTU-Setup.exe -Algorithm SHA256).Hash
if ($actual -eq $expected) { "OK" } else { "FALHA — não instale" }
```

## Linux

```bash
sha256sum -c checksums.sha256
```

## O que fazer se falhar

1. **Não instale** o ficheiro.
2. Re-descarregue apenas de [Releases oficiais](https://github.com/osvaldowafulua/mufutusoftware/releases).
3. Reporte a **seguranca@mufutu.ao** se suspeitar de adulteração.

Os hashes são publicados **no corpo do release** e no ficheiro `checksums.sha256`.
Nunca confie em hashes enviados por email não solicitado.
