# AI Review Guide: VibeMode (RaidMode)

This document is designed to bootstrap any future AI coding assistant tasked with reviewing, debugging, or extending the **VibeMode** (formerly *RaidMode*) mod for *Outward: Definitive Edition*. 

It outlines the project architecture, BepInEx/Harmony hook mechanics, network synchronization protocols, historical bugs to avoid regressing, and includes a ready-to-use review prompt.

---

## 1. Project Overview & Architecture

*   **Target Game:** *Outward: Definitive Edition* (Unity, C#)
*   **Modding Framework:** BepInEx (v5.x), utilizing Harmony for runtime method patching.
*   **Networking Engine:** Photon Unity Networking (PUN 1 / Photon Classic).
*   **Core Goal:** Extend multiplayer lobbies from the vanilla limit of 2 players up to 3–10 players, implementing dynamic scaling for difficulty, health, damage, rewards, and sleeping/reviving mechanics to balance the game.

### Directory Structure
*   `RaidMode/` - Core C# source code.
    *   `RaidModeMod.cs` - Plugin entry point (inherits `BaseUnityPlugin`). Initialized by BepInEx.
    *   `RaidModeConfig.cs` - Configuration bindings, settings synchronization logic, and RPC receiver.
    *   `Patches/` - Harmony patches targeting various base game systems.
*   `dist/` - Folder containing the compiled `VibeMode.dll`, `manifest.json` for Thunderstore/r2modman, and the mod's icon.

---

## 2. Key Systems & Network Protocol

### Host-Authoritative Settings Sync
VibeMode uses a host-authoritative model for settings. Clients can read configuration locally via BepInEx, but when connected to a room, the Master Client (Host) broadcasts its configuration to all clients.

*   **Communication Channel:** A custom `GameObject` named `RaidModeConfigRPC` is spawned on `Init()`. A `PhotonView` is dynamically attached with a hardcoded View ID of **`981`**.
*   **RPC Synchronization:** `UpdateLiveSettings(object[] data)` (marked with `[PunRPC]`) is called across all clients.
*   **Payload Size (`SETTINGS_PAYLOAD_SIZE = 29`):** The payload array must contain exactly 29 elements. Changes to this array require updating both the parser (`UpdateLiveSettings`) and the serializer (`PopulateSettingsData`), keeping this constant aligned. If they fall out of alignment, client synchronization completely breaks.
*   **Protocol Version (`SETTINGS_PROTOCOL_VERSION = 1`):** Sent as the last index of the payload. Ensures clients with mismatched mod versions are warned instead of breaking.

### Crucial Networking Pitfalls & Workarounds
1.  **Enum Unboxing Crash:** PUN serializes enums as standard integers (`int`). Direct unboxing from the RPC `object[]` array to a C# enum (e.g., `(DifficultyModeSetting)data[5]`) throws an `InvalidCastException` on clients, breaking the entire settings sync. All enums must be safely parsed using intermediate casting:
    `SafeEnum(data, index, fallback)` which validates definition and converts `object -> int -> TEnum`.
2.  **RPC Flooding Protection:** In `RaidModeConfig.cs`'s `Update()` loop, settings changes are broadcasted. Because network travel takes time, state flags like `settingsChanged` must be reset **before** sending the RPC. Resetting them after the RPC is received will cause the local loop to spam dozens of identical RPCs during the round-trip window.

---

## 3. Historical Bug Registry (Do Not Regress!)

| Bug ID | Component | Description & Fix Context |
| :--- | :--- | :--- |
| **BUG 1** | `RaidModeConfig.cs` | BepInEx/r2modman configuration parsing requires a named section header. An empty string (`""`) makes the configuration completely invisible or unparseable. Always use `"General"` or another non-empty string. |
| **BUG 2** | `NetworkLevelLoader.cs` | **Game-Resume Softlock:** During zone transitions, the game waits for all clients to report they are finished loading. Previously, the "done loading" status only updated locally. It now broadcasts an RPC to the Master Client so the game properly resumes for all players. |
| **BUG 4** | `RaidModeConfig.cs` | **Enum Unboxing Crash:** Described in Section 2. Enums transferred over PUN must be cast from `object` to `int` before converting to the enum type. |
| **BUG 5** | `RaidModeConfig.cs` | **RPC Flooding:** Described in Section 2. Reset sync flags (`settingsChanged`, `updateChars`) *before* calling `photonView.RPC` to prevent sending frames of redundant packets. |
| **BUG 7** | `GiveReward.cs` | **Reward Sharing Failure:** An early exit checking for "Everyone" was preventing remote players from getting shared loot. Ensure the reward sharing hook evaluates correctly for non-local split-screen players. |

---

## 4. Dependencies & Compilation

To build this project from a fresh clone:
1.  Obtain publicized assembly files from *Outward: Definitive Edition* (using a publicizer tool like AssemblyPublicizer on the game's `Assembly-CSharp.dll` and `Assembly-CSharp-firstpass.dll`).
2.  Place the publicized DLLs alongside standard BepInEx and Unity assemblies inside the folder:
    `RaidMode/bin/Debug/` (e.g., `Assembly-CSharp-publicized.dll`, `BepInEx.dll`, `0Harmony.dll`, `Photon3Unity3D.dll`, and `UnityEngine.CoreModule.dll`).
3.  Ensure the `.csproj` file successfully resolves the `<HintPath>` paths to `bin\Debug\`.
4.  Run `dotnet build -c Release` to output the final DLL to `bin/Release/` or copy it to your game's plugins folder.

---

## 5. Review & Debugging Prompt

*Copy and paste the prompt below to initiate an AI review session on this project.*

```text
You are an expert AI software engineer specializing in Unity modding, BepInEx/Harmony, and Photon Unity Networking (PUN 1) for Outward: Definitive Edition.

I want you to audit the VibeMode (formerly RaidMode) codebase. 
Please read D:/github/Mods/VibeMode/AI_REVIEW_GUIDE.md first to understand the architecture, network protocol, payload requirements, and historic bugs.

Focus your audit on:
1. RPC flow safety (preventing floods, guaranteeing flags are reset before broadcasting).
2. PUN 1 compatibility (ensuring enums are not unboxed directly from object arrays, validating payload bounds).
3. Null safety on Unity/Outward components (e.g., Character, CharacterStats, Global.Lobby, inventory references inside Patches).
4. Code quality, potential race conditions during level transitions, and reward sharing issues.

Please provide a detailed review highlighting any logic errors, performance bottlenecks, or stability risks you find.
```
