using HarmonyLib;
namespace RaidMode
{
    //Implments the rest of the area switching restrictions for No Man Left Behind
    [HarmonyPatch(typeof(InteractionSwitchArea), "OnActivate")]
    public class InteractionSwitchArea_OnActivate
    {
        public static bool Prefix (InteractionSwitchArea __instance)
        {
            if (RaidModeConfig.LiveSettings.ReviveNoManLeftBehind)
            {
                //Block area switching if a player is dead.
                string downedNames;
                if (RaidModeConfig.TryGetDownedPartyMembers(out downedNames))
                {
                    RaidModeConfig.ShowNoManLeftBehindBlock(__instance.LastCharacter, "leave the area");
                    __instance.InterruptActivation();
                    return false;
                }
            }
            return true;
        }
    }
}
