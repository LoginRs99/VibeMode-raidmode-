# VibeMode Audit — Sonnet Track

These are confirmed bugs with a clear, mechanical fix — no open questions, no live-testing needed to know what "correct" looks like. Good fit for a fast, cheap pass: change the value, add the guard, mirror the pattern that already exists elsewhere in the same file.

---

### 1. [Critical] Empty BepInEx config section — `RaidModeConfig.cs:108`

```csharp
#region Base Section
string section0 = "";
```

`section0` is used for six settings: `Language`, `Hide Room Name`, `Party Limit`, `Show Nameplates`, `Show Nameplates Globally`, `Debug Logging`. Every other section in the file uses a real name (`"Difficulty"`, `"Sharing Options"`, etc.).

**Fix:** `string section0 = "General";` (or any non-empty name consistent with the region's intent).

---

### 2. [High] Manual Difficulty Scaling off-by-one — `Character.cs:19`

The config tooltip (`RaidModeConfig.cs:116`) says the setting scales "as if that many **extra** players were present." `CoopStats.cs:41` implements that correctly:

```csharp
int playerCount = manualPlayerCount > 0 ? manualPlayerCount + 1 : lobbyPlayerCount;
```

`Character.cs:19` (`Character_SlowDown`) doesn't add the `+1`:

```csharp
int playerCount = manualPlayerCount > 0 ? manualPlayerCount : lobbyPlayerCount;
```

**Fix:** change line 19 to match CoopStats: `manualPlayerCount > 0 ? manualPlayerCount + 1 : lobbyPlayerCount`.

---

### 3. [High] Unguarded `m_sleepableScript` — `Patches/InteractionSleep.cs`

```csharp
public static bool Prefix (InteractionSleep __instance)
{
    if (RaidModeConfig.LiveSettings.CozyBeds && __instance.m_sleepableScript.IsInnsBed)
    {
        __instance.m_sleepableScript.Capacity = 2;
    }
    ...
```

No null check on `m_sleepableScript`, unlike every other patch in the codebase (including its own sibling, `Sleepable.cs`).

**Fix:** add `if (__instance.m_sleepableScript == null) return true;` at the top, matching the defensive style used everywhere else.

---

### 4. [Medium] Missing null guard on `_eventUID` — `Patches/QuestEventManager.cs`

`AddEvent.Prefix` guards against a null/empty UID (lines 45–50):

```csharp
if (string.IsNullOrEmpty(_eventUID))
{
    Debug.LogError("Tryied to add event but received empty UID");
    __result = false;
    return false;
}
```

`SetQuestEventStack.Prefix` has no equivalent check before calling `QuestEventManager.m_questEvents.TryGetValue(_eventUID, out questEventData)` — a null key throws `ArgumentNullException`.

**Fix:** copy the same guard block into `SetQuestEventStack.Prefix`.

---

### 5. [Medium] Unguarded singletons in `PauseMenu.cs`

`PauseMenu_Show.Postfix`:

```csharp
if (__instance.m_btnToggleNetwork != null)
{
    __instance.m_btnToggleNetwork.interactable = StoreManager.Instance.AllowOnlineFeatures
                                                 && !ConnectPhotonMaster.Instance.RequestingRooms;
}
```

`StoreManager.Instance` and `ConnectPhotonMaster.Instance` aren't null-checked, even though the surrounding code clearly has defensive intent (`m_btnToggleNetwork`, `m_btnSplit`, `m_gameNamingWindow` are all checked elsewhere in this file). Same gap for `LocalizationManager.Instance` in `PauseMenu_OnToggleNetwork.Prefix`.

**Fix:** add `!= null` checks for all three singleton references, following the pattern already used for the other guarded fields in this file.

---

### 6. [Low] Unguarded `m_animator` — `Character.cs` (`Character_StabilityHit`)

Three calls (`SetTrigger("Knockhurt")`, `SetInteger("KnockAngle", ...)`, `SetTrigger("BlockHit")`) dereference `__instance.m_animator` directly, while the same method carefully guards `Stats`, `CharacterCamera`, and `photonView`.

**Fix:** add a null check on `__instance.m_animator` consistent with the method's existing guards.

---

### 7. [Low] Unguarded `player.Inventory` — `GiveReward.cs` (`ShareRewardPayload`)

```csharp
if (rewardData.IsSkill)
    player.Inventory.ReceiveSkillReward(itemID);
else
    player.Inventory.ReceiveItemReward(itemID, rewardData.Quantity, rewardData.TryToEquip);
```

`player` is pre-validated when added to the list, but `.Inventory` itself isn't checked.

**Fix:** add a null check on `player.Inventory` before this branch (defense-in-depth; low real-world risk).

---

### 8. [Low] Unguarded `_container` parameter — `Patches/CharacterInventory.cs` (`TakeAllContent` Prefix)

`_container.ItemCount` / `.GetContainedItems()` are used without a null check on the parameter itself.

**Fix:** add `if (_container == null) return true;` at the top of the Prefix.

---

### 9. [Medium] Missing fallback branch for `impactResBonus` — `CoopStats.cs:73-93`

`CoopStats_ApplyToCharacter.Prefix`:

```csharp
float impactResBonus = 0;
if (baseImpactRes < 100f) { ... }
else if (baseImpactRes - 25f < 100f) { ... }
// no else — silently stays 0 when baseImpactRes >= 125
```

The sibling method `VanillaPlus` (lines 148–174) does the identical calculation but has a fallback:

```csharp
else { newImpactResBonus = baseImpactResBonus; }
```

**Fix:** add the same `else { impactResBonus = baseImpactResBonus; }` to `CoopStats_ApplyToCharacter.Prefix` — the pattern to copy is already in this file.

---

### 10. [Low] No retry cap — `Patches/NetworkCharacterControl.cs`

The wanted-info retry logic (~lines 106–164) is well-throttled (one attempt per `REQUEST_WANTED_INFO_RETRY_SECONDS` via `ConditionalWeakTable`) but has no upper bound — it retries silently forever if the master never responds.

**Fix:** add a max-attempt counter to `WantedInfoRetryState`, and promote the debug log to a warning once the cap is hit.

---

### 11. [Low] Duplicated Cozy Beds capacity logic

The same logic appears verbatim in two places:

- `Patches/InteractionSleep.cs` (`InteractionSleep_ProcessText.Prefix`)
- `Patches/Sleepable.cs` (`Sleepable_CheckProximity.Prefix`)

```csharp
Capacity = (RaidModeConfig.LiveSettings.CozyBeds && IsInnsBed) ? 2 : 1;
```

**Fix:** extract into a small shared helper (e.g. a static method on `RaidModeConfig`) and call it from both patches.

---

### 12. [Low] Hardcoded `Slider[19]` — `Patches/RestingMenu.cs:26`

```csharp
__instance.m_sldOtherPlayerCursors = new Slider[19];
```

Disconnected from `PlayerLimit`'s actual max of 10 (`AcceptableValueRange<int>(1, 10)` in `RaidModeConfig.cs`). Over-allocates up to 9 unused UI elements.

**Fix:** replace `19` with a reference to the configured/max player limit, or a named constant tied to it.

---

### 13. [Low] Magic numbers / hardcoded IDs (general cleanup)

Bare integer literals with no named constants, scattered across files:
- Reward item IDs in `GiveReward.cs`
- Healing item IDs in `InteractionRevive.cs`: `4400010`, `4300010`, `4300240`
- PhotonView IDs: `980` (`NetworkLevelLoader.cs`), `981` (`RaidModeConfig.cs`)

**Fix:** pull these into named constants (or a small lookup table with inline comments) so future edits to the reward/item whitelist are safer.
