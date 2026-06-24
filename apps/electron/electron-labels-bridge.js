/**
 * MUFUTU — bridge hardware etiquetas QR (Electron main process).
 * WiFi scan (OS), Ethernet (NIC), impressora Zebra ZPL (TCP 9100), Link-OS HTTP.
 */
const { execSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const net = require('net');
const path = require('path');
const http = require('http');

const ZPL_PORT = 9100;
const LINK_OS_PORT = 80;
const PROBE_MS = 1200;

function logLabel(message) {
  const line = `[mufutu-labels] ${message}`;
  process.stdout.write(`${line}\n`);
}

function readPrinterConfig(userDataPath) {
  const p = path.join(userDataPath, 'label-printer.json');
  try {
    if (fs.existsSync(p)) return JSON.parse(fs.readFileSync(p, 'utf8'));
  } catch {
    /* ignore */
  }
  return { host: null, model: 'Zebra ZD421' };
}

function writePrinterConfig(userDataPath, patch) {
  const p = path.join(userDataPath, 'label-printer.json');
  const current = readPrinterConfig(userDataPath);
  fs.writeFileSync(p, JSON.stringify({ ...current, ...patch }, null, 2));
  return { ...current, ...patch };
}

function scanWifiMac() {
  const airport =
    '/System/Library/PrivateFrameworks/Apple80211.framework/Versions/Current/Resources/airport';
  if (!fs.existsSync(airport)) return [];
  const out = execSync(`"${airport}" -s`, { encoding: 'utf8', timeout: 20000 });
  const networks = [];
  const seen = new Set();
  for (const line of out.split('\n').slice(1)) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    const rssiMatch = trimmed.match(/\s(-\d{2,3})\s+\d+\s/);
    if (!rssiMatch) continue;
    const signalDbm = parseInt(rssiMatch[1], 10);
    const beforeRssi = trimmed.slice(0, trimmed.indexOf(rssiMatch[1])).trim();
    const bssidParts = beforeRssi.split(/\s+/);
    const bssid = bssidParts[bssidParts.length - 1];
    const ssid = bssidParts.slice(0, -1).join(' ').trim();
    if (!ssid || seen.has(ssid)) continue;
    seen.add(ssid);
    const secured = /WPA|WEP|PSK/i.test(trimmed);
    networks.push({ ssid, signalDbm, secured });
  }
  return networks.sort((a, b) => b.signalDbm - a.signalDbm);
}

function scanWifiLinux() {
  try {
    const out = execSync('nmcli -t -f SSID,SIGNAL,SECURITY dev wifi list', {
      encoding: 'utf8',
      timeout: 20000,
    });
    const networks = [];
    const seen = new Set();
    for (const line of out.split('\n')) {
      const [ssid, signal, security] = line.split(':');
      if (!ssid || seen.has(ssid)) continue;
      seen.add(ssid);
      networks.push({
        ssid,
        signalDbm: -100 + Math.round((parseInt(signal, 10) / 100) * 60),
        secured: security && security !== '--',
      });
    }
    return networks;
  } catch {
    return [];
  }
}

function scanWifiWindows() {
  try {
    const out = execSync('netsh wlan show networks mode=bssid', {
      encoding: 'utf8',
      timeout: 20000,
    });
    const networks = [];
    const seen = new Set();
    let currentSsid = '';
    for (const line of out.split('\n')) {
      const ssidMatch = line.match(/SSID \d+ : (.+)/);
      if (ssidMatch) currentSsid = ssidMatch[1].trim();
      const sigMatch = line.match(/Signal\s+:\s+(\d+)%/);
      if (sigMatch && currentSsid && !seen.has(currentSsid)) {
        seen.add(currentSsid);
        const pct = parseInt(sigMatch[1], 10);
        networks.push({
          ssid: currentSsid,
          signalDbm: -100 + Math.round((pct / 100) * 60),
          secured: true,
        });
      }
    }
    return networks;
  } catch {
    return [];
  }
}

function scanWifiNetworks() {
  logLabel(`scan WiFi (${process.platform})`);
  if (process.platform === 'darwin') return scanWifiMac();
  if (process.platform === 'linux') return scanWifiLinux();
  if (process.platform === 'win32') return scanWifiWindows();
  return [];
}

function getDefaultGateway() {
  try {
    if (process.platform === 'darwin') {
      const out = execSync('route -n get default 2>/dev/null | grep gateway', {
        encoding: 'utf8',
      });
      const m = out.match(/gateway:\s*(\S+)/);
      return m ? m[1] : null;
    }
    if (process.platform === 'linux') {
      const out = execSync("ip route | awk '/default/ {print $3; exit}'", { encoding: 'utf8' });
      return out.trim() || null;
    }
    if (process.platform === 'win32') {
      const out = execSync('powershell -Command "(Get-NetRoute -DestinationPrefix 0.0.0.0/0 | Select-Object -First 1).NextHop"', {
        encoding: 'utf8',
      });
      return out.trim() || null;
    }
  } catch {
    /* ignore */
  }
  return null;
}

function getEthernetStatus() {
  const ifaces = os.networkInterfaces();
  let best = null;
  for (const [name, addrs] of Object.entries(ifaces)) {
    if (/lo|awdl|llw|utun|bridge/i.test(name)) continue;
    const ipv4 = (addrs || []).find((a) => a.family === 'IPv4' && !a.internal);
    if (!ipv4) continue;
    const isEth = /^(en|eth|Ethernet)/i.test(name);
    if (!best || isEth) {
      best = {
        link: 'up',
        dhcp: true,
        ip: ipv4.address,
        gateway: getDefaultGateway(),
        subnet: ipv4.netmask,
        interface: name,
      };
      if (isEth) break;
    }
  }
  return (
    best || {
      link: 'down',
      dhcp: true,
      ip: null,
      gateway: null,
      subnet: null,
      interface: null,
    }
  );
}

function getConnectedWifi(networks) {
  try {
    if (process.platform === 'darwin') {
      const airport =
        '/System/Library/PrivateFrameworks/Apple80211.framework/Versions/Current/Resources/airport';
      const out = execSync(`"${airport}" -I`, { encoding: 'utf8', timeout: 5000 });
      const ssid = out.match(/ SSID: (.+)/)?.[1]?.trim();
      const rssi = parseInt(out.match(/ agrCtlRSSI: (-\d+)/)?.[1] || '-60', 10);
      if (ssid) return { status: 'connected', ssid, signalDbm: rssi };
    }
    if (process.platform === 'linux') {
      const out = execSync('nmcli -t -f active,ssid,signal dev wifi', { encoding: 'utf8' });
      for (const line of out.split('\n')) {
        const [active, ssid, signal] = line.split(':');
        if (active === 'yes' && ssid) {
          return {
            status: 'connected',
            ssid,
            signalDbm: -100 + Math.round((parseInt(signal, 10) / 100) * 60),
          };
        }
      }
    }
  } catch {
    /* ignore */
  }
  return { status: 'disconnected', ssid: null, signalDbm: null };
}

function tcpProbe(host, port, timeoutMs = PROBE_MS) {
  return new Promise((resolve) => {
    const socket = new net.Socket();
    let done = false;
    const finish = (ok) => {
      if (done) return;
      done = true;
      try {
        socket.destroy();
      } catch {
        /* ignore */
      }
      resolve(ok);
    };
    socket.setTimeout(timeoutMs);
    socket.once('connect', () => finish(true));
    socket.once('timeout', () => finish(false));
    socket.once('error', () => finish(false));
    socket.connect(port, host);
  });
}

function sendZpl(host, zpl) {
  return new Promise((resolve, reject) => {
    const socket = new net.Socket();
    socket.setTimeout(8000);
    socket.connect(ZPL_PORT, host, () => {
      socket.write(zpl, 'utf8', () => {
        socket.end();
        resolve(true);
      });
    });
    socket.on('timeout', () => {
      socket.destroy();
      reject(new Error('Timeout ao enviar ZPL'));
    });
    socket.on('error', reject);
  });
}

function buildZplLabel(item) {
  const code = String(item.code || '').slice(0, 40);
  const label = String(item.label || code).slice(0, 32);
  const payload = String(item.qrPayload || `MUFUTU:${item.type}:${code}`);
  return `^XA
^FO40,30^BQN,2,5^FDMA,${payload}^FS
^FO40,200^A0N,28,28^FD${label}^FS
^FO40,240^A0N,22,22^FD${code}^FS
^XZ
`;
}

function httpGetJson(host, pathName, timeoutMs = 3000) {
  return new Promise((resolve, reject) => {
    const req = http.get(
      { host, port: LINK_OS_PORT, path: pathName, timeout: timeoutMs },
      (res) => {
        let body = '';
        res.on('data', (c) => {
          body += c;
        });
        res.on('end', () => {
          try {
            resolve(JSON.parse(body));
          } catch {
            resolve({ raw: body, statusCode: res.statusCode });
          }
        });
      },
    );
    req.on('error', reject);
    req.on('timeout', () => {
      req.destroy();
      reject(new Error('timeout'));
    });
  });
}

async function probePrinter(userDataPath) {
  const cfg = readPrinterConfig(userDataPath);
  if (!cfg.host) {
    return { status: 'offline', model: cfg.model || 'Zebra ZD421', labelsRemaining: 0, host: null };
  }
  const online = await tcpProbe(cfg.host, ZPL_PORT);
  if (!online) {
    return { status: 'offline', model: cfg.model, labelsRemaining: cfg.labelsRemaining ?? 0, host: cfg.host };
  }
  let labelsRemaining = cfg.labelsRemaining ?? 500;
  try {
    const status = await httpGetJson(cfg.host, '/server/status.json');
    if (status?.media?.labels_remaining != null) {
      labelsRemaining = status.media.labels_remaining;
      writePrinterConfig(userDataPath, { labelsRemaining });
    }
  } catch {
    /* ZPL-only printers may not expose Link-OS */
  }
  return { status: 'ready', model: cfg.model || 'Zebra ZD421', labelsRemaining, host: cfg.host };
}

async function discoverPrinters(userDataPath) {
  const eth = getEthernetStatus();
  const found = [];
  const cfg = readPrinterConfig(userDataPath);
  if (cfg.host) {
    const ok = await tcpProbe(cfg.host, ZPL_PORT);
    if (ok) found.push({ host: cfg.host, model: cfg.model || 'Zebra ZD421', port: ZPL_PORT });
  }
  if (eth.ip) {
    const parts = eth.ip.split('.');
    if (parts.length === 4) {
      const base = `${parts[0]}.${parts[1]}.${parts[2]}`;
      const candidates = [parts.slice(0, 3).join('.') + '.250', `${base}.100`, `${base}.101`, `${base}.200`];
      for (const host of candidates) {
        if (found.some((f) => f.host === host)) continue;
        if (await tcpProbe(host, ZPL_PORT)) {
          found.push({ host, model: 'Zebra (detectada)', port: ZPL_PORT });
        }
      }
    }
  }
  return found;
}

async function configurePrinterWifi(userDataPath, ssid, password) {
  const cfg = readPrinterConfig(userDataPath);
  if (!cfg.host) throw new Error('Defina o IP da impressora antes de configurar WiFi.');
  logLabel(`configure WiFi impressora ${cfg.host} → SSID ${ssid}`);
  const lines = [
    `! U1 setvar "wlan.ssid" "${ssid.replace(/"/g, '')}"`,
    '! U1 setvar "wlan.enable" "on"',
    password ? `! U1 setvar "wlan.wpa.enable" "on"` : '',
    password ? `! U1 setvar "wlan.wpa.psk" "${password.replace(/"/g, '')}"` : '',
    '! U1 do "wlan.commit" ""',
  ].filter(Boolean);
  await sendZpl(cfg.host, lines.join('\r\n') + '\r\n');
  writePrinterConfig(userDataPath, { lastWifiSsid: ssid });
  return true;
}

async function configurePrinterEthernet(userDataPath, { dhcp, ip, gateway, subnet }) {
  const cfg = readPrinterConfig(userDataPath);
  if (!cfg.host) throw new Error('Defina o IP da impressora antes de configurar Ethernet.');
  logLabel(`configure Ethernet impressora ${cfg.host} (dhcp=${dhcp})`);
  const cmds = dhcp
    ? ['! U1 setvar "ip.protocol" "dhcp"', '! U1 do "device.reset" ""']
    : [
        '! U1 setvar "ip.protocol" "Permanent"',
        `! U1 setvar "ip.addr" "${ip}"`,
        `! U1 setvar "ip.gateway" "${gateway || ''}"`,
        `! U1 setvar "ip.netmask" "${subnet || '255.255.255.0'}"`,
        '! U1 do "device.reset" ""',
      ];
  await sendZpl(cfg.host, cmds.join('\r\n') + '\r\n');
  return true;
}

async function getLiveStatus(userDataPath) {
  const networks = scanWifiNetworks();
  const wifi = getConnectedWifi(networks);
  const ethernet = getEthernetStatus();
  const printer = await probePrinter(userDataPath);
  const nfcAvailable = process.platform === 'android' || process.platform === 'win32';
  return {
    source: 'edge',
    stub: false,
    wifi,
    ethernet: {
      link: ethernet.link,
      dhcp: ethernet.dhcp,
      ip: ethernet.ip,
      gateway: ethernet.gateway,
      subnet: ethernet.subnet,
    },
    nfc: {
      available: nfcAvailable || typeof globalThis.NDEFReader !== 'undefined',
      pairStatus: 'idle',
      deviceName: null,
    },
    printer,
    wifiNetworks: networks,
    scannedAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };
}

async function printLabels(userDataPath, items) {
  const cfg = readPrinterConfig(userDataPath);
  if (!cfg.host) throw new Error('Impressora não configurada. Defina o IP em Conexão → Impressora.');
  logLabel(`print ${items.length} etiqueta(s) → ${cfg.host}:${ZPL_PORT}`);
  const zpl = items.map(buildZplLabel).join('\n');
  await sendZpl(cfg.host, zpl);
  const remaining = Math.max(0, (cfg.labelsRemaining ?? 500) - items.length);
  writePrinterConfig(userDataPath, { labelsRemaining: remaining });
  return { printed: items.length, labelsRemaining: remaining };
}

module.exports = {
  scanWifiNetworks,
  getEthernetStatus,
  getLiveStatus,
  discoverPrinters,
  readPrinterConfig,
  writePrinterConfig,
  configurePrinterWifi,
  configurePrinterEthernet,
  printLabels,
  buildZplLabel,
};
