using BepInEx;
using HarmonyLib;
using UnityEngine;
namespace RaidMode
{
    [BepInPlugin("com.spicerXD.raidmode", "Raid Mode", "4.2.5")]
    public class RaidModeMod : BaseUnityPlugin
    {

        public void Awake ()
        {
            Harmony harmony = new Harmony("com.spicerxd.raidmode");
            harmony.PatchAll();
            NetworkLevelLoader_Patch.Init();
            RaidModeConfig.Init(Config);
            Debug.Log("Raid Mode: 4.2.4 loaded!");
        }
    }
}
