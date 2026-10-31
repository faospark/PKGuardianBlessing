# PKGuardianBlessing

## Overview
**PKGuardianBlessing** is an independent quality-of-life mod for *Suikoden II HD Remaster* focusing on Jude's Guardian Deity statue shrine and workshop mechanics:
- **Repeatable Deity Blessings**: In retail vanilla, Jude only awards an item/rune blessing for the very first statue built. Rebuilding or modifying the statue at Jude's workshop normally yields nothing because Story Flag 148 bit 0x20 is locked. PKGuardianBlessing automatically re-arms the blessing event when designing a new statue combination.
- **Assembled 3D Statue Model Persistence**: Prevents the castle roof statue from reverting to plain bedrock (`parts_no[3] = 1`).

## Configuration
Located in `BepInEx/config/faospark.pkguardianblessing.cfg`:
- `EnableRepeatStatueRewards` (default: `true`): Arms Jude's blessing sequence upon new statue designs.
- `AlwaysKeepStatueAssembled` (default: `true`): Maintains full 3D statue rendering on the castle roof shrine.
- `EnableDebugLogging` (default: `false`): Logs state transitions and flag updates to `LogOutput.log`.
