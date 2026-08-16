#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

dotnet build SpinScript.Wasm.csproj

APP_BUNDLE=bin/Debug/net10.0/browser-wasm/AppBundle

# dotnet build's incremental check ignores index.html/main.js changes
# (they're wired in too late in the SDK's target to count as inputs),
# so copy them by hand to make sure the bundle is always fresh.
cp index.html main.js "$APP_BUNDLE/"

cd "$APP_BUNDLE"
npx http-serve .
