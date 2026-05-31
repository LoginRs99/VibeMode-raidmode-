# VibeMode

VibeMode is an Outward Definitive Edition co-op mod forked from Raid Mode by SpicerXD. It raises the online party limit and adds fixes/options for larger co-op groups.

## Requirements

- Outward Definitive Edition.
- BepInEx 5 for Outward / r2modman BepInEx profile support.
- The same VibeMode version installed for every player in the session.

VibeMode uses Harmony, Photon/PUN 1, Unity, NodeCanvas, and Outward game assemblies that are already provided by BepInEx and the game. SideLoader is not required by VibeMode itself, but it is fine to use if other mods in the profile need it.

## Install

Place `VibeMode.dll` in:

```text
BepInEx/plugins/
```

For r2modman, install the mod into the active Outward Definitive Edition profile and make sure every player uses the same DLL build.

VibeMode has a client-local `Language` option. English and Hungarian are currently supported for VibeMode's own in-game notifications.

## Host And Client Settings

The host/master client is authoritative. In online co-op, the host's VibeMode settings are synced to all clients. Non-host config changes made while connected are ignored and will log a warning.

Client-specific visual preferences are currently not separate. Nameplates, debug logging, travel readiness messages, and gameplay settings are all synced from the host.

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

Use the same VibeMode build on all machines. For first tests, use copied saves or test characters, then check the BepInEx log for VibeMode warnings after joining, area transitions, rest, death/revive, rewards, and late-join attempts.
