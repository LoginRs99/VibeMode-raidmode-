using HarmonyLib;
namespace RaidMode
{
    //Fixes split-players not getting init data when brought in during gameplay.
    [HarmonyPatch(typeof(NetworkCharacterControl), "Update")]
    public class NetworkCharacterControl_Update
    {
        public static bool Prefix (NetworkCharacterControl __instance)
        {
            //Handles spawning split-players during gameplay.
            if (!__instance.m_character.IsAI && !__instance.m_character.SendInitDone && __instance.m_character.Initialized
                && NetworkLevelLoader.Instance.IsOverallLoadingDone)
            {
                __instance.photonView.RPC("RequestInitInfo", __instance.photonView.owner, PhotonNetwork.player);
                __instance.m_character.SendInitDone = true;
            }
            return true;
        }
    }
}
