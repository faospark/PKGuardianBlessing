# Changelog - PKGuardianBlessing

All notable changes to PKGuardianBlessing will be documented in this file.

---

## [1.0.0] - 2026-10-27

### Added
- **Repeatable Guardian Deity Statue Rewards (`GuardianDeityRepeatRewardsPatch.cs`)**:
  - Resolves the retail vanilla restriction where Jude only awards a statue blessing (rune/item) for the first statue assembled.
  - Automatically re-arms Jude's blessing sequence upon designing a new statue combination by clearing Story Flag 148 bit 32 (`0x20` - Blessing Claimed Lock), setting Story Flag 148 bit 16 (`0x10` - Inspection Ready Trigger), and resetting party reaction evaluation bits on Flag 123 (`8`, `16`, `32`, `64`).
  - Tracks last rewarded statue combination byte (`parts_no[2]`) to ensure unique blessings upon each design iteration.
- **Assembled 3D Statue Model Persistence**:
  - Enforces `game_data.parts_no[3] = 1` during map transitions and workshop sessions, preventing the castle roof statue shrine from reverting to an untouched pedestal rock.
  - Safely cleanses ghost statue states when zero blueprints are possessed (`parts_no[0] == 0 && parts_no[1] == 0`).
- **Configuration & Diagnostics (`ModConfiguration.cs`)**:
  - Added `EnableRepeatStatueRewards` (default: `true`): Arms Jude's blessing sequence upon new statue designs.
  - Added `AlwaysKeepStatueAssembled` (default: `true`): Maintains full 3D statue rendering on the castle roof shrine.
  - Added `EnableDebugLogging` (default: `false`): Emits detailed state transition and flag updates to `LogOutput.log`.
