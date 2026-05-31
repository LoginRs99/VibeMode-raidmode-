using HarmonyLib;
using UnityEngine;
namespace RaidMode
{
    //This patch implements the Cozy Beds feature and increases the sleeping range limit so that inns are fully usable for large parties.
    [HarmonyPatch(typeof(Sleepable), "CheckProximity")]
    public class Sleepable_CheckProximity
    {
        private static float s_nextNoManLeftBehindNotificationTime;

        public static bool Prefix (Sleepable __instance, ref int __result, Character _character)
        {
            string downedNames;
            if (RaidModeConfig.LiveSettings.ReviveNoManLeftBehind
                && RaidModeConfig.TryGetDownedPartyMembers(out downedNames))
            {
                if (_character && _character.IsLocalPlayer && Time.time >= s_nextNoManLeftBehindNotificationTime)
                {
                    s_nextNoManLeftBehindNotificationTime = Time.time + 2f;
                    RaidModeConfig.ShowNoManLeftBehindBlock(_character, "rest");
                }
                __result = 1;
                return false;
            }

            //Implments the Cozy Beds feature for all proper beds by increasing their capacity just before the following sleep checks.
            if (RaidModeConfig.LiveSettings.CozyBeds && __instance.IsInnsBed)
            {
                __instance.Capacity = 2;
            }
            else
            {
                __instance.Capacity = 1;
            }
            int num = 0;
            if (CharacterManager.Instance != null
                && CharacterManager.Instance.RestLeader != null
                && _character
                && Vector3.Distance(CharacterManager.Instance.RestLeader.transform.position, _character.transform.position) > 40f)
            //Range increased from 15f to 40f.
            {
                num = 1;
            }
            if (num == 0)
            {
                if (__instance.m_occupants.Count < __instance.Capacity)
                {
                    num = 0;
                }
                else
                {
                    num = 2;
                }
            }
            __result = num;
            return false;
        }
    }
}
