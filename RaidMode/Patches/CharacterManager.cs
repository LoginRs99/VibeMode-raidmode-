using HarmonyLib;
namespace RaidMode
{
    //Block area switching if a player is dead when the No Man Left Behind feature is on.
    [HarmonyPatch(typeof(CharacterManager), "SendStartFastTravel")]
    public class CharacterManager_SendStartFastTravel
    {
        public static bool Prefix ()
        {
            if (RaidModeConfig.LiveSettings.ReviveNoManLeftBehind)
            {
                foreach (PlayerSystem player in Global.Lobby.PlayersInLobby)
                {
                    if (player.ControlledCharacter && player.ControlledCharacter.IsDead)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
