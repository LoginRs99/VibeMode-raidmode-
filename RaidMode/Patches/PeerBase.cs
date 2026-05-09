using ExitGames.Client.Photon;
using HarmonyLib;
namespace RaidMode
{
    //Makes the disconnect timeout in Photon more lenient for bad connections.
    [HarmonyPatch(typeof(PeerBase), "DisconnectTimeout", MethodType.Getter)]
    public class PeerBase_CreatePeerBase
    {
        public static bool Prefix (PeerBase __instance, ref int __result)
        {
            __result = 60000;
            return false;
        }
    }
}
