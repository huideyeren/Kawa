#!/usr/bin/env python3
"""Install a private template package and test generated projects against local NuGet packages."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PACKAGES = [
    "Kawa.Abstractions", "Kawa.Core", "Kawa.Web", "Kawa.Cli", "Kawa.Aspire",
    "Kawa.Aspire.Hosting", "Kawa.MagicOnion", "Kawa.MagicOnion.Generators",
    "Kawa.Grpc", "Kawa.Grpc.Generators", "Kawa.Grpc.ProtoAdapter", "Kawa.Tool", "Kawa.Templates",
]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--configuration", default="Debug", choices=["Debug", "Release"])
    options = parser.parse_args()
    work = Path(tempfile.mkdtemp(prefix="kawa-templates-"))
    feed = work / "packages"
    feed.mkdir()
    version = ET.parse(ROOT / "Directory.Build.props").findtext(".//Version")
    assets = json.loads((ROOT / "tests/Kawa.Templates.Tests/obj/project.assets.json").read_text())
    package_cache = next(iter(assets["packageFolders"]))
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / "cli-home"),
               NUGET_PACKAGES=str(work / "package-cache"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1",
               DOTNET_GENERATE_ASPNET_CERTIFICATE="false", DOTNET_ADD_GLOBAL_TOOLS_TO_PATH="false",
               DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE="false", DOTNET_USE_POLLING_FILE_WATCHER="1")
    number = 0

    def run(arguments, cwd=ROOT, expected=0):
        nonlocal number
        number += 1
        result = subprocess.run(arguments, cwd=cwd, env=env, capture_output=True, text=True, timeout=120)
        (work / f"{number:03d}.log").write_text(result.stdout + "\n" + result.stderr)
        if result.returncode != expected:
            raise AssertionError(f"Command failed ({result.returncode}, expected {expected}): {arguments}\n"
                                 + (result.stdout + result.stderr)[-12000:])
        return result

    try:
        run([sys.executable, "eng/sync-templates.py", "--check"])
        # No recursive build/test invocation: dotnet test already built this solution.
        for name in PACKAGES:
            run(["dotnet", "pack", f"src/{name}/{name}.csproj", "--no-restore", "--no-build",
                 "--configuration", options.configuration, "--output", str(feed),
                 "--disable-build-servers", "-m:1"])

        hive = work / "template-hive"
        template_options = ["--debug:custom-hive", str(hive)]
        run(["dotnet", "new", "install", str(feed / f"Kawa.Templates.{version}.nupkg"), "--force", *template_options])
        generated = {}
        for kind in ("cli", "magiconion", "grpc", "aspire"):
            name = "Smoke" + kind.title()
            directory = work / name
            run(["dotnet", "new", "kawa-" + kind, "--name", name, "--output", str(directory),
                 "--KawaVersion", version, *template_options])
            generated[kind] = directory
            configuration = ET.Element("configuration")
            sources = ET.SubElement(configuration, "packageSources")
            ET.SubElement(sources, "clear")
            ET.SubElement(sources, "add", key="local-kawa", value=str(feed))
            ET.SubElement(sources, "add", key="cached-dependencies", value=package_cache)
            fallback = ET.SubElement(configuration, "fallbackPackageFolders")
            ET.SubElement(fallback, "add", key="existing-cache", value=package_cache)
            config = directory / "NuGet.Config"
            ET.ElementTree(configuration).write(config, encoding="unicode")
            run(["dotnet", "restore", "--configfile", str(config), "-p:NuGetAudit=false",
                 "--disable-build-servers", "-m:1"], cwd=directory)
            run(["dotnet", "build", "--no-restore", "--configuration", options.configuration,
                 "--disable-build-servers", "-m:1", "-p:UseSharedCompilation=false"], cwd=directory)
            for path in directory.rglob("*"):
                if path.suffix not in (".cs", ".csproj", ".sln") or any(part in ("bin", "obj") for part in path.parts):
                    continue
                content = path.read_text()
                assert "__KAWA_VERSION__" not in content and "KawaStarter" not in content, path
                assert "../../src/" not in content, path
            print(f"PASS kawa-{kind}: generated, restored and built")

        launch = json.loads((generated["aspire"] / "SmokeAspire.AppHost/Properties/launchSettings.json").read_text())
        assert launch["profiles"]["https"]["applicationUrl"].startswith("https://localhost:")
        assert launch["profiles"]["http"]["environmentVariables"]["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] == "true"

        cli = generated["cli"]
        command = ["dotnet", "run", "--no-build", "--no-restore", "--configuration", options.configuration, "--"]
        success = run([*command, "--message", "hello"], cwd=cli)
        assert json.loads(success.stdout) == {"message": "hello"} and not success.stderr
        failure = run([*command, "--message", ""], cwd=cli, expected=1)
        assert not failure.stdout and json.loads(failure.stderr)["kind"] == "Validation"
        print("PASS CLI JSON, stdout/stderr isolation and exit codes")

        tools = work / "tools"
        run(["dotnet", "tool", "install", "Kawa.Tool", "--version", version, "--tool-path", str(tools),
             "--configfile", str(cli / "NuGet.Config")])
        schema = generated["grpc"] / f"SmokeGrpc.Contracts/obj/{options.configuration}/net10.0/Kawa/kawa.generated.proto"
        exported = work / "exported/Protos/chat.proto"
        tool = tools / ("kawa.exe" if os.name == "nt" else "kawa")
        export_command = [str(tool), "grpc", "export", "--schema", str(schema), "--output", str(exported)]
        result = run(export_command)
        assert exported.read_bytes() == schema.read_bytes()
        assert json.loads(result.stdout)["output"] == str(exported)
        run(export_command, expected=1)
        run([*export_command, "--force"])
        print("PASS packaged kawa grpc export, overwrite protection and --force")
        print("All four packaged template smoke tests passed")
    except Exception:
        print(f"Template verification logs retained at {work}", file=sys.stderr)
        raise
    else:
        shutil.rmtree(work)


if __name__ == "__main__":
    main()
