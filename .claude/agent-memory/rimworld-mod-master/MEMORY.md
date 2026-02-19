# Multiplayer Mod Memory

## Project Structure
- **Patch files**: 32 `.cs` files in `Source/Client/Patches/`
- **Early patches**: 2 files in `Source/Client/EarlyPatches/`
- **Prepatches**: `Source/MultiplayerLoader/Prepatches.cs` + `SyncRituals.cs`
- **Sync system**: `Source/Client/Syncing/Game/` contains SyncMethods, SyncDelegates, SyncFields, SyncActions

## Key Architecture Files
- `Source/Common/Networking/ConnectionBase.cs` - Base connection, fragmentation, packet dispatch
- `Source/Common/Networking/MpConnectionState.cs` - State machine, handler registration
- `Source/Common/Networking/AsyncConnectionState.cs` - Async await for server states
- `Source/Common/ByteReader.cs` / `ByteWriter.cs` - Serialization (bounds check added in Phase 1)
- `Source/Client/Patches/TickPatch.cs` - Core game tick loop, simulation
- `Source/Client/Session/MultiplayerSession.cs` - Session state, command scheduling
- `Source/Client/Session/Rejoiner.cs` - Rejoin/reconnect flow
- `Source/Common/PlayerManager.cs` - Server-side player management

## Key Patterns
- `Multiplayer.InInterface` guards prevent simulation-side effects during UI calls
- `Multiplayer.ExecutingCmds` indicates synced command execution
- Faction context push/pop wraps tick methods for multifaction support
- RNG isolation uses `Rand.PushState()`/`Rand.PopState()` pairs
- Hash code patches replace non-deterministic `System.HashCode.Combine` with `Gen.HashCombineInt`
- Transpilers match IL patterns to replace `Find.CurrentMap`, `Time.deltaTime`, etc.

## Networking Architecture
- LiteNetLib for UDP (ReliableOrdered / Unreliable); Steam P2P alternative; in-memory for local
- Fragmented packets: max 32MB, 1KB fragments, 64KB single packets
- `sendFragId` is `byte` wrapping at 255; `MaxFragmentedPackets = 1`
- Connection states: ClientSteam->ClientJoining->ClientLoading->ClientPlaying
- Rejoin sets `Lenient=true`, cleared on `Server_WorldDataStart`

## Phase 1 Fixes Completed (2026-02-17)
- ByteReader bounds checking in IncrementIndex
- ActionQueue per-action exception isolation
- SyncDictRimWorld: Hediff First->FirstOrDefault, comp index bounds checks, PlanetLayer null-safety, pawn sub-component null checks
- TypeCache deterministic ordering using FullName + assembly tiebreaker
- Protocol version bump 51->52

## Known Remaining Risk Areas (Phase 2 candidates, see `phase2-audit.md`)
- HashCodes.cs: `TradeableHashCode` completely skips original with `Prefix() => false`
- TickPatch.cs: exception catch in `TickTickable` logs but continues, allowing divergent state
- Several transpilers have no validation logging when patterns aren't matched
- Lambda ordinal references are fragile to game updates
- WorldPawns.cs and WorldObjectAdd patches are commented out (disabled WIP code)
- Dictionary iteration in ServerLoadingState.SendWorldData is non-deterministic
- DelegateSerialization.cs:22 method name only, no signature disambiguation
- SyncDictRimWorld.cs:507 QuestPart index no bounds check
- SyncWorkerEntry.Invoke no exception recovery leaves stream corrupted
- SyncCoordinator.cs:203 uses string.GetHashCode() (non-deterministic on .NET Core)
- ReadPrefixedUInts/ReadPrefixedULongs have no negative length validation
- Remaining comp type array accesses without bounds checks (abilityCompTypes, mapCompTypes, worldObjectCompTypes, worldCompTypes, gameCompTypes)
- PlanetLayer reader line 1061 no bounds check on layerId
- SyncDictDlc.cs MechanitorControlGroup reader no bounds check on index

## Mod Compatibility Architecture
- Types serialized by index in alphabetically-ordered arrays (TypeCache)
- Third-party API via MultiplayerAPIBridge (IAPI) in `Source/Client/MultiplayerAPIBridge.cs`
- DLC sync workers in `Source/Client/Syncing/Dict/SyncDictDlc.cs`
- Mod list + file hash validation in `Source/Client/Networking/JoinData.cs`
- DefInfo collection for desync detection in `Source/Client/MultiplayerData.cs`
- ModCompatibilityManager fetches remote compat DB from bot.rimworldmultiplayer.com
