# RimWorld Multiplayer

The RimWorld Multiplayer mod enables cooperative multiplayer gameplay in RimWorld. This documentation covers the mod's internal architecture for developers and contributors.

## Quick Links

- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2606448745)
- [Discord](https://discord.gg/S4bxXpv)
- [GitHub](https://github.com/rwmt/Multiplayer)
- [Website](https://rimworldmultiplayer.com)

## Getting Started

See [Dev Environment Setup](dev-setup) for prerequisites, building, testing, and debugging.

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
| [Dev Environment Setup](dev-setup) | Prerequisites, building, testing, debugging with dnSpy, duplicate installs, IDE tips |
| [Architecture](architecture) | Projects, key statics, initialization flow, how subsystems connect |
| [Sync System](sync-system) | SyncField, SyncMethod, SyncDelegate, SyncAction — how player actions become synced commands |
| [Serialization](serialization) | SyncWorkerDictionaryTree, handler chains, how to add a new type serializer |
| [Networking](networking) | Connection state machine, packet protocol, join flow, fragmentation |
| [Async Time](async-time) | ITickable, per-map ticking, command queues, RNG state tracking, time speed voting |
| [Desync Detection](desync-detection) | SyncCoordinator, opinion comparison, auto-rejoin, diagnostics |
| [Multiplayer API](multiplayer-api) | Public API for third-party mod compatibility — attributes, programmatic registration, field watching |
| [Localization](localization) | Languages submodule, translation keys, `.Translate()` usage, contributing translations |
