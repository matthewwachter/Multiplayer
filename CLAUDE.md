# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RimWorld Multiplayer mod — enables cooperative multiplayer gameplay in RimWorld. C# codebase (~53k SLOC, 359 files) using Harmony for runtime IL patching of the game.

## Build Commands

```bash
# Build all projects (debug)
dotnet build Source/

# Build release
dotnet build Source/ --configuration Release

# Run tests (NUnit + Verify snapshot testing)
dotnet test Source/ --no-restore

# Run a single test
dotnet test Source/ --no-restore --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

The solution is at `Source/Multiplayer.sln`. Building copies DLLs to `Assemblies/` and `AssembliesCustom/` via MSBuild targets.

## Architecture

### Projects

- **Client** (`Source/Client/`, targets .NET Framework 4.8) — Main mod loaded by RimWorld. Contains Harmony patches, UI, networking client, sync system, and async time management. Entry point: `Multiplayer.cs`.
- **Common** (`Source/Common/`, targets .NET Framework 4.8) — Shared library for both client and server. Networking protocol (LiteNetLib/Steam/in-memory), packet serialization (`ByteReader`/`ByteWriter`), version info, and sync type helpers.
- **Server** (`Source/Server/`, targets .NET 8.0) — Standalone headless server. Can be containerized via Dockerfile.
- **MultiplayerLoader** (`Source/MultiplayerLoader/`, targets .NET Framework 4.8) — Early mod loader using Zetrith.Prepatcher for pre-initialization patches.
- **Tests** (`Source/Tests/`, targets .NET 8.0) — NUnit tests with Verify for snapshot testing. Test data in `packet-serializations/`.

### Key Subsystems

- **Sync System** (`Source/Client/Syncing/`) — Core mechanism for synchronizing game state across players. Uses declarative field/method syncing with Harmony patches. Critical for determinism.
- **Async Time** (`Source/Client/AsyncTime/`) — Allows each map to tick independently. `AsyncTimeComp` (per-map) and `AsyncWorldTimeComp` (world-level).
- **Harmony Patches** (`Source/Client/Patches/`) — 34+ patch files that intercept RimWorld methods to enforce determinism and sync behavior.
- **Networking** (`Source/Common/Networking/`) — Packet-based protocol with support for LiteNetLib (UDP), Steam networking, and in-memory connections.
- **Desync Detection** (`Source/Client/Desyncs/`) — Tracks and reports determinism failures between clients.

### Key Statics

`Multiplayer` (static class in Client) holds core game state: `session`, `game`, `Client`, `LocalServer`. Check `Multiplayer.ShouldSync` and `Multiplayer.InInterface` for sync context. `Multiplayer.ExecutingCmds` and `Multiplayer.Ticking` indicate execution phase.

## Code Conventions

- **Naming**: PascalCase for types/methods/properties, camelCase for fields/parameters. Double/triple underscore prefixes (`__`, `___`) are used in Harmony patch parameters (game convention).
- **Formatting**: 4-space indentation for C#, CRLF line endings (see `Source/.editorconfig`).
- **CS0436 suppressed**: The project includes .NET Core polyfills that intentionally conflict with framework types.
- **Unsafe code**: Enabled in Client project for performance-critical paths.

## Git Workflow

- **`dev` branch**: All development happens here. PRs target `dev`, not `master`.
- **`master` branch**: Stable releases only.
- **Branch naming**: `issue-{number}-description` (e.g., `issue-9-feature`).
- **Commit messages**: Short and concise. Prefix with `#{issue_number}:` when applicable (e.g., `#9: create initial contributors document`). Do not add `Co-Authored-By` lines.
- **Merge method**: Squash and Merge. PRs require 2+ reviewer approvals.

## Key Dependencies

- **Lib.Harmony** (2.4.1) — Runtime IL patching
- **LiteNetLib** (1.3.1) — UDP networking
- **Krafs.Rimworld.Ref** (1.6.4566) — RimWorld API references (no game install needed to build)
- **Krafs.Publicizer** (2.3.0) — Exposes private game types at build time
- **RimWorld.MultiplayerAPI** (0.6.0) — Public API for mod compatibility

## Version Info

Version and protocol are defined in `Source/Common/Version.cs`. Protocol version must be bumped when network-incompatible changes are made.

## Documentation

Developer docs are in `docs/` (served with [Docsify](https://docsify.js.org)). When changing architecture, sync system, networking, serialization, async time, desync detection, or the Multiplayer API, update the corresponding doc page. When adding new translation keys, note the convention in the localization doc.
