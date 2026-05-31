using System.Collections.Generic;
using HarmonyLib;

namespace RaidMode
{
    // Replaces vanilla's static CharacterInventory.itemsToMove scratch list with
    // a local list so overlapping TakeAllContent calls cannot trample each other.
    [HarmonyPatch(typeof(CharacterInventory), "TakeAllContent")]
    public class CharacterInventory_TakeAllContent
    {
        public static bool Prefix (CharacterInventory __instance, ItemContainer _container, bool _forceChange, ref bool __result)
        {
            bool allItemsMoved = true;
            if (_container.ItemCount > 0)
            {
                IList<Item> containedItems = _container.GetContainedItems();
                int count = containedItems.Count;
                List<string> itemsToMove = new List<string>(count);

                for (int i = 0; i < containedItems.Count; i++)
                {
                    if (containedItems[i].GetIsDLCOwned())
                    {
                        itemsToMove.Add(containedItems[i].UID);
                    }
                }

                __instance.TakeItem(itemsToMove.ToArray(), !_container.IsChildToCharacter);
                allItemsMoved = itemsToMove.Count == count;

                if (!allItemsMoved)
                {
                    _container.NotifyMissingDLC(__instance.m_character);
                }

                if (_forceChange)
                {
                    for (int i = 0; i < itemsToMove.Count; i++)
                    {
                        Item item = ItemManager.Instance.GetItem(itemsToMove[i]);
                        if (item)
                        {
                            item.ForceUpdateParentChange();
                            if (PhotonNetwork.isNonMasterClientInRoom)
                            {
                                Global.RPCManager.SendItemSyncToMaster(item.ToNetworkData());
                            }
                        }
                    }
                }
            }

            if (_container.ContainedSilver > 0)
            {
                __instance.AddMoney(0, _container.ContainedSilver, true);
                _container.RemoveSilver(_container.ContainedSilver);
            }

            __result = allItemsMoved;
            return false;
        }
    }
}
