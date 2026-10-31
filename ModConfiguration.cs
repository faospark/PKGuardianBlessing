using BepInEx.Configuration;

namespace PKGuardianBlessing;

public class ModConfiguration
{
    private readonly ConfigFile _config;

    public ConfigEntry<bool> EnableRepeatStatueRewards { get; private set; }
    public ConfigEntry<bool> AlwaysKeepStatueAssembled { get; private set; }
    public ConfigEntry<bool> EnableDebugLogging { get; private set; }

    public ModConfiguration(ConfigFile config)
    {
        _config = config;
    }

    public void Init()
    {
        EnableRepeatStatueRewards = _config.Bind(
            "GuardianBlessing",
            "EnableRepeatStatueRewards",
            true,
            "When enabled, re-arms the Guardian Deity blessing reward whenever the statue design is rebuilt or altered at Jude's workshop."
        );

        AlwaysKeepStatueAssembled = _config.Bind(
            "GuardianBlessing",
            "AlwaysKeepStatueAssembled",
            true,
            "Ensures the Guardian Deity statue maintains its assembled 3D model (parts_no[3] = 1) instead of reverting to plain stone."
        );

        EnableDebugLogging = _config.Bind(
            "GuardianBlessing",
            "EnableDebugLogging",
            false,
            "Enable detailed diagnostics logging for Guardian Deity state transitions and event triggers."
        );
    }
}
