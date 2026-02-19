# Networking

The networking layer handles connections between clients and the server, packet serialization, and the join handshake. It supports three transport backends and uses a state machine to manage the connection lifecycle.

## Transport Layers

### LiteNetLib (UDP)

The primary transport for direct and LAN connections. Uses reliable ordered delivery by default.

- **Server**: `MpServerNetListener` (`Source/Common/Networking/NetworkingLiteNet.cs`) — implements `INetEventListener`
- **Client**: `ClientLiteNetConnection` (`Source/Client/Networking/NetworkingLiteNet.cs`) — wraps a `NetPeer`
- Configuration: IPv6 support detected automatically, reconnect delay 300ms, max 8 connect attempts

### Steam P2P

Uses Steamworks P2P networking for connections through Steam's relay infrastructure.

- **Base**: `SteamBaseConn` (`Source/Client/Networking/NetworkingSteam.cs`) — wraps `CSteamID` with send/receive channels
- **Client**: `SteamClientConn` — random receive channel, send on channel 0
- **Server**: `SteamServerConn` — tracks keep-alive for latency measurement
- **Manager**: `SteamP2PNetManager` — manages all Steam connections for the server

Steam packets include a flags byte:

```
bit 0: joinPacket
bit 1: reliable
bit 2: hasChannel
[ushort channel] (optional, if hasChannel flag set)
[packet data]
```

### In-Memory (Local)

Used when hosting and playing on the same machine. Zero-latency direct function calls between client and server threads.

- `LocalConnection` (`Source/Client/Networking/NetworkingInMemory.cs`)
- `Client(username)` — queues to server via `Multiplayer.LocalServer.Enqueue`
- `Server(username)` — queues to main thread via `OnMainThread.Enqueue`
- `Paired(username)` — creates bidirectional connection pair

## Connection State Machine

```
  CLIENT SIDE                          SERVER SIDE
┌───────────────┐                   ┌───────────────┐
│ ClientJoining │ ◀──────────────▶  │ ServerJoining │
└───────┬───────┘                   └───────┬───────┘
        │ (world request)                   │
        ▼                                   ▼
┌───────────────┐                   ┌───────────────┐
│ ClientLoading │ ◀──────────────▶  │ ServerLoading │
└───────┬───────┘                   └───────┬───────┘
        │ (world ready)                     │
        ▼                                   ▼
┌───────────────┐                   ┌───────────────┐
│ ClientPlaying │ ◀──────────────▶  │ ServerPlaying │
└───────────────┘                   └───────────────┘
```

States are defined in `ConnectionStateEnum`:

```csharp
enum ConnectionStateEnum : byte
{
    ClientJoining,    // Handshake: protocol, password, username, init data
    ClientLoading,    // Downloading and loading world data
    ClientPlaying,    // Active gameplay
    ClientSteam,      // Special Steam handshake state
    ServerJoining,    // Server-side handshake (async)
    ServerLoading,    // Server sending world data
    ServerPlaying,    // Server-side gameplay handlers
    ServerSteam,      // (unused)
    Count,            // Sentinel value for enum count
    Disconnected      // Terminal state
}
```

## Join Handshake

The complete flow from connection to gameplay:

```
CLIENT                                    SERVER
  │                                         │
  │─── Client_Protocol (version) ─────────▶│
  │                                         │ validate protocol
  │◀─────── Server_ProtocolOk (has_pw) ────│
  │                                         │
  │─── Client_Username (user, pwd) ───────▶│
  │                                         │ validate password, username
  │                                         │
  │◀─── Server_InitDataRequest ───────────│ (if server needs init data)
  │                                         │
  │─── Client_InitData (fragmented) ──────▶│
  │   (RW version, def hashes,              │ convert to ServerInitData
  │    sync handler hash, raw data)         │
  │                                         │
  │◀─────── Server_UsernameOk ────────────│
  │                                         │
  │─── Client_JoinData (fragmented) ──────▶│
  │   (round modes, def infos,              │ validate match
  │    sync handler hash)                   │
  │                                         │
  │◀─── Server_JoinData (fragmented) ─────│
  │   (game name, player ID,               │
  │    def status array, raw init data)     │
  │                                         │
  │─── Client_WorldRequest ───────────────▶│
  │                                         │ [State → Loading]
  │◀─── Server_WorldDataStart ────────────│
  │◀─── Server_WorldData (fragmented) ────│
  │   (faction, tick, cmds, frozen,         │
  │    compressed world + session,          │
  │    per-map cmds + data, sync infos)     │
  │                                         │ [State → Playing]
  │  (loads game locally)                   │
  │                                         │
  │─── Client_WorldReady ─────────────────▶│
  │                                         │
  │◀─── Server_PlayerList ────────────────│
  │                                         │
  │        (gameplay begins)                │
```

### Validation Steps

During the handshake, the server validates:

- **Protocol version**: Must match `MpVersion.Protocol` exactly
- **Password**: If enabled, checked before proceeding
- **Username**: Length limits, valid characters, no duplicates
- **Floating-point round modes**: Must match server's modes (ensures deterministic math)
- **Sync handler hash**: Must match (ensures identical sync handler registration)
- **Def statuses**: Each def checked for `Ok`, `Not_Found`, `Count_Diff`, or `Hash_Diff`

## Packet System

### Packet Types

The `Packets` enum (`Source/Common/Networking/Packets.cs`) defines 65 packet types covering the full lifecycle:

- **Joining**: `Client_Protocol`, `Server_ProtocolOk`, `Client_Username`, `Server_UsernameOk`, etc.
- **Loading**: `Server_WorldDataStart`, `Server_WorldData`
- **Playing (Client→Server)**: `Client_Command`, `Client_Chat`, `Client_SyncInfo`, `Client_KeepAlive`, etc.
- **Playing (Server→Client)**: `Server_Command`, `Server_TimeControl`, `Server_Chat`, `Server_SyncInfo`, etc.
- **Universal**: `Server_Disconnect` (valid in all states)

### Handler Attributes

Packet handlers are registered via attributes on state class methods:

```csharp
// Raw byte handler
[PacketHandler(Packets.Client_WorldReady)]
public void HandleWorldReady(ByteReader data) { ... }

// Typed packet handler (auto-deserializes)
[TypedPacketHandler]
public void HandleProtocol(ClientProtocolPacket packet) { ... }

// Fragmented packet progress handler
[FragmentedPacketHandler(Packets.Server_WorldData)]
public void HandleWorldDataProgress(FragmentedPacket packet) { ... }
```

Handler registration uses IL code generation for zero-reflection dispatch at runtime.

### Typed Packets

Packets implement `IPacket` with a bidirectional `Bind()` method:

```csharp
[PacketDefinition(Packets.Client_Protocol)]
public record struct ClientProtocolPacket : IPacket
{
    public int protocol;

    public void Bind(PacketBuffer buf)
    {
        buf.Bind(ref protocol);
    }
}
```

The same `Bind` code handles both reading and writing, keeping serialization in sync.

### Async State Handling

Server-side states use async/await for sequential packet handling:

```csharp
protected override async Task RunState()
{
    HandleProtocol(await TypedPacket<ClientProtocolPacket>());
    HandleUsername(await TypedPacket<ClientUsernamePacket>());
    // ... sequential awaits for the join flow
    connection.ChangeState(ConnectionStateEnum.ServerLoading);
}
```

## Fragmentation

Large packets (world data, init data) are automatically fragmented for transmission.

### Limits

| Limit | Value |
|-------|-------|
| Max single packet | 65,536 bytes |
| Max fragment chunk | 1,024 bytes per network frame |
| Max total fragmented | 33,554,432 bytes (32 MiB) |

### Fragment Header Format

**First fragment**:
```
byte:   [packetId (6 bits) | fragState (2 bits)]   // fragState = FragMore
byte:   fragId                                       // Counter to match fragments
ushort: expected parts count
uint32: expected total size
[data...]
```

**Subsequent fragments**:
```
byte:   [packetId (6 bits) | fragState (2 bits)]   // FragMore or FragEnd
byte:   fragId
[data...]
```

### Reassembly

`ConnectionBase.HandleReceiveFragment()` handles reassembly:

1. Match incoming fragment to existing `FragmentedPacket` by `fragId`
2. Append data to `MemoryStream`
3. If `FragmentedPacketHandler` is registered, invoke it for progress updates (e.g., download bar)
4. When all parts received, validate `ReceivedSize == ExpectedSize` and invoke the main handler

Only one fragmented packet can be in-flight per connection at a time.

## Disconnect Reasons

```csharp
enum MpDisconnectReason : byte
{
    Protocol,               // Version mismatch
    UsernameLength,         // Invalid username length
    UsernameChars,          // Invalid characters
    UsernameAlreadyOnline,  // Duplicate
    ServerClosed,           // Server shutting down
    ServerFull,             // Max players reached
    Kick,                   // Admin kick
    BadGamePassword,        // Wrong password
    // ... and more
}
```
