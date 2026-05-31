using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace RaidMode
{
    internal static class NavMeshPositionHelper
    {
        private const float NAVMESH_SAMPLE_DISTANCE = 8f;
        private const float NAVMESH_LOAD_SAMPLE_DISTANCE = 20f;

        internal static bool ShouldSnapDuringLoad (Character character)
        {
            if (!character)
                return false;
            if (!VibeModeNetwork.HasRemotePeers)
                return false;
            NetworkLevelLoader loader = NetworkLevelLoader.Instance;
            return !character.SendInitDone || loader == null || !loader.IsOverallLoadingDone;
        }

        internal static bool TrySnapToNavMesh (Vector3 source, float maxDistance, out Vector3 snapped)
        {
            snapped = source;
            if (!IsFinite(source))
                return false;

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(source, out hit, maxDistance, NavMesh.AllAreas))
                return false;

            snapped = hit.position;
            return true;
        }

        internal static bool TrySnapWantedPosition (Character character, ref Vector3 position, string reason)
        {
            if (!ShouldSnapDuringLoad(character))
                return false;

            Vector3 snapped;
            if (!TrySnapToNavMesh(position, NAVMESH_SAMPLE_DISTANCE, out snapped)
                && !TrySnapToNavMesh(position, NAVMESH_LOAD_SAMPLE_DISTANCE, out snapped))
            {
                RaidModeConfig.DebugWarning($"Wanted position was not near NavMesh. character={character.Name}, reason={reason}, position={position}");
                return false;
            }

            float distance = Vector3.Distance(position, snapped);
            if (distance <= 0.05f)
                return false;

            RaidModeConfig.DebugWarning($"Adjusted position to NavMesh. character={character.Name}, reason={reason}, distance={distance}");
            position = snapped;
            return true;
        }

        internal static void SnapCharacterTransform (Character character, string reason)
        {
            if (!ShouldSnapDuringLoad(character))
                return;

            Vector3 position = character.transform.position;
            if (TrySnapWantedPosition(character, ref position, reason))
            {
                NavMeshAgent agent = character.GetComponent<NavMeshAgent>();
                bool restoreAgent = agent != null && agent.enabled;
                if (restoreAgent)
                {
                    agent.enabled = false;
                }

                character.transform.position = position;
                character.WantedPosition = position;

                if (restoreAgent)
                {
                    agent.enabled = true;
                }
            }
        }

        private static bool IsFinite (Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite (float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    // Fixes player characters not getting wanted position/health sync when brought
    // in during gameplay. RequestWantedInfo is handled by the master client in the
    // base CharacterControl class and replies with SendWantedInfo.
    [HarmonyPatch(typeof(NetworkCharacterControl), "Update")]
    public class NetworkCharacterControl_Update
    {
        private const float REQUEST_WANTED_INFO_RETRY_SECONDS = 2f;
        private static readonly ConditionalWeakTable<NetworkCharacterControl, WantedInfoRetryState> s_wantedInfoRetryStates = new ConditionalWeakTable<NetworkCharacterControl, WantedInfoRetryState>();

        public static bool Prefix (NetworkCharacterControl __instance)
        {
            if (__instance == null || __instance.m_character == null)
            {
                return true;
            }

            NetworkLevelLoader loader = NetworkLevelLoader.Instance;
            if (__instance.m_character.IsAI && __instance.m_character.Initialized
                && loader != null && loader.IsOverallLoadingDone)
            {
                if (!VibeModeNetwork.HasRemotePeers || !PhotonNetwork.isNonMasterClientInRoom || __instance.ReceivedWantedInfo)
                {
                    s_wantedInfoRetryStates.Remove(__instance);
                }
                else
                {
                    WantedInfoRetryState retryState = s_wantedInfoRetryStates.GetOrCreateValue(__instance);
                    if (Time.time >= retryState.NextRequestTime)
                    {
                        RaidModeConfig.DebugWarning($"Retrying AI wanted info. character={__instance.m_character.Name}, localPlayer={PhotonNetwork.player.ID}");
                        __instance.photonView.RPC("RequestWantedInfo", PhotonTargets.MasterClient, PhotonNetwork.player);
                        retryState.NextRequestTime = Time.time + REQUEST_WANTED_INFO_RETRY_SECONDS;
                    }
                }
            }

            //Handles spawning split-players during gameplay.
            if (!__instance.m_character.IsAI && !__instance.m_character.SendInitDone && __instance.m_character.Initialized
                && loader != null && loader.IsOverallLoadingDone)
            {
                if (!VibeModeNetwork.HasRemotePeers)
                {
                    __instance.m_character.SendInitDone = true;
                    s_wantedInfoRetryStates.Remove(__instance);
                    return true;
                }

                if (__instance.ReceivedWantedInfo)
                {
                    RaidModeConfig.DebugLog($"Wanted info received. character={__instance.m_character.Name}, localPlayer={PhotonNetwork.player.ID}");
                    __instance.m_character.SendInitDone = true;
                    s_wantedInfoRetryStates.Remove(__instance);
                    return true;
                }

                if (!PhotonNetwork.isNonMasterClientInRoom)
                {
                    __instance.m_character.SendInitDone = true;
                    s_wantedInfoRetryStates.Remove(__instance);
                    return true;
                }

                WantedInfoRetryState retryState = s_wantedInfoRetryStates.GetOrCreateValue(__instance);
                if (Time.time >= retryState.NextRequestTime)
                {
                    RaidModeConfig.DebugWarning($"Requesting wanted info. character={__instance.m_character.Name}, localPlayer={PhotonNetwork.player.ID}");
                    __instance.photonView.RPC("RequestWantedInfo", PhotonTargets.MasterClient, PhotonNetwork.player);
                    retryState.NextRequestTime = Time.time + REQUEST_WANTED_INFO_RETRY_SECONDS;
                }
            }
            return true;
        }

        private class WantedInfoRetryState
        {
            public float NextRequestTime;
        }
    }

    [HarmonyPatch(typeof(CharacterControl), "SendWantedInfo")]
    public class CharacterControl_SendWantedInfo
    {
        public static void Prefix (CharacterControl __instance, ref Vector3 _wantedPosition)
        {
            if (__instance == null || __instance.m_character == null)
            {
                return;
            }

            NavMeshPositionHelper.TrySnapWantedPosition(__instance.m_character, ref _wantedPosition, "SendWantedInfo");
        }

        public static void Postfix (CharacterControl __instance)
        {
            if (__instance != null)
            {
                NavMeshPositionHelper.SnapCharacterTransform(__instance.m_character, "SendWantedInfo-post");
            }
        }
    }

    [HarmonyPatch(typeof(Character), "set_WantedPosition")]
    public class Character_set_WantedPosition
    {
        public static void Prefix (Character __instance, ref Vector3 value)
        {
            NavMeshPositionHelper.TrySnapWantedPosition(__instance, ref value, "WantedPosition");
        }
    }

    [HarmonyPatch(typeof(Character), "CheckLoadPosition")]
    public class Character_CheckLoadPosition
    {
        public static void Prefix (Character __instance)
        {
            NavMeshPositionHelper.SnapCharacterTransform(__instance, "CheckLoadPosition-pre");
        }

        public static void Postfix (Character __instance)
        {
            NavMeshPositionHelper.SnapCharacterTransform(__instance, "CheckLoadPosition-post");
        }
    }

    [HarmonyPatch(typeof(Character), "OnOverallLoadingDone")]
    public class Character_OnOverallLoadingDone
    {
        public static void Prefix (Character __instance)
        {
            NavMeshPositionHelper.SnapCharacterTransform(__instance, "OverallLoadingDone-pre");
        }
    }

    [HarmonyPatch(typeof(Character), "Teleport", new System.Type[] { typeof(Vector3), typeof(Quaternion) })]
    public class Character_Teleport_Quaternion
    {
        public static void Prefix (Character __instance, ref Vector3 __0)
        {
            NavMeshPositionHelper.TrySnapWantedPosition(__instance, ref __0, "Teleport");
        }
    }

    [HarmonyPatch(typeof(Character), "Teleport", new System.Type[] { typeof(Vector3), typeof(Vector3) })]
    public class Character_Teleport_Forward
    {
        public static void Prefix (Character __instance, ref Vector3 __0)
        {
            NavMeshPositionHelper.TrySnapWantedPosition(__instance, ref __0, "Teleport");
        }
    }
}
