using HarmonyLib;
namespace RaidMode
{
    //The below patches add addtional compass elements for extra players and fixes bugs with properly assigning elements to all other players.
    [HarmonyPatch(typeof(OtherPlayersCompass), "Update")]
    public class OtherPlayersCompass_Update
    {
        public static bool Prefix (OtherPlayersCompass __instance)
        {
            for (int i = 0; i < __instance.m_watchedElements.Length; i++)
            {
                __instance.m_watchedElements[i] = null;
            }
            return true;
        }
    }
}
