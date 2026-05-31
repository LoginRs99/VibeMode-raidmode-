using System;
using HarmonyLib;

namespace RaidMode
{
    internal static class VibeModeNetwork
    {
        internal static bool HasRemotePeers
        {
            get
            {
                return PhotonNetwork.inRoom
                       && PhotonNetwork.otherPlayers != null
                       && PhotonNetwork.otherPlayers.Length > 0;
            }
        }
    }

    // This patch fixes issues where clients would not see properly synced split
    // stacks dropped by other clients. It is disabled in solo because no peer needs
    // the rebroadcast and it only adds PUN queue work.
    [HarmonyPatch(typeof(RPCManager), "SendItemSplitSyncToMaster")]
    public class RPCManager_SendItemSplitSyncToMaster
    {
        public static bool Prefix (RPCManager __instance, string _itemInfos)
        {
            if (!VibeModeNetwork.HasRemotePeers || string.IsNullOrEmpty(_itemInfos))
            {
                return false;
            }

            __instance.photonView.RPC("SendItemSyncRPC", PhotonTargets.Others, _itemInfos, true);
            return false;
        }
    }

    [HarmonyPatch(typeof(Character), "SendPerformSpellCastTrivial", new Type[] { typeof(int), typeof(string), typeof(int), typeof(int), typeof(float) })]
    public class Character_SendPerformSpellCastTrivial
    {
        public static bool Prefix (string __1)
        {
            return VibeModeNetwork.HasRemotePeers || !string.IsNullOrEmpty(__1);
        }
    }

    [HarmonyPatch(typeof(Character), "SendPerformSpellCastItem", new Type[] { typeof(int), typeof(string), typeof(int), typeof(int), typeof(float) })]
    public class Character_SendPerformSpellCastItem
    {
        public static bool Prefix (string __1)
        {
            return VibeModeNetwork.HasRemotePeers || !string.IsNullOrEmpty(__1);
        }
    }
}
