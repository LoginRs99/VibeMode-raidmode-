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
                    __instance.LastCharacter.CharacterUI.ShowInfoNotification(RaidModeConfig.Text("Must be out of combat to revive a teammate!",
                                                                                                  "Harc kozben nem tudsz csapattarsat eleszteni!"));
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
                    __instance.LastCharacter.CharacterUI.ShowInfoNotification(RaidModeConfig.Text("The party must be out of combat to revive a teammate!",
                                                                                                  "A csapatnak ki kell kerulnie a harcbol az eleszteshez!"));
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
                        __instance.LastCharacter.CharacterUI.ShowInfoNotification(RaidModeConfig.Text("A healing item is required to revive a teammate.",
                                                                                                      "Gyogyito targy szukseges a csapattars elesztesehez."));
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
                            __instance.LastCharacter.CharacterUI.ShowInfoNotification(RaidModeConfig.Text("Used a Bandage to revive teammate.",
                                                                                                          "Bandazs felhasznalva a csapattars elesztesehez."));
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
                            __instance.LastCharacter.CharacterUI.ShowInfoNotification(RaidModeConfig.Text("Used a Life Potion to revive teammate.",
                                                                                                          "Eletital felhasznalva a csapattars elesztesehez."));
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
                            __instance.LastCharacter.CharacterUI.ShowInfoNotification(RaidModeConfig.Text("Used a Great Life Potion to revive teammate.",
                                                                                                          "Nagy eletital felhasznalva a csapattars elesztesehez."));
                        }
                    }
                }
            }
            return true;
        }
    }
}
