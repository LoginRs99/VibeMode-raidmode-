using System;
using HarmonyLib;
using UnityEngine;
namespace RaidMode
{
    //The below patches fixes some quests events not syncing correctly in co-op. Especially in the case of events caused by one client not syncing to other clients.
    [HarmonyPatch(typeof(QuestEventManager), "SetQuestEventStack")]
    public class QuestEventManager_SetQuestEventStack
    {
        public static bool Prefix (QuestEventManager __instance, string _eventUID, int _stackAmount, bool _sendEvent, ref bool __result)
        {
            QuestEventData questEventData;
            if (!QuestEventManager.m_questEvents.TryGetValue(_eventUID, out questEventData))
            {
                __result = __instance.AddEvent(_eventUID, _stackAmount, _sendEvent);
                return false;
            }
            questEventData.ResetTime();
            questEventData.SetStack(_stackAmount);
            __instance.NotifyOnQEAddedListeners(questEventData);
            if (_sendEvent)
            {
                if (!PhotonNetwork.isNonMasterClientInRoom)
                {
                    //PhotonTargets.Others magic sauce
                    __instance.photonView.RPC("SendSyncQuestEventAdd", PhotonTargets.Others, _eventUID, questEventData.ToNetworkData());
                }
                else
                {
                    //PhotonTargets.Others magic sauce
                    __instance.photonView.RPC("SendSetQuestEventStack", PhotonTargets.Others, _eventUID, _stackAmount);
                }
            }
            __result = true;
            return false;
        }
    }
    [HarmonyPatch(typeof(QuestEventManager), "AddEvent", new Type[] { typeof(string), typeof(int), typeof(bool) })]
    public class QuestEventManager_AddEvent
    {
        public static bool Prefix (QuestEventManager __instance, ref bool __result, string _eventUID, int _stackAmount, bool _sendEvent)
        {
            if (string.IsNullOrEmpty(_eventUID))
            {
                Debug.LogError("Tryied to add event but received empty UID");
                __result = false;
                return false;
            }
            QuestEventData questEventData = null;
            if (!QuestEventManager.m_questEvents.TryGetValue(_eventUID, out questEventData))
            {
                questEventData = QuestEventData.CreateEventData(_eventUID);
                if (questEventData == null)
                {
                    __result = false;
                    return false;
                }
                questEventData.SetStack(_stackAmount);
                QuestEventManager.m_questEvents.Add(_eventUID, questEventData);
            }
            else if (questEventData.IsStackable)
            {
                questEventData.IncreaseStack(_stackAmount);
            }
            if (questEventData.IsEphemeral)
            {
                QuestEventManager.m_ephemeralEvents.Add(questEventData.EventUID);
            }
            questEventData.UpdateTime();
            __instance.NotifyOnQEAddedListeners(questEventData);
            if (_sendEvent)
            {
                if (!PhotonNetwork.isNonMasterClientInRoom)
                {
                    //PhotonTargets.Others magic sauce
                    __instance.photonView.RPC("SendSyncQuestEventAdd", PhotonTargets.Others, _eventUID, questEventData.ToNetworkData());
                }
                else
                {
                    //PhotonTargets.Others magic sauce
                    __instance.photonView.RPC("SendAddQuestEvent", PhotonTargets.Others, _eventUID, _stackAmount);
                }
            }
            __result = true;
            return false;
        }
    }
}
