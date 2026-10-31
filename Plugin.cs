using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PKGuardianBlessing.Patches;

namespace PKGuardianBlessing;

[BepInPlugin("faospark.pkguardianblessing", "PK Guardian Blessing", "1.0.0")]
[BepInDependency("faospark.pkcore", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    public static Plugin Instance { get; private set; }
    public static new ManualLogSource Log;
    public static new ModConfiguration Config;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;

        Log.LogInfo("Loading PKGuardianBlessing (Guardian Deity Repeatable Rewards & Model Fix)...");

        Config = new ModConfiguration(base.Config);
        Config.Init();

        Harmony harmony = new Harmony("faospark.pkguardianblessing");

        harmony.PatchAll(typeof(GuardianDeityRepeatRewardsPatch));

        Log.LogInfo("PKGuardianBlessing loaded successfully!");
    }
}
