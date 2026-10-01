# Network model samples

These definitions are not part of the match. `DataInstaller` does not bind them, except `PlayerStats` (see below).

## Shared samples

`DebugCounter`, `AtomicPair`, and `SyncCoverage` are shared models. Generated code stays in `Scripts/Generated`.

To try `DebugCounter` in a session:

1. Call `DebugCounterModelInstaller.Install(Container)` from `DataInstaller`.
2. Put a networked object in a scene with `NetworkIdentity`, `DebugCounterBridge`, and `DebugCounterSample`, and assign the bridge on the sample.
3. On the host, `DebugCounterSample` sends `CmdBump` once. Remove the object before shipping.

`AtomicPair` and `SyncCoverage` only need their installer and a bridge on a `NetworkIdentity` if you want them spawned.

## Per-player sample

`PlayerStats` (`int Score`, `string Title`) is `Scope = PerPlayer`. The registry is bound from `DataInstaller` because `DummyPlayer` carries `PlayerStatsBridge` and `PlayerStatsSample`, so a host session can show create and rejoin. That binding is runtime, not a compile requirement.

The sample seeds `Score` to 7 and `Title` to `kept` only while those fields are still empty, so a later join with the same key keeps the stored values.

## Player key

`IPlayerIdentityService.GetKey` reads `ConnectionAuthenticator.AuthRequestMessage`:

- Steam: `steam:<steamId>` when `steamId` is not 0.
- Direct IP: `client:<guid>`. The guid is created once and stored in `PlayerPrefs` under `network-model.client-id`, so the same editor process keeps it across play sessions. The persona name is not the key.
