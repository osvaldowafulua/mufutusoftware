// "Version gate": bloqueia o arranque quando a versão instalada está abaixo da
// mínima aceite, publicada em releases/latest.json (main). Suba minimumVersion
// nesse ficheiro e faça commit para forçar todos os clientes a actualizar — não
// precisa de publicar um novo release. Nunca bloqueia por falha de rede/manifesto
// indisponível: o app tem de continuar a funcionar offline no terreno.
const { compareSemver } = require('./electron-semver');

const MANIFEST_URL =
  'https://raw.githubusercontent.com/osvaldowafulua/mufutusoftware/main/releases/latest.json';
const PLATFORM_KEY = 'macos';
const GATE_TIMEOUT_MS = 6000;

async function checkVersionGate(
  currentVersion,
  { manifestUrl = MANIFEST_URL, platformKey = PLATFORM_KEY, timeoutMs = GATE_TIMEOUT_MS } = {},
) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const res = await fetch(manifestUrl, { signal: controller.signal });
    if (!res.ok) {
      return { status: 'check-failed', reason: `HTTP ${res.status}` };
    }

    const manifest = await res.json();
    const platform = manifest?.platforms?.[platformKey];
    if (!platform) {
      return { status: 'check-failed', reason: 'plataforma ausente do manifesto' };
    }

    const { latestVersion, minimumVersion, downloadUrl, note } = platform;

    if (minimumVersion && compareSemver(currentVersion, minimumVersion) < 0) {
      return {
        status: 'update-required',
        latestVersion,
        minimumVersion,
        downloadUrl,
        note,
      };
    }

    if (latestVersion && compareSemver(latestVersion, currentVersion) > 0) {
      return { status: 'update-available', latestVersion, minimumVersion, downloadUrl, note };
    }

    return { status: 'up-to-date', latestVersion, minimumVersion };
  } catch (err) {
    return { status: 'check-failed', reason: String(err?.message || err) };
  } finally {
    clearTimeout(timeout);
  }
}

module.exports = { checkVersionGate, MANIFEST_URL, PLATFORM_KEY };
