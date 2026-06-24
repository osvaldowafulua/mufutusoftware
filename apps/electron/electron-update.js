const { app, dialog } = require('electron');

const GITHUB_OWNER = 'osvaldowafulua';
const GITHUB_REPO = 'mufutusoftware';
const TAG_PREFIXES = ['v'];

let updateCheckInProgress = false;
let logFn = (msg) => process.stdout.write(`[mufutu-update] ${msg}\n`);
let autoUpdater;

function getAutoUpdater() {
  if (!autoUpdater) {
    ({ autoUpdater } = require('electron-updater'));
  }
  return autoUpdater;
}

function setUpdateLogger(fn) {
  logFn = fn;
}

function parseVersionFromTag(tag) {
  const semver = String(tag).match(/^v(.+)$/i);
  if (semver) return semver[1];
  const legacy = String(tag).match(/^desktop(?:-mac)?\/v?(.+)$/i);
  return legacy ? legacy[1] : null;
}

function compareSemver(a, b) {
  const pa = String(a).replace(/^v/i, '').split('.').map((n) => parseInt(n, 10) || 0);
  const pb = String(b).replace(/^v/i, '').split('.').map((n) => parseInt(n, 10) || 0);
  const len = Math.max(pa.length, pb.length, 3);
  for (let i = 0; i < len; i += 1) {
    const da = pa[i] || 0;
    const db = pb[i] || 0;
    if (da > db) return 1;
    if (da < db) return -1;
  }
  return 0;
}

async function fetchLatestMacRelease() {
  const url = `https://api.github.com/repos/${GITHUB_OWNER}/${GITHUB_REPO}/releases?per_page=30`;
  const res = await fetch(url, {
    headers: {
      Accept: 'application/vnd.github+json',
      'User-Agent': 'MUFUTU-Desktop',
    },
  });
  if (!res.ok) {
    throw new Error(`GitHub API respondeu ${res.status}`);
  }
  const releases = await res.json();
  const candidates = releases.filter(
    (r) =>
      !r.draft
      && !r.prerelease
      && TAG_PREFIXES.some((prefix) => String(r.tag_name || '').startsWith(prefix)),
  );
  if (!candidates.length) return null;
  return candidates.sort(
    (a, b) => compareSemver(parseVersionFromTag(b.tag_name), parseVersionFromTag(a.tag_name)),
  )[0];
}

function setupAutoUpdater(mainWindow) {
  const updater = getAutoUpdater();
  updater.autoDownload = false;
  updater.autoInstallOnAppQuit = true;
  updater.allowDowngrade = false;

  updater.on('checking-for-update', () => {
    logFn('A verificar actualizações…');
  });

  updater.on('error', (err) => {
    logFn(`Erro auto-update: ${err?.message || err}`);
    if (mainWindow && !mainWindow.isDestroyed()) {
      dialog.showErrorBox('Actualização', `Erro ao actualizar: ${err?.message || err}`);
    }
  });

  updater.on('update-available', (info) => {
    if (!mainWindow || mainWindow.isDestroyed()) return;
    dialog
      .showMessageBox(mainWindow, {
        type: 'info',
        title: 'Actualização disponível',
        message: `Versão ${info.version} disponível`,
        detail: 'Deseja descarregar a actualização agora?',
        buttons: ['Descarregar', 'Mais tarde'],
        defaultId: 0,
        cancelId: 1,
      })
      .then(({ response }) => {
        if (response === 0) {
          logFn('A descarregar actualização…');
          updater.downloadUpdate();
        }
      });
  });

  updater.on('download-progress', (progress) => {
    const pct = Math.round(progress.percent || 0);
    logFn(`A descarregar… ${pct}%`);
  });

  updater.on('update-downloaded', () => {
    if (!mainWindow || mainWindow.isDestroyed()) return;
    dialog
      .showMessageBox(mainWindow, {
        type: 'info',
        title: 'Actualização pronta',
        message: 'Reiniciar para instalar',
        detail: 'A actualização foi descarregada. Reinicie a aplicação para aplicar.',
        buttons: ['Reiniciar agora', 'Mais tarde'],
        defaultId: 0,
        cancelId: 1,
      })
      .then(({ response }) => {
        if (response === 0) {
          updater.quitAndInstall(false, true);
        }
      });
  });

  updater.on('update-not-available', () => {
    logFn('Sem actualizações (feed electron-updater)');
  });
}

async function checkForUpdates(mainWindow, { silent = false, isPackaged = true } = {}) {
  if (!isPackaged) {
    if (!silent && mainWindow && !mainWindow.isDestroyed()) {
      dialog.showMessageBox(mainWindow, {
        type: 'info',
        title: 'MUFUTU',
        message: 'Actualizações automáticas só estão disponíveis na aplicação instalada (DMG).',
      });
    }
    return;
  }

  if (updateCheckInProgress) return;
  updateCheckInProgress = true;

  try {
    const release = await fetchLatestMacRelease();
    if (!release) {
      if (!silent && mainWindow && !mainWindow.isDestroyed()) {
        dialog.showMessageBox(mainWindow, {
          type: 'info',
          title: 'MUFUTU',
          message: 'Nenhuma release macOS encontrada no GitHub.',
        });
      }
      return;
    }

    const remoteVersion = parseVersionFromTag(release.tag_name);
    const currentVersion = app.getVersion();
    logFn(`Versão actual: ${currentVersion} · remota: ${remoteVersion} (${release.tag_name})`);

    if (remoteVersion && compareSemver(remoteVersion, currentVersion) <= 0) {
      if (!silent && mainWindow && !mainWindow.isDestroyed()) {
        dialog.showMessageBox(mainWindow, {
          type: 'info',
          title: 'MUFUTU',
          message: 'Está na versão mais recente.',
          detail: `Versão instalada: ${currentVersion}`,
        });
      }
      return;
    }

    const tag = release.tag_name;
    const updater = getAutoUpdater();
    updater.setFeedURL({
      provider: 'generic',
      url: `https://github.com/${GITHUB_OWNER}/${GITHUB_REPO}/releases/download/${tag}/`,
    });

    const result = await updater.checkForUpdates();
    if (!result?.updateInfo && !silent && mainWindow && !mainWindow.isDestroyed()) {
      dialog.showMessageBox(mainWindow, {
        type: 'info',
        title: 'MUFUTU',
        message: 'Não foi possível obter metadados de actualização (latest-mac.yml).',
        detail: `Abra a release ${tag} no GitHub para descarregar manualmente.`,
      });
    }
  } catch (err) {
    logFn(`Falha verificação: ${err?.message || err}`);
    if (!silent && mainWindow && !mainWindow.isDestroyed()) {
      dialog.showMessageBox(mainWindow, {
        type: 'error',
        title: 'Actualização',
        message: 'Não foi possível verificar actualizações.',
        detail: String(err?.message || err),
      });
    }
  } finally {
    updateCheckInProgress = false;
  }
}

module.exports = {
  setupAutoUpdater,
  checkForUpdates,
  setUpdateLogger,
  GITHUB_RELEASES_URL: `https://github.com/${GITHUB_OWNER}/${GITHUB_REPO}/releases`,
};
