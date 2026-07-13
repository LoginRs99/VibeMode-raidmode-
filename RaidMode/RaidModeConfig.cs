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
        public enum LanguageSetting
        {
            English,
            Hungarian
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
            public bool ShowTravelReadinessMessages;
            public bool CozyBeds;
            public bool ShareBlacksmithRepairs;
            public bool ShareSideQuestArtifacts;
            public bool ShareSideQuestSkills;
            public bool ShareStoryArtifacts;
            public bool ShareStorySkills;
            public bool ShareWorldArtifacts;
            public bool DebugLogging;
        }
        static ConfigEntry<bool> HideRoomName;
        static ConfigEntry<int> PlayerLimit;
        static ConfigEntry<bool> ShowNameplates;
        static ConfigEntry<bool> ShowNameplatesGlobally;
        static ConfigEntry<bool> DebugLogging;
        static ConfigEntry<LanguageSetting> Language;
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
        static ConfigEntry<bool> ShowTravelReadinessMessages;
        static ConfigEntry<bool> CozyBeds;
        static ConfigEntry<bool> ShareBlacksmithRepairs;
        static ConfigEntry<bool> ShareSideQuestArtifacts;
        static ConfigEntry<bool> ShareSideQuestSkills;
        static ConfigEntry<bool> ShareStoryArtifacts;
        static ConfigEntry<bool> ShareStorySkills;
        static ConfigEntry<bool> ShareWorldArtifacts;
        internal static RaidModeConfig Instance;
        internal const int VIEW_ID = 981;
        internal const int SETTINGS_PROTOCOL_VERSION = 1;
        internal const int SETTINGS_PAYLOAD_SIZE = 29;
        public static SettingsData LiveSettings;

        // FIX (BUG 5): These flags are now reset BEFORE the RPC is sent,
        // not after it is received back. This prevents Update() from
        // sending dozens of duplicate RPCs during the round-trip window.
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
            string section0 = "General";
            Language = config.Bind(section0, "Language", LanguageSetting.English,
                "Client-local UI language for VibeMode notifications. This setting is not synced by the host.\nKliens oldali VibeMode nyelv az ertesitesekhez. Ezt a beallitast a host nem szinkronizalja.");
            HideRoomName = config.Bind(section0, "Hide Room Name", false,
                Text("[Host-synced] Hides the online room name when entering a room and in the pause menu. Useful for streaming or recording. The host's value is synced to all clients.",
                     "[Host-szinkronizalt] Elrejti az online szoba nevet belepeskor es a szunet menuben. Hasznos streameleshez vagy felvetelhez. A host erteke szinkronizalodik a kliensekre."));
            HideRoomName.SettingChanged += SettingChanged;
            PlayerLimit = config.Bind(section0, "Party Limit", 5,
                new ConfigDescription(Text("[Host-only / host-synced] Maximum number of players allowed in the online room. Non-host changes are ignored while connected. All players should run the same VibeMode version.",
                                           "[Csak host / host-szinkronizalt] Maximalis jatekosszam az online szobaban. A kliens oldali valtoztatasok figyelmen kivul lesznek hagyva. Minden jatekosnak ugyanazt a VibeMode verziot kell futtatnia."),
                new AcceptableValueRange<int>(1, 10)));
            PlayerLimit.SettingChanged += SettingChanged;
            ShowNameplates = config.Bind(section0, "Show Nameplates", true,
                Text("[Host-synced UI] Shows player names on other players' health bars. The host's value is synced to all clients.",
                     "[Host-szinkronizalt UI] Megjeleniti a jatekosok nevet a tobbi jatekos eleterocsikjan. A host erteke szinkronizalodik a kliensekre."));
            ShowNameplates.SettingChanged += SettingChanged;
            ShowNameplatesGlobally = config.Bind(section0, "Show Nameplates Globally", true,
                Text("[Host-synced UI] Keeps player nameplates visible at long range and through line-of-sight checks after they have appeared once. The host's value is synced to all clients.",
                     "[Host-szinkronizalt UI] A nevtablakat lathatova teszi nagy tavolsagbol es a latomezon kivul is, miutan egyszer mar megjelentek. A host erteke szinkronizalodik a kliensekre."));
            ShowNameplatesGlobally.SettingChanged += SettingChanged;
            DebugLogging = config.Bind(section0, "Debug Logging", false,
                Text("[Host-synced diagnostics] Writes extra VibeMode network/debug messages to the BepInEx log. Enable only while testing or collecting logs. The host's value is synced to all clients.",
                     "[Host-szinkronizalt diagnosztika] Reszletes halozati es hibakeresesi uzeneteket ir a BepInEx naploba. Csak teszteleshez es hibakereshez javasolt. A host erteke szinkronizalodik a kliensekre."));
            DebugLogging.SettingChanged += SettingChanged;
            #endregion
            #region Difficulty Section
            string section1 = "Difficulty";
            DifficultyMode = config.Bind(section1, "Difficulty Mode", DifficultyModeSetting.Default,
                Text("[Host-synced] Controls enemy scaling for larger parties." +
                "\n\nDefault: VibeMode's balanced scaling for 3+ players." +
                "\nCustom: Uses the custom percentage options below." +
                "\nVanillaPlus: Extends vanilla-style scaling beyond 2 players; usually the hardest option." +
                "\nJustVanilla: Leaves Outward's original scaling alone; not recommended for 3+ players." +
                "\nNoScaling: Disables VibeMode difficulty scaling.",
                     "[Host-szinkronizalt] Szabalyozza az ellensegek skalaris erosebbedeset nagyobb csapatoknal." +
                "\n\nDefault: VibeMode kiegyensulyozott skalaris erosebbedese 3+ jatekoshoz." +
                "\nCustom: Az alabbi egyedi szazalekos beallitasokat hasznalja." +
                "\nVanillaPlus: Kiterjeszti a vanilla stilusú nehezseget 2 jatekos felett; altalaban ez a legnehezebb opcio." +
                "\nJustVanilla: Meghagyja az Outward eredeti nehezsegi skalarasat; nem javasolt 3+ jatekoshoz." +
                "\nNoScaling: Kikapcsolja a VibeMode nehezsegi skalarasat."));
            DifficultyMode.SettingChanged += DifficultySettingChanged;
            HardMode = config.Bind(section1, "Hard Mode", false,
                Text("[Host-synced] Doubles VibeMode, Custom, and VanillaPlus scaling bonuses. This makes enemies much tougher in larger parties.",
                     "[Host-szinkronizalt] Megduplazza a VibeMode, Custom es VanillaPlus skalaris bonuszait. Ez sokkal erosebbe teszi az ellensegeket nagyobb csapatok eseten."));
            HardMode.SettingChanged += DifficultySettingChanged;
            ManualDifficultyScaling = config.Bind(section1, "Manual Difficulty Scaling", 0,
                new ConfigDescription(Text("[Host-synced] Overrides automatic party-size scaling. 0 uses the real player count. Values above 0 scale as if that many extra players were present.",
                                           "[Host-szinkronizalt] Felulirja az automatikus csapatmeret alapu skalarast. A 0 a valodi jatekosszamot hasznalja. A 0 feletti ertekekkel ugy skalarodik, mintha ennyivel tobb extra jatekos lenne jelen."),
                new AcceptableValueRange<int>(0, 10)));
            ManualDifficultyScaling.SettingChanged += DifficultySettingChanged;
            RevivalHealthBurn = config.Bind(section1, "Revival Health Burn", 50,
                new ConfigDescription(Text("[Host-synced] Percent of max health burned when another player revives you. Higher values reduce revive chaining in combat.",
                                           "[Host-szinkronizalt] A maximalis eletero szazalekos csokkenese, amikor egy masik jatekos eleszti. A magasabb ertekek csokkentik az elesztesi lancokat harc kozben."),
                new AcceptableValueRange<int>(0, 100)));
            RevivalHealthBurn.SettingChanged += SettingChanged;
            RevivalStaminaBurn = config.Bind(section1, "Revival Stamina Burn", 50,
                new ConfigDescription(Text("[Host-synced] Percent of current stamina burned when another player revives you. Higher values reduce revive chaining in combat.",
                                           "[Host-szinkronizalt] A jelenlegi stamina szazalekos csokkenese, amikor egy masik jatekos eleszti. A magasabb ertekek csokkentik az elesztesi lancokat harc kozben."),
                new AcceptableValueRange<int>(0, 100)));
            RevivalStaminaBurn.SettingChanged += SettingChanged;
            StabilityRework = config.Bind(section1, "Stability Rework", true,
                Text("[Host-synced] Changes enemy stagger behavior to reduce stagger-locking in multiplayer. Enemies stagger at 50%, 33%, and 16% stability and only knock down at 0%. Recommended for co-op.",
                     "[Host-szinkronizalt] Megvaltoztatja az ellensegek megtorpanasi viselkedeset, hogy csokkentse a folyamatos megingatast tobbjatekos modban. Az ellensegek 50%, 33% es 16% stabilitasnal torpannak meg, es csak 0% stabilitasnal esnek el. Kooperativ jatekhoz javasolt."));
            StabilityRework.SettingChanged += SettingChanged;
            #endregion
            #region Custom Mode Section
            string section2 = "Difficulty Custom Mode";
            DamageScaling = config.Bind(section2, "Damage Scaling", 10,
                new ConfigDescription(Text("[Host-synced / Custom mode] Percent all-damage bonus enemies gain per extra player.",
                                           "[Host-szinkronizalt / Custom mod] Szazalekos sebzesi bonusz, amit az ellensegek kapnak minden extra jatekos utan."),
                new AcceptableValueRange<int>(0, 100)));
            DamageScaling.SettingChanged += DifficultySettingChanged;
            EffectiveStabilityScaling = config.Bind(section2, "Effective Stability Scaling", 50,
                new ConfigDescription(Text("[Host-synced / Custom mode] Percent effective stability bonus enemies gain per extra player.",
                                           "[Host-szinkronizalt / Custom mod] Szazalekos stabilitasi bonusz, amit az ellensegek kapnak minden extra jatekos utan."),
                new AcceptableValueRange<int>(0, 100)));
            EffectiveStabilityScaling.SettingChanged += DifficultySettingChanged;
            HealthScaling = config.Bind(section2, "Health Scaling", 50,
                new ConfigDescription(Text("[Host-synced / Custom mode] Percent max-health bonus enemies gain per extra player.",
                                           "[Host-szinkronizalt / Custom mod] Szazalekos maximalis eletero bonusz, amit az ellensegek kapnak minden extra jatekos utan."),
                new AcceptableValueRange<int>(0, 100)));
            HealthScaling.SettingChanged += DifficultySettingChanged;
            ImpactDamageScaling = config.Bind(section2, "Impact Damage Scaling", 10,
                new ConfigDescription(Text("[Host-synced / Custom mode] Percent impact-damage bonus enemies gain per extra player.",
                                           "[Host-szinkronizalt / Custom mod] Szazalekos stabilitasi sebzes bonusz, amit az ellensegek kapnak minden extra jatekos utan."),
                new AcceptableValueRange<int>(0, 100)));
            ImpactDamageScaling.SettingChanged += DifficultySettingChanged;
            SlowdownScaling = config.Bind(section2, "Slowdown Scaling", true,
                Text("[Host-synced / Custom mode] Reduces enemy animation slowdown from repeated hits so large parties cannot easily freeze enemies in place.",
                     "[Host-szinkronizalt / Custom mod] Csokkenti az ellensegek animacios lelassulasat ismetelt talalatok eseten, hogy a nagyobb csapatok ne tudjak konnyen lefagyasztani oket."));
            SlowdownScaling.SettingChanged += SettingChanged;
            #endregion
            #region Revive Restrictions Section
            string section3 = "Revive Restrictions";
            ReviveCombatRestrictions = config.Bind(section3, "Combat Resitrictions", ReviveCombatSetting.Anytime,
                Text("[Host-synced] Controls combat revive rules. Anytime: revives are always allowed. OnlyReviver: the reviver must be out of combat. Party: the whole party must be out of combat.",
                     "[Host-szinkronizalt] Szabalyozza a harci elesztesi szabalyokat. Anytime: az elesztes mindig engedelyezett. OnlyReviver: az elesztonek ki kell kerulnie a harcbol. Party: az egesz csapatnak ki kell kerulnie a harcbol."));
            ReviveCombatRestrictions.SettingChanged += SettingChanged;
            ReviveItemNeeded = config.Bind(section3, "Healing Item Needed", true,
                Text("[Host-synced] Requires the reviver to spend a Bandage, Life Potion, or Great Life Potion when reviving a teammate.",
                     "[Host-szinkronizalt] Megkoveteli az elesztotol egy Bandazs, Eletital vagy Nagy Eletital felhasznalasat csapattars elesztesekor."));
            ReviveItemNeeded.SettingChanged += SettingChanged;
            ReviveNoManLeftBehind = config.Bind(section3, "No Man Left Behind", true,
                Text("[Host-synced] Prevents area travel/rest while teammates are downed. This stops the vanilla behavior where a survivor can drag downed teammates through an area transition.",
                     "[Host-szinkronizalt] Megakadalyozza a teruletvaltast vagy pihenest, amig vannak eszmeletlen csapattarsak. Ez leallitja a vanilla jatek azon viselkedeset, ahol a tulelo at tudja vinni az eszmeletlen csapattarsakat masik teruletre."));
            ReviveNoManLeftBehind.SettingChanged += SettingChanged;
            ShowTravelReadinessMessages = config.Bind(section3, "Show Travel Readiness Messages", true,
                Text("[Host-synced UI] Shows a message naming downed teammates who are blocking travel or rest when No Man Left Behind is enabled.",
                     "[Host-szinkronizalt UI] Megjeleniti az eszmeletlen csapattarsak nevet, akik blokkoljak az utazast vagy pihenest, ha a No Man Left Behind engedelyezve van."));
            ShowTravelReadinessMessages.SettingChanged += SettingChanged;
            #endregion
            #region Sharing Options Section
            string section4 = "Sharing Options";
            CozyBeds = config.Bind(section4, "Cozy Beds", true,
                Text("[Host-synced] Lets two players share supported house/inn beds when resting.",
                     "[Host-szinkronizalt] Lehetove teszi, hogy ket jatekos megossza a tamogatott haz/fogado agyakat a pihenes soran."));
            CozyBeds.SettingChanged += SettingChanged;
            ShareBlacksmithRepairs = config.Bind(section4, "Blacksmith Repairs", true,
                Text("[Host-synced] Blacksmith repair services repair equipment for all player characters.",
                     "[Host-szinkronizalt] A kovacs javitasi szolgaltatasai minden jatekos felszereleset megjavitjak."));
            ShareBlacksmithRepairs.SettingChanged += SettingChanged;
            ShareSideQuestArtifacts = config.Bind(section4, "Side Quest Artifacts", false,
                Text("[Host-synced] Unique artifact items awarded by side quests are also awarded to other players.",
                     "[Host-szinkronizalt] A mellekkuldetesek altal adott egyedi artefaktum targyak a tobbi jatekosnak is megadatnak."));
            ShareSideQuestArtifacts.SettingChanged += SettingChanged;
            ShareSideQuestSkills = config.Bind(section4, "Side Quest Skills", true,
                Text("[Host-synced] Skills and passives awarded by side quests are also awarded to other players.",
                     "[Host-szinkronizalt] A mellekkuldetesek altal adott kepessegek es passzivok a tobbi jatekosnak is megadatnak."));
            ShareSideQuestSkills.SettingChanged += SettingChanged;
            ShareStoryArtifacts = config.Bind(section4, "Story Artifacts", false,
                Text("[Host-synced] Unique artifact items awarded by main story quests are also awarded to other players.",
                     "[Host-szinkronizalt] A fő tortenetkuldetesek altal adott egyedi artefaktum targyak a tobbi jatekosnak is megadatnak."));
            ShareStoryArtifacts.SettingChanged += SettingChanged;
            ShareStorySkills = config.Bind(section4, "Story Skills", false,
                Text("[Host-synced] Skills and passives awarded by main story quests are also awarded to other players.",
                     "[Host-szinkronizalt] A fő tortenetkuldetesek altal adott kepessegek es passzivok a tobbi jatekosnak is megadatnak."));
            ShareStorySkills.SettingChanged += SettingChanged;
            ShareWorldArtifacts = config.Bind(section4, "World Artifacts", false,
                Text("[Host-synced] Unique artifact items found in the world are also awarded to other players when VibeMode recognizes the reward.",
                     "[Host-szinkronizalt] A vilagban talalt egyedi artefaktum targyak a tobbi jatekosnak is megadatnak, ha a VibeMode felismeri a jutalmat."));
            ShareWorldArtifacts.SettingChanged += SettingChanged;
            #endregion
            //Sets up the live settings actually used by the patches.
            ValidateSettingsPayloadSize();
            Instance.UpdateLiveSettings(PopulateSettingsData());
        }

        // Sends an RPC if settings need to be updated and looks for
        // changes in player count to trigger a scaling update.
        private void Update ()
        {
            int playersInLobby = Global.Lobby != null ? Global.Lobby.PlayersInLobbyCount : _playerCount;
            if (PhotonNetwork.isMasterClient && _playerCount != playersInLobby)
            {
                _playerCount = playersInLobby;
                updateChars = true;
            }
            if (settingsChanged || updateChars)
            {
                DebugLog($"Sending settings update. settingsChanged={settingsChanged}, updateChars={updateChars}, players={playersInLobby}");

                // FIX (BUG 5): Reset flags BEFORE sending the RPC.
                // The original code reset them inside UpdateLiveSettings (the receiver),
                // which meant Update() kept firing the RPC every frame for ~60 frames
                // during the network round-trip, flooding Photon with duplicate packets.
                settingsChanged = false;
                updateChars = false;

                if (!PhotonNetwork.inRoom || !VibeModeNetwork.HasRemotePeers)
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
            {
                settingsChanged = true;
            }
            else
            {
                WarnNonHostConfigChange();
            }
        }
        //Update and sync settings and additionally refresh the difficulty scaling for all characters.
        private static void DifficultySettingChanged (object sender, System.EventArgs e)
        {
            if (PhotonNetwork.isMasterClient)
            {
                settingsChanged = true;
                updateChars = true;
            }
            else
            {
                WarnNonHostConfigChange();
            }
        }
        // Updates the live settings from a data array.
        [PunRPC]
        public void UpdateLiveSettings (object[] data)
        {
            if (data == null)
            {
                Debug.LogWarning("[VibeMode] Ignored settings sync because payload was null.");
                return;
            }
            if (data.Length < SETTINGS_PAYLOAD_SIZE)
            {
                Debug.LogWarning($"[VibeMode] Ignored settings sync because payload had {data.Length} values; expected {SETTINGS_PAYLOAD_SIZE}. This usually means mixed mod versions.");
                return;
            }
            for (int i = 0; i < SETTINGS_PAYLOAD_SIZE; i++)
            {
                if (data[i] == null)
                {
                    Debug.LogWarning($"[VibeMode] Ignored settings sync because payload value {i} was null.");
                    return;
                }
            }
            int protocolVersion = SafeInt(data, SETTINGS_PAYLOAD_SIZE - 1, -1);
            if (protocolVersion != SETTINGS_PROTOCOL_VERSION)
            {
                Debug.LogWarning($"[VibeMode] Settings sync protocol mismatch. host={protocolVersion}, local={SETTINGS_PROTOCOL_VERSION}. This usually means mixed mod versions.");
            }

            // FIX (BUG 4): Photon serializes object[] elements with their runtime type.
            // A C# enum cannot be directly unboxed from `object` — you must go
            // object -> int -> enum. Without the intermediate (int) cast, this method
            // threw an InvalidCastException on every non-host client, silently dropped
            // by PUN's RPC dispatcher, leaving all clients on default settings forever.
            bool shouldUpdateCharacters = SafeBool(data, 0, false);
            LiveSettings = new SettingsData
            {
                HideRoomName                = SafeBool(data, 1, LiveSettings.HideRoomName),
                PlayerLimit                 = SafeInt(data, 2, LiveSettings.PlayerLimit),
                ShowNameplates              = SafeBool(data, 3, LiveSettings.ShowNameplates),
                ShowNameplatesGlobally      = SafeBool(data, 4, LiveSettings.ShowNameplatesGlobally),
                DifficultyMode              = SafeEnum(data, 5, LiveSettings.DifficultyMode),
                HardMode                    = SafeBool(data, 6, LiveSettings.HardMode),
                ManualDifficultyScaling     = SafeInt(data, 7, LiveSettings.ManualDifficultyScaling),
                RevivalHealthBurn           = SafeInt(data, 8, LiveSettings.RevivalHealthBurn),
                RevivalStaminaBurn          = SafeInt(data, 9, LiveSettings.RevivalStaminaBurn),
                StabilityRework             = SafeBool(data, 10, LiveSettings.StabilityRework),
                DamageScaling               = SafeInt(data, 11, LiveSettings.DamageScaling),
                EffectiveStabilityScaling   = SafeInt(data, 12, LiveSettings.EffectiveStabilityScaling),
                HealthScaling               = SafeInt(data, 13, LiveSettings.HealthScaling),
                ImpactDamageScaling         = SafeInt(data, 14, LiveSettings.ImpactDamageScaling),
                SlowdownScaling             = SafeBool(data, 15, LiveSettings.SlowdownScaling),
                ReviveCombatRestrictions    = SafeEnum(data, 16, LiveSettings.ReviveCombatRestrictions),
                ReviveItemNeeded            = SafeBool(data, 17, LiveSettings.ReviveItemNeeded),
                ReviveNoManLeftBehind       = SafeBool(data, 18, LiveSettings.ReviveNoManLeftBehind),
                ShowTravelReadinessMessages = SafeBool(data, 19, LiveSettings.ShowTravelReadinessMessages),
                CozyBeds                    = SafeBool(data, 20, LiveSettings.CozyBeds),
                ShareBlacksmithRepairs      = SafeBool(data, 21, LiveSettings.ShareBlacksmithRepairs),
                ShareSideQuestArtifacts     = SafeBool(data, 22, LiveSettings.ShareSideQuestArtifacts),
                ShareSideQuestSkills        = SafeBool(data, 23, LiveSettings.ShareSideQuestSkills),
                ShareStoryArtifacts         = SafeBool(data, 24, LiveSettings.ShareStoryArtifacts),
                ShareStorySkills            = SafeBool(data, 25, LiveSettings.ShareStorySkills),
                ShareWorldArtifacts         = SafeBool(data, 26, LiveSettings.ShareWorldArtifacts),
                DebugLogging                = SafeBool(data, 27, LiveSettings.DebugLogging),
            };

            int playersInLobby = Global.Lobby != null ? Global.Lobby.PlayersInLobbyCount : 0;
            DebugLog($"Settings synced. players={playersInLobby}, limit={LiveSettings.PlayerLimit}, difficulty={LiveSettings.DifficultyMode}, updateChars={shouldUpdateCharacters}");
            // NOTE: settingsChanged and updateChars are now reset in Update() before
            // sending, so we no longer reset them here. The data[0] flag still drives
            // the character scaling refresh correctly.
            if (shouldUpdateCharacters)
            {
                UpdateCharacters();
            }
        }

        [PunRPC]
        public void ReceiveRewardShareRequest (string receiverUIDs, string rewardPayload)
        {
            if (!PhotonNetwork.isMasterClient)
                return;

            GiveReward_OnExecute.ShareRewardPayload(receiverUIDs, rewardPayload);
        }

        private static bool SafeBool (object[] data, int index, bool fallback)
        {
            return data[index] is bool value ? value : fallback;
        }

        private static int SafeInt (object[] data, int index, int fallback)
        {
            if (data[index] is int value)
                return value;
            if (data[index] is byte byteValue)
                return byteValue;
            return fallback;
        }

        private static TEnum SafeEnum<TEnum> (object[] data, int index, TEnum fallback) where TEnum : struct
        {
            if (!(data[index] is int value))
                return fallback;
            return System.Enum.IsDefined(typeof(TEnum), value) ? (TEnum)System.Enum.ToObject(typeof(TEnum), value) : fallback;
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
                    ShowTravelReadinessMessages.Value,
                    CozyBeds.Value,
                    ShareBlacksmithRepairs.Value,
                    ShareSideQuestArtifacts.Value,
                    ShareSideQuestSkills.Value,
                    ShareStoryArtifacts.Value,
                    ShareStorySkills.Value,
                    ShareWorldArtifacts.Value,
                    DebugLogging.Value,
                    SETTINGS_PROTOCOL_VERSION,
                };
        }
        private static void ValidateSettingsPayloadSize ()
        {
            int actualSize = PopulateSettingsData().Length;
            if (actualSize != SETTINGS_PAYLOAD_SIZE)
            {
                Debug.LogError($"[VibeMode] SETTINGS_PAYLOAD_SIZE is {SETTINGS_PAYLOAD_SIZE}, but PopulateSettingsData() returns {actualSize}. Settings sync will be broken until this is fixed.");
            }
        }

        private static void WarnNonHostConfigChange ()
        {
            if (PhotonNetwork.inRoom && !PhotonNetwork.isMasterClient)
            {
                Debug.LogWarning("[VibeMode] " + Text("Config change ignored on non-host client. Ask the host to change VibeMode settings; host settings are authoritative.",
                                                      "A kliens oldali beallitasmodositast a mod figyelmen kivul hagyta. A VibeMode beallitasokat a hostnak kell modositani, mert a host beallitasai ervenyesek."));
            }
        }

        internal static string Text (string english, string hungarian)
        {
            return Language != null && Language.Value == LanguageSetting.Hungarian ? hungarian : english;
        }

        private static string TranslateActionName (string actionName)
        {
            if (Language == null || Language.Value != LanguageSetting.Hungarian)
            {
                return actionName;
            }

            switch (actionName)
            {
                case "fast travel":
                    return "gyorsutazni";
                case "leave the area":
                    return "teruletet valtani";
                case "rest":
                    return "pihenni";
                default:
                    return actionName;
            }
        }

        internal static void DebugLog (string message)
        {
            if (LiveSettings.DebugLogging)
            {
                Debug.Log($"[VibeMode:Debug] {message}");
            }
        }

        internal static void DebugWarning (string message)
        {
            if (LiveSettings.DebugLogging)
            {
                Debug.LogWarning($"[VibeMode:Debug] {message}");
            }
        }

        internal static bool TryGetDownedPartyMembers (out string names)
        {
            names = "";
            if (Global.Lobby == null)
            {
                return false;
            }
            if (Global.Lobby.PlayersInLobbyCount <= 1)
            {
                return false;
            }

            foreach (PlayerSystem player in Global.Lobby.PlayersInLobby)
            {
                Character character = player.ControlledCharacter;
                if (character && character.IsDead)
                {
                    if (names.Length > 0)
                    {
                        names += ", ";
                    }
                    names += character.Name;
                }
            }
            return names.Length > 0;
        }

        internal static string GetNoManLeftBehindBlockMessage (string actionName)
        {
            string downedNames;
            if (TryGetDownedPartyMembers(out downedNames))
            {
                return Text($"Can not {actionName} while these teammates are downed: {downedNames}",
                            $"Nem lehet {TranslateActionName(actionName)}, amig ezek a csapattarsak eszmeletlenek: {downedNames}");
            }
            return Text($"Can not {actionName} while there are downed teammates!",
                        $"Nem lehet {TranslateActionName(actionName)}, amig van eszmeletlen csapattars!");
        }

        internal static void ShowNoManLeftBehindBlock (Character character, string actionName)
        {
            if (character && character.IsLocalPlayer && LiveSettings.ShowTravelReadinessMessages)
            {
                string message = GetNoManLeftBehindBlockMessage(actionName);
                character.CharacterUI.ShowInfoNotification(message);
                DebugLog(message);
            }
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
