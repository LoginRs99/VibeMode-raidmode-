using HarmonyLib;
namespace RaidMode
{
    //A patch to let the coop scaling function always run, even in singleplayer
    [HarmonyPatch(typeof(CharacterStats), "ApplyCoopStats")]
    public class CharacterStats_ApplyCoopStats
    {
        public static bool Prefix (CharacterStats __instance)
        {
            if (NetworkLevelLoader.Instance.IsGameplayLoading)
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
                float healthRatio = __instance.m_character.HealthRatio;
                __instance.CoopStats.ApplyToCharacter(__instance.m_character, Global.Lobby.PlayersInLobbyCount);
                __instance.m_character.Stats.UpdateStats();
                __instance.m_character.Stats.SetHealth(healthRatio * __instance.m_character.Stats.ActiveMaxHealth);
            }
            return false;
        }
    }
}
