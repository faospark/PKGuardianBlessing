extern alias GSD2;

using System;
using HarmonyLib;

namespace PKGuardianBlessing.Patches;

/// <summary>
/// Enables repeatable Guardian Deity statue rewards and preserves 3D statue rendering in Suikoden 2.
/// 
/// Native Engine Flag & State Semantics:
/// - parts_no[0..1]: Blueprint bitmasks (Plans 1-8, 9-16).
/// - parts_no[2]: Packed statue combination byte (0 = Pure Dragon).
/// - parts_no[3]: Assembled statue flag (0 = Untouched pedestal rock; 1 = 3D statue model rendered).
/// - Story Flag 148 bit 16 (0x10): Inspection Ready Trigger (triggers Jude's roof unveiling warp & ev[16]).
/// - Story Flag 148 bit 32 (0x20): Blessing Claimed Lock (set by ev[16] when item is received; blocks repeat blessings).
/// - Flag 123 bits (8, 16, 32, 64): Party reaction evaluation flags.
/// </summary>
public static class GuardianDeityRepeatRewardsPatch
{
    private static int _lastRewardedStatueCombo = -1;
    private static byte _statueBeforeCustomizer = 0;
    private static byte _statueAssembledBeforeCustomizer = 0;
    private static byte _plansBeforeCustomizer0 = 0;
    private static byte _plansBeforeCustomizer1 = 0;
    private static bool _customizerActive = false;
    private static byte _lastObservedStep = 0;
    private static byte _lastActiveStep = 0;
    private static bool _statueConfirmedInWorkshop = false;

    public static void SetLastRewardedStatue(int comboId)
    {
        _lastRewardedStatueCombo = comboId;
    }

    public static bool HasAnyBlueprints(GSD2::GAME_WORK gameWork)
    {
        if (gameWork?.game_data?.parts_no == null || gameWork.game_data.parts_no.Length < 2)
            return false;
        return (gameWork.game_data.parts_no[0] != 0 || gameWork.game_data.parts_no[1] != 0);
    }

    public static void LogState(string context)
    {
        if (!Plugin.Config.EnableDebugLogging.Value) return;

        try
        {
            var gw = GSD2::OldSrcBase.game_work;
            if (gw?.game_data == null) return;

            var parts = gw.game_data.parts_no;
            string partsStr = parts != null ? $"[{string.Join(", ", parts)}]" : "null";
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

        // 1. Keep statue assembled in memory (1 = assembled & active)
        if (Plugin.Config.AlwaysKeepStatueAssembled.Value)
        {
            gameWork.game_data.parts_no[3] = 1;
        }

        if (Plugin.Config.EnableRepeatStatueRewards.Value)
        {
            // 2. Clear Story Flag 148 bit 32 (0x20) - Blessing Claimed Lock
            gameWork.eventFlgOFF(148, 0x20);

            // 3. Set Story Flag 148 bit 16 (0x10) - Inspection Ready Trigger
            gameWork.eventFlgON(148, 0x10);

            // 4. Clear Flag 123 evaluation bits
            gameWork.eventFlgOFF(123, 8);
            gameWork.eventFlgOFF(123, 16);
            gameWork.eventFlgOFF(123, 32);
            gameWork.eventFlgOFF(123, 64);

            _lastRewardedStatueCombo = -1;
        }

        byte currentCombo = gameWork.game_data.parts_no[2];
        LogState($"ArmStatueReward (Combo: 0x{currentCombo:X2}, Reason: {reason})");
    }

    /// <summary>
    /// Hook map initialization.
    /// Cleanses ghost statue state when no blueprints exist (parts_no[3] = 0).
    /// Re-arms repeat rewards and preserves 3D model when a statue exists.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::MACHICON), "OnMapInit")]
    [HarmonyPostfix]
    public static void Machicon_OnMapInit_Postfix()
    {
        try
        {
            var gw = GSD2::OldSrcBase.game_work;
            if (gw?.game_data?.parts_no == null || gw.game_data.parts_no.Length <= 3) return;

            if (!HasAnyBlueprints(gw))
            {
                // No blueprints exist in inventory = impossible to have built a statue in vanilla.
                // Reset to default untouched rock.
                gw.game_data.parts_no[3] = 0;
                gw.game_data.parts_no[2] = 0;
                gw.eventFlgOFF(148, 0x10);
                gw.eventFlgOFF(148, 0x20);
            }
            else if (gw.game_data.parts_no[3] == 1)
            {
                if (Plugin.Config.AlwaysKeepStatueAssembled.Value)
                {
                    gw.game_data.parts_no[3] = 1;
                }

                if (Plugin.Config.EnableRepeatStatueRewards.Value)
                {
                    // Clear the Blessing Claimed Lock so repeat rewards are always available!
                    gw.eventFlgOFF(148, 0x20);
                    gw.eventFlgON(148, 0x10);
                    gw.eventFlgOFF(123, 8);
                    gw.eventFlgOFF(123, 16);
                    gw.eventFlgOFF(123, 32);
                    gw.eventFlgOFF(123, 64);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[GuardianDeity] Error during OnMapInit: {ex}");
        }
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
            if (gw?.game_data?.parts_no != null && gw.game_data.parts_no.Length > 3)
            {
                if (!HasAnyBlueprints(gw))
                {
                    gw.game_data.parts_no[3] = 0;
                    gw.game_data.parts_no[2] = 0;
                    gw.eventFlgOFF(148, 0x10);
                    gw.eventFlgOFF(148, 0x20);
                }

                _statueBeforeCustomizer = gw.game_data.parts_no[2];
                _statueAssembledBeforeCustomizer = gw.game_data.parts_no[3];
                _plansBeforeCustomizer0 = gw.game_data.parts_no[0];
                _plansBeforeCustomizer1 = gw.game_data.parts_no[1];
                _customizerActive = true;
                _lastObservedStep = 0;
                _lastActiveStep = 0;
                _statueConfirmedInWorkshop = false;
            }

            LogState("h_shugo.Entry Prefix - Customizer opened");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[GuardianDeity] Error during h_shugo.Entry prefix: {ex}");
        }
    }

    /// <summary>
    /// Intercepts native eventFlgON calls.
    /// When Jude's workshop confirms a statue build, native GameAssembly.dll unconditionally
    /// executes OldSrcBase.eventFlgON(148, 0x10). Cancellation jumps to 0x1820b8eaf, never setting this flag.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::OldSrcBase), "eventFlgON")]
    [HarmonyPostfix]
    public static void EventFlgON_Postfix(int _fno, byte _bno)
    {
        if (_customizerActive && _fno == 148 && (_bno & 0x10) != 0)
        {
            _statueConfirmedInWorkshop = true;
            Plugin.Log.LogInfo("[GuardianDeity] Native statue confirmation detected (Flag 148 bit 0x10 set)!");
        }
    }

    /// <summary>
    /// Tracks active step transitions in Jude's statue customizer (h_shugo.ShugoMain).
    /// Executed every frame while the customizer is open before shugo_work is freed on exit.
    /// Step 0x5b = 'Quit creating statue?' dialog prompt.
    /// Step 0x5c = Player selected 'Yes' (quit confirmed), transitioning to teardown (0x63).
    /// Step 0x5d..0x5e = Player finished/confirmed statue build.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.h_shugo), "ShugoMain")]
    [HarmonyPrefix]
    public static void H_Shugo_ShugoMain_Prefix(GSD2::EventOverlayClass.h_shugo __instance)
    {
        if (!_customizerActive || __instance == null) return;

        try
        {
            var work = __instance.shugo_work;
            if (work == null) return;

            byte step = work.step;
            if (step != _lastObservedStep)
            {
                if (Plugin.Config.EnableDebugLogging.Value)
                {
                    Plugin.Log.LogInfo($"[GuardianDeity] [ShugoMain] Step transition: 0x{_lastObservedStep:X2} -> 0x{step:X2}");
                }
                _lastObservedStep = step;
            }

            if (step != 0x63)
            {
                _lastActiveStep = step;
            }
        }
        catch (Exception ex)
        {
            if (Plugin.Config.EnableDebugLogging.Value)
            {
                Plugin.Log.LogError($"[GuardianDeity] Error in ShugoMain prefix: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Hook Jude's statue customizer main update loop completion (h_shugo.ShugoMain).
    /// Returns 1 when the player closes/exits the customization workshop.
    /// In vanilla, shugo_work is set to null during Step 0x63 before returning 1,
    /// so exit disposition is evaluated from native confirmation flag state.
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.h_shugo), "ShugoMain")]
    [HarmonyPostfix]
    public static void H_Shugo_ShugoMain_Postfix(GSD2::EventOverlayClass.h_shugo __instance, int __result)
    {
        try
        {
            if (__result != 1 || !_customizerActive) return;
            _customizerActive = false;

            var gw = GSD2::OldSrcBase.game_work;
            if (gw?.game_data?.parts_no == null || gw.game_data.parts_no.Length <= 3) return;

            // Preserve blueprint bitmasks
            gw.game_data.parts_no[0] |= _plansBeforeCustomizer0;
            gw.game_data.parts_no[1] |= _plansBeforeCustomizer1;

            // Native GameAssembly.dll calls eventFlgON(148, 0x10) exclusively when confirming a build.
            // When the player cancels/quits, native code jumps past eventFlgON.
            bool isCancelled = !_statueConfirmedInWorkshop;

            byte currentCombo = gw.game_data.parts_no[2];

            if (isCancelled)
            {
                Plugin.Log.LogInfo($"[GuardianDeity] Workshop Exit: CANCELLED (statueConfirmedInWorkshop=false, lastActiveStep=0x{_lastActiveStep:X2}, assembledBefore={_statueAssembledBeforeCustomizer})");

                if (_statueAssembledBeforeCustomizer == 0)
                {
                    // Player cancelled first statue creation: keep untouched stone rock
                    gw.game_data.parts_no[3] = 0;
                    gw.game_data.parts_no[2] = 0;
                    gw.eventFlgOFF(148, 0x10);
                    gw.eventFlgOFF(148, 0x20);
                    LogState("First statue creation cancelled - keeping untouched rock");
                }
                else
                {
                    // Player cancelled rebuild: revert combination to prior state without re-arming
                    gw.game_data.parts_no[2] = _statueBeforeCustomizer;
                    if (Plugin.Config.AlwaysKeepStatueAssembled.Value)
                    {
                        gw.game_data.parts_no[3] = 1;
                    }
                    LogState($"Statue rebuild cancelled - reverted combo to 0x{_statueBeforeCustomizer:X2}");
                }
            }
            else
            {
                Plugin.Log.LogInfo($"[GuardianDeity] Workshop Exit: CONFIRMED (statueConfirmedInWorkshop=true, combo=0x{currentCombo:X2}, prevCombo=0x{_statueBeforeCustomizer:X2})");

                if (_statueAssembledBeforeCustomizer == 0)
                {
                    // Player confirmed first statue: mark assembled and arm blessing
                    gw.game_data.parts_no[3] = 1;
                    ArmStatueReward(gw, "First Statue Confirmed");
                }
                else
                {
                    // Player confirmed rebuild: preserve assembled flag and re-arm blessing
                    if (Plugin.Config.AlwaysKeepStatueAssembled.Value)
                    {
                        gw.game_data.parts_no[3] = 1;
                    }
                    ArmStatueReward(gw, "Statue Rebuild Confirmed");
                }
            }

            LogState($"Workshop closed (Verdict: {(isCancelled ? "CANCELLED" : "CONFIRMED")}, combo=0x{gw.game_data.parts_no[2]:X2}, assembled={gw.game_data.parts_no[3]})");
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

            // If no statue is assembled (untouched rock), do not arm any blessings
            if (gw.game_data.parts_no[3] == 0 || !HasAnyBlueprints(gw))
            {
                return;
            }

            if (Plugin.Config.AlwaysKeepStatueAssembled.Value)
            {
                gw.game_data.parts_no[3] = 1;
            }

            if (Plugin.Config.EnableRepeatStatueRewards.Value)
            {
                // Clear Story Flag 148 bit 32 (0x20) - Blessing Claimed Lock!
                gw.eventFlgOFF(148, 0x20);

                // Set Story Flag 148 bit 16 (0x10) - Inspection Ready Trigger!
                gw.eventFlgON(148, 0x10);

                // Clear Flag 123 evaluation bits so blessing and party reactions trigger cleanly
                gw.eventFlgOFF(123, 8);
                gw.eventFlgOFF(123, 16);
                gw.eventFlgOFF(123, 32);
                gw.eventFlgOFF(123, 64);

                LogState("syugoSet.Entry Prefix - Roof Shrine loaded, repeat blessing armed (Flag 148 bit 32 cleared, bit 16 set)");
            }
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
    /// </summary>
    [HarmonyPatch(typeof(GSD2::EventOverlayClass.syugoitm), "s_OverlayAddItem2Main")]
    [HarmonyPostfix]
    public static void Syugoitm_Reward_Postfix(int __result)
    {
        if (!Plugin.Config.EnableRepeatStatueRewards.Value) return;

        try
        {
            if (__result == 1)
            {
                var gw = GSD2::OldSrcBase.game_work;
                if (gw?.game_data?.parts_no != null && gw.game_data.parts_no.Length > 2)
                {
                    byte currentCombo = gw.game_data.parts_no[2];
                    SetLastRewardedStatue(currentCombo);

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
