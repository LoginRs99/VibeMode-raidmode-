using HarmonyLib;
namespace RaidMode
{
    //Implments the restrictions and costs for reviving players with the No Man Left Behind .
    [HarmonyPatch(typeof(InteractionRevive), "OnActivate")]
    public class InteractionRevive_OnActivate
    {
        public static bool Prefix (InteractionBase __instance)
        {
            if (Global.CombatManager == null)
                return true;

            //Block reviving if the reviver is in combat.
            if (RaidModeConfig.LiveSettings.ReviveCombatRestrictions == RaidModeConfig.ReviveCombatSetting.OnlyReviver
                && Global.CombatManager.PlayersInCombat.Contains(__instance.LastCharacter))
            {
                RaidModeConfig.DebugLog($"Revive blocked: reviver in combat. reviver={__instance.LastCharacter.Name}");
                if (__instance.LastCharacter.IsLocalPlayer)
                {
                    __instance.LastCharacter.CharacterUI.ShowInfoNotification("Must be out of combat to revive a teammate!");
                }
                __instance.InterruptActivation();
                return false;
            }
            //Block reviving if any players are in combat.
            if (RaidModeConfig.LiveSettings.ReviveCombatRestrictions == RaidModeConfig.ReviveCombatSetting.Party &&
                Global.CombatManager.PlayersInCombat.Count > 0)
            {
                RaidModeConfig.DebugLog($"Revive blocked: party in combat. reviver={__instance.LastCharacter.Name}, playersInCombat={Global.CombatManager.PlayersInCombat.Count}");
                if (__instance.LastCharacter.IsLocalPlayer)
                {
                    __instance.LastCharacter.CharacterUI.ShowInfoNotification("The party must be out of combat to revive a teammate!");
                }
                __instance.InterruptActivation();
                return false;
            }
            if (RaidModeConfig.LiveSettings.ReviveItemNeeded)
            {
                //Require a bandage, life potion, or great life potion to revive players.
                if (!__instance.LastCharacter.Inventory.OwnsItem(4400010)
                    && !__instance.LastCharacter.Inventory.OwnsItem(4300010)
                    && !__instance.LastCharacter.Inventory.OwnsItem(4300240))
                {
                    RaidModeConfig.DebugLog($"Revive blocked: missing healing item. reviver={__instance.LastCharacter.Name}");
                    if (__instance.LastCharacter.IsLocalPlayer)
                    {
                        __instance.LastCharacter.CharacterUI.ShowInfoNotification("A healing item is required to revive a teammate.");
                    }
                    __instance.InterruptActivation();
                    return false;
                }
                else
                {
                    if (__instance.LastCharacter.Inventory.OwnsItem(4400010)) //Use a bandage.
                    {
                        RaidModeConfig.DebugLog($"Revive consuming Bandage. reviver={__instance.LastCharacter.Name}, isLocalOwner={__instance.LastCharacter.IsPhotonPlayerLocal}");
                        if (__instance.LastCharacter.IsPhotonPlayerLocal)
                        {
                            __instance.LastCharacter.Inventory.RemoveItem(4400010, 1);
                        }
                        if (__instance.LastCharacter.IsLocalPlayer)
                        {
                            __instance.LastCharacter.CharacterUI.ShowInfoNotification("Used a Bandage to revive teammate.");
                        }
                    }
                    else if (__instance.LastCharacter.Inventory.OwnsItem(4300010)) //Or use a life potion.
                    {
                        RaidModeConfig.DebugLog($"Revive consuming Life Potion. reviver={__instance.LastCharacter.Name}, isLocalOwner={__instance.LastCharacter.IsPhotonPlayerLocal}");
                        if (__instance.LastCharacter.IsPhotonPlayerLocal)
                        {
                            __instance.LastCharacter.Inventory.RemoveItem(4300010, 1);
                        }
                        if (__instance.LastCharacter.IsLocalPlayer)
                        {
                            __instance.LastCharacter.CharacterUI.ShowInfoNotification("Used a Life Potion to revive teammate.");
                        }
                    }
                    else //Or finally, use a great life potion.
                    {
                        RaidModeConfig.DebugLog($"Revive consuming Great Life Potion. reviver={__instance.LastCharacter.Name}, isLocalOwner={__instance.LastCharacter.IsPhotonPlayerLocal}");
                        if (__instance.LastCharacter.IsPhotonPlayerLocal)
                        {
                            __instance.LastCharacter.Inventory.RemoveItem(4300240, 1);
                        }
                        if (__instance.LastCharacter.IsLocalPlayer)
                        {
                            __instance.LastCharacter.CharacterUI.ShowInfoNotification("Used a Great Life Potion to revive teammate.");
                        }
                    }
                }
            }
            return true;
        }
    }
}
