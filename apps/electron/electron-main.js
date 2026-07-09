const { app, BrowserWindow, Menu, ipcMain, safeStorage, session } = require('electron');
const path = require('path');
const fs = require('fs');
const crypto = require('crypto');
const { spawn } = require('child_process');
const http = require('http');
const https = require('https');
const labelsBridge = require('./electron-labels-bridge');
const connectivityBridge = require('./electron-connectivity-bridge');
const { buildUpstreamTarget, isLocalHost } = require('./electron-ipv4');

// Guarda: o servidor Next.js embebido é lançado via spawn do próprio binário com
// ELECTRON_RUN_AS_NODE. Se essa variável se perder (ofuscação/ambiente), o filho
// arranca como segunda app GUI no Dock do macOS — sair imediatamente nesse caso.
if (process.env.MUFUTU_SERVER_CHILD === '1' && !process.env.ELECTRON_RUN_AS_NODE) {
  app.quit();
  process.exit(0);
}

const isDev = !app.isPackaged && process.env.NODE_ENV === 'development';
const DESKTOP_PORT = Number(process.env.AYOMANT_PORT || 3847);
const SPLASH_TIMEOUT_MS = 30_000;
const DESKTOP_LOCALES = new Set(['pt-AO', 'en']);

function localeFilePath() {
  return path.join(app.getPath('userData'), 'locale.json');
}

function readDesktopLocale() {
  try {
    const raw = JSON.parse(fs.readFileSync(localeFilePath(), 'utf8'));
    const loc = raw?.locale;
    return DESKTOP_LOCALES.has(loc) ? loc : 'pt-AO';
  } catch {
    return 'pt-AO';
  }
}

function writeDesktopLocale(locale) {
  const loc = DESKTOP_LOCALES.has(locale) ? locale : 'pt-AO';
  fs.mkdirSync(path.dirname(localeFilePath()), { recursive: true });
  fs.writeFileSync(localeFilePath(), JSON.stringify({ locale: loc }));
  return loc;
}

async function applyLocaleCookieForUrl(loadUrl, locale) {
  try {
    const u = new URL(loadUrl);
    await session.defaultSession.cookies.set({
      url: `${u.protocol}//${u.host}`,
      name: 'mufutu_locale',
      value: locale,
      path: '/',
    });
  } catch (err) {
    logDesktop(`locale cookie: ${err}`);
  }
}

let mainWindow;
let splashWindow;
let serverProcess;
let splashTimeout;

function getElectronAppRoot() {
  if (app.isPackaged) {
    const unpacked = path.join(process.resourcesPath, 'app.asar.unpacked', 'electron-app');
    if (fs.existsSync(path.join(unpacked, 'server-manifest.json'))) {
      return unpacked;
    }
  }
  return path.join(__dirname, 'electron-app');
}

function logDesktop(message) {
  const line = `[mufutu-desktop] ${message}\n`;
  process.stdout.write(line);
  try {
    const logPath = path.join(app.getPath('userData'), 'desktop.log');
    fs.appendFileSync(logPath, `${new Date().toISOString()} ${message}\n`);
  } catch {
    // ignore logging errors
  }
}

function getEncryptionKey() {
  const seed = app.getPath('userData') + '|mufutu-desktop-v1';
  return crypto.createHash('sha256').update(seed).digest();
}

function encryptAes256Gcm(plaintext) {
  const key = getEncryptionKey();
  const iv = crypto.randomBytes(12);
  const cipher = crypto.createCipheriv('aes-256-gcm', key, iv);
  const enc = Buffer.concat([cipher.update(String(plaintext), 'utf8'), cipher.final()]);
  const tag = cipher.getAuthTag();
  return Buffer.concat([iv, tag, enc]).toString('base64');
}

function decryptAes256Gcm(payloadB64) {
  const key = getEncryptionKey();
  const buf = Buffer.from(payloadB64, 'base64');
  const iv = buf.subarray(0, 12);
  const tag = buf.subarray(12, 28);
  const data = buf.subarray(28);
  const decipher = crypto.createDecipheriv('aes-256-gcm', key, iv);
  decipher.setAuthTag(tag);
  return Buffer.concat([decipher.update(data), decipher.final()]).toString('utf8');
}

function resolveSplashLogo() {
  const candidates = [
    path.join(__dirname, 'assets', 'splash-logo.png'),
    path.join(__dirname, 'assets', 'svg', 'splash-mark.svg'),
  ];
  return candidates.find((p) => fs.existsSync(p));
}

function createSplashWindow() {
  const logo = resolveSplashLogo();
  splashWindow = new BrowserWindow({
    width: 420,
    height: 420,
    frame: false,
    transparent: false,
    alwaysOnTop: true,
    center: true,
    resizable: false,
    skipTaskbar: true,
    show: false,
    backgroundColor: '#EB5E28',
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
    },
  });
  const query = logo ? { logo: encodeURIComponent(`file://${logo}`) } : {};
  splashWindow.loadFile(path.join(__dirname, 'electron-splash.html'), { query });
  splashWindow.once('ready-to-show', () => splashWindow?.show());
}

function closeSplashWindow() {
  if (splashTimeout) {
    clearTimeout(splashTimeout);
    splashTimeout = null;
  }
  if (splashWindow && !splashWindow.isDestroyed()) {
    splashWindow.close();
    splashWindow = null;
  }
}

function armSplashTimeout() {
  splashTimeout = setTimeout(() => {
    logDesktop('Splash timeout — a mostrar janela principal');
    closeSplashWindow();
    mainWindow?.show();
  }, SPLASH_TIMEOUT_MS);
}

function readCachedApiOrigin() {
  try {
    const cachePath = path.join(app.getPath('userData'), 'api-origin-cache.json');
    if (!fs.existsSync(cachePath)) return null;
    const parsed = JSON.parse(fs.readFileSync(cachePath, 'utf8'));
    if (typeof parsed?.origin === 'string' && parsed.origin.trim()) {
      return parsed.origin.trim().replace(/\/$/, '').replace(/\/api$/, '');
    }
  } catch (err) {
    logDesktop(`api-origin-cache inválido: ${err}`);
  }
  return null;
}

function writeCachedApiOrigin(origin) {
  try {
    const cachePath = path.join(app.getPath('userData'), 'api-origin-cache.json');
    fs.writeFileSync(
      cachePath,
      JSON.stringify({ origin: origin.replace(/\/$/, '').replace(/\/api$/, ''), ts: Date.now() }),
    );
  } catch {
    /* ignore */
  }
}

async function refreshApiOriginCache() {
  const localOk = await probeApiHealth('http://127.0.0.1:6000', 1200);
  const origin = localOk ? 'http://127.0.0.1:6000' : 'https://api.mufutu.ao';
  writeCachedApiOrigin(origin);
  logDesktop(`API origin cache actualizado → ${origin}`);
  return origin;
}

function readDesktopApiConfigOrigin() {
  const configPath = path.join(app.getPath('userData'), 'api-config.json');
  try {
    if (fs.existsSync(configPath)) {
      const cfg = JSON.parse(fs.readFileSync(configPath, 'utf8'));
      const url = cfg.apiOrigin || cfg.apiUrl;
      if (typeof url === 'string' && url.trim()) {
        return url.trim().replace(/\/$/, '').replace(/\/api$/, '');
      }
    }
  } catch (err) {
    logDesktop(`api-config.json inválido: ${err}`);
  }
  return null;
}

function probeApiHealth(origin, timeoutMs = 5000) {
  return new Promise((resolve) => {
    void (async () => {
      try {
        const normalized = String(origin).replace(/\/$/, '').replace(/\/api$/, '');
        const target = new URL(`${normalized}/api/health`);
        const port = Number(target.port || (target.protocol === 'https:' ? 443 : 80));
        const isHttps = target.protocol === 'https:';
        const upstream = await buildUpstreamTarget(target.hostname, port, isHttps);
        const transport = isHttps ? https : http;

        logDesktop(
          `Health probe ${target.hostname} → ${upstream.connectHost}:${port} (IPv4=${!isLocalHost(target.hostname)})`,
        );

        const req = transport.get(
          {
            hostname: upstream.connectHost,
            port,
            path: `${target.pathname}${target.search}`,
            timeout: timeoutMs,
            servername: upstream.servername,
          },
          (res) => {
            res.resume();
            const ok = res.statusCode === 200;
            if (!ok) logDesktop(`Health probe falhou: HTTP ${res.statusCode}`);
            resolve(ok);
          },
        );
        req.on('error', (err) => {
          logDesktop(`Health probe erro: ${err.message}`);
          resolve(false);
        });
        req.on('timeout', () => {
          req.destroy();
          logDesktop('Health probe timeout');
          resolve(false);
        });
      } catch (err) {
        logDesktop(`Health probe setup: ${err.message}`);
        resolve(false);
      }
    })();
  });
}

async function loadDesktopApiOrigin() {
  const configured = readDesktopApiConfigOrigin();
  if (configured) return configured;

  const fromEnv = process.env.MUFUTU_API_URL || process.env.API_INTERNAL_URL;
  if (fromEnv) return fromEnv.trim().replace(/\/$/, '').replace(/\/api$/, '');

  const cached = readCachedApiOrigin();
  if (cached) {
    void refreshApiOriginCache();
    return cached;
  }

  // Primeiro arranque: não bloquear splash — usar produção; probe local em background
  void refreshApiOriginCache();
  return 'https://api.mufutu.ao';
}

function loadManifest() {
  const base = getElectronAppRoot();
  const manifestPath = path.join(base, 'server-manifest.json');
  if (!fs.existsSync(manifestPath)) {
    logDesktop(`Manifest não encontrado: ${manifestPath}`);
    return null;
  }
  try {
    const raw = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
    const serverJs = path.join(base, raw.serverJs);
    const serverDir = path.join(base, raw.serverDir);
    if (!fs.existsSync(serverJs)) {
      logDesktop(`server.js em falta: ${serverJs}`);
      return null;
    }
    return {
      ...raw,
      serverJs,
      serverDir,
    };
  } catch (err) {
    logDesktop(`Erro ao ler manifest: ${err}`);
    return null;
  }
}

function waitForServer(port, timeoutMs = 120000, childProcess = null) {
  const started = Date.now();
  return new Promise((resolve, reject) => {
    let settled = false;
    const finish = (fn, value) => {
      if (settled) return;
      settled = true;
      if (pollTimer) clearInterval(pollTimer);
      fn(value);
    };

    if (childProcess) {
      childProcess.once('exit', (code) => {
        if (code && code !== 0) {
          finish(reject, new Error(`Servidor terminou com código ${code}`));
        }
      });
    }

    const tick = () => {
      const req = http.get(`http://127.0.0.1:${port}`, (res) => {
        res.resume();
        finish(resolve);
      });
      req.on('error', () => {
        if (Date.now() - started > timeoutMs) {
          finish(reject, new Error(`Servidor não respondeu em ${timeoutMs}ms`));
        }
      });
    };

    tick();
    const pollTimer = setInterval(tick, 150);
  });
}

async function startEmbeddedServer() {
  const manifest = loadManifest();
  if (!manifest?.serverJs) return null;

  const port = manifest.port || DESKTOP_PORT;
  const apiOrigin = await loadDesktopApiOrigin();
  const launchScript = path.join(getElectronAppRoot(), 'launch-server.js');
  const serverEntry = fs.existsSync(launchScript) ? launchScript : manifest.serverJs;
  logDesktop(
    `A iniciar Next.js via ${serverEntry} (API ${apiOrigin}/api, porta ${port})`,
  );

  const serverEnv = {
    ...process.env,
    ELECTRON_RUN_AS_NODE: '1',
    MUFUTU_SERVER_CHILD: '1',
    NODE_ENV: 'production',
    PORT: String(port),
    HOSTNAME: '127.0.0.1',
    API_INTERNAL_URL: apiOrigin,
    MUFUTU_API_URL: apiOrigin,
  };
  const serverCwd = path.dirname(serverEntry);

  // spawn + ELECTRON_RUN_AS_NODE — utilityProcess não executa launch-server.js (exit 2304)
  serverProcess = spawn(process.execPath, [serverEntry], {
    cwd: serverCwd,
    env: serverEnv,
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true,
    detached: false,
  });
  logDesktop('Servidor Next.js em segundo plano (spawn oculto)');

  serverProcess.stdout?.on('data', (d) => {
    process.stdout.write(`[next] ${d}`);
    logDesktop(`[next stdout] ${String(d).trim()}`);
  });
  serverProcess.stderr?.on('data', (d) => {
    process.stderr.write(`[next] ${d}`);
    logDesktop(`[next stderr] ${String(d).trim()}`);
  });
  serverProcess.on('exit', (code) => {
    if (code && code !== 0) {
      logDesktop(`Servidor Next.js terminou com código ${code}`);
    }
  });

  await waitForServer(port, 120000, serverProcess);
  logDesktop(`Next.js pronto em http://127.0.0.1:${port}`);
  return `http://127.0.0.1:${port}/login`;
}

function stopEmbeddedServer() {
  if (!serverProcess) return;
  try {
    if (typeof serverProcess.kill === 'function') {
      serverProcess.kill();
    }
  } catch {
    /* ignore */
  }
  serverProcess = null;
}

function resolveIcon() {
  const icns = path.join(__dirname, 'assets', 'icon.icns');
  const png = path.join(__dirname, 'assets', 'icon.png');
  const ico = path.join(__dirname, 'assets', 'icon.ico');
  if (process.platform === 'darwin' && fs.existsSync(icns)) return icns;
  if (process.platform === 'win32' && fs.existsSync(ico)) return ico;
  if (fs.existsSync(png)) return png;
  return undefined;
}

async function createWindow() {
  createSplashWindow();
  armSplashTimeout();

  const icon = resolveIcon();
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1200,
    minHeight: 800,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      enableRemoteModule: false,
      preload: path.join(__dirname, 'preload.js'),
    },
    icon,
    titleBarStyle: process.platform === 'darwin' ? 'hiddenInset' : 'default',
    show: false,
  });

  let loadUrl;
  if (isDev) {
    loadUrl = process.env.AYOMANT_DEV_URL || 'http://localhost:3000';
  } else {
    try {
      loadUrl = await startEmbeddedServer();
      if (!loadUrl) {
        throw new Error('Bundle electron-app indisponível no pacote');
      }
    } catch (err) {
      logDesktop(`Falha ao iniciar servidor embebido: ${err}`);
      closeSplashWindow();
      mainWindow.show();
      await mainWindow.loadURL(
        `data:text/html;charset=utf-8,${encodeURIComponent(
          `<!DOCTYPE html><html lang="pt-AO"><head><meta charset="utf-8"/><title>MUFUTU</title><style>
            body{font-family:Inter,system-ui,sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;margin:0;background:var(--bg,#f5f7fa);color:#1a1a1a}
            .box{max-width:420px;padding:32px;text-align:center}
            h1{font-size:22px;margin:0 0 12px;color:#EB5E28}
            p{margin:0 0 20px;line-height:1.5;color:#555}
            button{padding:12px 24px;border:none;border-radius:10px;background:#1565C0;color:#fff;font-weight:700;font-size:14px;cursor:pointer}
          </style></head><body><div class="box"><h1>MUFUTU</h1><p>Não foi possível iniciar o servidor local.</p><p style="font-size:13px">Instale a versão 1.0.3 ou superior e tente novamente.</p><button onclick="location.reload()">Tentar novamente</button></div></body></html>`,
        )}`,
      );
      return;
    }
  }

  let revealed = false;
  const revealMainWindow = () => {
    if (revealed || !mainWindow || mainWindow.isDestroyed()) return;
    revealed = true;
    closeSplashWindow();
    mainWindow.show();
  };

  mainWindow.webContents.on('did-fail-load', (_event, code, description, url) => {
    logDesktop(`did-fail-load ${code} ${description} ${url}`);
    revealMainWindow();
  });

  const desktopLocale = readDesktopLocale();
  await applyLocaleCookieForUrl(loadUrl, desktopLocale);

  void mainWindow.loadURL(loadUrl).catch((err) => {
    logDesktop(`loadURL falhou: ${err}`);
    revealMainWindow();
  });

  mainWindow.webContents.once('did-finish-load', revealMainWindow);
  mainWindow.once('ready-to-show', revealMainWindow);
  setTimeout(revealMainWindow, 6000);

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

function createMenu() {
  const isMac = process.platform === 'darwin';
  const accel = (key) => (isMac ? `Cmd+${key}` : `Ctrl+${key}`);

  const template = [
    ...(isMac
      ? [{
          label: app.getName(),
          submenu: [
            { role: 'about' },
            { type: 'separator' },
            { role: 'services' },
            { type: 'separator' },
            { role: 'hide' },
            { role: 'hideOthers' },
            { role: 'unhide' },
            { type: 'separator' },
            { role: 'quit' },
          ],
        }]
      : []),
    {
      label: 'Ficheiro',
      submenu: [
        {
          label: 'Nova Ordem de Trabalho',
          accelerator: accel('N'),
          click: () => mainWindow?.webContents.send('new-work-order'),
        },
        { type: 'separator' },
        isMac ? { role: 'close' } : { role: 'quit' },
      ],
    },
    {
      label: 'Editar',
      submenu: [
        { role: 'undo' },
        { role: 'redo' },
        { type: 'separator' },
        { role: 'cut' },
        { role: 'copy' },
        { role: 'paste' },
        { role: 'selectAll' },
      ],
    },
    {
      label: 'Ver',
      submenu: [
        {
          label: 'Recarregar',
          accelerator: accel('R'),
          click: () => mainWindow?.reload(),
        },
        { role: 'toggleDevTools' },
        { type: 'separator' },
        { role: 'resetZoom' },
        { role: 'zoomIn' },
        { role: 'zoomOut' },
        { type: 'separator' },
        { role: 'togglefullscreen' },
      ],
    },
    {
      label: 'Ajuda',
      submenu: [
        {
          label: 'Documentação',
          click: () => require('electron').shell.openExternal('https://mufutu.ao/docs'),
        },
      ],
    },
  ];

  Menu.setApplicationMenu(Menu.buildFromTemplate(template));
}

const gotTheLock = app.requestSingleInstanceLock();
if (!gotTheLock) {
  app.quit();
} else {
  app.on('second-instance', () => {
    if (mainWindow) {
      if (mainWindow.isMinimized()) mainWindow.restore();
      mainWindow.focus();
    }
  });
}

app.whenReady().then(async () => {
  await createWindow();
  createMenu();
  app.on('activate', async () => {
    if (BrowserWindow.getAllWindows().length === 0) await createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

app.on('before-quit', () => {
  stopEmbeddedServer();
});

ipcMain.handle('get-app-version', () => app.getVersion());
ipcMain.handle('get-app-name', () => app.getName());

ipcMain.handle('language:get', () => readDesktopLocale());
ipcMain.handle('language:set', async (_e, lang) => {
  const loc = writeDesktopLocale(lang);
  if (mainWindow && !mainWindow.isDestroyed()) {
    const url = mainWindow.webContents.getURL();
    if (url.startsWith('http')) {
      await applyLocaleCookieForUrl(url, loc);
    }
    mainWindow.webContents.reload();
  }
  return loc;
});

ipcMain.handle('localData:get', (_e, key) => {
  const vaultPath = path.join(app.getPath('userData'), 'vault', `${key}.enc`);
  if (!fs.existsSync(vaultPath)) return null;
  try {
    const raw = fs.readFileSync(vaultPath, 'utf8');
    if (safeStorage.isEncryptionAvailable()) {
      return JSON.parse(safeStorage.decryptString(Buffer.from(raw, 'base64')));
    }
    return JSON.parse(decryptAes256Gcm(raw));
  } catch {
    return null;
  }
});

ipcMain.handle('localData:set', (_e, key, value) => {
  const dir = path.join(app.getPath('userData'), 'vault');
  fs.mkdirSync(dir, { recursive: true });
  const vaultPath = path.join(dir, `${key}.enc`);
  const json = JSON.stringify(value);
  if (safeStorage.isEncryptionAvailable()) {
    fs.writeFileSync(vaultPath, safeStorage.encryptString(json).toString('base64'));
  } else {
    fs.writeFileSync(vaultPath, encryptAes256Gcm(json));
  }
  return true;
});

function userData() {
  return app.getPath('userData');
}

ipcMain.handle('labels:scanWifi', () => ({
  source: 'edge',
  stub: false,
  networks: labelsBridge.scanWifiNetworks(),
  scannedAt: new Date().toISOString(),
}));

ipcMain.handle('labels:getLiveStatus', () => labelsBridge.getLiveStatus(userData()));

ipcMain.handle('labels:discoverPrinters', () => labelsBridge.discoverPrinters(userData()));

ipcMain.handle('labels:setPrinterHost', (_e, host, model) => {
  labelsBridge.writePrinterConfig(userData(), { host, model: model || 'Zebra ZD421' });
  return labelsBridge.readPrinterConfig(userData());
});

ipcMain.handle('labels:getPrinterConfig', () => labelsBridge.readPrinterConfig(userData()));

ipcMain.handle('labels:connectWifi', async (_e, { ssid, password }) => {
  await labelsBridge.configurePrinterWifi(userData(), ssid, password);
  return labelsBridge.getLiveStatus(userData());
});

ipcMain.handle('labels:configureEthernet', async (_e, config) => {
  await labelsBridge.configurePrinterEthernet(userData(), config);
  return labelsBridge.getLiveStatus(userData());
});

ipcMain.handle('labels:printLabels', async (_e, items) => {
  return labelsBridge.printLabels(userData(), items);
});

ipcMain.handle('print:qrcode', async (_e, items) => {
  const list = Array.isArray(items) ? items : [items];
  return labelsBridge.printLabels(userData(), list);
});

ipcMain.handle('connectivity:getStatus', () => connectivityBridge.getSystemConnectivityStatus());

ipcMain.handle('connectivity:scanWifi', () => ({
  networks: connectivityBridge.scanWifiNetworks(),
  scannedAt: new Date().toISOString(),
}));

ipcMain.handle('connectivity:connectWifi', async (_e, { ssid, password }) => {
  connectivityBridge.connectHostWifi(ssid, password);
  return connectivityBridge.getSystemConnectivityStatus();
});
