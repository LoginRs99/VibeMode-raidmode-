using System;
using HarmonyLib;
namespace RaidMode
{
    //Modifies the default player limit for online lobbies.
    [HarmonyPatch(typeof(ConnectPhotonMaster), "CreateOrJoin")]
    public class ConnectPhotonMaster_CreateOrJoin
    {
        public static bool Prefix (string _roomName)
        {
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
            PhotonNetwork.CreateRoom(_roomName, roomOptions, TypedLobby.Default);
            return false;
        }
    }
}
