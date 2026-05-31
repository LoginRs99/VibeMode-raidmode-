using System;
using HarmonyLib;
namespace RaidMode
{
    // Modifies the default player limit for online lobbies.
    // STATUS: No logic changes from original. Documented for clarity.
    //
    // NOTE on PlayerLimit + 1:
    // The base game uses MaxPlayers = 2 for its vanilla 2-player sessions.
    // Through IL analysis we confirmed the base game hardcodes ldc.i4.2 in CreateRoom.
    // The +1 here appears to be a deliberate buffer carried over from the original mod,
    // but its exact intent is unconfirmed. It means a party of 5 creates a room with
    // MaxPlayers = 6. This has not been traced to any desync and is left unchanged
    // pending further base-game investigation of who reads MaxPlayers at runtime.
    [HarmonyPatch(typeof(ConnectPhotonMaster), "CreateOrJoin")]
    public class ConnectPhotonMaster_CreateOrJoin
    {
        public static bool Prefix (string _roomName)
        {
            RaidModeConfig.DebugLog($"JoinOrCreateRoom. room={_roomName}, configuredLimit={RaidModeConfig.LiveSettings.PlayerLimit}, photonMaxPlayers={RaidModeConfig.LiveSettings.PlayerLimit + 1}");
            PhotonNetwork.JoinOrCreateRoom(_roomName, new RoomOptions
            {
                IsVisible = true,
                MaxPlayers = (byte)(RaidModeConfig.LiveSettings.PlayerLimit + 1),
            }, TypedLobby.Default);
            return false;
        }
    }

    [HarmonyPatch(typeof(ConnectPhotonMaster), "CreateRoom", new Type[] { typeof(string), typeof(int), typeof(string), typeof(bool), typeof(int) })]
    public class ConnectPhotonMaster_CreateRoom
    {
        public static bool Prefix (string _roomName, int _storeID, string _lobbyID, bool _hardcore, int _dlcid)
        {
            RoomOptions roomOptions = new RoomOptions();
            ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
            if (_storeID != -1 && !string.IsNullOrEmpty(_lobbyID))
            {
                hashtable.Add("Store", _storeID);
                hashtable.Add("LobbyID", _lobbyID);
            }
            hashtable.Add("Hardcore", _hardcore);
            hashtable.Add("AreaDLC", _dlcid);
            roomOptions.CustomRoomProperties = hashtable;
            roomOptions.IsVisible = true;
            roomOptions.MaxPlayers = (byte)(RaidModeConfig.LiveSettings.PlayerLimit + 1);
            RaidModeConfig.DebugLog($"CreateRoom. room={_roomName}, configuredLimit={RaidModeConfig.LiveSettings.PlayerLimit}, photonMaxPlayers={roomOptions.MaxPlayers}, store={_storeID}, lobby={_lobbyID}, dlc={_dlcid}");
            PhotonNetwork.CreateRoom(_roomName, roomOptions, TypedLobby.Default);
            return false;
        }
    }

    [HarmonyPatch(typeof(ConnectPhotonMaster), "OnJoinedRoom")]
    public class ConnectPhotonMaster_OnJoinedRoom
    {
        public static void Postfix ()
        {
            if (RaidModeMod.Instance != null)
            {
                RaidModeMod.Instance.StartCoroutine(RaidModeMod.Instance.CheckViewIDCollisionsAfterJoin());
            }
        }
    }
}
