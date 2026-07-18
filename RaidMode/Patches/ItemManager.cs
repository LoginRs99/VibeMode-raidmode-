using System.Collections.Generic;
using HarmonyLib;

namespace RaidMode
{
    // The below patches fix online players not seeing any items of other players
    // that joined after them.
    [HarmonyPatch(typeof(ItemManager), "UpdateSyncItem")]
    public class ItemManager_UpdateSyncItem
    {
        public static bool Prefix (ItemManager __instance)
        {
            // Do not force m_itemSyncTimer to 0 every frame. That turns vanilla's
            // scheduled item sync into a near-frame-rate sync loop during busy
            // transitions, which can overload PUN/Unity temp allocations in 3+ player
            // sessions. The targeted load/stash rebroadcasts below handle late-join
            // visibility without increasing the global sync cadence.
            return true;
        }
    }

    [HarmonyPatch(typeof(ItemManager), "LoadItemsForCharacter")]
    public class ItemManager_LoadItemsForCharacter
    {
        public static void Postfix (ItemManager __instance, string _charUID, BasicSaveData[] _itemSaves)
        {
            if (!VibeModeNetwork.HasRemotePeers || PhotonNetwork.isMasterClient || _itemSaves == null)
            {
                return;
            }

            if (_itemSaves.Length != 0)
            {
                string text = __instance.ItemSavesToString(_itemSaves);
                List<string> itemInfoChunks = new List<string>();
                ItemManager.CompressDataToSend(text, ref itemInfoChunks);

                RaidModeConfig.DebugLog($"Client broadcasting character items to non-master peers. char={_charUID}, itemCount={_itemSaves.Length}, chunks={itemInfoChunks.Count}, localPlayer={PhotonNetwork.player.ID}");
                ItemManagerPeerSync.SendChunksToNonMasterPeers(__instance, "SendLoadItemsForCharacter", _charUID, itemInfoChunks);
            }
        }
    }

    [HarmonyPatch(typeof(ItemManager), "LoadStashForCharacter")]
    public class ItemManager_LoadStashForCharacter
    {
        public static void Postfix (ItemManager __instance, string _charUID, BasicSaveData[] _stashedItems)
        {
            if (!VibeModeNetwork.HasRemotePeers || PhotonNetwork.isMasterClient || _stashedItems == null)
            {
                return;
            }

            if (_stashedItems.Length != 0)
            {
                string text = __instance.ItemSavesToString(_stashedItems);
                List<string> stashInfoChunks = new List<string>();
                ItemManager.CompressDataToSend(text, ref stashInfoChunks);

                RaidModeConfig.DebugLog($"Client broadcasting character stash to non-master peers. char={_charUID}, itemCount={_stashedItems.Length}, chunks={stashInfoChunks.Count}, localPlayer={PhotonNetwork.player.ID}");
                ItemManagerPeerSync.SendChunksToNonMasterPeers(__instance, "SendLoadStashItemsForCharacter", _charUID, stashInfoChunks);
            }
        }
    }

    internal static class ItemManagerPeerSync
    {
        internal static void SendChunksToNonMasterPeers (ItemManager itemManager, string rpcName, string charUID, List<string> chunks)
        {
            if (!VibeModeNetwork.HasRemotePeers || chunks == null || chunks.Count == 0 || PhotonNetwork.playerList == null)
                return;

            if (RaidModeMod.Instance != null)
            {
                RaidModeMod.Instance.StartCoroutine(SendChunksCoroutine(itemManager, rpcName, charUID, chunks));
            }
        }

        private static System.Collections.IEnumerator SendChunksCoroutine (ItemManager itemManager, string rpcName, string charUID, List<string> chunks)
        {
            PhotonPlayer localPlayer = PhotonNetwork.player;
            PhotonPlayer masterPlayer = PhotonNetwork.masterClient;

            List<PhotonPlayer> validPeers = new List<PhotonPlayer>();
            foreach (PhotonPlayer player in PhotonNetwork.playerList)
            {
                if (player != null && (localPlayer == null || player.ID != localPlayer.ID) && (masterPlayer == null || player.ID != masterPlayer.ID))
                {
                    validPeers.Add(player);
                }
            }

            for (int i = 0; i < chunks.Count; i++)
            {
                if (itemManager == null || itemManager.photonView == null)
                    yield break;

                foreach (PhotonPlayer player in validPeers)
                {
                    itemManager.photonView.RPC(rpcName, player, charUID, chunks[i], i, chunks.Count);
                }
                yield return null;
            }

            RaidModeConfig.DebugLog($"Sent {rpcName} to {validPeers.Count} non-master peers via coroutine. char={charUID}, chunks={chunks.Count}");
        }
    }
}
