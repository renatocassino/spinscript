#!/usr/bin/env bash
# Build used by Cloudflare (Workers Builds / Pages): its build image has Node
# but no .NET, so this installs the .NET SDK + the wasm-tools workload into
# $HOME, publishes SpinScript.Wasm (Release, trimmed) and builds the Astro
# site into spinscript-web/dist — the folder wrangler.jsonc serves.
#
# Also works locally: `bash scripts/cloudflare-build.sh`.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks | grep -q '^10\.'; then
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$DOTNET_ROOT:$PATH"
  if [ ! -x "$DOTNET_ROOT/dotnet" ]; then
    echo "==> Installing .NET 10 SDK into $DOTNET_ROOT"
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_ROOT"
  fi
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

if ! dotnet workload list | grep -q wasm-tools; then
  echo "==> Installing wasm-tools workload"
  dotnet workload install wasm-tools --skip-manifest-update
fi

echo "==> Publishing SpinScript.Wasm (Release)"
dotnet publish SpinScript.Wasm -c Release

echo "==> Building spinscript-web"
cd spinscript-web
npm ci
SPINSCRIPT_WASM_CONFIG=Release npm run build
