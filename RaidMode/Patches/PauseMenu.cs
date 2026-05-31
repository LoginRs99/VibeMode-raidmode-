using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
namespace RaidMode
{
    //These patches modify the pause menu so that split-screen play is accessible when hosting a game.
    [HarmonyPatch(typeof(PauseMenu), "Update")]
    public class PauseMenu_Update
    {
        public static bool Prefix (PauseMenu __instance)
        {
            //Forward-ported base functions
            if (__instance.m_hideWanted && __instance.IsDisplayed && Time.time - __instance.m_lastToggleTime >= __instance.MinActiveTime)
                __instance.OnHide();
            if (__instance.m_displayTarget != 0)
                __instance.UpdateShowHide();
            if ((bool)__instance.LocalCharacter)
            {
                bool canUseSplitButton = __instance.LocalCharacter.Alive
                                         && (PhotonNetwork.offlineMode || PhotonNetwork.isMasterClient);
                if (__instance.m_btnSplit.interactable != canUseSplitButton)
                    __instance.m_btnSplit.interactable = canUseSplitButton;

                if (!__instance.m_suicide && ((Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.U)) || ControlsInput.GamepadUnstuckCheat(__instance.PlayerID)))
                {
                    __instance.m_suicide = true;
                    if ((bool)__instance.m_btnDie && !__instance.m_btnDie.gameObject.activeSelf)
                        __instance.m_btnDie.gameObject.SetActive(value: true);
                }
            }
            while (__instance.m_actionQueue.Count > 0)
            {
                __instance.m_actionQueue.Dequeue()?.Invoke();
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(PauseMenu), "Show")]
    public class PauseMenu_Show
    {
        public static void Postfix (PauseMenu __instance)
        {
            __instance.m_btnSplit.interactable = __instance.LocalCharacter
                                                 && __instance.LocalCharacter.Alive
                                                 && (PhotonNetwork.offlineMode || PhotonNetwork.isMasterClient);
            __instance.m_btnToggleNetwork.interactable = StoreManager.Instance.AllowOnlineFeatures && !ConnectPhotonMaster.Instance.RequestingRooms;
        }
    }
    [HarmonyPatch(typeof(PauseMenu), "OnToggleNetwork")]
    public class PauseMenu_OnToggleNetwork
    {
        public static bool Prefix (PauseMenu __instance)
        {
            if (PhotonNetwork.offlineMode)
            {
                __instance.m_gameNamingWindow.Show(LocalizationManager.Instance.GetLoc("MessageBox_Online_RoomNameCreate"), __instance.OnConfirmRoomCreation);
                return false;
            }
            return true;
        }
    }
}
