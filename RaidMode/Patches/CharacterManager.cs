using HarmonyLib;
namespace RaidMode
{
    // Block area switching if a player is dead when the No Man Left Behind feature is on.
    // STATUS: No changes. Audited and confirmed clean.
    // Iterates Global.Lobby.PlayersInLobby (a dictionary — no fixed size).
    // Handles any player count correctly.
    [HarmonyPatch(typeof(CharacterManager), "SendStartFastTravel")]
    public class CharacterManager_SendStartFastTravel
    {
        public static bool Prefix ()
        {
            if (RaidModeConfig.LiveSettings.ReviveNoManLeftBehind)
            {
                string downedNames;
                if (RaidModeConfig.TryGetDownedPartyMembers(out downedNames))
                {
                    Character localCharacter = CharacterManager.Instance ? CharacterManager.Instance.GetFirstLocalCharacter() : null;
                    if (localCharacter)
                    {
                        RaidModeConfig.ShowNoManLeftBehindBlock(localCharacter, "fast travel");
                    }
                    else
                    {
                        RaidModeConfig.DebugLog(RaidModeConfig.GetNoManLeftBehindBlockMessage("fast travel"));
                    }
                    return false;
                }
            }
            return true;
        }
    }
}
