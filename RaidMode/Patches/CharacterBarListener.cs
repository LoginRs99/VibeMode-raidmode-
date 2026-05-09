using HarmonyLib;
namespace RaidMode
{
    //Adds a nameplate to the hp bar for other players and keeps them visible.
    [HarmonyPatch(typeof(CharacterBarListener), "UpdateDisplay")]
    public class CharacterBarListener_UpdateDisplay
    {
        public static void Postfix (CharacterBarListener __instance)
        {
            if (__instance.TargetCharacter && __instance.TargetCharacter.OwnerPlayerSys && __instance.TargetCharacter != __instance.CharacterUI.TargetCharacter && __instance.m_healthBar && __instance.m_healthBar.m_lblValue)
            {
                __instance.m_healthBar.HideIfFull = !RaidModeConfig.LiveSettings.ShowNameplates;
                __instance.m_healthBar.m_lblValue.gameObject.SetActive(RaidModeConfig.LiveSettings.ShowNameplates || MenuManager.Instance && MenuManager.Instance.DisplayDebugInfo);
                if (RaidModeConfig.LiveSettings.ShowNameplates)
                    __instance.m_healthBar.m_lblValue.text = __instance.TargetCharacter.Name;
            }
        }
    }
    //Workarounds for line of sight and distance checks to implement globally visible nameplates.
    //However nameplates must first be in normal range to be globalized. But since players typically start together, it shouldn't be an issue.
    [HarmonyPatch(typeof(CharacterBarListener), "UpdateVisibility")]
    public class CharacterBarListener_UpdateVisibility
    {
        public static void Postfix (CharacterBarListener __instance)
        {
            if (__instance.TargetCharacter && __instance.TargetCharacter.OwnerPlayerSys && __instance.TargetCharacter != __instance.CharacterUI.TargetCharacter && __instance.LocalCharacter && __instance.LocalCharacter.IsStartInitDone && __instance.m_targetCharacterBarManager && __instance.LocalCharacter.CharacterCamera && __instance.LocalCharacter.CharacterCamera.CameraScript)
            {
                __instance.m_targetCharacterBarManager.DisplayDistance = 100000f;
                if (RaidModeConfig.LiveSettings.ShowNameplates && RaidModeConfig.LiveSettings.ShowNameplatesGlobally)
                {
                    __instance.m_inRange = true;
                    __instance.UpdateSeenByCamera();
                    __instance.m_inSight = true;
                }
                else
                {
                    __instance.m_inRange = __instance.m_sqrDistanceWithLocalPlayer <= 20f * 20f;
                }
            }
        }
    }
}