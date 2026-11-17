# Milestone 6 — Slot D (Zach): the system, the world, and the blackout mode

**You own the plumbing everyone else plugs into, the sound of the building itself, and the dark mode.** Your gate
is the only blocking item in the milestone: until the API exists, A, B and C can write generators and synthesise
wavs but cannot wire a single sound into the game. Ship the gate first and small.

Read [README.md](README.md) for the seven no-conflict rules — you wrote them, and you are the one who has to hold
the line when somebody asks to put an `AudioSource` in a scene.

## Your namespace

`env.`, `mus.`, `amb.`, `dark.`.

## Part 1 — the gate (ship this first, everyone is waiting on it)

`Assets/Scripts/Environment/Audio/`

| Piece | What it is |
|---|---|
| `SoundBus.cs` | `enum { Music, Ambience, Sfx, Voice }`. **Append-only**, like a Core contract. |
| `SoundCue.cs` | ScriptableObject: `id`, `clips[]`, `bus`, volume, pitch jitter, `spatial`, min/max distance, `loop`. Several clips per cue = random variant, which is what stops repeated one-shots sounding mechanical. |
| `SoundCueSet.cs` | A per-slot bag of cues. **One asset per slot**, each in that slot's own `Resources/Audio/`. There is deliberately no global list — that is rule 3. |
| `Soundscape.cs` | The one runtime component, in `MVP_Main`, surviving door swaps. Discovers every `SoundCueSet` with `Resources.LoadAll`, owns the bus volumes (PlayerPrefs), the music/ambience crossfades and a pooled set of one-shot sources. |
| `Soundscape` statics | `Play(id)`, `PlayAt(id, pos)`, `PlayOn(id, transform)`, `Loop(id, transform) -> handle`, `SetBusVolume`, `GetBusVolume`. **Must no-op safely when no instance exists** so three other people's EditMode tests and solo scenes keep passing. Unknown id logs once, never throws. |
| `AudioSpatial.cs` | Applies the project spatial defaults to a source (README's table). One place, so "spatial audio" is a decision rather than 60 copies of it. |
| `Logic/BusLogic.cs` | Pure: volume clamping, dB mapping, PlayerPrefs keys, the blackout duck. Tested. |
| `Logic/SoundscapeLogic.cs` | Pure: scene name → which music + ambience bed, crossfade curve, variant picking, one-shot pool eviction. Tested. |

Keep `Soundscape` dumb and fast: object-pooled sources, no allocation per one-shot, no `GetComponent` in the call
path. It will be hit several times a second once four people are calling it.

## Part 2 — the sound of the world

Music beds (`mus.`), crossfaded by area from `SceneFlow.CurrentScene`:

| Bed | Areas | Character |
|---|---|---|
| `mus.floor` | `MVP_Casino`, `MVP_VIP` | Warm low lounge, brushed kit, nothing with a melody you'd notice twice |
| `mus.tense` | `MVP_SecurityHall`, `MVP_SecurityOffice`, `MVP_CameraRoom` | Thin, airless, one sustained note and a pulse |
| `mus.vault` | `MVP_Vault` | Near-silence — a single low tone, mostly room |
| `mus.lobby` | start screen | The one place a tune is allowed |
| `mus.blackout` | blackout mode | Almost nothing. Dropping the music *is* the effect |

Ambience beds (`amb.`), one per area, 2D or very wide 3D, streamed:
`amb.casino_floor` (murmur, distant clatter, slot wash), `amb.vip` (same, quieter, more reverb), `amb.hallway`
(the floor heard through a wall), `amb.back_of_house` (HVAC and fluorescent), `amb.vault` (hum and nothing else),
`amb.street` (heard through the entrance glass).

**The room tone is the map.** Each bed must be identifiable in two seconds with your eyes shut, because in the
blackout that is exactly what it is for. Test them that way before you ship them.

World one-shots (`env.`):

| Cue | Fires when |
|---|---|
| `env.step.carpet` / `.wood` / `.concrete` / `.tile` / `.metal` | Footsteps, chosen by what's under the player |
| `env.door.open` / `.close` | `DoorPortal` travel |
| `env.travel.depart` / `.arrive` | Fast travel from the watch |
| `env.teleport.arrive` | Teleport landing — a single soft step, paired with `TeleportBlink`'s fade |
| `env.chandelier` / `env.ac` / `env.pipe.drip` | Runtime point emitters |

`FootstepAudio.cs` on the rig: accumulate distance from `MoveRamp`'s actual velocity, fire a step every ~0.7 m,
raycast down and classify the surface from the renderer's material name, pitch-jitter per step, fire once on
teleport arrival. Pure `FootstepLogic` for the stride accumulator and the material→surface mapping, so it is
testable without a rig. **Footsteps are the single biggest immersion win per line of code in VR** and they are the
spine of blackout navigation — do them properly.

`SoundEmitters.cs` — the ambience-emitter equivalent of `TeleportSurfaces`: when an area loads, find the anchors
by name/component and attach looping emitters at runtime. **No `AudioSource` in any scene file** (rule 1); A, B and
C each attach their own from their own builders, and this handles the world's own (chandeliers, air handling,
dripping pipes, the street through the glass).

## Part 3 — the blackout mode

`Assets/Scripts/Environment/Blackout/`

### Going dark, properly

"Turn the lights off" is not enough in this project — every area is **lightmapped**, so disabling `Light`
components leaves the baked light exactly where it was. `BlackoutMode.Begin()` has to take all of it, and restore
precisely what it changed:

| Kill | How |
|---|---|
| Realtime lights | Disable every enabled `Light`, remember which |
| **Baked light** | `LightmapSettings.lightmaps = empty` — this is the one people forget |
| Ambient | `RenderSettings.ambientLight` → black, `ambientIntensity` → 0, `AmbientMode.Flat` |
| Skybox | `RenderSettings.skybox` → null, camera clear → solid black |
| Reflection probes | Disable, plus `RenderSettings.defaultReflectionMode` → custom/null |
| Emissive props | Renderers on the blackout list switch off (slot machines, holograms, monitors, strip lights, the watch, the notebook) |

Restore is not optional and not approximate: store every original value, put it all back on `End()`, and make the
play check assert that the scene after a blackout is byte-identical in those settings to before it. A blackout that
leaves the casino dark is a game-breaking bug, and it will happen if restore is sloppy.

### Hearing in the dark

| Piece | What it does |
|---|---|
| `SonarPing.cs` | The player's own echolocation. Press a controller button → `dark.sonar.ping`, then a fan of raycasts from the head; for each hit, schedule a return one-shot at the hit point, delayed by distance and pitched by surface hardness (`dark.sonar.return.soft` / `.hard`). Cooldown ~1.5 s so it can't be spammed into a solid picture. |
| `BlackoutProximity.cs` | A continuous tone that rises as the player nears geometry — the difference between groping and navigating. Keep it subtle; it should read as the room being close, not as a warning. |
| `AudioBeacon.cs` | Marks a goal — the exit door, the breaker, the keypad. Audible always, promoted in the dark. Placed by builders, never by hand in a scene. |
| `BlackoutMix` | The dark mix: music down hard, ambience and the beacons up, Sfx up slightly. One place, driven off `BusLogic`. |
| `BlackoutRun.cs` | The replayable challenge: objectives, timer, completion, failure, the Guide's `vo.guide.blackout.*` lines. Pure `BlackoutRunLogic` for objectives and scoring. |
| `BlackoutTrigger.cs` | Watches Ashton's `WirePanelPuzzle` solved state **from your folder** — do not edit his file. He is exposing the state for you. |

### Two ways in

1. **In story:** pull the breaker in the security office → `sec.breaker.throw` → the lights die across the back of
   house → the player crosses to the vault by ear. The darkness is something they chose and caused.
2. **Direct:** a `BLACKOUT RUN` row on the start-screen console, so a tester or a grader can be in the mode fifteen
   seconds after putting the headset on. Add it via `StartScreenBuilder` — which is Slot C's file, so either ask
   Pak or add the row from your own builder the way the fast-travel watch column was done. **Ask first this time.**

## Part 4 — standards, shared files, verification

- `ProjectSettings/AudioManager.asset` — speaker mode, DSP buffer, 32 real / 512 virtual voices. Yours alone.
- **HRTF decision.** Unity's built-in 3D audio is panning + distance only. Real binaural on Quest needs the Meta XR
  Audio SDK package. It would improve the mode that depends most on localisation — but it is a package add that
  needs an on-device test, and no APK has been built by anyone yet. Decide it deliberately, log it in Decisions,
  and don't add the package on the same day as the round 3 build.
- **[StyleGuide §11](../StyleGuide.md#11-audio-and-soundscape)** — the standards page, written the way §10 was: the
  rules, the spatial defaults, the budget, and "checking it".
- **Audio memory budget** — add it to §7. Beds and music streamed, one-shots ADPCM and force-mono, and a ceiling
  on resident audio so sixty cues across four slots can't quietly eat the frame budget.
- `Environment/Editor/AudioBuilder.cs` — **High Stakes > Audio > Build Soundscape**: import your clips with Quest
  settings, write `CueSet_Environment.asset`, add the components to `MVP_Main`. Called from `WorldBuilder.BuildMain`
  so a world rebuild doesn't drop the audio. **The import block in here is the one the other three copy** — make it
  clean and comment it.
- `Environment/Editor/CueValidator.cs` — **High Stakes > Audio > Validate Cues**: duplicate ids, ids outside the
  caller's namespace, missing clips, 3D cues with no distance set, resident-memory total. This is what actually
  enforces the four-way split, so write it before the others start adding cues, not after.
- `Environment/Editor/AudioPlayCheck.cs` — **High Stakes > Run Audio Play Check**, headless-runnable like
  `LocomotionPlayCheck`: walk all six areas, assert each has a music bed and an ambience bed playing and its
  emitters attached, then enter the blackout, assert the scene is genuinely dark, exit, and **assert every lighting
  value is restored**. Write results to `Logs/AudioQA/` — not `Temp/`, which Unity deletes on clean shutdown and
  which cost four runs' evidence last milestone.

## Order of work

1. The gate (part 1) + `CueValidator` → push → tell A, B and C.
2. `synth.py` + your generator + the beds and footsteps (part 2).
3. The blackout mode (part 3) — the biggest single piece, and the thing the milestone is actually graded on.
4. §11, the GDD, the round 3 protocol, the play check.
5. Round 3 APK → sessions → fixes → the write-up.

## Definition of done

- A, B and C can each add a sound touching nothing of anyone else's — ask them to confirm, don't assume.
- Six areas, each with its own music and room tone, crossfading cleanly through doors.
- Footsteps on every surface; you can tell carpet from concrete blind.
- The blackout is genuinely black, navigable by ear, enterable both ways, and **restores perfectly** every time.
- `Run Audio Play Check` recorded as a PASS in `Logs/AudioQA/`, like the locomotion check.
- §11 written, GDD Sound Design rewritten as built, round 3 logged in `UserTesting.md`.
- The §7 budget still holds on device — and this is the milestone where "no APK has ever been built" finally has to
  stop being true, because audio memory and voice count are things you cannot check in the simulator.

## Round 3 user testing (you run it)

Protocol in `UserTesting.md` §12. The questions that are specifically yours:

- *"Could you tell which room you were in with your eyes closed?"* — the room-tone test, and the premise the whole
  blackout rests on.
- *"Did the footsteps feel like yours?"* — wrong-surface or wrong-rate footsteps are worse than none.
- *"In the dark: could you get out? How did it feel?"* — and watch for nausea, because people move differently when
  they can't see, and that interacts with everything Milestone 4 tuned.
