using System;
using HarmonyLib;
using UnityEngine.Events;
using UnityEngine.UI;
namespace RaidMode
{
    //Implements room name obfuscation when entering it in the prompt.
    [HarmonyPatch(typeof(RoomCreationPanel), "Show", new Type[] { typeof(string), typeof(UnityAction<string>), typeof(UnityAction) })]
    public class RoomCreationPanel_Show
    {
        public static bool Prefix (RoomCreationPanel __instance)
        {
            if (RaidModeConfig.LiveSettings.HideRoomName)
                __instance.m_txtRoomName.inputType = InputField.InputType.Password;
            else
                __instance.m_txtRoomName.inputType = InputField.InputType.Standard;
            return true;
        }
    }
}
