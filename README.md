# P.K. - Guardian Blessing

## Overview
**PKGuardianBlessing** is an independent quality-of-life mod for *Suikoden II HD Remaster* focusing on Jude's Guardian Deity statue shrine and workshop mechanics:
- **Repeatable Deity Blessings**: In retail vanilla, Jude only awards an item/rune blessing for the very first statue built. Rebuilding or modifying the statue at Jude's workshop normally yields nothing because Story Flag 148 bit 0x20 is locked. PKGuardianBlessing automatically re-arms the blessing event when designing a new statue combination.
- **Assembled 3D Statue Model Persistence**: Prevents the castle roof statue from reverting to plain bedrock (`parts_no[3] = 1`).

## Configuration
Located in `BepInEx/config/faospark.pkguardianblessing.cfg`:
- `EnableRepeatStatueRewards` (default: `true`): Arms Jude's blessing sequence upon new statue designs.
- `AlwaysKeepStatueAssembled` (default: `true`): Maintains full 3D statue rendering on the castle roof shrine.
- `EnableDebugLogging` (default: `false`): Logs state transitions and flag updates to `LogOutput.log`.

---

## Installation

- **Prerequisite**: [BepInEx 6.0 (Unity IL2CPP x64)](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip) installed in your game root directory.
- **Install**: Extract the release ZIP directly into your game's root directory (or place `PKGuardianBlessing.dll` into `BepInEx/plugins/`).
- **Verify**: Launch the game; `BepInEx/LogOutput.log` will confirm `[PKGuardianBlessing] loaded successfully!`.

---

## Credits & Acknowledgments

- **[d3xMachina](https://github.com/d3xMachina/Suikoden-Fix)**: Creator of [Suikoden-Fix](https://github.com/d3xMachina/Suikoden-Fix). Bootstrapped the foundational BepInEx IL2CPP implementation and provided early inspiration for Remaster modding.
- **[asilverthorn](https://github.com/asilverthorn/suikoden_ref)**: Creator of [suikoden_ref](https://github.com/asilverthorn/suikoden_ref). Invaluable early reference tables, internal IDs, and save structure research.
- **[faospark](https://github.com/faospark)**: Primary Author & Lead Researcher.
- **Moogles & Mods Discord Community**: For ongoing support, research, collaboration, and keeping the Suikoden modding scene thriving. Join us on the [Moogles & Mods Discord](https://discord.com/invite/bSnpVBV).
- **Framework**: [BepInEx](https://github.com/BepInEx/BepInEx) & [HarmonyX](https://github.com/BepInEx/HarmonyX).

---

## License

Distributed under the [MIT License](LICENSE).
