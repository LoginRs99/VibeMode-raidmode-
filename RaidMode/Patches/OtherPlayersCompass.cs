using HarmonyLib;
namespace RaidMode
{
    //The below patches add addtional compass elements for extra players and fixes bugs with properly assigning elements to all other players.
    [HarmonyPatch(typeof(OtherPlayersCompass), "Update")]
    public class OtherPlayersCompass_Update
    {
        public static bool Prefix (OtherPlayersCompass __instance)
        {
            if (__instance == null || __instance.m_watchedElements == null || Global.Lobby == null)
                return true;

            int targetCount = UnityEngine.Mathf.Max(0, Global.Lobby.PlayersInLobbyCount - 1);
            if (__instance.m_watchedElements.Length != targetCount)
            {
                System.Array.Resize(ref __instance.m_watchedElements, targetCount);
                System.Array.Resize(ref __instance.m_indicators, targetCount);
            }

            for (int i = 0; i < __instance.m_watchedElements.Length; i++)
            {
                __instance.m_watchedElements[i] = null;
            }
            return true;
        }
    }
}
