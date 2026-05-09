using HarmonyLib;
namespace RaidMode
{
    //Another fix to improve multiplayer item sync speed.
    [HarmonyPatch(typeof(Item), "UpdateProcessing")]
    public class Item_UpdateProcessing
    {
        public static bool Prefix (Item __instance)
        {
            __instance.m_updateSpeed = 0f;
            return true;
        }
    }
}
