#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIGURATION="${CONFIGURATION:-Release}"
OUTPUT_DIR="${OUTPUT_DIR:-$ROOT_DIR/artifacts/packages}"
PACKAGE_VERSION="${PACKAGE_VERSION:-}"

msbuild_properties=(
  "-p:ContinuousIntegrationBuild=true"
)

if [[ -n "$PACKAGE_VERSION" ]]; then
  python3 - "$ROOT_DIR/Directory.Build.props" "$PACKAGE_VERSION" <<'PYVERSION'
import sys
import xml.etree.ElementTree as ET
version = ET.parse(sys.argv[1]).findtext('.//Version')
if version != sys.argv[2]:
    raise SystemExit(f'Package version {sys.argv[2]} differs from Directory.Build.props {version}; update the repository version first.')
PYVERSION
  msbuild_properties+=("-p:Version=$PACKAGE_VERSION")
fi

python3 "$ROOT_DIR/eng/sync-templates.py" --check

mkdir -p "$OUTPUT_DIR"
find "$OUTPUT_DIR" -maxdepth 1 -type f \( -name 'Kawa.*.nupkg' -o -name 'Kawa.*.snupkg' \) -delete

projects=(
  "src/Kawa.Abstractions/Kawa.Abstractions.csproj"
  "src/Kawa.Core/Kawa.Core.csproj"
  "src/Kawa.Cli/Kawa.Cli.csproj"
  "src/Kawa.Aspire/Kawa.Aspire.csproj"
  "src/Kawa.Aspire.Hosting/Kawa.Aspire.Hosting.csproj"
  "src/Kawa.Tool/Kawa.Tool.csproj"
  "src/Kawa.Templates/Kawa.Templates.csproj"
  "src/Kawa.Web/Kawa.Web.csproj"
  "src/Kawa.FSharp/Kawa.FSharp.fsproj"
  "src/Kawa.MagicOnion/Kawa.MagicOnion.csproj"
  "src/Kawa.MagicOnion.Generators/Kawa.MagicOnion.Generators.csproj"
  "src/Kawa.Grpc/Kawa.Grpc.csproj"
  "src/Kawa.Grpc.Generators/Kawa.Grpc.Generators.csproj"
  "src/Kawa.Grpc.ProtoAdapter/Kawa.Grpc.ProtoAdapter.csproj"
)

test_projects=(
  "tests/Kawa.Core.Tests/Kawa.Core.Tests.csproj"
  "tests/Kawa.Cli.Tests/Kawa.Cli.Tests.csproj"
  "tests/Kawa.Aspire.Tests/Kawa.Aspire.Tests.csproj"
  "tests/Kawa.Templates.Tests/Kawa.Templates.Tests.csproj"
  "tests/Kawa.Web.Tests/Kawa.Web.Tests.csproj"
  "tests/Kawa.MagicOnion.Tests/Kawa.MagicOnion.Tests.csproj"
  "tests/Kawa.MagicOnion.Generators.Tests/Kawa.MagicOnion.Generators.Tests.csproj"
  "tests/Kawa.Grpc.Tests/Kawa.Grpc.Tests.csproj"
)

for project in "${projects[@]}" "${test_projects[@]}"; do
  dotnet build "$ROOT_DIR/$project" \
    --no-restore \
    --configuration "$CONFIGURATION" \
    "${msbuild_properties[@]}" \
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
    "${msbuild_properties[@]}" \
    --disable-build-servers \
    -maxcpucount:1
done
