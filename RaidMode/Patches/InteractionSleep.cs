using HarmonyLib;
namespace RaidMode
{
    //Implments the Cozy Beds feature here too so it shows on the interaction text.
    [HarmonyPatch(typeof(InteractionSleep), "ProcessText")]
    public class InteractionSleep_ProcessText
    {
        public static bool Prefix (InteractionSleep __instance)
        {
            if (RaidModeConfig.LiveSettings.CozyBeds && __instance.m_sleepableScript.IsInnsBed)
            {
                __instance.m_sleepableScript.Capacity = 2;
            }
            else
            {
                __instance.m_sleepableScript.Capacity = 1;
            }
            return true;
        }
    }
}
