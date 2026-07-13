using HarmonyLib;
namespace RaidMode
{
    //Implements room name obfuscation in the pause menu.
    [HarmonyPatch(typeof(CurrentRoomNameDisplay), "OnEnable")]
    public class CurrentRoomNameDisplay_OnEnable
    {
        public static void Postfix (CurrentRoomNameDisplay __instance)
        {
            if (__instance == null || __instance.m_lblRoomName == null)
                return;

            if (RaidModeConfig.LiveSettings.HideRoomName)
                __instance.m_lblRoomName.text = "-";
            else if (PhotonNetwork.room != null)
                __instance.m_lblRoomName.text = PhotonNetwork.room.Name;
        }
    }
}
