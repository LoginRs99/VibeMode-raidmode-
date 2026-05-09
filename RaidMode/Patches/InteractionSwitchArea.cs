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
                foreach (PlayerSystem player in Global.Lobby.PlayersInLobby)
                {
                    if (player.ControlledCharacter && player.ControlledCharacter.IsDead)
                    {
                        if (__instance.LastCharacter.IsLocalPlayer)
                        {
                            __instance.LastCharacter.CharacterUI.ShowInfoNotification("Can not leave area while there are downed teammates!");
                        }
                        __instance.InterruptActivation();
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
