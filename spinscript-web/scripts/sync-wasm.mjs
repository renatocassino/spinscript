#!/usr/bin/env node
// Copies the SpinScript.Wasm build output into public/wasm so the browser
// can fetch it. Not a Vite build input (dotnet build owns it), so this
// runs as a predev/prebuild step instead of via the bundler.
import { cpSync, existsSync, mkdirSync, rmSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
// `dotnet build` (Debug) is the quick dev loop; `dotnet publish -c Release`
// is the small, trimmed build used for deploys. SPINSCRIPT_WASM_CONFIG picks
// one explicitly; otherwise whichever was built most recently wins.
const bundleFor = (config) =>
  join(projectRoot, '..', 'SpinScript.Wasm', 'bin', config, 'net10.0', 'browser-wasm', 'AppBundle', '_framework');

const builtAt = (dir) => {
  const marker = join(dir, 'dotnet.boot.js');
  return existsSync(marker) ? statSync(marker).mtimeMs : -1;
};

const requested = process.env.SPINSCRIPT_WASM_CONFIG;
const source = requested
  ? bundleFor(requested)
  : [bundleFor('Release'), bundleFor('Debug')].sort((a, b) => builtAt(b) - builtAt(a))[0];
const destination = join(projectRoot, 'public', 'wasm', '_framework');

if (!existsSync(source)) {
  console.error(
    `SpinScript.Wasm build output not found at:\n  ${source}\n\n` +
      'Build it first:\n  dotnet publish ../SpinScript.Wasm -c Release   (or `dotnet build` for Debug)',
  );
  process.exit(1);
}

rmSync(destination, { recursive: true, force: true });
mkdirSync(dirname(destination), { recursive: true });
cpSync(source, destination, { recursive: true });

console.log(`Synced SpinScript WASM runtime -> ${destination}`);
