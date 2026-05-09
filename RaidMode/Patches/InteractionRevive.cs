using HarmonyLib;
namespace RaidMode
{
    //Implments the restrictions and costs for reviving players with the No Man Left Behind .
    [HarmonyPatch(typeof(InteractionRevive), "OnActivate")]
    public class InteractionRevive_OnActivate
    {
        public static bool Prefix (InteractionBase __instance)
        {
            //Block reviving if the reviver is in combat.
            if (RaidModeConfig.LiveSettings.ReviveCombatRestrictions == RaidModeConfig.ReviveCombatSetting.OnlyReviver
                && Global.CombatManager.PlayersInCombat.Contains(__instance.LastCharacter))
            {
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
                        if (PhotonNetwork.isMasterClient)
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
                        if (PhotonNetwork.isMasterClient)
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
                        if (PhotonNetwork.isMasterClient)
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
