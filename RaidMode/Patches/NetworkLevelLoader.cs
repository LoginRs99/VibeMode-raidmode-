using HarmonyLib;
using UnityEngine;
namespace RaidMode
{
    // Special thanks to Sinai for the original foundation of this fix.
    // Rewritten to correctly handle players joining during active gameplay,
    // including proper two-way sync of the done-loading state with the master client.
    public class NetworkLevelLoader_Patch : Photon.MonoBehaviour
    {
        internal static NetworkLevelLoader_Patch Instance;
        internal const int VIEW_ID = 980;
        private const float GAMEPLAY_RESUMED_RETRY_SECONDS = 1f;
        internal static bool s_isSendingGameplayResumedRequest;
        internal static bool s_lastGameplayResumedResult;
        private static float s_nextGameplayResumedRequestTime;

        private static void ResetGameplayResumedHandshake ()
        {
            RaidModeConfig.DebugLog("Resetting gameplay-resumed handshake for a new load sequence.");
            s_lastGameplayResumedResult = false;
            s_isSendingGameplayResumedRequest = false;
            s_nextGameplayResumedRequestTime = 0f;
        }

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
                // Only non-master clients need this handshake.
                // The master client is authoritative and already knows its own state.
                if (!PhotonNetwork.isNonMasterClientInRoom)
                    return;

                // Once we've confirmed the game is resumed and broadcast our done-loading
                // state to all clients (including master), we have nothing left to do.
                if (s_lastGameplayResumedResult)
                    return;

                // Send the request if we're not already waiting for a response.
                if (!s_isSendingGameplayResumedRequest)
                {
                    SendRequestIsGameplayResumed();
                }
            }
        }

        [HarmonyPatch(typeof(NetworkLevelLoader), "BaseLoadLevel")]
        public class NetworkLevelLoader_BaseLoadLevel
        {
            public static void Postfix ()
            {
                // BaseLoadLevel clears vanilla's done-loading lists for each area
                // transition, so the late-join handshake must be allowed to run again.
                ResetGameplayResumedHandshake();
            }
        }

        public static void SendRequestIsGameplayResumed ()
        {
            if (s_isSendingGameplayResumedRequest)
                return;
            if (Time.time < s_nextGameplayResumedRequestTime)
                return;
            if (Instance == null || Instance.photonView == null)
                return;

            s_isSendingGameplayResumedRequest = true;
            RaidModeConfig.DebugLog($"Requesting gameplay-resumed state from master. localPlayer={PhotonNetwork.player.ID}");
            Instance.photonView.RPC(nameof(ReceiveRequestIsGameplayResumed), PhotonTargets.MasterClient);
        }

        // Received by master client only.
        // Master checks if gameplay is actually running and replies to the requesting client.
        [PunRPC]
        public void ReceiveRequestIsGameplayResumed (PhotonMessageInfo info)
        {
            NetworkLevelLoader loader = NetworkLevelLoader.Instance;
            Character firstLocalCharacter = CharacterManager.Instance != null ? CharacterManager.Instance.GetFirstLocalCharacter() : null;
            bool resumed = loader != null && !loader.IsGameplayPaused && firstLocalCharacter;
            RaidModeConfig.DebugLog($"Master received gameplay-resumed request from player={info.sender.ID}. resumed={resumed}");
            Instance.photonView.RPC(nameof(ReceiveIsGameplayResumed), info.sender, resumed);
        }

        // Received by the joining client only (reply from master).
        [PunRPC]
        public void ReceiveIsGameplayResumed (bool resumed)
        {
            s_isSendingGameplayResumedRequest = false;

            if (!resumed)
            {
                // Game is not ready yet. Retry later, not every frame.
                RaidModeConfig.DebugLog("Gameplay-resumed reply was false. Will retry.");
                s_nextGameplayResumedRequestTime = Time.time + GAMEPLAY_RESUMED_RETRY_SECONDS;
                return;
            }

            // FIX (BUG 2): The original code only modified the local client's
            // m_doneLoadingPlayers list, which the master client never reads.
            // The master's own copy remained unmodified, so it never unblocked.
            //
            // The correct fix: broadcast our player ID to ALL clients via a new RPC.
            // This adds the joining player to m_doneLoadingPlayers on EVERY client,
            // including the master, which is the one actually gating gameplay resumption.
            s_lastGameplayResumedResult = true;
            RaidModeConfig.DebugLog($"Gameplay resumed confirmed. Broadcasting done-loading for player={PhotonNetwork.player.ID}");
            Instance.photonView.RPC(nameof(ReceivePlayerDoneLoading),
                                    PhotonTargets.All,
                                    PhotonNetwork.player.ID);
        }

        // Received by ALL clients (including master and the joining player itself).
        // Marks the joining player as done loading in the local NetworkLevelLoader state.
        [PunRPC]
        public void ReceivePlayerDoneLoading (int playerID)
        {
            var loader = NetworkLevelLoader.Instance;
            if (loader == null)
            {
                RaidModeConfig.DebugWarning($"ReceivePlayerDoneLoading: NetworkLevelLoader.Instance is null for playerID {playerID}");
                return;
            }

            if (!loader.m_doneLoadingPlayers.Contains(playerID))
            {
                loader.m_doneLoadingPlayers.Add(playerID);
                RaidModeConfig.DebugLog($"Added player={playerID} to done-loading list on localPlayer={PhotonNetwork.player.ID}.");
            }
            else
            {
                RaidModeConfig.DebugLog($"Player={playerID} was already in done-loading list on localPlayer={PhotonNetwork.player.ID}.");
            }

            if (!loader.m_readyToContinuePlayers.Contains(playerID))
            {
                loader.m_readyToContinuePlayers.Add(playerID);
                RaidModeConfig.DebugLog($"Added player={playerID} to ready-to-continue list on localPlayer={PhotonNetwork.player.ID}.");
            }

            RaidModeConfig.DebugLog($"Player {playerID} marked as done loading on client {PhotonNetwork.player.ID}");

            if (PhotonNetwork.isMasterClient)
            {
                SendDamagedAIWantedInfoToPlayer(playerID);
            }
        }

        private static void SendDamagedAIWantedInfoToPlayer (int playerID)
        {
            if (CharacterManager.Instance == null)
                return;

            PhotonPlayer targetPlayer = GetPhotonPlayer(playerID);
            if (targetPlayer == null)
                return;

            int sent = 0;
            foreach (Character character in CharacterManager.Instance.Characters.Values)
            {
                if (!character || !character.IsAI || character.Health >= character.ActiveMaxHealth)
                    continue;

                CharacterControl control = character.CharacterControl;
                if (control == null || control.photonView == null)
                    continue;

                control.photonView.RPC("SendWantedInfo", targetPlayer, character.WantedPosition, character.Health);
                sent++;
            }

            if (sent > 0)
            {
                RaidModeConfig.DebugLog($"Sent damaged AI wanted info refresh to player={playerID}. count={sent}");
            }
        }

        private static PhotonPlayer GetPhotonPlayer (int playerID)
        {
            if (PhotonNetwork.playerList == null)
                return null;

            foreach (PhotonPlayer player in PhotonNetwork.playerList)
            {
                if (player != null && player.ID == playerID)
                    return player;
            }

            return null;
        }
    }
}
