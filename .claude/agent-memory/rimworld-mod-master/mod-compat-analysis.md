# Mod Compatibility and DLC Analysis

## Summary
This document captures findings from a thorough analysis of how the Multiplayer mod
handles third-party mod compatibility, DLC/expansion content, and the key desync risks
that arise from modded games.

## Type Serialization by Index (Critical Desync Risk)
All component types, Def types, and interface implementations are serialized as ushort
indices into alphabetically-sorted arrays built at startup. If mod lists differ (even
in load order), these indices shift, causing desync.

Key files:
- `Source/Client/Util/TypeCache.cs` - CacheTypeHierarchy builds sorted lists
- `Source/Client/Syncing/CompSerialization.cs` - Component type arrays
- `Source/Client/Syncing/DefSerialization.cs` - Def type arrays
- `Source/Client/Syncing/ApiSerialization.cs` - ISyncSimple/Session arrays
- `Source/Client/Syncing/ImplSerialization.cs` - Interface implementation registration
- `Source/Common/Syncing/SyncTypeHelper.cs` - Index lookup for impl types

## DLC Coverage
Sync workers exist for Royalty, Ideology, Biotech, and Anomaly in SyncDictDlc.cs.
No conditional loading is used; types that don't exist simply won't appear in type caches.
Odyssey content (1.6 / Fishing Zones) is partially covered in SyncFields.FishingZone.cs
and Odyssey.cs patches.

## Third-Party Mod API
- `Source/Client/MultiplayerAPIBridge.cs` - IAPI implementation
- Mods register via `RegisterAll(Assembly)` which scans for attributes
- SyncWorker registration adds to the shared `SyncDict.syncWorkers` tree
- API version checking in `Multiplayer.CheckInterfaceVersions()`
