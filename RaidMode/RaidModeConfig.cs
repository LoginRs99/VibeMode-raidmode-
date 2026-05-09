using BepInEx.Configuration;
using UnityEngine;
namespace RaidMode
{
    //Handles the config stuff and syncing changes between clients and the host.
    public class RaidModeConfig : Photon.MonoBehaviour
    {
        public enum DifficultyModeSetting
        {
            Default,
            Custom,
            VanillaPlus,
            JustVanilla,
            NoScaling
        }
        public enum ReviveCombatSetting
        {
            Anytime,
            OnlyReviver,
            Party
        }
        public struct SettingsData
        {
            public bool HideRoomName;
            public int PlayerLimit;
            public bool ShowNameplates;
            public bool ShowNameplatesGlobally;
            public DifficultyModeSetting DifficultyMode;
            public bool HardMode;
            public int ManualDifficultyScaling;
            public int RevivalHealthBurn;
            public int RevivalStaminaBurn;
            public bool StabilityRework;
            public int DamageScaling;
            public int EffectiveStabilityScaling;
            public int HealthScaling;
            public int ImpactDamageScaling;
            public bool SlowdownScaling;
            public ReviveCombatSetting ReviveCombatRestrictions;
            public bool ReviveItemNeeded;
            public bool ReviveNoManLeftBehind;
            public bool CozyBeds;
            public bool ShareBlacksmithRepairs;
            public bool ShareSideQuestArtifacts;
            public bool ShareSideQuestSkills;
            public bool ShareStoryArtifacts;
            public bool ShareStorySkills;
            public bool ShareWorldArtifacts;
        }
        static ConfigEntry<bool> HideRoomName;
        static ConfigEntry<int> PlayerLimit;
        static ConfigEntry<bool> ShowNameplates;
        static ConfigEntry<bool> ShowNameplatesGlobally;
        static ConfigEntry<DifficultyModeSetting> DifficultyMode;
        static ConfigEntry<bool> HardMode;
        static ConfigEntry<int> ManualDifficultyScaling;
        static ConfigEntry<int> RevivalHealthBurn;
        static ConfigEntry<int> RevivalStaminaBurn;
        static ConfigEntry<bool> StabilityRework;
        static ConfigEntry<int> DamageScaling;
        static ConfigEntry<int> EffectiveStabilityScaling;
        static ConfigEntry<int> HealthScaling;
        static ConfigEntry<int> ImpactDamageScaling;
        static ConfigEntry<bool> SlowdownScaling;
        static ConfigEntry<ReviveCombatSetting> ReviveCombatRestrictions;
        static ConfigEntry<bool> ReviveItemNeeded;
        static ConfigEntry<bool> ReviveNoManLeftBehind;
        static ConfigEntry<bool> CozyBeds;
        static ConfigEntry<bool> ShareBlacksmithRepairs;
        static ConfigEntry<bool> ShareSideQuestArtifacts;
        static ConfigEntry<bool> ShareSideQuestSkills;
        static ConfigEntry<bool> ShareStoryArtifacts;
        static ConfigEntry<bool> ShareStorySkills;
        static ConfigEntry<bool> ShareWorldArtifacts;
        internal static RaidModeConfig Instance;
        internal const int VIEW_ID = 981;
        public static SettingsData LiveSettings;
        static bool settingsChanged = false;
        static bool updateChars = false;
        static int _playerCount = 1;
        public static void Init (ConfigFile config)
        {
            //This is what sends the RPC calls to sync settings to clients.
            var obj = new GameObject("RaidModeConfigRPC");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<RaidModeConfig>();
            var view = obj.AddComponent<PhotonView>();
            view.viewID = VIEW_ID;
            #region Base Section
            string section0 = "";
            HideRoomName = config.Bind(section0, "Hide Room Name", false,
                "Obfuscates the room name when entering it and hides it in the pause menu." +
                "\nThis option is for people that wanna hide from the world, or more specifically, their chat.");
            HideRoomName.SettingChanged += SettingChanged;
            PlayerLimit = config.Bind(section0, "Party Limit", 5,
                new ConfigDescription("The maximum number of players that can join your party.",
                new AcceptableValueRange<int>(1, 10)));
            PlayerLimit.SettingChanged += SettingChanged;
            ShowNameplates = config.Bind(section0, "Show Nameplates", true,
                "Enable nameplates for other players.");
            ShowNameplates.SettingChanged += SettingChanged;
            ShowNameplatesGlobally = config.Bind(section0, "Show Nameplates Globally", true,
                "Show player nameplates no matter how far away they are and through walls.");
            ShowNameplatesGlobally.SettingChanged += SettingChanged;
            #endregion
            #region Difficulty Section
            string section1 = "Difficulty";
            DifficultyMode = config.Bind(section1, "Difficulty Mode", DifficultyModeSetting.Default,
                "Choose what kind of difficulty scaling Raid Mode will use to balance the game as more players join." +
                "\n\n- Raid Mode - \n A new scaling mode that is less agressive than vanilla's scaling. But limits abusive tactics by players and is arguably the most balanced mode." +
                "\n\n- Custom Mode -\n Use the customization options below to tune your own experience." +
                "\n\n- Vanilla Plus - \n Based on the vanilla difficulty scaling. But extends it to work beyond just 2 players. Is the toughest mode and is way more aggressive than Raid Mode's Default mode." +
                "\n\n- Just Vanilla -\n Leaves the game's difficulty scaling alone. This is not advised when in parties of 3 or more. As the vanilla scaling doesn't generally handle anything beyond 2 players." +
                "\n\n- No Scaling -\n Completely disables all scaling. Good for players that wanna play with friends but don't want the game to be any tougher than when playing alone.");
            DifficultyMode.SettingChanged += DifficultySettingChanged;
            HardMode = config.Bind(section1, "Hard Mode", false,
                "Doubles the difficulty scaling per player for the Raid Mode, Custom Mode, and Vanilla Plus difficulty modes.");
            HardMode.SettingChanged += DifficultySettingChanged;
            ManualDifficultyScaling = config.Bind(section1, "Manual Difficulty Scaling", 0,
                new ConfigDescription("Values above 0 will force the difficulty scaling to calculate the difficulty as if there was that many extra players in the party instead of the actual count.",
                new AcceptableValueRange<int>(0, 10)));
            ManualDifficultyScaling.SettingChanged += DifficultySettingChanged;
            RevivalHealthBurn = config.Bind(section1, "Revival Health Burn", 50,
                new ConfigDescription("Sets how much of a player's current max health is burned when they are revived by another player." +
                "\nIt is recommended to keep this value high to limit revive chaining abuse in tough fights.",
                new AcceptableValueRange<int>(0, 100)));
            RevivalHealthBurn.SettingChanged += SettingChanged;
            RevivalStaminaBurn = config.Bind(section1, "Revival Stamina Burn", 50,
                new ConfigDescription("Sets how much of a player's current stamina is burned when they are revived by another player." +
                "\nIt is recommended to keep this value high to limit revive chaining abuse in tough fights.",
                new AcceptableValueRange<int>(0, 100)));
            RevivalStaminaBurn.SettingChanged += SettingChanged;
            StabilityRework = config.Bind(section1, "Stability Rework", true,
                "Changes some aspects of the stability system to make it less abusable, especially in multiplayer." +
                "\nIt is highly recommended to be left enabled in co-op. But it does make the game tougher." +
                "\nThe main change is that enemies are now only staggered when their stability is brought below the new 50%, 33%, and 16% breakpoints. And they are only knocked down when brought to 0% stability.");
            StabilityRework.SettingChanged += SettingChanged;
            #endregion
            #region Custom Mode Section
            string section2 = "Difficulty Custom Mode";
            DamageScaling = config.Bind(section2, "Damage Scaling", 10,
                new ConfigDescription("The damage bonus given to enemies for each extra player in the game.",
                new AcceptableValueRange<int>(0, 100)));
            DamageScaling.SettingChanged += DifficultySettingChanged;
            EffectiveStabilityScaling = config.Bind(section2, "Effective Stability Scaling", 50,
                new ConfigDescription("The effective stability increase given to genemies for each extra player in the game.",
                new AcceptableValueRange<int>(0, 100)));
            EffectiveStabilityScaling.SettingChanged += DifficultySettingChanged;
            HealthScaling = config.Bind(section2, "Health Scaling", 50,
                new ConfigDescription("The health bonus given to enemies for each extra player in the game.",
                new AcceptableValueRange<int>(0, 100)));
            HealthScaling.SettingChanged += DifficultySettingChanged;
            ImpactDamageScaling = config.Bind(section2, "Impact Damage Scaling", 10,
                new ConfigDescription("The impact damage bonus given to enemies for each extra player in the game.",
                new AcceptableValueRange<int>(0, 100)));
            ImpactDamageScaling.SettingChanged += DifficultySettingChanged;
            SlowdownScaling = config.Bind(section2, "Slowdown Scaling", true,
                "Enables the scaling down of the animation slowdown effect on enemies when they are hit or hit others." +
                "\nThis stops a large group of players from effectively freezing enemies in place when hitting them all at once repeatedly.");
            SlowdownScaling.SettingChanged += SettingChanged;
            #endregion
            #region Revive Restrictions Section
            string section3 = "Revive Restrictions";
            ReviveCombatRestrictions = config.Bind(section3, "Combat Resitrictions", ReviveCombatSetting.Anytime,
                "Sets whether there should be restrictions on reviving teammates when the revivor or any player in the party is in combat.");
            ReviveCombatRestrictions.SettingChanged += SettingChanged;
            ReviveItemNeeded = config.Bind(section3, "Healing Item Needed", true,
                "Sets whether a healing item is needed to revive a downed teammate.");
            ReviveItemNeeded.SettingChanged += SettingChanged;
            ReviveNoManLeftBehind = config.Bind(section3, "No Man Left Behind", true,
                "Sets whether an area can be left when there are downed teammates." +
                "\nIn vanilla, a sole surviving player could enter a different area and all their downed teammates would be transported with them.");
            ReviveNoManLeftBehind.SettingChanged += SettingChanged;
            #endregion
            #region Sharing Options Section
            string section4 = "Sharing Options";
            CozyBeds = config.Bind(section4, "Cozy Beds", true, "Lets two players share the beds in player houses and inns.");
            CozyBeds.SettingChanged += SettingChanged;
            ShareBlacksmithRepairs = config.Bind(section4, "Blacksmith Repairs", true,
                "Blacksmiths will repair the equipment of all players.");
            ShareBlacksmithRepairs.SettingChanged += SettingChanged;
            ShareSideQuestArtifacts = config.Bind(section4, "Side Quest Artifacts", false,
                "Artifact items awarded by side-quests will be awarded to all players.");
            ShareSideQuestArtifacts.SettingChanged += SettingChanged;
            ShareSideQuestSkills = config.Bind(section4, "Side Quest Skills", true,
                "Skills and passives awarded by side-quests will be awarded to all players.");
            ShareSideQuestSkills.SettingChanged += SettingChanged;
            ShareStoryArtifacts = config.Bind(section4, "Story Artifacts", false,
                "Artifact items awarded by story quests will be awarded to all players.");
            ShareStoryArtifacts.SettingChanged += SettingChanged;
            ShareStorySkills = config.Bind(section4, "Story Skills", false,
                "Skills and passives awarded by story quests will be awarded to all players.");
            ShareStorySkills.SettingChanged += SettingChanged;
            ShareWorldArtifacts = config.Bind(section4, "World Artifacts", false,
                "Artifact items found in the world will be awarded to all players.");
            ShareWorldArtifacts.SettingChanged += SettingChanged;
            #endregion
            //Sets up the live settings actually used by the patches.
            Instance.UpdateLiveSettings(PopulateSettingsData());
        }
        //Sends an RPC if settings need to be updated and looks for changes in player count to trigger a scaling update.
        private void Update ()
        {
            if (PhotonNetwork.isMasterClient && _playerCount != Global.Lobby.PlayersInLobbyCount)
            {
                _playerCount = Global.Lobby.PlayersInLobbyCount;
                updateChars = true;
            }
            if (settingsChanged || updateChars)
            {
                if (!PhotonNetwork.inRoom)
                {
                    Instance.UpdateLiveSettings(PopulateSettingsData());
                }
                else
                {
                    Instance.photonView.RPC(nameof(UpdateLiveSettings), PhotonTargets.All, PopulateSettingsData());
                }
            }
        }
        //Only update and sync basic settings.
        private static void SettingChanged (object sender, System.EventArgs e)
        {
            if (PhotonNetwork.isMasterClient)
                settingsChanged = true;
        }
        //Update and sync settings and additionally refresh the difficutly scaling for all characters.
        private static void DifficultySettingChanged (object sender, System.EventArgs e)
        {
            if (PhotonNetwork.isMasterClient)
            {
                settingsChanged = true;
                updateChars = true;
            }
        }
        //Updates the live settings from a data array.
        [PunRPC]
        public void UpdateLiveSettings (object[] data)
        {
            LiveSettings = new SettingsData
            {
                HideRoomName = (bool)data[1],
                PlayerLimit = (int)data[2],
                ShowNameplates = (bool)data[3],
                ShowNameplatesGlobally = (bool)data[4],
                DifficultyMode = (DifficultyModeSetting)data[5],
                HardMode = (bool)data[6],
                ManualDifficultyScaling = (int)data[7],
                RevivalHealthBurn = (int)data[8],
                RevivalStaminaBurn = (int)data[9],
                StabilityRework = (bool)data[10],
                DamageScaling = (int)data[11],
                EffectiveStabilityScaling = (int)data[12],
                HealthScaling = (int)data[13],
                ImpactDamageScaling = (int)data[14],
                SlowdownScaling = (bool)data[15],
                ReviveCombatRestrictions = (ReviveCombatSetting)data[16],
                ReviveItemNeeded = (bool)data[17],
                ReviveNoManLeftBehind = (bool)data[18],
                CozyBeds = (bool)data[19],
                ShareBlacksmithRepairs = (bool)data[20],
                ShareSideQuestArtifacts = (bool)data[21],
                ShareSideQuestSkills = (bool)data[22],
                ShareStoryArtifacts = (bool)data[23],
                ShareStorySkills = (bool)data[24],
                ShareWorldArtifacts = (bool)data[25],
            };
            settingsChanged = false;
            if ((bool)data[0])
            {
                UpdateCharacters();
                updateChars = false;
            }
        }
        private static object[] PopulateSettingsData ()
        {
            return new object[]
                {
                    updateChars,
                    HideRoomName.Value,
                    PlayerLimit.Value,
                    ShowNameplates.Value,
                    ShowNameplatesGlobally.Value,
                    (int)DifficultyMode.Value,
                    HardMode.Value,
                    ManualDifficultyScaling.Value,
                    RevivalHealthBurn.Value,
                    RevivalStaminaBurn.Value,
                    StabilityRework.Value,
                    DamageScaling.Value,
                    EffectiveStabilityScaling.Value,
                    HealthScaling.Value,
                    ImpactDamageScaling.Value,
                    SlowdownScaling.Value,
                    (int)ReviveCombatRestrictions.Value,
                    ReviveItemNeeded.Value,
                    ReviveNoManLeftBehind.Value,
                    CozyBeds.Value,
                    ShareBlacksmithRepairs.Value,
                    ShareSideQuestArtifacts.Value,
                    ShareSideQuestSkills.Value,
                    ShareStoryArtifacts.Value,
                    ShareStorySkills.Value,
                    ShareWorldArtifacts.Value,
                };
        }
        //Steps through each character and triggers a stat scaling update.
        private static void UpdateCharacters ()
        {
            if (CharacterManager.Instance)
            {
                foreach (Character character in CharacterManager.Instance.Characters.Values)
                {
                    if (character.Stats)
                    {
                        character.Stats.m_delayedApplyCoopStats = true;
                    }
                }
            }
        }
    }
}
