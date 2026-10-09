// Servidor estático mínimo para o smoke do instalador web (win-electron-build.yml).
// Serve os ficheiros de <pasta> em 127.0.0.1:<porta> COM suporte a Range: o stub
// NSIS descarrega com inetc /RESUME e um servidor que ignore Range não prova o
// caminho real de retoma. Sem dependências — o Node já existe no runner.
//
// Uso: node serve-range.js <pasta> [porta=18080]
const http = require('http');
const fs = require('fs');
const path = require('path');

const root = process.argv[2];
const port = Number(process.argv[3] || 18080);
if (!root || !fs.existsSync(root)) {
  console.error('uso: node serve-range.js <pasta> [porta]');
  process.exit(2);
}

http
  .createServer((req, res) => {
    // Só o nome do ficheiro: nunca sair de <pasta> (sem ../).
    const name = path.basename(decodeURIComponent(req.url.split('?')[0]));
    const file = path.join(root, name);
    if (!name || !fs.existsSync(file) || !fs.statSync(file).isFile()) {
      res.writeHead(404);
      return res.end();
    }
    const size = fs.statSync(file).size;
    const range = /^bytes=(\d+)-(\d*)$/.exec(req.headers.range || '');
    if (range) {
      const start = Number(range[1]);
      const end = range[2] ? Math.min(Number(range[2]), size - 1) : size - 1;
      if (start > end || start >= size) {
        res.writeHead(416, { 'Content-Range': `bytes */${size}` });
        return res.end();
      }
      res.writeHead(206, {
        'Content-Range': `bytes ${start}-${end}/${size}`,
        'Accept-Ranges': 'bytes',
        'Content-Length': end - start + 1,
      });
      return req.method === 'HEAD' ? res.end() : fs.createReadStream(file, { start, end }).pipe(res);
    }
    res.writeHead(200, { 'Accept-Ranges': 'bytes', 'Content-Length': size });
    return req.method === 'HEAD' ? res.end() : fs.createReadStream(file).pipe(res);
  })
  .listen(port, '127.0.0.1', () => console.log(`a servir ${root} em http://127.0.0.1:${port}`));
