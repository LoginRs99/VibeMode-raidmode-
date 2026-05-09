using HarmonyLib;
using UnityEngine;
namespace RaidMode
{
    //Special thanks to Sinai for this fix which properly handles players joining at previously not well handled times, like during loading screens.
    //I'm not gonna mess with it, so it just works. XD
    public class NetworkLevelLoader_Patch : Photon.MonoBehaviour
    {
        internal static NetworkLevelLoader_Patch Instance;
        internal const int VIEW_ID = 980;
        internal static bool s_isSendingGameplayResumedRequest;
        internal static bool s_lastGameplayResumedResult;
        public static void Init ()
        {
            var obj = new GameObject("RaidModeNetLevelLoaderRPC");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<NetworkLevelLoader_Patch>();
            var view = obj.AddComponent<PhotonView>();
            view.viewID = VIEW_ID;
        }
        [HarmonyPatch(typeof(NetworkLevelLoader), "UpdateCheckAllPlayerDoneLoading")]
        public class NetworkLevelLoader_UpdateCheckAllPlayerDoneLoading
        {
            public static void Postfix (NetworkLevelLoader __instance)
            {
                if (!PhotonNetwork.isNonMasterClientInRoom)
                {
                    return;
                }
                if (!s_isSendingGameplayResumedRequest)
                {
                    if (s_lastGameplayResumedResult)
                    {
                        var selfPlayer = PhotonNetwork.player;
                        foreach (var player in PhotonNetwork.playerList)
                        {
                            if (player.ID == selfPlayer.ID)
                            {
                                continue;
                            }
                            if (!__instance.m_doneLoadingPlayers.Contains(player.ID))
                            {
                                __instance.m_doneLoadingPlayers.Add(player.ID);
                            }
                            if (!__instance.m_readyToContinuePlayers.Contains(player.ID))
                            {
                                __instance.m_readyToContinuePlayers.Add(player.ID);
                            }
                        }
                    }
                    else
                    {
                        SendRequestIsGameplayResumed();
                    }
                }
            }
        }
        public static void SendRequestIsGameplayResumed ()
        {
            if (s_isSendingGameplayResumedRequest)
            {
                return;
            }
            s_lastGameplayResumedResult = false;
            s_isSendingGameplayResumedRequest = true;
            Instance.photonView.RPC(nameof(ReceiveRequestIsGameplayResumed), PhotonTargets.MasterClient);
        }
        [PunRPC]
        public void ReceiveRequestIsGameplayResumed (PhotonMessageInfo info)
        {
            NetworkLevelLoader_Patch.Instance.photonView.RPC(nameof(ReceiveIsGameplayResumed), info.sender, (bool)(!NetworkLevelLoader.Instance.IsGameplayPaused && CharacterManager.Instance?.GetFirstLocalCharacter()));
        }
        [PunRPC]
        public void ReceiveIsGameplayResumed (bool resumed)
        {
            s_lastGameplayResumedResult = resumed;
            s_isSendingGameplayResumedRequest = false;
        }
    }
}