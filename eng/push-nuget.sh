#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGE_DIR="${PACKAGE_DIR:-$ROOT_DIR/artifacts/packages}"
NUGET_SOURCE="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"

if [[ -z "${NUGET_API_KEY:-}" ]]; then
  echo "NUGET_API_KEY is required." >&2
  exit 1
fi

shopt -s nullglob
packages=("$PACKAGE_DIR"/*.nupkg)

if (( ${#packages[@]} == 0 )); then
  echo "No .nupkg files found in $PACKAGE_DIR. Run bash eng/pack.sh first." >&2
  exit 1
fi

for package in "${packages[@]}"; do
  dotnet nuget push "$package" \
    --api-key "$NUGET_API_KEY" \
    --source "$NUGET_SOURCE" \
    --skip-duplicate
done
