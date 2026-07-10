function parseVersionFromTag(tag) {
  const semver = String(tag).match(/^v(.+)$/i);
  if (semver) return semver[1];
  const legacy = String(tag).match(/^desktop(?:-mac|-win)?\/v?(.+)$/i);
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

module.exports = { parseVersionFromTag, compareSemver };
