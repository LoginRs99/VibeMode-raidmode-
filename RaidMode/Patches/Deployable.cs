using HarmonyLib;

namespace RaidMode
{
    //Fixes bug with the drums and chimes skills in co-op.
    [HarmonyPatch(typeof(Deployable), "DeployableCast")]
    public class Deployable_DeployableCast
    {
        public static bool Prefix(Deployable __instance)
        {
            if (!__instance.IsDeployed)
                __instance.m_character = __instance.Item.OwnerCharacter;

            return true;
		}
    }
}
