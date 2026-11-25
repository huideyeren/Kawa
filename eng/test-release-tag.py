#!/usr/bin/env python3
"""Check the actual release-tag shell block with benign injection probes."""
import os
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]
workflow = (root / ".github/workflows/release-nuget.yml").read_text()
block = workflow.split("      - name: Resolve release tag\n", 1)[1].split("      - name: Checkout", 1)[0]
assert "RELEASE_TAG_INPUT: ${{ inputs.tag }}" in block
shell = block.split("        run: |\n", 1)[1]
shell = "\n".join(line[10:] for line in shell.splitlines() if line.strip())
assert "${{" not in shell, "Expressions must not be embedded in shell source"
for event, tag, expected in [
    ("workflow_dispatch", "v0.5.0", 0),
    ("workflow_dispatch", "invalid", 1),
    ("workflow_dispatch", "v0.5.0$(printf KAWA_PROBE >&2)", 1),
    ("workflow_dispatch", 'v0.5.0"; printf KAWA_PROBE >&2; #', 1),
    ("workflow_dispatch", "v0.5.0`printf KAWA_PROBE >&2`", 1),
    ("push", "ignored", 0),
]:
    env = dict(os.environ, RELEASE_EVENT_NAME=event, RELEASE_TAG_INPUT=tag,
               GITHUB_REF_NAME="v0.5.0", GITHUB_OUTPUT=os.devnull)
    result = subprocess.run(["bash", "-eu", "-c", shell], env=env, capture_output=True, text=True)
    assert result.returncode == expected, (tag, result.returncode)
    assert "KAWA_PROBE" not in result.stdout + result.stderr
print("Release tag security checks: 6 passed")

# Run the actual pre-publication Python against isolated release metadata.
import tempfile
preflight = workflow.split('      - name: Validate release metadata before publishing\n', 1)[1].split('      - name: Setup .NET', 1)[0]
source = preflight.split("          python3 - <<'PYRELEASE'\n", 1)[1].split('          PYRELEASE', 1)[0]
source = '\n'.join(line[10:] for line in source.splitlines())
assert workflow.index('Validate release metadata before publishing') < workflow.index('Publish to NuGet')
with tempfile.TemporaryDirectory(prefix='kawa-release-metadata-') as temporary:
    directory = Path(temporary)
    (directory / 'Directory.Build.props').write_text('<Project><PropertyGroup><Version>0.5.0</Version></PropertyGroup></Project>')
    for name in ('CHANGELOG.md', 'CHANGELOG.ja.md'):
        (directory / name).write_text('## [0.5.0] - 2026-11-19\n')
    for version, expected in [('0.5.0', 0), ('0.5.1', 1)]:
        result = subprocess.run(['python3', '-c', source], cwd=directory,
            env=dict(os.environ, KAWA_RELEASE_VERSION=version), capture_output=True, text=True)
        assert result.returncode == expected, result.stderr
    (directory / 'CHANGELOG.ja.md').write_text('## [Unreleased]\n')
    result = subprocess.run(['python3', '-c', source], cwd=directory,
        env=dict(os.environ, KAWA_RELEASE_VERSION='0.5.0'), capture_output=True, text=True)
    assert result.returncode == 1, 'Missing Japanese release notes must block publishing'
print('Release metadata preflight checks: 3 passed')
