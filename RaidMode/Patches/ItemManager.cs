using HarmonyLib;
using static ItemManager;
namespace RaidMode
{
    //The below patches increase item sync rates and fix online players not seeing any items of other players that joined after them.
    [HarmonyPatch(typeof(ItemManager), "UpdateSyncItem")]
    public class ItemManager_UpdateSyncItem
    {
        public static bool Prefix (ItemManager __instance)
        {
            __instance.m_itemSyncTimer = 0f;
            return true;
        }
    }
    [HarmonyPatch(typeof(ItemManager), "LoadItemsForCharacter")]
    public class ItemManager_LoadItemsForCharacter
    {
        public static void Postfix (ItemManager __instance, string _charUID, BasicSaveData[] _itemSaves)
        {
            string text = __instance.ItemSavesToString(_itemSaves);
            if (!PhotonNetwork.inRoom || PhotonNetwork.isMasterClient)
            {
                __instance.DestroyAllItemsOfChar(_charUID);
                __instance.OnReceiveItemSync(text, ItemManager.ItemSyncType.Character);
                return;
            }
            else if (_itemSaves.Length != 0)
            {
                __instance.OnReceiveItemSync(text, ItemManager.ItemSyncType.Character, _charUID);
                ItemManager.CompressDataToSend(text, ref ItemManager.m_pendingSyncItemInfos);
                for (int i = 0; i < ItemManager.m_pendingSyncItemInfos.Count; i++)
                {
                    //Once again, PhotonTargets.Others is all it takes, lawl.
                    __instance.photonView.RPC("SendLoadItemsForCharacter", PhotonTargets.Others,
                         _charUID, ItemManager.m_pendingSyncItemInfos[i], i, ItemManager.m_pendingSyncItemInfos.Count);
                }
            }
        }
    }
    [HarmonyPatch(typeof(ItemManager), "LoadStashForCharacter")]
    public class ItemManager_LoadStashForCharacter
    {
        public static void Postfix (ItemManager __instance, string _charUID, BasicSaveData[] _stashedItems)
        {
            string text = __instance.ItemSavesToString(_stashedItems);
            if (!PhotonNetwork.inRoom || PhotonNetwork.isMasterClient)
            {
                __instance.DestroyAllStashedItemsOfChar(_charUID);
                __instance.OnReceiveItemSync(text, ItemSyncType.CharacterStash, _charUID);
            }
            else if (_stashedItems.Length != 0)
            {
                __instance.OnReceiveItemSync(text, ItemSyncType.CharacterStash, _charUID);
                CompressDataToSend(text, ref m_pendingSyncItemInfos);
                for (int i = 0; i < m_pendingSyncItemInfos.Count; i++)
                {
                    __instance.photonView.RPC("SendLoadStashItemsForCharacter", PhotonTargets.Others,
                        _charUID, m_pendingSyncItemInfos[i], i, m_pendingSyncItemInfos.Count);
                }
            }
        }
    }
}