# Networking Layer Audit - Feb 2026

Comprehensive analysis of desync/crash/reconnect risks in the multiplayer networking stack.

## Critical Issues Found

### 1. ByteReader lacks bounds checking
### 2. ActionQueue silently drops remaining actions on exception
### 3. Dictionary iteration order in world data serialization
### 4. Fragment ID collision window
### 5. Lenient mode hides protocol errors during rejoin
### 6. Client packet handling exceptions during tick can leave partial state
### 7. ReadPrefixedUInts/ReadPrefixedULongs missing length validation

See main conversation for full details.
