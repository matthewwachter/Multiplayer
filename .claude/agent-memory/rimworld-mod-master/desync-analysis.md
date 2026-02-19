# Desync Risk Analysis - Full Patch Audit (2026-02-17)

## Audit Scope
All 32 patch files in Source/Client/Patches/, 2 files in EarlyPatches/,
Prepatches.cs, SyncRituals.cs, and key Syncing/Game/ files.

## Finding Categories
1. Exception swallowing / divergent error recovery
2. Fragile transpiler patterns
3. Missing/incomplete InInterface guards
4. Static state risks
5. RNG concerns
6. Mod compatibility gaps
7. Hash code non-determinism gaps
8. Prefix skip-originals risks

See main response for full details per file.
