# Vibecode

Vibecode is an Outward Definitive Edition co-op mod forked from Raid Mode by SpicerXD. It raises the online party limit and adds fixes/options for larger co-op groups.

## Requirements

- Outward Definitive Edition.
- BepInEx 5 for Outward / r2modman BepInEx profile support.
- The same Vibecode version installed for every player in the session.

Vibecode uses Harmony, Photon/PUN 1, Unity, NodeCanvas, and Outward game assemblies that are already provided by BepInEx and the game. SideLoader is not required by Vibecode itself, but it is fine to use if other mods in the profile need it.

## Install

Place `Vibecode.dll` in:

```text
BepInEx/plugins/
```

For r2modman, install the mod into the active Outward Definitive Edition profile and make sure every player uses the same DLL build.

Vibecode has a client-local `Language` option. English and Hungarian are currently supported for Vibecode's own in-game notifications.

## Host And Client Settings

The host/master client is authoritative. In online co-op, the host's Vibecode settings are synced to all clients. Non-host config changes made while connected are ignored and will log a warning.

Client-specific visual preferences are currently not separate. Nameplates, debug logging, travel readiness messages, and gameplay settings are all synced from the host.

## Multiplayer Connectivity & Desync Prevention

**How to Host & Connect:**
1. The Host opens their game to multiplayer exactly like the vanilla game. No special configuration is needed.
2. Ensure the Host has the `Party Limit` configuration adjusted in the BepInEx `Vibecode.cfg` file to support your group size before opening the lobby.
3. Friends can join using the standard Find Match/Lobby system or by typing in the exact lobby name.

**Port Forwarding & Networking:**
Outward uses Photon Unity Networking (PUN) which connects players via cloud servers. Because of this, **no port-forwarding is required**. As long as everyone has a stable internet connection, you can play together across the internet immediately.

**Managing Desyncs (4-6 Players):**
The vanilla game was hard-coded for 2 players, meaning a 4-6 player lobby pushes the game's engine to its limits. While Vibecode fixes many internal network desyncs related to combat and questing, you should follow these best practices for a smooth playthrough:
* **The Host should lead:** Have the Host trigger region transitions, interact with main story NPCs, and perform large inventory sorts.
* **Avoid flooding the network:** 6 players dropping items or looting the same body at the exact same moment can cause packet loss. Try to stagger interactions when looting large chests.
* **If a desync happens:** You may occasionally encounter invisible items, enemies acting strangely, or someone getting stuck during a loading screen. If this occurs, simply have the desynced player disconnect and reconnect to the lobby. The Host does not need to restart.


## Important Options

- `Party Limit`: Host-only. Sets the maximum online room size.
- `Difficulty Mode`: Host-synced. Chooses enemy scaling mode.
- `Hard Mode`: Host-synced. Doubles supported scaling bonuses.
- `Manual Difficulty Scaling`: Host-synced. Overrides automatic party-size scaling.
- `Revival Health Burn` / `Revival Stamina Burn`: Host-synced. Controls revive penalties.
- `Stability Rework`: Host-synced. Reduces enemy stagger-locking in larger groups.
- `No Man Left Behind`: Host-synced. Blocks travel/rest while teammates are downed.
- `Show Travel Readiness Messages`: Host-synced UI. Shows who is blocking travel/rest.
- `Cozy Beds`: Host-synced. Lets two players share supported house/inn beds.
- Reward sharing options: Host-synced. Controls whether selected quest/story/world rewards are shared.
- `Debug Logging`: Host-synced diagnostics. Enable only when collecting logs.

## Testing Notes

Use the same Vibecode build on all machines. For first tests, use copied saves or test characters, then check the BepInEx log for Vibecode warnings after joining, area transitions, rest, death/revive, rewards, and late-join attempts.
