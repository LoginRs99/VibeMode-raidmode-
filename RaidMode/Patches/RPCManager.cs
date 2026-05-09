using HarmonyLib;
namespace RaidMode
{
    //This patches fixes issues where clients would not see properly synced split stacks dropped by other clients.
    [HarmonyPatch(typeof(RPCManager), "SendItemSplitSyncToMaster")]
    public class RPCManager_SendItemSplitSyncToMaster
    {
        public static bool Prefix (RPCManager __instance, string _itemInfos)
        {
            //Another classic PhotonTargets.Others fix.
            __instance.photonView.RPC("SendItemSyncRPC", PhotonTargets.Others, _itemInfos, true);
            return false;
        }
    }
}
