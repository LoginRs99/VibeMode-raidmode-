using System.Collections.Generic;
using HarmonyLib;
using NodeCanvas.Framework;
using NodeCanvas.Tasks.Actions;
namespace RaidMode
{
    //Implements the reward sharing options.
    [HarmonyPatch(typeof(GiveReward), "OnExecute")]
    public class GiveReward_OnExecute
    {
        public static void Postfix (GiveReward __instance)
        {
            if (__instance.RewardReceiver != GiveReward.Receiver.Everyone)
            {
                Character originalReciever = CharacterManager.Instance.GetWorldHostCharacter();
                if (__instance.RewardReceiver == GiveReward.Receiver.Instigator)
                {
                    if (__instance.blackboard != null)
                    {
                        Variable<Character> instigator = __instance.blackboard.GetVariable<Character>("gInstigator");
                        if (instigator != null)
                        {
                            originalReciever = instigator.value;
                        }
                    }
                }

                //Get all other players.
                List<Character> otherPlayers = new List<Character>();
                for (int i = 0; i < CharacterManager.Instance.PlayerCharacters.Count; i++)
                {
                    Character character = CharacterManager.Instance.GetCharacter(CharacterManager.Instance.PlayerCharacters.Values[i]);
                    if (character && character != originalReciever)
                    {
                        otherPlayers.Add(character);
                    }
                }

                //Share item rewards
                foreach (NodeCanvas.Tasks.Actions.ItemQuantity reward in __instance.ItemReward)
                {
                    if (rewardDatabase.TryGetValue(reward.Item.value.ItemID, out RewardType rewardType))
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
                        //Not being shared.
                        continue;
                    }

                    //Share among players.
                    foreach (Character player in otherPlayers)
                    {
                        int quantity = reward.Quantity != null ? reward.Quantity.value : 1;
                        bool tryToEquip = reward.TryToEquip != null && reward.TryToEquip.value;
                        if (reward.Item.value.RefItem is Skill)
                        {
                            player.Inventory.ReceiveSkillReward(reward.Item.value.ItemID);
                        }
                        else
                        {
                            player.Inventory.ReceiveItemReward(reward.Item.value.ItemID, quantity, tryToEquip);
                        }
                    }
                }
            }
        }
        enum RewardType
        {
            SideQuestArtifact,
            SideQuestSkill,
            StoryArtifact,
            StorySkill,
            WorldArtifact
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
    }
}
