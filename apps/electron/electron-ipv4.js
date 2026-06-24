/**
 * Resolução IPv4 para pedidos HTTPS externos (evita ETIMEDOUT em IPv6).
 */
const dns = require('dns').promises;

const ipv4Cache = new Map();

async function resolveHostnameIPv4(hostname) {
  if (ipv4Cache.has(hostname)) return ipv4Cache.get(hostname);
  const entries = await dns.lookup(hostname, { all: true });
  const v4 = entries.find((entry) => entry.family === 4);
  if (!v4) throw new Error(`Sem endereço IPv4 para ${hostname}`);
  ipv4Cache.set(hostname, v4.address);
  return v4.address;
}

function isLocalHost(hostname) {
  return hostname === 'localhost' || hostname === '127.0.0.1';
}

async function buildUpstreamTarget(hostname, port, isHttps) {
  if (isLocalHost(hostname)) {
    return { connectHost: hostname, port, servername: undefined, isHttps };
  }
  const ip = await resolveHostnameIPv4(hostname);
  return { connectHost: ip, port, servername: hostname, isHttps };
}

module.exports = {
  resolveHostnameIPv4,
  isLocalHost,
  buildUpstreamTarget,
};
