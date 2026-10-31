# PKCore & PKRecruitmentPatch Workspace Instructions

## 1. Prime Directives & Anti-Overengineering
- **Strict Scope Discipline & Zero Hallucinated Features**:
  - Never invent, split, decouple, or add extra configuration options or architecture variations that the user did not explicitly request (e.g. splitting a unified auto-battle feature into separate memory and persistence toggles).
  - One requested feature must remain **ONE single, unified implementation**.
  - Do not fantasize edge cases or unilaterally create secondary toggles/flags that undermine or reset the feature's expected baseline behavior.
- **Strict File & Subsystem Boundaries (Zero Unrelated File Edits)**:
  - When implementing or updating a feature for a specific subsystem (e.g. the Dear ImGui overlay in `ImGuiOverlayUI.cs`), do **NOT** modify or add fields to unrelated files or systems (such as `CustomObjectInsertion.cs`, map object injection, or entity loaders).
  - Use existing cached data and properties (`MACHICON_OnMapInitPatch.CurrentMapId`, etc.) and keep all UI, caching, and input handling localized strictly within the component itself.
- **Course of Action & User Approval**:
  - Always outline a clear plan / course of action before starting work on complex tasks, refactors, or new features.
  - Present the proposed steps directly to the user and ask how they would like to proceed before modifying code or executing changes.
- **Zero Over-Research on Straightforward Features**:
  - When a task requests a routine, well-understood edit in our own project files (such as adding a HUD widget, a menu toggle, or a config entry), do **NOT** launch exploratory greps, multi-slice file reads, or treat it as an unknown binary mystery. Target the exact known file and function immediately, apply the edit cleanly, and run `dotnet build`.
- **Immediate Accountability & No Evasion**:
  - If the user questions a decision, catches a regression, or calls out unrequested changes, **STOP all background tool executions immediately**.
  - Never fire off silent background searches, log inspections, or evasive tool calls to cover tracks or stall.
  - Directly, plainly, and immediately acknowledge what was done, own the mistake, and apply the exact fix requested without debate or side-tangents.

## 2. Workspace Boundaries & Build Discipline
- **Target Projects**:
  - All mod projects reside in `d:\Appz\` (`PKCore`, `PKButtonPrompts`, `PKRecruitmentPatch`, `PKBattlePatch`, `PKOpenPaths`, `PKRuneSagePlus`, `PKS1CastleStorageAnywhere`, `PKS2BathNPCPatch`, `PKTenzanPassPatch`).
  - Code changes and builds target the specific mod project requested by the user.
- **Read-Only References (Never Edit)**:
  - `d:\Appz\Suisource\GSD1`, `GSD2`, `GSDShare` — decompiled game source, used only for grounding. Implement behavior changes as Harmony patches, never by editing decompiled sources.
  - `d:\Appz\Suisource\Ideas` — notes/ideas, read for context only.
  - `...\Suikoden I and II HD Remaster\BepInEx` — game install and runtime output (logs, configs, textures). Do not write project code there.
- **Build & Symlink Execution**:
  - The build output directory is symlinked directly to the game's BepInEx plugins directory (`...\BepInEx\plugins\`).
  - **NEVER** run manual `Copy-Item` or file copy commands to the game folder. Running `dotnet build` is sufficient.
  - Build individual projects (`dotnet build PKCore\PKCore.csproj`) or the full suite (`dotnet build d:\Appz\SuikodenMods.sln`).
  - **Runtime Diagnostics & Logs**: Inspect `...\Suikoden I and II HD Remaster\BepInEx\LogOutput.log` for runtime output, text message IDs, character slot IDs, and errors.

## 3. Grounding & Fast Analysis Tooling
- **High-Speed Symbol Search (`sym.bat` / `tools\sym.bat`)**:
  - Always query the local symbol index before doing broad grep searches across Suisource. Responds in <3ms.
  - Callable from root (`d:\Appz\sym.bat`) or project tools folder (`tools\sym.bat`).
  - Method / Symbol Search: `sym.bat <SymbolName>` (e.g. `sym.bat EventInit`)
  - Type-Scoped Member Search: `sym.bat <TypeName>.<MemberName>` (e.g. `sym.bat MACHICON.mstep`)
  - Full Class Member Dump: `sym.bat <TypeName> -m` (e.g. `sym.bat MACHICON -m --methods`)
  - Native Address Resolution: `sym.bat <0xAddress>` (e.g. `sym.bat 0x1818bb190` $\rightarrow$ resolves native symbol, signature, C# file, and line)
- **Direct Fast-Disassembly (`disasm.bat` / `tools\disasm.bat`)**:
  - Disassembles `GameAssembly.dll` native x86_64 instructions in <100ms: `disasm.bat <0xAddress> [bytes]`
  - **NEVER attempt full Ghidra decompiler runs on `GameAssembly.dll`**: Full headless decompile hangs because the 63 MB binary is too massive. All symbols and addresses are already known and indexed.
- **REA Synergy**: Anchor with `sym.bat` $\rightarrow$ analyze native flow with REA disassembler $\rightarrow$ cycle call targets back to `sym.bat <0xAddress>` $\rightarrow$ implement Harmony hook.
- **AST Knowledge Graph**: After modifying workspace code, run `graphify update .` to keep AST data synchronized.

## 4. Harmony & IL2CPP Rules
- **Game Scoping & Namespaces**:
  - S1 uses `extern alias GSD1;` (types under `GSD1::` like `h_monsyo_c`, `Village_c`, `Event_c`).
  - S2 uses `extern alias GSD2;` (types under `GSD2::` like `MACHICON`, `G2_SYS`, `G2_PAD`, `s_phase`).
  - Guard universal or dynamic hooks using `Plugin.CurrentGame` or `GameDetection.CurrentGame` (`GameType.Suikoden1` vs `GameType.Suikoden2`).
- **IL2CPP Memory & Collections**:
  - IL2CPP menu arrays and collections must be instantiated via `new Il2CppStructArray<T>(count)` or `Il2CppReferenceArray<T>`.
  - Cursor bounds: native scroll handlers clamp to `max`. Prefix the cursor function (`window_cursor_updown`) to expand `ref int max`.
  - Native pad polling: Poll native pad with `Pad.PadRRead()` (S1) or `G2_PAD.PadRRead()` (S2) checking `P1_OK` / `P1_CANCEL`.
- **Map & Asset Patching Pattern (`MACHICON.EventInit`)**:
  - S2: Hook `[HarmonyPatch(typeof(MACHICON), "EventInit")]` (`[HarmonyPostfix]`). Modify `__result.mdat` (`MACHIDAT`) directly in memory (e.g. `mapeventdat`, `eventobj`, `eventForm`, `battle_dat`, `monster_party`).
  - S1: Hook `MACHICON_OnMapInitPatch.CurrentMapId` or `G1_fdata_c`. Avoid hooking per-frame step routines like `Village_c.village_main` for map transitions.
- **STRICT Byte-Length Preservation & Zero Array Truncation**:
  - In S2 event bytecode (`eventForm` / `eventCom`), **NEVER truncate, shrink, or delete bytes**. Chopping bytes desyncs parser offsets and breaks engine timing.
  - Always preserve 100% exact array length matching vanilla `Mobobehaviour/<map>.json`.
  - To bypass condition checks or story flags, **replace the 3 bytes with NOP padding: `[0, 0, 0]`**.
  - Neutralize entire triggers by filling with zeroes `[0, 0, ..., 254]` preserving full length.
  - Leave cutscene `eventCom` sequences untouched unless fixing a verified single opcode branch.

## 5. UI & Configuration Architecture
- **Dear ImGui Subsystem (`ImGuiOverlayUI.cs`)**:
  - Keep overlay rendering, input capture, and UI state localized strictly within `ImGuiOverlayUI.cs`.
  - Separate UI presentation from game state logic; do not leak UI state into unrelated patches.
  - Avoid heavy allocations or LINQ queries inside per-frame ImGui render loops.
- **Centralized Configuration (`ModConfiguration.cs`)**:
  - All feature toggles, hotkeys, and configurable parameters must route through `ModConfiguration.cs` using standard BepInEx `ConfigFile` bindings.
  - Never create ad-hoc secondary config files or unconfigurable hardcoded flags.

## 6. Documentation & Release Standards
- **Feature Documentation Updates**:
  - **`CHANGELOG.md`**: Add technical details, affected patches, and configuration settings under the current date section.
  - **`README.md`**: Update the feature list under the appropriate category (`[Can Be Toggled]`, `[Performance Features]`, `[Baked In]`, or `[Utility]`).
- **Markdown Link Formatting**:
  - **NEVER use `file:///` URLs in `.md` files** (they break across GitHub and web viewers). Use relative markdown links (e.g. `[MemoryAutoBattlePatch.cs](Patches/MemoryAutoBattlePatch.cs)`) or plain inline code.
- **Nexus Mods Description**: When writing Nexus descriptions, follow the standardized BBCode template in `.agents/rules/nexusmods_template.md`.
- **Domain References**:
  - Combat Camera & Battle Detection: Consult `.agents/rules/combat_camera.md`.
  - Area Catalogs & Castle Level Opcodes: Consult `.agents/rules/area_documentation.md` and `PKCore\Documentation\areas\`.
