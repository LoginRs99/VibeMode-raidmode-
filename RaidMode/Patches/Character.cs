using System;
using HarmonyLib;
using UnityEngine;
namespace RaidMode
{
    //Implements the animation slowdown scaling feature for enemies.
    //Which keeps enemies from being animation locked when continuously attacked by a lot of players.
    [HarmonyPatch(typeof(Character), "SlowDown")]
    public class Character_SlowDown
    {
        public static bool Prefix (Character __instance, ref float _slowVal, ref float _timeTo, ref float _timeStay, ref float _timeFrom)
        {
            if (RaidModeConfig.LiveSettings.DifficultyMode == RaidModeConfig.DifficultyModeSetting.NoScaling || RaidModeConfig.LiveSettings.DifficultyMode == RaidModeConfig.DifficultyModeSetting.JustVanilla)
                return true;
            if (RaidModeConfig.LiveSettings.DifficultyMode == RaidModeConfig.DifficultyModeSetting.Custom && RaidModeConfig.LiveSettings.SlowdownScaling)
                return true;
            int manualPlayerCount = RaidModeConfig.LiveSettings.ManualDifficultyScaling;
            int playerCount = manualPlayerCount > 0 ? manualPlayerCount : Global.Lobby.PlayersInLobbyCount;
            if (playerCount == 0)
                return true;
            if (__instance.Faction != Character.Factions.Player)
            {
                float plus = RaidModeConfig.LiveSettings.DifficultyMode == RaidModeConfig.DifficultyModeSetting.VanillaPlus ? 1f : 0f;
                float num = (1f + plus) / playerCount / (RaidModeConfig.LiveSettings.HardMode ? 2f : 1f);
                _slowVal *= num;
                _timeTo *= num;
                _timeStay *= num;
                _timeFrom *= num;
            }
            return true;
        }
    }
    //Implements the stat burn penalty options for revived players.
    //And makes this function master-only.
    [HarmonyPatch(typeof(Character), "Resurrect", new Type[] { typeof(PlayerSaveData), typeof(bool) })]
    public class Character_Resurrect
    {
        public static bool Prefix (Character __instance, PlayerSaveData _resurrectState, bool _playAnim)
        {
            if (_resurrectState != null && _playAnim)
            {
                _resurrectState.BurntHealth -= __instance.ActiveMaxHealth * 0.5f;
                float hpBurn = __instance.ActiveMaxHealth * (RaidModeConfig.LiveSettings.RevivalHealthBurn / 100f);
                _resurrectState.BurntHealth += hpBurn;
                float stamBurn = _resurrectState.Stamina * (RaidModeConfig.LiveSettings.RevivalStaminaBurn / 100f);
                _resurrectState.BurntStamina += stamBurn;
            }
            return true;
        }
    }
    //Implements the Confusion cleanse when a character is knock down as part of the Stability Rework.
    [HarmonyPatch(typeof(Character), "Knock")]
    public class Character_Knock
    {
        public static void Postfix (Character __instance, bool _down)
        {
            if (RaidModeConfig.LiveSettings.StabilityRework && _down && !__instance.Dodging)
            {
                __instance.StatusEffectMngr.CleanseStatusEffect("Confusion");
            }
        }
    }
    //Implements the stability breakpoint system for the Stability Rework.
    [HarmonyPatch(typeof(Character), "StabilityHit")]
    public class Character_StabilityHit
    {
        public static bool Prefix (Character __instance, float _knockValue, float _angle, bool _block, Character _dealerChar)
        {
            if (!RaidModeConfig.LiveSettings.StabilityRework)
                return true;
            //Record previous stability to know if passed breakpoints.
            float prevStability = __instance.m_stability;
            float num = _knockValue;
            if (__instance.IsPetrified)
            {
                num = 0f;
            }
            if (num < 0f)
            {
                num = 0f;
            }
            if (!__instance.m_impactImmune && num > 0f && !__instance.m_pendingDeath)
            {
                if (__instance.Stats.CurrentStamina < 1f)
                {
                    float num2 = __instance.m_shieldStability + __instance.m_stability - 49f;
                    if (num < num2)
                    {
                        num = num2;
                    }
                }
                __instance.m_timeOfLastStabilityHit = Time.time;
                if (__instance.CharacterCamera != null && num > 0f)
                {
                    __instance.CharacterCamera.Hit(num * 6f);
                }
                if (_block && __instance.m_shieldStability > 0f)
                {
                    if (num > __instance.m_shieldStability)
                    {
                        __instance.m_stability -= num - __instance.m_shieldStability;
                    }
                    __instance.m_shieldStability = Mathf.Clamp(__instance.m_shieldStability - num, 0f, 50f);
                }
                else
                {
                    __instance.m_stability = Mathf.Clamp(__instance.m_stability - num, 0f, 100f);
                }
                //No more knockback count.
                if (__instance.m_stability <= 0f)
                {
                    if ((!__instance.IsAI && __instance.photonView.isMine) || (__instance.IsAI && (_dealerChar == null || _dealerChar.photonView.isMine)))
                    {
                        __instance.photonView.RPC("SendKnock", PhotonTargets.All, true, __instance.m_stability);
                    }
                    else
                    {
                        __instance.Knock(true);
                    }
                    __instance.m_stability = 0f;
                    if (__instance.IsPhotonPlayerLocal)
                    {
                        __instance.BlockInput(false);
                    }
                    __instance.Invoke("DelayedCheckFootStep", 0.1f);
                }
                else if (__instance.m_stability <= 50f) //New breakpoint system
                {
                    if (prevStability > 50f && __instance.m_knockbackCount == 0)
                    {
                        if ((!__instance.IsAI && __instance.photonView.isMine) || (__instance.IsAI && (_dealerChar == null || _dealerChar.photonView.isMine)))
                        {
                            __instance.photonView.RPC("SendKnock", PhotonTargets.All, false, __instance.m_stability);
                        }
                        else
                        {
                            __instance.Knock(false);
                        }
                    }
                    else if (__instance.m_stability <= 33f && prevStability > 33f && __instance.m_knockbackCount <= 1)
                    {
                        if ((!__instance.IsAI && __instance.photonView.isMine) || (__instance.IsAI && (_dealerChar == null || _dealerChar.photonView.isMine)))
                        {
                            __instance.photonView.RPC("SendKnock", PhotonTargets.All, false, __instance.m_stability);
                        }
                        else
                        {
                            __instance.Knock(false);
                        }
                    }
                    else if (__instance.m_stability <= 16f && prevStability > 16f && __instance.m_knockbackCount <= 2)
                    {
                        if ((!__instance.IsAI && __instance.photonView.isMine) || (__instance.IsAI && (_dealerChar == null || _dealerChar.photonView.isMine)))
                        {
                            __instance.photonView.RPC("SendKnock", PhotonTargets.All, false, __instance.m_stability);
                        }
                        else
                        {
                            __instance.Knock(false);
                        }
                    }
                    if (__instance.IsPhotonPlayerLocal && _block)
                    {
                        __instance.BlockInput(false);
                    }
                }
                else if (!_block)
                {
                    if (__instance.m_knockHurtAllowed)
                    {
                        __instance.m_hurtType = Character.HurtType.Hurt;
                        if (__instance.m_currentlyChargingAttack)
                        {
                            __instance.CancelCharging();
                        }
                        __instance.m_animator.SetTrigger("Knockhurt");
                        if (__instance.knockhurt != null)
                            __instance.StopCoroutine(__instance.knockhurt);
                        __instance.knockhurt = __instance.StartCoroutine(__instance.KnockhurtRoutine(num));
                    }
                }
                else
                {
                    __instance.m_hurtType = Character.HurtType.NONE;
                    if (__instance.InLocomotion)
                    {
                        __instance.m_animator.SetTrigger("BlockHit");
                    }
                }
                __instance.m_animator.SetInteger("KnockAngle", (int)_angle);
                __instance.StabilityHitCall?.Invoke();
            }
            return false;
        }
    }
}