#!/usr/bin/env python3
"""Render package-backed templates from the runnable repository samples."""
import argparse
import json
from pathlib import Path
import re
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
GROUPS = {
    "cli": {"Kawa.Sample.Cli": "."},
    "magiconion": {f"Kawa.Sample.MagicOnion.{part}": f"KawaStarter.{part}"
                   for part in ("Contracts", "Server", "Client")},
    "grpc": {f"Kawa.Sample.Grpc.{part}": f"KawaStarter.{part}"
             for part in ("Contracts", "Server", "Client")},
    "aspire": {
        **{f"Kawa.Sample.Aspire.{part}": f"KawaStarter.{part}"
           for part in ("AppHost", "ServiceDefaults", "Service", "GrpcService", "Client")},
        "Kawa.Sample.MagicOnion.Contracts": "KawaStarter.ChatContracts",
        "Kawa.Sample.Grpc.Contracts": "KawaStarter.ProtoContracts",
    },
}


def solution(projects):
    text = "Microsoft Visual Studio Solution File, Format Version 12.00\n# Visual Studio Version 17\n"
    entries = []
    for path in projects:
        guid = "{" + str(uuid.uuid5(uuid.NAMESPACE_URL, path)).upper() + "}"
        name = Path(path).stem
        text += f'Project("{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}") = "{name}", "{path.replace(chr(47), chr(92))}", "{guid}"\nEndProject\n'
        entries.append(guid)
    text += "Global\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n\t\tDebug|Any CPU = Debug|Any CPU\n\t\tRelease|Any CPU = Release|Any CPU\n\tEndGlobalSection\n\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\n"
    for guid in entries:
        for config in ("Debug", "Release"):
            for action in ("ActiveCfg", "Build.0"):
                text += f"\t\t{guid}.{config}|Any CPU.{action} = {config}|Any CPU\n"
    return text + "\tEndGlobalSection\nEndGlobal\n"


def package_project(text):
    tree = ET.fromstring(text)
    for group in list(tree):
        for item in list(group):
            if item.tag == "ProjectReference" and "/src/Kawa." in item.attrib.get("Include", "").replace("\\", "/"):
                package = Path(item.attrib["Include"].replace("\\", "/")).stem
                attributes = {"Include": package, "Version": "__KAWA_VERSION__"}
                if package.endswith(".Generators"):
                    attributes["PrivateAssets"] = "all"
                group.remove(item)
                ET.SubElement(group, "PackageReference", attributes)
            elif item.tag == "KawaGrpcGeneratorsAssemblyPath":
                group.remove(item)
        if group.tag == "Import" and "Kawa.Grpc.Generators" in group.attrib.get("Project", ""):
            tree.remove(group)
    ET.indent(tree, space="  ")
    return ET.tostring(tree, encoding="unicode") + "\n"


def payload(kind, mapping):
    files = {}
    project_paths = []
    replacements = {source: ("KawaStarter" if target == "." else target) for source, target in mapping.items()}
    for source, target in mapping.items():
        directory = ROOT / "samples" / source
        for path in sorted(directory.rglob("*")):
            relative = path.relative_to(directory)
            if not path.is_file() or any(part in ("bin", "obj") for part in relative.parts):
                continue
            if path.suffix not in (".cs", ".csproj", ".proto", ".json"):
                continue
            text = path.read_text()
            if path.suffix == ".csproj":
                text = package_project(text)
            name = str(relative)
            for old, new in replacements.items():
                name = name.replace(old, new)
                text = text.replace(old, new)
            name = str(Path(target) / name)
            files[name] = text
            if path.suffix == ".csproj":
                project_paths.append(name)
    if kind != "cli":
        files["KawaStarter.sln"] = solution(project_paths)
    files[".template.config/template.json"] = json.dumps({
        "$schema": "http://json.schemastore.org/template",
        "author": "Kawa contributors", "identity": f"Kawa.{kind}.Project",
        "name": f"Kawa {kind} application", "shortName": f"kawa-{kind}",
        "classifications": ["Kawa", kind], "sourceName": "KawaStarter",
        "preferNameDirectory": True, "tags": {"language": "C#", "type": "project"},
        "symbols": {"KawaVersion": {"type": "parameter", "datatype": "string",
                    "defaultValue": "0.5.0", "replaces": "__KAWA_VERSION__"}},
    }, indent=2) + "\n"
    commands = {
        "cli": "dotnet run -- --message hello",
        "magiconion": "dotnet run --project KawaStarter.Server\n# In another terminal:\ndotnet run --project KawaStarter.Client",
        "grpc": "dotnet run --project KawaStarter.Server\n# In another terminal:\ndotnet run --project KawaStarter.Client",
        "aspire": "dotnet run --project KawaStarter.AppHost",
    }
    files["README.md"] = f"""# KawaStarter ({kind})

Requires .NET 10 SDK. Restore and build before running:

```sh
dotnet restore
dotnet build --no-restore
{commands[kind]}
```

Kawa package version defaults to 0.5.0; `--KawaVersion` can select another version during generation.
"""
    if kind in ("magiconion", "grpc"):
        port = 5010 if kind == "magiconion" else 5020
        files["README.md"] += f"""
The client demonstrates unary, server streaming, client streaming and duplex calls.
The same Send use case is available over HTTP at `http://localhost:{port}/messages`.
Use a JSON string for MagicOnion's HTTP request, or `{{"message":"hello"}}` for protobuf's HTTP request.
These localhost examples are unauthenticated; configure authentication before exposing them externally.
"""
    if kind == "magiconion":
        files["README.md"] += "The client also connects two typed StreamingHub receivers to the lobby and verifies group broadcast.\n"
    if kind == "grpc":
        files["README.md"] += """
`Server/Protos/legacy.proto` demonstrates an existing schema with explicit request/response adapters.
Generated Kawa schemas stay under `Contracts/obj/`. To export one explicitly after building:

```sh
dotnet tool install --global Kawa.Tool --version 0.5.0
kawa grpc export --schema KawaStarter.Contracts/obj/Debug/net10.0/Kawa/kawa.generated.proto --output Protos/chat.proto
```

The export command refuses to overwrite an existing file unless `--force` is supplied.
"""
    if kind == "aspire":
        files["README.md"] += """
The AppHost orchestrates MagicOnion and protobuf gRPC servers and two CLI jobs, using named HTTP/2 endpoints and service discovery. ServiceDefaults configures telemetry and health checks. The Dashboard URL/token is printed by Aspire at startup. The default https launch profile requires the ASP.NET Core development certificate; `--launch-profile http` explicitly selects localhost-only HTTP. Install Aspire's local prerequisites before running the AppHost; building does not launch its resources. Hosting dependencies and the AppHost SDK require patched Aspire 13.6.5 or later. The local example uses plaintext HTTP/2; configure TLS/authentication for deployment.
"""
    return files


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    options = parser.parse_args()
    differences = []
    for kind, mapping in GROUPS.items():
        directory = ROOT / "templates" / ("kawa-" + kind)
        expected = payload(kind, mapping)
        for name, text in expected.items():
            path = directory / name
            if not path.exists() or path.read_text() != text:
                differences.append(str(path.relative_to(ROOT)))
                if not options.check:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(text)
        actual = {str(path.relative_to(directory)) for path in directory.rglob("*") if path.is_file()}
        stale = actual - expected.keys()
        for name in stale:
            differences.append(str((directory / name).relative_to(ROOT)))
            if not options.check:
                (directory / name).unlink()
    if options.check and differences:
        raise SystemExit("Template drift; run python3 eng/sync-templates.py:\n" + "\n".join(differences))
    print("4 template payloads " + ("verified" if options.check else "synchronized"))


if __name__ == "__main__":
    main()
