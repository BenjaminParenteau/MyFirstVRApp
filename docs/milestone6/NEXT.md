# Milestone 6 — pick up here

Handoff note for the next session (written 2026-11-17, Slot D / Zach). Everything below is Slot D's work unless
it says otherwise. Status and the full checklist live in
[Progress_Vol1.md](../../Progress_Vol1.md#milestone-6--audio-spatial-sound-and-the-blackout-mode); the split and
the per-member briefs are in [README.md](README.md).

## The one-line summary

**The plumbing is built and tested; nothing has been heard yet.** The audio system, the validator and the whole
blackout mode are on `dev` and verified, but **no Slot D clips exist**, `Build Soundscape` has never been run, and
the darkness has never been on screen. Ashton has already landed 47 real cues, so the system now has actual
content to be tested against for the first time.

## Do these first, in this order

**1. Run `High Stakes > Audio > Validate Cues`.** ← start here, it needs nothing else

It has **never been run**, and there are now two cue sets from two different slots (mine and Ashton's), so this is
the first real test of the thing the whole four-way split rests on: that `Resources.LoadAll` discovers every
slot's cue set with no shared file listing them. That was reasoned from how Unity merges `Resources` folders, not
observed. It will either confirm the design or find the first genuine integration problem — and it is much cheaper
to find now than after Ben and Pak add theirs.

Expect it to report Slot D's area beds as missing (they are — see step 3). Everything of Ashton's should pass:
his namespace, cue-set location, loop flags and import settings were all checked by hand on 2026-11-17 and were
correct.

**2. Run `High Stakes > Audio > Build Soundscape`.**

None of the components are in `MVP_Main` yet, so right now nothing runs at all. This adds `Soundscape`,
`BlackoutMode`, `SonarPing`, `BlackoutProximity`, `BlackoutBreath` and `BlackoutTrigger`, and wires the blackout to
SceneFlow's ScreenFade quad. Commit `MVP_Main.unity` after — it is Slot D's scene, so no conflict risk.

**3. Write `tools/audio/generate_environment_audio.py`.** ← the big one

**This is what turns all of it from verified code into something you can experience.** Nothing makes a sound until
it exists. Import the shared helpers (`from synth import ...` — `tools/audio/synth.py` is written and verified: 22
primitives, seamless loops, variants, 48 kHz/16-bit). The cue list is already specified in
`Environment/Audio/Editor/AudioBuilder.cs` → `Specs`, and a cue whose clips don't exist is skipped, so you can
generate them in any order:

| Group | Clips |
|---|---|
| Music beds | `MUS_Floor`, `MUS_Tense`, `MUS_Vault`, `MUS_Lobby`, `MUS_Blackout` |
| Room tones | `AMB_CasinoFloor`, `AMB_Vip`, `AMB_Hallway`, `AMB_BackOfHouse`, `AMB_Vault`, `AMB_Street` |
| Footsteps | `SFX_Step{Carpet,Wood,Concrete,Tile,Metal}_1..4` (4 variants each) |
| Doors / travel | `SFX_DoorOpen`, `SFX_DoorClose`, `SFX_TeleportArrive`, `SFX_TravelDepart`, `SFX_TravelArrive` |
| Blackout | `SFX_PowerDown`, `SFX_PowerUp`, `SFX_SonarPing`, `SFX_SonarReturn{Soft,Hard}`, `SFX_Proximity`, `SFX_Beacon`, `SFX_BreathIn_1..3`, `SFX_BreathOut_1..3` |

Use `loopable()` on every bed or you get a click once per cycle. **The room tones are the priority** — each one has
to be identifiable in two seconds with your eyes shut, because in the blackout that is literally the map
(StyleGuide §11).

**4. Then, in rough order:** `FootstepAudio` + `SoundEmitters` (specified in
[Audio_SlotD_Zach.md](Audio_SlotD_Zach.md) part 2) · `BlackoutRun` (the scored challenge and the Guide's nudge
escalation — `BlackoutLogic` already has the scoring, ranks and nudge timings, all tested) · `AudioPlayCheck` ·
the GDD Sound Design rewrite · `UserTesting.md` §12 round 3 · then the APK and the sessions.

## Waiting on other people (don't block on these)

- **Pak — the `BLACKOUT RUN` start-screen row.** Asked in Open questions and *not yet answered*. `StartScreen.cs`
  is hers, and I deliberately asked rather than adding it the way the fast-travel watch column went in last
  milestone. Until it lands, the blackout only has the story trigger, so a grader would have to solve the whole
  back-room puzzle chain to see the headline feature. **Chase this one** — it is small and it gates how the
  milestone demos.
- **Pak — the Guide bypasses the VOICE bus** (one line in `GuideVoice.cs`). Flagged in her brief with the fix.
- **Pak — `ui.tier.up`**, and **Ashton — `sec.npc.step`** (the Pit Boss patrols in silence).
- **Ashton — the three Milestone 4 lightmap bakes.** His CPU-lighting commit (`be8e103`) landed, so these may now
  be done; check before assuming. Round 3 runs on the same build.

## How verification works here (worth knowing before you trust anything)

**The Editor is usually open, which blocks `-batchmode` entirely** ("another Unity instance is running"). Don't
kill it. Two ways round it, both used on 2026-11-17:

1. **The dotnet harness** — the logic assemblies are engine-free by design, so they compile *and the NUnit tests
   actually execute* outside Unity. The Unity-dependent files also compile cleanly if you reference
   `<Editor>/Data/Managed/UnityEngine/*.dll` (139 DLLs) plus `Library/ScriptAssemblies/Unity.InputSystem.dll`,
   with small stubs for `SceneFlow` and `CctvOutage`. This caught **four real bugs** that would otherwise have
   reached the team: `save_wav` crashing outside the repo root, `Destroy` unreachable from a nested class, two
   missing `using` directives, and a vestigial stub. Worth rebuilding — it takes a few minutes and is far faster
   than the Editor.
2. **Ask for one click into Unity**, which imports and compiles; then confirm via `Library/ScriptAssemblies/*.dll`
   timestamps and `grep "error CS"` on `%LOCALAPPDATA%/Unity/Editor/Editor.log`.

**What is actually proven** (as of `84286e5`): 60 logic tests executed and passing · every assembly compiles in the
real Editor with 0 `error CS` · `synth.py` exercised end to end · Ashton's cue set audited by hand.
**What is NOT proven:** nothing has been played or heard, the blackout has never been on screen, `Validate Cues`
and `Build Soundscape` have never run, no cross-slot cue discovery has been observed at runtime, and **no APK has
ever been built by anyone on this team** — so the §7 budget and the audio voice/memory cost are still unmeasured.
Be careful not to let the green tests read as "the audio works".

## One small thing

`Assets/Scripts/Environment/Blackout/BlackoutBreath.cs.meta` was **hand-written** (2-line form, byte-identical in
structure to its siblings, fixed GUID) because the Editor had not re-imported when it was committed. If Unity
rewrites it on import, commit the result — the GUID itself is what matters and it is already stable.
