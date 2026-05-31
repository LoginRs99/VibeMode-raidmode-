using HarmonyLib;
using UnityEngine;
namespace RaidMode
{
    //Fixes backpacks not respawning with players when there isn't enough scatter points defined.
    [HarmonyPatch(typeof(DefeatPlayerSpawn), "GetScatterPoint")]
    public class DefeatPlayerSpawn_GetScatterPoint
    {
        public static void Postfix (DefeatPlayerSpawn __instance, ref Transform __result, Transform[] positions)
        {
            if (positions != null && positions.Length > 0 && __instance.ID >= positions.Length)
            {
                __result = positions[positions.Length - 1];
            }
        }
    }
}
