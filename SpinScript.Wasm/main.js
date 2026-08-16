import { dotnet } from './_framework/dotnet.js';

globalThis.SpinScriptReady = (async () => {
  const { getAssemblyExports } = await dotnet.create();
  const exports = await getAssemblyExports('SpinScript.Wasm.dll');

  globalThis.SpinScript = exports.SpinScript.Wasm.Exports;
  return globalThis.SpinScript;
})().catch((error) => {
  console.error('Failed to load SpinScript wasm module:', error);
  throw error;
});
