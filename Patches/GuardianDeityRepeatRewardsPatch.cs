extern alias GSD2;

using System;
using HarmonyLib;

namespace PKGuardianBlessing.Patches;

/// <summary>
/// Enables repeatable Guardian Deity statue rewards and preserves 3D statue rendering in Suikoden 2.
/// In retail, Jude only bestows a blessing reward for the very first statue built.
/// When the player subsequently alters or rebuilds the statue at Jude's workshop (h_shugo),
/// this patch clears the reward claimed lock (Story Flag 148 bit 32), resets statue evaluation
/// flags (Flag 123), and arms the inspection trigger (Story Flag 148 bit 16), while keeping
/// parts_no[3] = 1 in memory so the statue always renders its 3D model instead of the plain rock.
/// </summary>
public static class GuardianDeityRepeatRewardsPatch
{
    private static int _lastRewardedStatueCombo = -1;
    private static byte _statueBeforeCustomizer = 0;
    private static bool _customizerActive = false;

    public static void SetLastRewardedStatue(int comboId)
    {
        _lastRewardedStatueCombo = comboId;
    }

    /// <summary>
    /// Formatted diagnostics logger for tracking Guardian Deity state transitions.
    /// </summary>
    public static void LogState(string context)
    {
        if (!Plugin.Config.EnableDebugLogging.Value) return;

        try
        {
            var gw = GSD2::OldSrcBase.game_work;
            if (gw?.game_data == null)
            {
                Plugin.Log.LogInfo($"[GuardianDeity] [{context}] game_work or game_data is null");
                return;
            }

            var parts = gw.game_data.parts_no;
            string partsStr = parts != null
                ? $"[{string.Join(", ", parts)}]"
                : "null";

            bool flag16 = gw.eventFlgCHK(148, 0x10);
            bool flag32 = gw.eventFlgCHK(148, 0x20);

            Plugin.Log.LogInfo($"[GuardianDeity] [{context}] parts_no={partsStr} | Flag148[16]={flag16} Flag148[32]={flag32} | LastRewarded=0x{_lastRewardedStatueCombo:X2}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[GuardianDeity] [{context}] LogState error: {ex.Message}");
        }
    }

    public static void ArmStatueReward(GSD2::GAME_WORK gameWork, string reason)
    {
        if (gameWork?.game_data?.parts_no == null || gameWork.game_data.parts_no.Length <= 3)
            return;

        byte currentCombo = gameWork.game_data.parts_no[2];

        // 1. Keep statue assembled in memory! (1 = assembled & active)
        if (Plugin.Config.AlwaysKeepStatueAssembled.Value)
        {
            gameWork.game_data.parts_no[3] = 1;
        }

        if (Plugin.Config.EnableRepeatStatueRewards.Value)
        {
            // 2. Clear Story Flag 148 bit 32 (0x20) - Reward Received Lock
            gameWork.eventFlgOFF(148, 0x20);

            // 3. Set Story Flag 148 bit 16 (0x10) - Inspection Ready Trigger
            gameWork.eventFlgON(148, 0x10);

            // 4. Clear Flag 123 evaluation bits so new party reactions & blessing dialogue evaluate cleanly
            gameWork.eventFlgOFF(123, 8);
            gameWork.eventFlgOFF(123, 16);
            gameWork.eventFlgOFF(123, 32);
            gameWork.eventFlgOFF(123, 64);

            _lastRewardedStatueCombo = -1;
        }

        LogState($"ArmStatueReward (Combo: 0x{currentCombo:X2}, Reason: {reason})");
    }

    /// <summary>
    /// Hook Jude's statue customizer initialization (h_shugo.Entry).
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.h_shugo), "Entry")]
    [HarmonyPrefix]
    public static void H_Shugo_Entry_Prefix()
    {
        try
        {
            var gw = GSD2::OldSrcBase.game_work;
            if (gw?.game_data?.parts_no != null && gw.game_data.parts_no.Length > 2)
            {
                _statueBeforeCustomizer = gw.game_data.parts_no[2];
                _customizerActive = true;
            }

            LogState("h_shugo.Entry Prefix - Customizer opened");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[GuardianDeity] Error during h_shugo.Entry prefix: {ex}");
        }
    }

    /// <summary>
    /// Hook Jude's statue customizer main update loop (h_shugo.ShugoMain).
    /// Returns 1 when the player closes/exits the customization workshop.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.h_shugo), "ShugoMain")]
    [HarmonyPostfix]
    public static void H_Shugo_ShugoMain_Postfix(GSD2::EventOverlayClass.h_shugo __instance, int __result)
    {
        try
        {
            // ShugoMain returns 1 when the workshop overlay terminates
            if (__result != 1 || !_customizerActive) return;
            _customizerActive = false;

            var gw = GSD2::OldSrcBase.game_work;
            if (gw?.game_data?.parts_no == null || gw.game_data.parts_no.Length <= 3) return;

            byte currentCombo = gw.game_data.parts_no[2];

            // Check if build was confirmed (step 0x5b) or if combination changed
            bool confirmed = false;
            byte step = 0;
            if (__instance?.shugo_work != null)
            {
                step = (byte)__instance.shugo_work.step;
                confirmed = (step == 0x5b);
            }

            LogState($"h_shugo.ShugoMain Postfix - Workshop exit (step=0x{step:X2}, confirmed={confirmed}, before=0x{_statueBeforeCustomizer:X2}, now=0x{currentCombo:X2})");

            if (confirmed || currentCombo != _statueBeforeCustomizer)
            {
                ArmStatueReward(gw, confirmed ? "Workshop Build Confirmed" : "Statue Combination Changed");
            }
            else if (Plugin.Config.AlwaysKeepStatueAssembled.Value)
            {
                // Ensure parts_no[3] stays 1 (assembled) in memory
                gw.game_data.parts_no[3] = 1;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[GuardianDeity] Error during h_shugo.ShugoMain postfix: {ex}");
        }
    }

    /// <summary>
    /// Hook the Guardian Deity Shrine evaluation initialization (syugoSet.Entry).
    /// Called when visiting the roof shrine (vk33).
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.syugoSet), "Entry")]
    [HarmonyPrefix]
    public static void SyugoSet_Entry_Prefix()
    {
        try
        {
            var gw = GSD2::OldSrcBase.game_work;
            if (gw?.game_data?.parts_no == null || gw.game_data.parts_no.Length <= 3) return;

            byte currentCombo = gw.game_data.parts_no[2];

            // Always ensure the statue is assembled (parts_no[3] = 1) in memory so it renders the 3D model
            if (Plugin.Config.AlwaysKeepStatueAssembled.Value)
            {
                gw.game_data.parts_no[3] = 1;
            }

            if (Plugin.Config.EnableRepeatStatueRewards.Value)
            {
                // If the player altered the statue combination or re-armed the reward
                if (_lastRewardedStatueCombo >= 0 && currentCombo != _lastRewardedStatueCombo)
                {
                    gw.eventFlgOFF(148, 0x20); // Clear Claimed Lock
                    gw.eventFlgON(148, 0x10);  // Set Ready Flag
                }
                else if (_lastRewardedStatueCombo < 0 && !gw.eventFlgCHK(148, 0x20))
                {
                    // Unrewarded statue from save file
                    gw.eventFlgON(148, 0x10);
                }
                else if (_lastRewardedStatueCombo < 0)
                {
                    // Already claimed in a prior session
                    _lastRewardedStatueCombo = currentCombo;
                }
            }

            LogState("syugoSet.Entry Prefix - Roof Shrine loaded");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[GuardianDeity] Error during syugoSet.Entry prefix: {ex}");
        }
    }

    [HarmonyPatch(typeof(GSD2::EventOverlayClass.syugoSet), "Entry")]
    [HarmonyPostfix]
    public static void SyugoSet_Entry_Postfix()
    {
        LogState("syugoSet.Entry Postfix - Roof Shrine setup completed");
    }

    /// <summary>
    /// Hook the item reward delivery (syugoitm.s_OverlayAddItem2Main).
    /// Marks the current statue combination as rewarded once Jude grants the blessing.
    /// Note: We do NOT touch Flag 148 here because the native event script (Ev 16)
    /// continues running after syugoitm to fade in and naturally sets Flag 148 bit 32 at its end.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.syugoitm), "s_OverlayAddItem2Main")]
    [HarmonyPostfix]
    public static void Syugoitm_Reward_Postfix(int __result)
    {
        if (!Plugin.Config.EnableRepeatStatueRewards.Value) return;

        try
        {
            // Returns 1 when item delivery sequence completes
            if (__result == 1)
            {
                var gw = GSD2::OldSrcBase.game_work;
                if (gw?.game_data?.parts_no != null && gw.game_data.parts_no.Length > 2)
                {
                    byte currentCombo = gw.game_data.parts_no[2];
                    SetLastRewardedStatue(currentCombo);

                    // Keep statue assembled in memory
                    if (Plugin.Config.AlwaysKeepStatueAssembled.Value && gw.game_data.parts_no.Length > 3)
                    {
                        gw.game_data.parts_no[3] = 1;
                    }

                    LogState($"syugoitm Postfix - Reward delivered for combo 0x{currentCombo:X2}");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[GuardianDeity] Error during syugoitm reward postfix: {ex}");
        }
    }
}
