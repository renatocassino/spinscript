#!/usr/bin/env node
// Copies the SpinScript.Wasm build output into public/wasm so the browser
// can fetch it. Not a Vite build input (dotnet build owns it), so this
// runs as a predev/prebuild step instead of via the bundler.
import { cpSync, existsSync, mkdirSync, rmSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const source = join(
  projectRoot,
  '..',
  'SpinScript.Wasm',
  'bin',
  'Debug',
  'net10.0',
  'browser-wasm',
  'AppBundle',
  '_framework',
);
const destination = join(projectRoot, 'public', 'wasm', '_framework');

if (!existsSync(source)) {
  console.error(
    `SpinScript.Wasm build output not found at:\n  ${source}\n\n` +
      'Build it first:\n  (cd ../SpinScript.Wasm && dotnet build)',
  );
  process.exit(1);
}

rmSync(destination, { recursive: true, force: true });
mkdirSync(dirname(destination), { recursive: true });
cpSync(source, destination, { recursive: true });

console.log(`Synced SpinScript WASM runtime -> ${destination}`);
