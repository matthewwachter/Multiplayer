# Dev Environment Setup

This guide covers setting up a development environment for contributing to the RimWorld Multiplayer mod.

## Prerequisites

- **.NET SDK** — .NET 8.0+ SDK (includes .NET Framework 4.8 targeting pack on Windows; on macOS/Linux, Mono provides the framework)
- **IDE** — Visual Studio 2022, JetBrains Rider, or VS Code with the C# Dev Kit extension
- **RimWorld** (optional) — NuGet references (`Krafs.Rimworld.Ref`) mean you can build without a game install, but you need RimWorld to test in-game

## Cloning

Clone the repository. If you plan to test in-game, cloning directly into your RimWorld `Mods/` directory is convenient but not required:

```bash
git clone https://github.com/rwmt/Multiplayer.git
cd Multiplayer

# Initialize the Languages submodule (translations)
git submodule update --init
```

Create a working branch from `dev`:

```bash
git checkout dev
git checkout -b issue-{number}-description
```

## Building

```bash
# Debug build (default)
dotnet build Source/

# Release build
dotnet build Source/ --configuration Release
```

The solution is at `Source/Multiplayer.sln`. MSBuild targets copy output DLLs to `Assemblies/` and `AssembliesCustom/`.

## Testing

```bash
# Run all tests (NUnit + Verify snapshot testing)
dotnet test Source/ --no-restore

# Run a single test by name
dotnet test Source/ --no-restore --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

Test data for snapshot tests lives in `Source/Tests/packet-serializations/`.

## Debugging

### dnSpy

[dnSpy](https://github.com/dnSpyEx/dnSpy) can attach to a running RimWorld process for live debugging:

1. Launch RimWorld with the mod enabled.
2. In dnSpy, go to **Debug → Attach to Process** and select the RimWorld process.
3. Open the mod's DLLs from `Assemblies/` and set breakpoints.
4. Harmony-patched methods appear in the decompiled game assemblies — you can set breakpoints on both the original and patched IL.

### BepInEx / Unity Doorstop

For early debugging (before RimWorld's mod loader runs), configure [Unity Doorstop](https://github.com/NeighTools/UnityDoorstop) or [BepInEx](https://github.com/BepInEx/BepInEx) as a preloader. This lets you attach a debugger before any Harmony patches are applied.

## Duplicate Game Installs

Keeping a separate RimWorld install for multiplayer development avoids conflicts with your regular mods and saves:

- **Steam library folder trick** — In Steam, go to **Settings → Storage** and add a second library folder. Right-click RimWorld → **Properties → Local Files → Move Install Folder** to create an independent copy.
- **Separate settings** — Launch the dev install with the `--settingsDir` argument to isolate config, saves, and log files:

```
RimWorldWin64.exe --settingsDir "C:\RimWorldMP-Dev\Settings"
```

## IDE Tips

### EditorConfig

The project includes `Source/.editorconfig` which configures:

- 4-space indentation for C# files
- CRLF line endings
- UTF-8 encoding

Most IDEs pick this up automatically. Verify your editor respects EditorConfig settings.

### CS0436 Suppression

The project includes .NET Core polyfills that intentionally shadow framework types, producing CS0436 warnings. These are suppressed in the EditorConfig — if your IDE still shows them, the warnings are safe to ignore.

### Useful IDE Extensions

- **ILSpy** / **dnSpy** — Decompile RimWorld assemblies to understand what you're patching
- **Harmony documentation** — Familiarize yourself with [Harmony's patching API](https://harmony.pardeike.net/articles/intro.html) (Prefix, Postfix, Transpiler)
