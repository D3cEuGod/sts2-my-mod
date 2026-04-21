# StS2 evidence checklist

Rule: Do not infer StS2 behavior from StS1 reference material. Files under `docs/reference-sts1/` and `examples/reference-sts1/` are legacy reference only, not evidence for StS2 behavior.

## Engine

- status: confirmed
- evidence source: StS2 example repo currently stored in this repository
- file path or link placeholder: `examples/sts2/ModConfig-sts2/project.godot`, `examples/sts2/ModConfig-sts2/ModConfig.csproj`, `examples/sts2/ModConfig-sts2/README.md`
- notes: The example uses Godot project files and `Godot.NET.Sdk/4.5.1`. Treat this as example evidence, not complete official documentation.

## Recommended language

- status: confirmed
- evidence source: StS2 example repo currently stored in this repository
- file path or link placeholder: `examples/sts2/ModConfig-sts2/MainFile.cs`, `examples/sts2/ModConfig-sts2/ModConfig.csproj`, `examples/sts2/ModConfig-sts2/README.md`
- notes: The example is C# targeting `net9.0`. This confirms C# is used by this example; it does not prove it is the only supported language.

## Loader/framework

- status: confirmed
- evidence source: StS2 example repo currently stored in this repository
- file path or link placeholder: `examples/sts2/ModConfig-sts2/MainFile.cs`, `examples/sts2/ModConfig-sts2/ModConfig.csproj`
- notes: The example imports `MegaCrit.Sts2.Core.Modding` and uses `ModInitializer`. The full loader contract is not yet documented locally.

## Entrypoint

- status: confirmed
- evidence source: StS2 example repo currently stored in this repository
- file path or link placeholder: `examples/sts2/ModConfig-sts2/MainFile.cs`, `examples/sts2/ModConfig-sts2/README.md`
- notes: The example marks a class with `[ModInitializer("Initialize")]` and defines `public static void Initialize()`. More examples or official docs should confirm whether this is the minimal required pattern.

## Packaging format

- status: confirmed
- evidence source: StS2 example repo currently stored in this repository
- file path or link placeholder: `examples/sts2/ModConfig-sts2/README.md`, `examples/sts2/ModConfig-sts2/ModConfig.csproj`, `examples/sts2/ModConfig-sts2/export_presets.cfg`
- notes: The example README says to install `ModConfig.dll` and `ModConfig.pck`. The project file builds/copies the DLL, and the README gives a Godot export command for the PCK.

## Install location

- status: confirmed
- evidence source: StS2 example repo currently stored in this repository
- file path or link placeholder: `examples/sts2/ModConfig-sts2/README.md`, `examples/sts2/ModConfig-sts2/ModConfig.csproj`
- notes: The example README says to place files under `<Game>/mods/ModConfig/`. The project file copies build output to `$(Sts2Dir)\mods\$(MSBuildProjectName)\`.

## Minimal hello-world example

- status: unconfirmed
- evidence source: none yet
- file path or link placeholder: TODO: add a verified minimal StS2 hello-world mod under `examples/sts2/`
- notes: `examples/sts2/ModConfig-sts2/` is a useful real example, but it is a configuration framework, not a minimal starter mod.

## Build/run/debug workflow

- status: unconfirmed
- evidence source: partial evidence from StS2 example repo currently stored in this repository
- file path or link placeholder: `examples/sts2/ModConfig-sts2/README.md`, `examples/sts2/ModConfig-sts2/ModConfig.csproj`
- notes: The example documents `.NET 9.0 SDK`, `Godot 4.5.1 Mono`, `dotnet build ModConfig.csproj`, and a Godot PCK export command. A complete run/debug workflow is not yet documented locally.

## Best current reference repo

- status: confirmed
- evidence source: StS2 example repo currently stored in this repository
- file path or link placeholder: `examples/sts2/ModConfig-sts2/`
- notes: This is currently the only StS2-specific example in the repository. Add source URL/provenance here when available.
