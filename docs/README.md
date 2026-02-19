# RimWorld Multiplayer

The RimWorld Multiplayer mod enables cooperative multiplayer gameplay in RimWorld. This documentation covers the mod's internal architecture for developers and contributors.

## Quick Links

- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2606448745)
- [Discord](https://discord.gg/S4bxXpv)
- [GitHub](https://github.com/rwmt/Multiplayer)
- [Website](https://rimworldmultiplayer.com)

## Building & Testing

```bash
# Build all projects (debug)
dotnet build Source/

# Build release
dotnet build Source/ --configuration Release

# Run tests
dotnet test Source/ --no-restore

# Run a single test
dotnet test Source/ --no-restore --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

The solution is at `Source/Multiplayer.sln`. Building copies DLLs to `Assemblies/` and `AssembliesCustom/` via MSBuild targets.

## Contributing

1. Clone into your RimWorld `Mods` directory (or anywhere, since build references are NuGet-based).
2. Create a branch from `dev` named `issue-{number}-description`.
3. Prefix commits with `#{issue_number}:` (e.g., `#9: create initial contributors document`).
4. Open a PR targeting the `dev` branch.
5. Get 2+ reviewer approvals, then Squash and Merge.

See [CONTRIBUTORS.md](https://github.com/rwmt/Multiplayer/blob/dev/CONTRIBUTORS.md) for full details.

## Documentation Map

| Page | Description |
|------|-------------|
| [Architecture](architecture) | Projects, key statics, initialization flow, how subsystems connect |
| [Sync System](sync-system) | SyncField, SyncMethod, SyncDelegate, SyncAction — how player actions become synced commands |
| [Serialization](serialization) | SyncWorkerDictionaryTree, handler chains, how to add a new type serializer |
| [Networking](networking) | Connection state machine, packet protocol, join flow, fragmentation |
| [Async Time](async-time) | ITickable, per-map ticking, command queues, RNG state tracking, time speed voting |
| [Desync Detection](desync-detection) | SyncCoordinator, opinion comparison, auto-rejoin, diagnostics |
