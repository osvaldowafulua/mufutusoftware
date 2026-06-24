/**
 * MUFUTU — conectividade do sistema (host OS).
 * WiFi/Ethernet/Dados móveis — separado das etiquetas QR (impressora).
 */
const { execSync } = require('child_process');
const os = require('os');
const {
  scanWifiNetworks,
  getEthernetStatus,
  getConnectedWifi,
} = require('./electron-labels-bridge');

function logConnectivity(message) {
  process.stdout.write(`[mufutu-connectivity] ${message}\n`);
}

function getWifiInterface() {
  try {
    if (process.platform === 'darwin') {
      const out = execSync('networksetup -listallhardwareports', { encoding: 'utf8', timeout: 8000 });
      const match = out.match(/Hardware Port: Wi-Fi\nDevice: (\S+)/);
      return match ? match[1] : 'en0';
    }
    if (process.platform === 'linux') {
      const out = execSync('nmcli -t -f DEVICE,TYPE dev status', { encoding: 'utf8', timeout: 8000 });
      for (const line of out.split('\n')) {
        const [device, type] = line.split(':');
        if (type === 'wifi') return device;
      }
      return 'wlan0';
    }
    if (process.platform === 'win32') {
      return 'Wi-Fi';
    }
  } catch {
    /* ignore */
  }
  return process.platform === 'darwin' ? 'en0' : 'wlan0';
}

function connectHostWifiMac(ssid, password) {
  const iface = getWifiInterface();
  const safeSsid = ssid.replace(/"/g, '\\"');
  const safePass = (password || '').replace(/"/g, '\\"');
  const cmd = password
    ? `networksetup -setairportnetwork "${iface}" "${safeSsid}" "${safePass}"`
    : `networksetup -setairportnetwork "${iface}" "${safeSsid}"`;
  execSync(cmd, { encoding: 'utf8', timeout: 45000 });
}

function connectHostWifiLinux(ssid, password) {
  if (password) {
    execSync(`nmcli dev wifi connect "${ssid.replace(/"/g, '')}" password "${password.replace(/"/g, '')}"`, {
      encoding: 'utf8',
      timeout: 45000,
    });
  } else {
    execSync(`nmcli dev wifi connect "${ssid.replace(/"/g, '')}"`, {
      encoding: 'utf8',
      timeout: 45000,
    });
  }
}

function connectHostWifiWindows(ssid, password) {
  if (password) {
    const xml = `<?xml version="1.0"?>
<WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
  <name>${ssid.replace(/&/g, '&amp;').replace(/</g, '&lt;')}</name>
  <SSIDConfig><SSID><name>${ssid.replace(/&/g, '&amp;').replace(/</g, '&lt;')}</name></SSID></SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM><security><authEncryption><authentication>WPA2PSK</authentication><encryption>AES</encryption></authEncryption>
  <sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>${password.replace(/&/g, '&amp;').replace(/</g, '&lt;')}</keyMaterial></sharedKey>
  </security></MSM>
</WLANProfile>`;
    const fs = require('fs');
    const path = require('path');
    const tmp = path.join(os.tmpdir(), `mufutu-wifi-${Date.now()}.xml`);
    fs.writeFileSync(tmp, xml);
    try {
      execSync(`netsh wlan add profile filename="${tmp}" user=current`, { encoding: 'utf8', timeout: 15000 });
    } finally {
      try {
        fs.unlinkSync(tmp);
      } catch {
        /* ignore */
      }
    }
  }
  execSync(`netsh wlan connect name="${ssid.replace(/"/g, '')}" ssid="${ssid.replace(/"/g, '')}"`, {
    encoding: 'utf8',
    timeout: 45000,
  });
}

function connectHostWifi(ssid, password) {
  logConnectivity(`connect host WiFi → ${ssid} (${process.platform})`);
  if (process.platform === 'darwin') connectHostWifiMac(ssid, password);
  else if (process.platform === 'linux') connectHostWifiLinux(ssid, password);
  else if (process.platform === 'win32') connectHostWifiWindows(ssid, password);
  else throw new Error('Ligação WiFi do sistema não suportada nesta plataforma.');
  return getConnectedWifi(scanWifiNetworks());
}

function getCellularStatus() {
  const result = {
    available: false,
    connected: false,
    interface: null,
    type: null,
    note: null,
  };

  try {
    if (process.platform === 'darwin') {
      const out = execSync('networksetup -listallhardwareports', { encoding: 'utf8', timeout: 8000 });
      const blocks = out.split(/\n\n+/);
      for (const block of blocks) {
        if (/iPhone USB|Thunderbolt Bridge|USB.*Modem|Cellular/i.test(block)) {
          const iface = block.match(/Device: (\S+)/)?.[1];
          const name = block.match(/Hardware Port: (.+)/)?.[1]?.trim();
          if (iface) {
            result.available = true;
            result.interface = iface;
            result.type = name || 'usb_tethering';
            const addrs = os.networkInterfaces()[iface] || [];
            const ipv4 = addrs.find((a) => a.family === 'IPv4' && !a.internal);
            result.connected = !!ipv4;
            result.note =
              'macOS não permite activar dados móveis por API. Use Partilha de Internet do iPhone ou modem USB.';
            break;
          }
        }
      }
      if (!result.available) {
        result.note =
          'Dados móveis não detectados. Ligue iPhone por USB com Partilha de Internet ou use hotspot WiFi.';
      }
    } else if (process.platform === 'win32') {
      const out = execSync(
        'powershell -NoProfile -Command "Get-NetAdapter | Where-Object { $_.InterfaceDescription -match \'Mobile|Cellular|WWAN|LTE\' } | Select-Object -First 1 Name,Status,InterfaceDescription | ConvertTo-Json -Compress"',
        { encoding: 'utf8', timeout: 12000 },
      );
      const parsed = JSON.parse(out || '{}');
      if (parsed?.Name) {
        result.available = true;
        result.interface = parsed.Name;
        result.type = parsed.InterfaceDescription || 'mobile';
        result.connected = String(parsed.Status).toLowerCase() === 'up';
        result.note = 'Modem/dados móveis detectados via adaptador Windows.';
      } else {
        result.note = 'Nenhum adaptador móvel detectado. Verifique modem USB ou hotspot.';
      }
    } else {
      result.note = 'Detecção de dados móveis limitada nesta plataforma.';
    }
  } catch {
    result.note = 'Não foi possível ler estado de dados móveis neste dispositivo.';
  }

  return result;
}

function resolveActivePath(wifi, ethernet, cellular) {
  if (ethernet?.link === 'up' && ethernet?.ip) return 'ethernet';
  if (wifi?.status === 'connected') return 'wifi';
  if (cellular?.connected) return 'cellular';
  if (wifi?.status === 'connected') return 'wifi';
  return 'offline';
}

function getSystemConnectivityStatus() {
  const networks = scanWifiNetworks();
  const wifi = getConnectedWifi(networks);
  const ethernet = getEthernetStatus();
  const cellular = getCellularStatus();
  const activePath = resolveActivePath(wifi, ethernet, cellular);

  return {
    source: 'edge',
    stub: false,
    platform: process.platform,
    activePath,
    wifi: {
      status: wifi.status,
      ssid: wifi.ssid,
      signalDbm: wifi.signalDbm,
      interface: getWifiInterface(),
    },
    ethernet: {
      link: ethernet.link,
      dhcp: ethernet.dhcp,
      ip: ethernet.ip,
      gateway: ethernet.gateway,
      subnet: ethernet.subnet,
      interface: ethernet.interface,
    },
    cellular,
    wifiNetworks: networks,
    scannedAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };
}

module.exports = {
  connectHostWifi,
  getCellularStatus,
  getSystemConnectivityStatus,
  scanWifiNetworks,
  getEthernetStatus,
};
