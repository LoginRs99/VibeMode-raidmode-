using System.Collections.Generic;
using HarmonyLib;
namespace RaidMode
{
    //Implements all of the enemy stat scaling features.
    [HarmonyPatch(typeof(CoopStats), "ApplyToCharacter")]
    public class CoopStats_ApplyToCharacter
    {
        public static bool Prefix (Character _char)
        {
            CharacterStats stats = _char.Stats;
            stats.RemoveStatStack(TagSourceManager.Instance.GetTag("77"), "Coop_Stat", true);
            stats.RemoveStatStack(TagSourceManager.Instance.GetTag("95"), "Coop_Stat", true);
            stats.RemoveStatStack(TagSourceManager.Instance.GetTag("96"), "Coop_Stat", true);
            stats.RemoveStatStack(TagSourceManager.Instance.GetTag("84"), "Coop_Stat", false);
            if (RaidModeConfig.LiveSettings.DifficultyMode == RaidModeConfig.DifficultyModeSetting.NoScaling)
                return false;
            if (RaidModeConfig.LiveSettings.DifficultyMode == RaidModeConfig.DifficultyModeSetting.JustVanilla)
                if (Global.Lobby.PlayersInLobbyCount > 1)
                    return true;
                else
                    return false;
            int manualPlayerCount = RaidModeConfig.LiveSettings.ManualDifficultyScaling;
            int playerCount = manualPlayerCount > 0 ? manualPlayerCount + 1 : Global.Lobby.PlayersInLobbyCount;
            if (RaidModeConfig.LiveSettings.DifficultyMode == RaidModeConfig.DifficultyModeSetting.VanillaPlus)
            {
                VanillaPlus(_char, playerCount, stats.CoopStats.StatData);
                return false;
            }
            float healthMult = 0.5f;
            float impactMult = 0.1f;
            float damageMult = 0.1f;
            float stabilityMult = 0.5f;
            if (RaidModeConfig.LiveSettings.DifficultyMode == RaidModeConfig.DifficultyModeSetting.Custom)
            {
                healthMult = RaidModeConfig.LiveSettings.HealthScaling / 100f;
                impactMult = RaidModeConfig.LiveSettings.ImpactDamageScaling / 100f;
                damageMult = RaidModeConfig.LiveSettings.DamageScaling / 100f;
                stabilityMult = RaidModeConfig.LiveSettings.EffectiveStabilityScaling / 100f;
            }
            float mult = RaidModeConfig.LiveSettings.HardMode ? 2 : 1;
            float healthBonus = healthMult * (playerCount - 1f) * mult;
            StatStack healthStack = new StatStack("Coop_Stat", -1f, healthBonus);
            stats.AddStatStack(TagSourceManager.Instance.GetTag("77"), healthStack, true);
            float impactBonus = impactMult * (playerCount - 1f) * mult;
            StatStack impactStack = new StatStack("Coop_Stat", -1f, impactBonus);
            stats.AddStatStack(TagSourceManager.Instance.GetTag("95"), impactStack, true);
            float allDamagesBonus = damageMult * (playerCount - 1f) * mult;
            StatStack allDamagesStack = new StatStack("Coop_Stat", -1f, allDamagesBonus);
            stats.AddStatStack(TagSourceManager.Instance.GetTag("96"), allDamagesStack, true);
            float baseImpactRes = stats.ImpactResistanceStat.BaseValue + _char.Inventory.Equipment.GetEquipmentImpactResistance();
            float impactResBonus = 0;
            if (baseImpactRes < 100f)
            {
                float baseResMod = 1f - baseImpactRes / 100f;
                float effectiveAmt = 1f / baseResMod;
                float newEffectiveAmt = effectiveAmt / (1f / playerCount);
                float delta = newEffectiveAmt - effectiveAmt;
                float modifiedAmt = delta * stabilityMult * mult + effectiveAmt;
                float newResMod = 1f / modifiedAmt;
                impactResBonus = (1f - newResMod) * 100f - baseImpactRes;
            }
            else if (baseImpactRes - 25f < 100f)
            {
                float baseResMod = 1f - (baseImpactRes - 25f) / 100f;
                float effectiveAmt = 1f / baseResMod;
                float newEffectiveAmt = effectiveAmt / (1f / playerCount);
                float delta = newEffectiveAmt - effectiveAmt;
                float modifiedAmt = delta * stabilityMult * mult + effectiveAmt;
                float newResMod = 1f / modifiedAmt;
                impactResBonus = (1f - newResMod) * 100f - baseImpactRes + 0.25f;
            }
            StatStack impactResStack = new StatStack("Coop_Stat", -1f, impactResBonus);
            stats.AddStatStack(TagSourceManager.Instance.GetTag("84"), impactResStack, false);
            return false;
        }
        //Implements the Vanilla Plus mode's stat scaling.
        public static bool VanillaPlus (Character _char, float _playerCount, CoopStatData[] statData)
        {
            CharacterStats stats = _char.Stats;
            List<string> vanillaBonuses = new List<string>();
            float baseImpactResBonus = 0;
            float mult = RaidModeConfig.LiveSettings.HardMode ? 2 : 1;
            for (int i = 0; i < statData.Length; i++)
            {
                if (statData[i].Stat.Tag.TagName == "ImpactResistance" && _playerCount > 2)
                {
                    baseImpactResBonus = statData[i].Value[0];
                }
                else
                {
                    float bonus = statData[i].Value[0] * (_playerCount - 1f) * mult;
                    StatStack stack = new StatStack("Coop_Stat", -1f, bonus);
                    stats.AddStatStack(statData[i].Stat.Tag, stack, statData[i].Modifier);
                    vanillaBonuses.Add(statData[i].Stat.Tag.TagName);
                }
            }
            if (_playerCount > 2)
            {
                if (!vanillaBonuses.Contains("MaxHealth"))
                {
                    float healthBonus = 0.5f * (_playerCount - 2f) * mult;
                    StatStack healthStack = new StatStack("Coop_Stat", -1f, healthBonus);
                    stats.AddStatStack(TagSourceManager.Instance.GetTag("77"), healthStack, true);
                }
                if (!vanillaBonuses.Contains("Impact"))
                {
                    float impactBonus = 0.75f * (_playerCount - 2f) * mult;
                    StatStack impactStack = new StatStack("Coop_Stat", -1f, impactBonus);
                    stats.AddStatStack(TagSourceManager.Instance.GetTag("95"), impactStack, true);
                }
                if (!vanillaBonuses.Contains("AllDamages"))
                {
                    float allDamagesBonus = 0.15f * (_playerCount - 2f) * mult;
                    StatStack allDamagesStack = new StatStack("Coop_Stat", -1f, allDamagesBonus);
                    stats.AddStatStack(TagSourceManager.Instance.GetTag("96"), allDamagesStack, true);
                }
                float baseImpactRes = stats.ImpactResistanceStat.BaseValue + _char.Inventory.Equipment.GetEquipmentImpactResistance();
                float newImpactResBonus;
                if (baseImpactRes < 100f)
                {
                    float baseResMod = 1f - baseImpactRes / 100f;
                    float baseEffectiveAmt = 1f / baseResMod;
                    float bonusResMod = 1f - (baseImpactRes + baseImpactResBonus) / 100f;
                    float bonusEffectiveAmt = 1f / bonusResMod;
                    float delta = bonusEffectiveAmt - baseEffectiveAmt;
                    float modifiedAmt = delta * (_playerCount - 1f) * mult + baseEffectiveAmt;
                    float newResMod = 1f / modifiedAmt;
                    newImpactResBonus = (1f - newResMod) * 100f - baseImpactRes;
                }
                else if (baseImpactRes - 25f < 100f)
                {
                    float baseResMod = 1f - (baseImpactRes - 25f) / 100f;
                    float baseEffectiveAmt = 1f / baseResMod;
                    float bonusResMod = 1f - (baseImpactRes - 25f + baseImpactResBonus) / 100f;
                    float bonusEffectiveAmt = 1f / bonusResMod;
                    float delta = bonusEffectiveAmt - baseEffectiveAmt;
                    float modifiedAmt = delta * (_playerCount - 1f) * mult + baseEffectiveAmt;
                    float newResMod = 1f / modifiedAmt;
                    newImpactResBonus = (1f - newResMod) * 100f - baseImpactRes + 25f;
                }
                else
                {
                    newImpactResBonus = baseImpactResBonus;
                }
                StatStack impactResStack = new StatStack("Coop_Stat", -1f, newImpactResBonus);
                stats.AddStatStack(TagSourceManager.Instance.GetTag("84"), impactResStack, false);
            }
            return false;
        }
    }
}
