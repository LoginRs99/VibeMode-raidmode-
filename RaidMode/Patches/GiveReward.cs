using System.Collections.Generic;
using HarmonyLib;
using NodeCanvas.Framework;
using NodeCanvas.Tasks.Actions;
using UnityEngine;
namespace RaidMode
{
    // Implements the reward sharing options.
    // STATUS: Fixed. See BUG 7 note below.
    //
    // NETWORKING NOTE (confirmed via IL analysis of Assembly-CSharp.dll):
    // ReceiveItemReward and ReceiveSkillReward are self-routing.
    // They call get_IsPhotonPlayerLocal internally. If the target character is remote,
    // they automatically fire an RPC to that character's owning client via RPCManager.
    // Calling these methods directly from the master client IS the correct approach.
    // No double-rewarding, no save corruption risk.
    //
    // BUG 7 FIX: The original code had an early-exit when RewardReceiver == Everyone,
    // skipping all sharing logic. But in Outward's original 2-player design, "Everyone"
    // only means local split-screen players — remote network players (3, 4, 5...) are
    // not "local" from the host's perspective and receive nothing from the base game's
    // Everyone path. The fix: remove the early-exit and always run sharing logic,
    // building the otherPlayers list regardless of receiver type.
    [HarmonyPatch(typeof(GiveReward), "OnExecute")]
    public class GiveReward_OnExecute
    {
        public static void Postfix (GiveReward __instance)
        {
            if (__instance == null)
                return;
            if (CharacterManager.Instance == null)
                return;

            string receiverUIDs = CollectReceiverUIDs(__instance);
            string rewardPayload = BuildRewardPayload(__instance);

            if (string.IsNullOrEmpty(rewardPayload))
            {
                RaidModeConfig.DebugLog("Reward sharing skipped because no shareable reward payload was found.");
                return;
            }

            if (PhotonNetwork.inRoom && !PhotonNetwork.isMasterClient)
            {
                if (RaidModeConfig.Instance == null || RaidModeConfig.Instance.photonView == null)
                    return;

                RaidModeConfig.DebugLog($"Forwarding reward share request to master. receivers={receiverUIDs}, payload={rewardPayload}");
                RaidModeConfig.Instance.photonView.RPC("ReceiveRewardShareRequest", PhotonTargets.MasterClient, receiverUIDs, rewardPayload);
                return;
            }

            ShareRewardPayload(receiverUIDs, rewardPayload);
        }

        internal static void ShareRewardPayload (string receiverUIDs, string rewardPayload)
        {
            if (CharacterManager.Instance == null || string.IsNullOrEmpty(rewardPayload))
                return;

            HashSet<string> vanillaReceivers = ParseUIDs(receiverUIDs);
            List<Character> otherPlayers = new List<Character>();
            for (int i = 0; i < CharacterManager.Instance.PlayerCharacters.Count; i++)
            {
                Character character = CharacterManager.Instance.GetCharacter(CharacterManager.Instance.PlayerCharacters.Values[i]);
                if (character && !vanillaReceivers.Contains(GetCharacterUID(character)))
                {
                    otherPlayers.Add(character);
                }
            }

            if (otherPlayers.Count == 0)
            {
                RaidModeConfig.DebugLog($"Reward sharing skipped. vanillaReceivers={receiverUIDs}, otherPlayers=0");
                return;
            }

            RaidModeConfig.DebugLog($"Reward sharing started. vanillaReceivers={receiverUIDs}, otherPlayers={otherPlayers.Count}, payload={rewardPayload}");
            string[] rewards = rewardPayload.Split('|');
            for (int i = 0; i < rewards.Length; i++)
            {
                RewardShareData rewardData;
                if (!TryParseRewardShareData(rewards[i], out rewardData))
                {
                    RaidModeConfig.DebugWarning($"Reward item skipped because payload entry was invalid. entry={rewards[i]}");
                    continue;
                }

                int itemID = rewardData.ItemID;
                if (rewardDatabase.TryGetValue(itemID, out RewardType rewardType))
                {
                    switch (rewardType)
                    {
                        case RewardType.SideQuestArtifact:
                            if (!RaidModeConfig.LiveSettings.ShareSideQuestArtifacts)
                                continue;
                            break;
                        case RewardType.SideQuestSkill:
                            if (!RaidModeConfig.LiveSettings.ShareSideQuestSkills)
                                continue;
                            break;
                        case RewardType.StoryArtifact:
                            if (!RaidModeConfig.LiveSettings.ShareStoryArtifacts)
                                continue;
                            break;
                        case RewardType.StorySkill:
                            if (!RaidModeConfig.LiveSettings.ShareStorySkills)
                                continue;
                            break;
                        case RewardType.WorldArtifact:
                            if (!RaidModeConfig.LiveSettings.ShareWorldArtifacts)
                                continue;
                            break;
                    }
                }
                else
                {
                    // Item not in the sharing database — not shared.
                    WarnUnregisteredRewardItem(itemID);
                    continue;
                }

                // Deliver reward to each other player.
                // ReceiveItemReward/ReceiveSkillReward internally check IsPhotonPlayerLocal
                // and automatically RPC to the correct owning client if remote.
                foreach (Character player in otherPlayers)
                {
                    if (rewardData.IsSkill)
                    {
                        RaidModeConfig.DebugLog($"Sharing skill reward. itemID={itemID}, type={rewardType}, target={player.Name}");
                        player.Inventory.ReceiveSkillReward(itemID);
                    }
                    else
                    {
                        RaidModeConfig.DebugLog($"Sharing item reward. itemID={itemID}, type={rewardType}, quantity={rewardData.Quantity}, target={player.Name}");
                        player.Inventory.ReceiveItemReward(itemID, rewardData.Quantity, rewardData.TryToEquip);
                    }
                }
            }
        }

        private static string BuildRewardPayload (GiveReward rewardTask)
        {
            List<string> entries = new List<string>();

            foreach (NodeCanvas.Tasks.Actions.ItemQuantity reward in rewardTask.ItemReward)
            {
                if (reward == null || reward.Item == null || reward.Item.value == null)
                {
                    RaidModeConfig.DebugWarning("Reward item skipped because GiveReward contained a null item entry.");
                    continue;
                }

                var rewardItem = reward.Item.value;
                int quantity = reward.Quantity != null ? reward.Quantity.value : 1;
                int equip = reward.TryToEquip != null && reward.TryToEquip.value ? 1 : 0;
                int skill = rewardItem.RefItem is Skill ? 1 : 0;
                entries.Add($"{rewardItem.ItemID},{quantity},{equip},{skill}");
            }

            return string.Join("|", entries.ToArray());
        }

        private static bool TryParseRewardShareData (string entry, out RewardShareData data)
        {
            data = new RewardShareData();
            if (string.IsNullOrEmpty(entry))
                return false;

            string[] parts = entry.Split(',');
            if (parts.Length != 4)
                return false;

            int itemID;
            int quantity;
            int tryToEquip;
            int isSkill;
            if (!int.TryParse(parts[0], out itemID)
                || !int.TryParse(parts[1], out quantity)
                || !int.TryParse(parts[2], out tryToEquip)
                || !int.TryParse(parts[3], out isSkill))
                return false;

            data.ItemID = itemID;
            data.Quantity = quantity;
            data.TryToEquip = tryToEquip != 0;
            data.IsSkill = isSkill != 0;
            return true;
        }

        private static string CollectReceiverUIDs (GiveReward rewardTask)
        {
            if (rewardTask.receivers == null || rewardTask.receivers.Count == 0)
                return string.Empty;

            List<string> receiverUIDs = new List<string>();
            foreach (Character receiver in rewardTask.receivers)
            {
                string uid = GetCharacterUID(receiver);
                if (!string.IsNullOrEmpty(uid))
                    receiverUIDs.Add(uid);
            }
            return string.Join(";", receiverUIDs.ToArray());
        }

        private static HashSet<string> ParseUIDs (string receiverUIDs)
        {
            HashSet<string> parsed = new HashSet<string>();
            if (string.IsNullOrEmpty(receiverUIDs))
                return parsed;

            string[] split = receiverUIDs.Split(';');
            for (int i = 0; i < split.Length; i++)
            {
                if (!string.IsNullOrEmpty(split[i]))
                    parsed.Add(split[i]);
            }
            return parsed;
        }

        private static string GetCharacterUID (Character character)
        {
            if (!character || character.UID == null)
                return string.Empty;
            return character.UID.Value;
        }

        private static void WarnUnregisteredRewardItem (int itemID)
        {
            if (!AnyRewardSharingEnabled() || !s_unregisteredRewardWarnings.Add(itemID))
                return;

            Debug.LogWarning($"[VibeMode] Reward item {itemID} is not in the VibeMode sharing database, so it was not shared. If this is a unique quest/DLC reward, add it to GiveReward.rewardDatabase.");
        }

        private static bool AnyRewardSharingEnabled ()
        {
            return RaidModeConfig.LiveSettings.ShareSideQuestArtifacts
                   || RaidModeConfig.LiveSettings.ShareSideQuestSkills
                   || RaidModeConfig.LiveSettings.ShareStoryArtifacts
                   || RaidModeConfig.LiveSettings.ShareStorySkills
                   || RaidModeConfig.LiveSettings.ShareWorldArtifacts;
        }

        enum RewardType
        {
            SideQuestArtifact,
            SideQuestSkill,
            StoryArtifact,
            StorySkill,
            WorldArtifact
        }

        private struct RewardShareData
        {
            public int ItemID;
            public int Quantity;
            public bool TryToEquip;
            public bool IsSkill;
        }

        static Dictionary<int, RewardType> rewardDatabase = new Dictionary<int, RewardType>
        {
            //Unique items awarded by side-quests.
            [2140120] = RewardType.SideQuestArtifact, //Dreamer Halberd
            [3000210] = RewardType.SideQuestArtifact, //Gold-Lich Armor
            [3000212] = RewardType.SideQuestArtifact, //Gold-Lich Boots
            [3000211] = RewardType.SideQuestArtifact, //Gold-Lich Mask
            [3000043] = RewardType.SideQuestArtifact, //Jade-Lich Boots
            [3000041] = RewardType.SideQuestArtifact, //Jade-Lich Mask
            [3000040] = RewardType.SideQuestArtifact, //Jade-Lich Robes
            [5300170] = RewardType.SideQuestArtifact, //Light Mender's Backpack
            [5300160] = RewardType.SideQuestArtifact, //Preservation Backpack
            [2300180] = RewardType.SideQuestArtifact, //Ornate Bone Shield
            [3000360] = RewardType.SideQuestArtifact, //Rust Lich Armor
            [3000362] = RewardType.SideQuestArtifact, //Rust Lich Boots
            [3000361] = RewardType.SideQuestArtifact, //Rust Lich Helmet
            [5110070] = RewardType.SideQuestArtifact, //Mysterious Chakram
            //Skills awarded by side-quests.
            [8200031] = RewardType.SideQuestSkill, //Sigil of Fire
            [8100090] = RewardType.SideQuestSkill, //Flamethrower
            [8200010] = RewardType.SideQuestSkill, //Reveal Soul
            [8100300] = RewardType.SideQuestSkill, //Execution
            [8100310] = RewardType.SideQuestSkill, //Juggernaut
            [8100070] = RewardType.SideQuestSkill, //Backstab
            [8100100] = RewardType.SideQuestSkill, //Evasion Shot
            [8100290] = RewardType.SideQuestSkill, //Puncture
            [8100362] = RewardType.SideQuestSkill, //Pommel Counter
            [8100320] = RewardType.SideQuestSkill, //Moon Swipe
            [8100270] = RewardType.SideQuestSkill, //Mace Infusion
            [8100380] = RewardType.SideQuestSkill, //Talus Cleaver
            [8100340] = RewardType.SideQuestSkill, //Simeon's Gambit
            [8200190] = RewardType.SideQuestSkill, //Possessed (skill)
            [8200130] = RewardType.SideQuestSkill, //Warm (skill)
            [8200170] = RewardType.SideQuestSkill, //Mist (skill)
            [8200140] = RewardType.SideQuestSkill, //Cool (skill)
            [8200110] = RewardType.SideQuestSkill, //Blessed (skill)
            [8200180] = RewardType.SideQuestSkill, //Enrage
            [8201020] = RewardType.SideQuestSkill, //Scorch Hex
            [8201021] = RewardType.SideQuestSkill, //Chill Hex
            [8201022] = RewardType.SideQuestSkill, //Doom Hex
            [8201023] = RewardType.SideQuestSkill, //Curse Hex
            [8201024] = RewardType.SideQuestSkill, //Haunt Hex
            [8400018] = RewardType.SideQuestSkill, //Severed Obsidian
            [8400014] = RewardType.SideQuestSkill, //Kid Calygrey
            [8400019] = RewardType.SideQuestSkill, //Blade Puppy
            [8400013] = RewardType.SideQuestSkill, //Daughter Medyse
            [8400021] = RewardType.SideQuestSkill, //Golden Watcher
            [8400011] = RewardType.SideQuestSkill, //Pet Crescent Shark
            //Unique items awarded by mainline story quests.
            [3100040] = RewardType.StoryArtifact, //Candle Plate Armor
            [3100042] = RewardType.StoryArtifact, //Candle Plate Boots
            [3100041] = RewardType.StoryArtifact, //Candle Plate Helm
            [3100090] = RewardType.StoryArtifact, //Crimson Plate Armor
            [3100092] = RewardType.StoryArtifact, //Crimson Plate Boots
            [3100091] = RewardType.StoryArtifact, //Crimson Plate Mask
            [2200100] = RewardType.StoryArtifact, //Tsar Bow
            [5110097] = RewardType.StoryArtifact, //Tsar Chakram
            [3100031] = RewardType.StoryArtifact, //Zagis' Armor
            [3100030] = RewardType.StoryArtifact, //Zagis' Mask
            [2000031] = RewardType.StoryArtifact, //Radiant Wolf Sword
            [5100080] = RewardType.StoryArtifact, //Lantern of Souls
            [4300270] = RewardType.StoryArtifact, //Peacemaker Elixir
            [5100110] = RewardType.StoryArtifact, //Djinn's Lamp
            //Unique skills awarded by mainline story quests.
            [8200100] = RewardType.StorySkill, //Infuse Light
            [8200105] = RewardType.StorySkill, //Infuse Blood
            [8200104] = RewardType.StorySkill, //Infuse Mana
            [8202004] = RewardType.StorySkill, //Elatt's Intervention
            [8202005] = RewardType.StorySkill, //Kirouac's Breakthrough
            [8205280] = RewardType.StorySkill, //Acceptance
            [8205350] = RewardType.StorySkill, //Alchemical Experiment
            [8205340] = RewardType.StorySkill, //Ancestors' Memories
            [8205330] = RewardType.StorySkill, //Blood of Giants
            [8205310] = RewardType.StorySkill, //Divine Assistance
            [8205999] = RewardType.StorySkill, //Exalted
            [8205998] = RewardType.StorySkill, //Logistics Expert
            [8205261] = RewardType.StorySkill, //Painful Sacrifice
            [8205997] = RewardType.StorySkill, //Preferential Treatment
            [8205270] = RewardType.StorySkill, //Purified
            [8205260] = RewardType.StorySkill, //Sacrifice
            [8205311] = RewardType.StorySkill, //Sanctified Assistance
            [8205300] = RewardType.StorySkill, //Sanctified Protection
            [8205240] = RewardType.StorySkill, //Spiritual Communion
            //Unique items found in the world.
            [2000151] = RewardType.WorldArtifact, //Strange Rusted Sword
            [2150170] = RewardType.WorldArtifact, //Ruined Halberd
            [2000320] = RewardType.WorldArtifact, //Mysterious Blade
            [2120270] = RewardType.WorldArtifact, //De-powered Bludgeon
            [2110260] = RewardType.WorldArtifact, //Fossilized Greataxe
            [2020070] = RewardType.WorldArtifact, //Merton's Firepoker
            [2200190] = RewardType.WorldArtifact, //Ceremonial Bow
            [2120070] = RewardType.WorldArtifact, //Pillar Greathammer
            [2010280] = RewardType.WorldArtifact, //Warm Axe
            [2020330] = RewardType.WorldArtifact, //Sealed Mace
            [2130310] = RewardType.WorldArtifact, //Rusted Spear
            [2010070] = RewardType.WorldArtifact, //Sunfall Axe
            [2140060] = RewardType.WorldArtifact, //Thrice-Wrought Halberd
            [2160230] = RewardType.WorldArtifact, //Unusual Knuckles
            [2160100] = RewardType.WorldArtifact, //Tsar Fists
            [2130021] = RewardType.WorldArtifact, //Werlig Spear
            [2300360] = RewardType.WorldArtifact, //Slumbering Shield
            [5110112] = RewardType.WorldArtifact, //Experimental Chakram
            [5110340] = RewardType.WorldArtifact, //Scarred Dagger
            [5110002] = RewardType.WorldArtifact, //Red Lady's Dagger
            [2300030] = RewardType.WorldArtifact, //Zhorn's Demon Shield
            [5110005] = RewardType.WorldArtifact, //Zhorn's Glowstone Dagger
            [3200030] = RewardType.WorldArtifact, //Merton's Ribcage
            [3200032] = RewardType.WorldArtifact, //Merton's Shinbones
            [3200031] = RewardType.WorldArtifact, //Merton's Skull
            [5300070] = RewardType.WorldArtifact, //Brass-Wolf Backpack
            [5300030] = RewardType.WorldArtifact, //Mefino's Trade Backpack
            [5300180] = RewardType.WorldArtifact, //Zhorn's Hunting Backpack
            [5300050] = RewardType.WorldArtifact, //Glowstone Backpack
            [5100510] = RewardType.WorldArtifact, //Light Mender's Lexicon
        };
        private static readonly HashSet<int> s_unregisteredRewardWarnings = new HashSet<int>();
    }
}
