#!/usr/bin/env node
import { createRequire } from 'module';
import { dirname, join, resolve } from 'path';
import { fileURLToPath } from 'url';
import { readFileSync, writeFileSync, copyFileSync, existsSync, mkdirSync } from 'fs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = resolve(__dirname, '../../..');
const electronRoot = join(root, 'apps', 'electron');
const require = createRequire(join(root, 'electron/package.json'));
const JavaScriptObfuscator = require('javascript-obfuscator');
const backupDir = join(electronRoot, '.electron-obfuscate-backup');

const targets = ['electron-main.js', 'preload.js'];
const options = {
  compact: true,
  controlFlowFlattening: false,
  deadCodeInjection: false,
  stringArray: true,
  stringArrayThreshold: 0.5,
  rotateStringArray: true,
  selfDefending: false,
  target: 'node',
};

mkdirSync(backupDir, { recursive: true });

for (const file of targets) {
  const src = join(electronRoot, file);
  if (!existsSync(src)) {
    console.warn(`skip ${file}`);
    continue;
  }
  copyFileSync(src, join(backupDir, file));
  const result = JavaScriptObfuscator.obfuscate(readFileSync(src, 'utf8'), options);
  writeFileSync(src, result.getObfuscatedCode());
  console.log(`ofuscado: ${file}`);
}
