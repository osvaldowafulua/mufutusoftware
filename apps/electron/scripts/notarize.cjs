/**
 * afterSign hook — notarização Apple (Developer ID + App Store Connect).
 * Requer: APPLE_ID, APPLE_APP_SPECIFIC_PASSWORD, APPLE_TEAM_ID
 * Opcional em CI: importar certificado via import-apple-cert.sh antes do build.
 */
const { notarize } = require('@electron/notarize');

exports.default = async function notarizeMac(context) {
  if (context.electronPlatformName !== 'darwin') return;

  const { appleId, appleIdPassword, teamId } = {
    appleId: process.env.APPLE_ID,
    appleIdPassword: process.env.APPLE_APP_SPECIFIC_PASSWORD,
    teamId: process.env.APPLE_TEAM_ID,
  };

  if (!appleId || !appleIdPassword || !teamId) {
    console.log(
      '[notarize] Ignorado — defina APPLE_ID, APPLE_APP_SPECIFIC_PASSWORD e APPLE_TEAM_ID para notarizar.',
    );
    return;
  }

  const appName = context.packager.appInfo.productFilename;
  const appPath = `${context.appOutDir}/${appName}.app`;

  console.log(`[notarize] A enviar ${appPath} para Apple…`);
  await notarize({
    appPath,
    appleId,
    appleIdPassword,
    teamId,
  });
  console.log('[notarize] Concluído.');
};
