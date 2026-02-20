# Dev Environment Setup

This guide covers setting up a development environment for contributing to the RimWorld Multiplayer mod.

## Prerequisites

### .NET 8.0+ SDK

Install the .NET 8.0 SDK (or newer) for your platform:

- **Windows:** `winget install Microsoft.DotNet.SDK.8`
- **macOS:** `brew install dotnet-sdk`
- **Any platform:** Download from https://dotnet.microsoft.com/download/dotnet/8.0

Verify the installation:

```bash
dotnet --version
# Should print 8.0.x or higher
```

> **Note:** You do not need to install .NET Framework 4.8 separately. The SDK handles cross-targeting via NuGet reference assemblies.

### IDE

Use any of the following:

- **Visual Studio 2022** (Windows)
- **JetBrains Rider** (cross-platform)
- **VS Code** with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension

### RimWorld (optional)

NuGet references (`Krafs.Rimworld.Ref`) mean you can build without a game install, but you need RimWorld to test in-game.

## Cloning

You can clone the repository anywhere — NuGet references (`Krafs.Rimworld.Ref`) mean the build doesn't depend on a game install. However, if you plan to test in-game, cloning directly into RimWorld's `Mods/` directory is the easiest way to get the mod loaded:

- **Windows (Steam):** `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
- **macOS (Steam):** `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/`
- **Linux (Steam):** `~/.steam/steam/steamapps/common/RimWorld/Mods/`

```bash
# Clone into Mods/ for in-game testing
cd /path/to/RimWorld/Mods
git clone https://github.com/rwmt/Multiplayer.git
cd Multiplayer

# Or clone anywhere if you only need to build
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
