# VibeMode Audit — Opus Track

These items aren't simple value swaps. Each one either needs live-testing/deeper reasoning to confirm the mechanism before you can fix it correctly, or needs new code that mirrors an existing pattern with real correctness tradeoffs (double-repair risk, echo loops, race conditions). Worth the extra reasoning depth.

---

### 1. [High] "Share Blacksmith Repairs" only works when the host repairs — `Patches/RepairEquipmentNodeAction.cs`

```csharp
if (PhotonNetwork.inRoom && !PhotonNetwork.isMasterClient)
    return true;   // falls through to vanilla: local-only repair
```

`GiveReward.cs` solves the same "non-master client triggers a shareable action" problem by forwarding to the master via RPC (`ReceiveRewardShareRequest` → `PhotonTargets.MasterClient`), then the master executes and distributes. `RepairEquipmentNodeAction` doesn't — it just defers to vanilla for non-host triggers, so only the interacting player's own gear gets fixed.

**What's needed, not just what to change:** this isn't a one-line fix. You need to design and add a new RPC forwarding path mirroring GiveReward's, decide what data needs to cross the wire (which node/interaction, which character), add a `[PunRPC]` receiver, and make sure the master doesn't end up double-repairing if it also runs the local path. Read `GiveReward.cs`'s full flow (`OnExecute` → `ReceiveRewardShareRequest` → `ShareRewardPayload`) as the template before writing this.

---

### 2. [High] RPC burst scales with party size × inventory size — `Patches/ItemManager.cs`

```csharp
foreach (PhotonPlayer player in PhotonNetwork.playerList)
{
    if (player == null || localPlayer != null && player.ID == localPlayer.ID) continue;
    if (masterPlayer != null && player.ID == masterPlayer.ID) continue;
    recipients++;
    for (int i = 0; i < chunks.Count; i++)
    {
        itemManager.photonView.RPC(rpcName, player, charUID, chunks[i], i, chunks.Count);
    }
}
```

`O(recipients × chunks)` RPCs fired synchronously in one call, no batching or throttling — worse exactly as party size grows toward the mod's own 10-player ceiling. Notably, the patch directly above this one in the same file has a comment explicitly warning against causing "a near-frame-rate sync loop" that can "overload PUN... in 3+ player sessions" — so the codebase already understands this risk class, just not here.

**What's needed:** a design decision, not a value change. Two real options: (a) space chunk dispatch across frames via a coroutine, or (b) combine a peer's chunks into a single RPC call with an array payload (the settings-sync RPC already shows a working multi-field-payload pattern in this same codebase — worth reading as precedent). Pick one and think through what happens if a peer disconnects mid-dispatch.

---

### 3. [Medium — needs live testing] Possible quest-event broadcast echo — `Patches/QuestEventManager.cs`

`SetQuestEventStack`/`AddEvent` broadcast to `PhotonTargets.Others` whenever `_sendEvent` is true. The RPC names they call (`SendSyncQuestEventAdd`, `SendSetQuestEventStack`, `SendAddQuestEvent`) aren't defined anywhere in this codebase — they're almost certainly vanilla `[PunRPC]` methods being re-targeted from `PhotonTargets.Other` (2-player) to `.Others` (N-player).

The risk: this Harmony patch intercepts **every** call to `SetQuestEventStack`/`AddEvent`, regardless of origin. If the vanilla RPC receiver internally calls back into either method with `_sendEvent=true` to apply the incoming data, it re-enters this same Prefix and could re-broadcast — bouncing an update around instead of settling after one hop.

**What's needed:** this can't be fixed blind. Get 3+ clients in a session, trigger a quest event, and watch the `Quest AddEvent sync` / `Quest SetStack sync` debug logs for the same event UID showing up more than once per actual event. If it echoes, the likely fix is tracking "currently applying a received update" state and suppressing the outbound RPC during that window — but confirm the mechanism first.

---

### 4. [Medium — needs live testing] `MaxPlayers = PlayerLimit + 1` may not match its own tooltip — `Patches/ConnectPhotonMaster.cs`

The file's own comment admits this is unresolved:

> "The +1 here appears to be a deliberate buffer carried over from the original mod, but its exact intent is unconfirmed... left unchanged pending further base-game investigation of who reads MaxPlayers at runtime."

Cross-referencing `RaidModeConfig.cs:116`, the "Party Limit" tooltip says: *"Maximum number of players allowed in the online room."* That implies `PlayerLimit` should map 1:1 to Photon's `MaxPlayers`, not sit one below it. As written, Party Limit = 5 creates a room that actually accepts 6.

**What's needed:** confirm before changing anything. Set Party Limit to 2, get a 3rd connection attempt, and see whether it's actually rejected or let in. If the room really does accept one extra player, decide whether to drop the `+1` (both call sites — `CreateRoom` and `CreateOrJoin` — use it) or fix the tooltip to be honest about the real cap. Don't just delete the `+1` without testing — the original comment's caution about "who reads MaxPlayers at runtime" is worth taking seriously.

---

### 5. [Medium — needs design judgment] ViewID collisions are detected but not prevented — `RaidModeMod.cs` / `Patches/ConnectPhotonMaster.cs`

`NetworkLevelLoader_Patch.VIEW_ID = 980` and `RaidModeConfig.VIEW_ID = 981` are hardcoded. `CheckViewIDCollisionsAfterJoin()` does detect collisions post-join, but the only action on collision is `Debug.LogError(...)` — buried in a console almost no player checks. If it fires, both the settings-sync and level-load-resume channels — the two systems the project's own review guide flags as most critical — would silently misroute.

**What's needed:** two tiers of possible fix, and picking between them is a judgment call. Quick mitigation: surface the collision to the player via an in-game notification instead of just a log line. Real fix: switch to `PhotonNetwork.AllocateViewID()` for dynamic assignment, which removes the collision risk at the source — but that touches initialization in both `RaidModeConfig.Init()` and `NetworkLevelLoader_Patch`'s setup, and you'd need to confirm nothing else in the codebase assumes IDs 980/981 specifically before making the change.

---

### 6. [Medium — needs confirmation against vanilla] `StabilityHit` Prefix unconditionally returns `false` — `Character.cs`

The Prefix wraps its logic in `if (!__instance.m_impactImmune && num > 0f && !__instance.m_pendingDeath) { ... }`, but returns `false` (skip vanilla entirely) regardless of whether that condition is true. For impact-immune, zero-knockback, or pending-death characters, **neither** this mod's logic **nor** vanilla's runs — a Harmony Prefix returning `false` always suppresses the original method.

**What's needed:** you can't tell from this codebase alone whether vanilla `StabilityHit` has any side effect for those three cases (an impact sound cue, an animation flag, anything short of the actual stagger) that's now being silently dropped. This needs either decompiling the vanilla method to check, or testing in-game whether impact-immune characters lose some vanilla feedback they used to have. If something's missing, the fix is gating the `return false` behind the same condition — falling through to `return true` (run vanilla) otherwise — rather than a blanket skip.

---

### 7. [Low-Medium — worth stress-testing] Possible duplicate knock RPCs under simultaneous hits — `Character.cs` (`Character_StabilityHit`)

```csharp
bool shouldSendRPC = (!__instance.IsAI && instanceIsMine) || (__instance.IsAI && dealerIsMine);
```

This assumes each hit's damage-dealer is unambiguously owned by exactly one client. If two players land breakpoint-crossing hits on the same AI in close succession, before either RPC round-trips, both could independently satisfy `dealerIsMine` and both fire `SendKnock`.

**What's needed:** this depends on Outward's actual AI-ownership model, which isn't visible from this codebase. Worth a stress test: 3+ players focus-fire one enemy and watch for a double-triggered knock animation or duplicate `SendKnock` calls in the logs. Only worth changing the ownership condition if the test actually reproduces it.

---

### 8. [Low — open question, not a code bug] Unresolved co-op restriction override — `Patches/AreaManager.cs`

```csharp
//Simply allows co-op to be accessed in any region. Not sure tho if this is problematic though.
[HarmonyPatch(typeof(AreaManager), "IsCoopRestricted")]
public class AreaManager_IsCoopRestricted
{
    public static void Postfix (ref bool __result)
    {
        __result = false;
    }
}
```

The original developer flagged their own uncertainty in the comment. This isn't a bug to fix — it's an open question to resolve: are there specific regions/story beats in Outward where forcing co-op access causes a problem (scripted solo sequences, story gating, etc.)?

**What's needed:** investigation, not a patch. Check which regions vanilla restricts co-op in and why, then either confirm this override is safe everywhere or scope it to exclude the specific problem regions.
