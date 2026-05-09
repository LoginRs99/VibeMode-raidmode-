using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
namespace RaidMode
{
    // Adds extra player sleep schedule markers to support more players.
    // And also increments their heights so each one is heigher than the previous to increase readability when there's lots of players.
    [HarmonyPatch(typeof(RestingMenu), "StartInit")]
    public class RestingMenu_StartInit
    {
        public static void Postfix (RestingMenu __instance)
        {
            if (__instance.m_sldOtherPlayerCursorTemplate)
            {
                for (int i = 0; i < __instance.m_sldOtherPlayerCursors.Length; i++)
                {
                    GameObject.Destroy(__instance.m_sldOtherPlayerCursors[i].gameObject);
                    __instance.m_sldOtherPlayerCursors[i] = null;
                }
                __instance.m_sldOtherPlayerCursors = new Slider[19];
                __instance.m_sldOtherPlayerCursorTemplate.gameObject.SetActive(true);
                for (int i = 0; i < __instance.m_sldOtherPlayerCursors.Length; i++)
                {
                    __instance.m_sldOtherPlayerCursors[i] = GameObject.Instantiate(__instance.m_sldOtherPlayerCursorTemplate);
                    __instance.m_sldOtherPlayerCursors[i].transform.SetParent(__instance.m_sldOtherPlayerCursorTemplate.transform.parent);
                    __instance.m_sldOtherPlayerCursors[i].transform.ResetRectTrans();
                    __instance.m_sldOtherPlayerCursors[i].transform.Translate(0, i + 1, 0);
                }
                __instance.m_sldOtherPlayerCursorTemplate.gameObject.SetActive(false);
                __instance.ResetMenu();
            }
        }
    }
}
