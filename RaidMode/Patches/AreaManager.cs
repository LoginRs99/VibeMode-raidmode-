using HarmonyLib;
namespace RaidMode
{
    //Simply allows co-op to be accessed in any region. Not sure tho if this is problematic though.
    [HarmonyPatch(typeof(AreaManager), "IsCoopRestricted")]
    public class AreaManager_IsCoopRestricted
    {
        public static void Postfix (ref bool __result)
        {
            __result = false;
        }
    }
}
