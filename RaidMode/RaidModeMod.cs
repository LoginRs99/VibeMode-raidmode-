using BepInEx;
using HarmonyLib;
using System.Collections;
using UnityEngine;
namespace RaidMode
{
    [BepInPlugin("com.loginrs.vibemode", "VibeMode", "5.0.1")]
    public class RaidModeMod : BaseUnityPlugin
    {
        internal static RaidModeMod Instance;

        private static bool IsVibeModeView (PhotonView view)
        {
            return view != null
                   && ((NetworkLevelLoader_Patch.Instance != null && view == NetworkLevelLoader_Patch.Instance.photonView)
                       || (RaidModeConfig.Instance != null && view == RaidModeConfig.Instance.photonView));
        }

        internal static void CheckViewIDCollision (int viewID)
        {
            PhotonView foundView = null;
            foreach (PhotonView view in GameObject.FindObjectsOfType<PhotonView>())
            {
                if (view.viewID == viewID && !IsVibeModeView(view))
                {
                    foundView = view;
                    break;
                }
            }

            if (foundView != null)
                Debug.LogError($"[VibeMode] COLLISION: ViewID {viewID} is also claimed by: {foundView.gameObject.name}");
            else
                RaidModeConfig.DebugLog($"ViewID {viewID} has no non-VibeMode collision.");
        }

        public void Awake ()
        {
            Instance = this;
            Harmony harmony = new Harmony("com.loginrs.vibemode");
            harmony.PatchAll();
            NetworkLevelLoader_Patch.Init();
            RaidModeConfig.Init(Config);
            Debug.Log("VibeMode: 5.0.1 loaded! Forked from Raid Mode by SpicerXD.");
        }

        internal IEnumerator CheckViewIDCollisionsAfterJoin ()
        {
            // Let PUN finish registering room/scene PhotonViews before checking.
            yield return null;
            yield return null;
            CheckViewIDCollision(NetworkLevelLoader_Patch.VIEW_ID);
            CheckViewIDCollision(RaidModeConfig.VIEW_ID);
        }
    }
}
