using HarmonyLib;
namespace RaidMode
{
    // A patch to let the coop scaling function always run, even in singleplayer.
    // STATUS: No changes. Audited and confirmed clean.
    // Uses Global.Lobby.PlayersInLobbyCount dynamically — no hardcoded limits.
    // The PlayersInLobbyCount parameter passed to ApplyToCharacter is redundant
    // (CoopStats_ApplyToCharacter re-reads it internally) but harmless.
    [HarmonyPatch(typeof(CharacterStats), "ApplyCoopStats")]
    public class CharacterStats_ApplyCoopStats
    {
        public static bool Prefix (CharacterStats __instance)
        {
            NetworkLevelLoader loader = NetworkLevelLoader.Instance;
            if (loader != null && loader.IsGameplayLoading)
            {
                __instance.m_delayedApplyCoopStats = true;
                return false;
            }
            if (__instance.CoopStats)
            {
                if (!__instance.m_character)
                {
                    __instance.m_character = __instance.GetComponent<Character>();
                }
                if (!__instance.m_character)
                    return false;

                float healthRatio = __instance.m_character.HealthRatio;
                int playerCount = Global.Lobby != null ? Global.Lobby.PlayersInLobbyCount : 1;
                __instance.CoopStats.ApplyToCharacter(__instance.m_character, playerCount);
                __instance.m_character.Stats.UpdateStats();
                __instance.m_character.Stats.SetHealth(healthRatio * __instance.m_character.Stats.ActiveMaxHealth);
            }
            return false;
        }
    }
}
