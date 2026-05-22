#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIGURATION="${CONFIGURATION:-Release}"
OUTPUT_DIR="${OUTPUT_DIR:-$ROOT_DIR/artifacts/packages}"

mkdir -p "$OUTPUT_DIR"
find "$OUTPUT_DIR" -maxdepth 1 -type f \( -name 'Kawa.*.nupkg' -o -name 'Kawa.*.snupkg' \) -delete

projects=(
  "src/Kawa.Abstractions/Kawa.Abstractions.csproj"
  "src/Kawa.Core/Kawa.Core.csproj"
  "src/Kawa.Web/Kawa.Web.csproj"
  "src/Kawa.FSharp/Kawa.FSharp.fsproj"
)

test_projects=(
  "tests/Kawa.Core.Tests/Kawa.Core.Tests.csproj"
  "tests/Kawa.Web.Tests/Kawa.Web.Tests.csproj"
)

for project in "${projects[@]}" "${test_projects[@]}"; do
  dotnet build "$ROOT_DIR/$project" \
    --no-restore \
    --configuration "$CONFIGURATION" \
    --disable-build-servers \
    -maxcpucount:1
done

for project in "${test_projects[@]}"; do
  dotnet test "$ROOT_DIR/$project" \
    --no-restore \
    --no-build \
    --configuration "$CONFIGURATION" \
    --disable-build-servers \
    -maxcpucount:1
done

for project in "${projects[@]}"; do
  dotnet pack "$ROOT_DIR/$project" \
    --no-restore \
    --no-build \
    --configuration "$CONFIGURATION" \
    --output "$OUTPUT_DIR" \
    --disable-build-servers \
    -maxcpucount:1
done
