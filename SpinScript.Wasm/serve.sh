#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

dotnet build SpinScript.Wasm.csproj

cd bin/Debug/net10.0/browser-wasm/AppBundle
npx http-serve .
