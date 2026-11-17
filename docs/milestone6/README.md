# Milestone 6 — audio, spatial sound and the blackout mode

Four people are authoring sound at the same time. This folder is how we do that without standing on each
other. Read this page once, then read **only your own brief** — each one is standalone and self-contained.

| Slot | Member | Brief | Owns, in one line |
|---|---|---|---|
| A | Ben (Benjamin Parenteau) | [Audio_SlotA_Ben.md](Audio_SlotA_Ben.md) | Everything the **games** make a noise about: chips, cards, wheel, dice, cage, wins |
| B | Ashton (Ashton Calkins) | [Audio_SlotB_Ashton.md](Audio_SlotB_Ashton.md) | Everything **back of house and locked**: readers, keypads, bolts, CCTV, vault, alarms |
| C | Pak (Pakrinha Sim) | [Audio_SlotC_Pak.md](Audio_SlotC_Pak.md) | Everything with a **voice or a finger on it**: the Guide, UI, notebook, watch, the audio settings page |
| D | Zach (Zachary Scheer) | [Audio_SlotD_Zach.md](Audio_SlotD_Zach.md) | The **system and the world**: buses, the API, spatial rules, music, ambience, footsteps, the blackout mode |

Standards live in [StyleGuide §11](../StyleGuide.md#11-audio-and-soundscape). Status and the checklist live in
[Progress_Vol1.md](../../Progress_Vol1.md#milestone-6--audio-spatial-sound-and-the-blackout-mode). Test protocol:
[UserTesting.md §12](../UserTesting.md).

## What the milestone asks for

> Create and implement immersive audio elements, including background music, ambient sounds, and sound effects.
> Integrate spatial audio to enhance the player's sense of immersion. Test and iterate on the audio design based on
> user feedback. Then add a mode in which the environment turns completely dark and force the user to use only sound
> to navigate, to act, and orient. Engage the player in a meaningful way to consider nonvisual elements.

Mapped onto our game:

| Asked for | Where it lands | Owner |
|---|---|---|
| Background music | Three beds that crossfade by area: warm lounge on the floor, thin and tense back of house, near-silence in the vault | D |
| Ambient sounds | A room tone per area, plus point emitters on the things that hum, rattle and chatter | D (beds) · all (their own props) |
| Sound effects | Every interaction that already exists gets a sound, authored by whoever owns the interaction | A, B, C |
| Spatial audio | One set of 3D defaults applied to every cue; distance curves tuned so you can find things by ear | D |
| Test and iterate | Round 3 of user testing, audio-specific, ≥ 5 testers, then fixes | D runs it, all fix |
| Completely dark mode | `BLACKOUT RUN` — the heist's power cut, playable as a story beat and selectable from the start screen | D (mode) · all (making their things audible) |
| Nonvisual engagement | See [below](#why-the-blackout-is-not-a-gimmick) | all |

## The seven rules that keep us out of merge conflicts

Audio is unusually good at causing conflicts, because the obvious way to build it — drop `AudioSource`
components in scenes and point them at one shared mixer — puts all four of us in the same three files. So we
don't do that. These rules are not style preferences; each one removes a specific file we would otherwise fight over.

**1. No `AudioSource` in any scene file. Ever.**
Same decision as teleport surfaces (`Progress_Vol1.md` → Decisions, 2026-11-10): if sound objects live in scenes,
every slot edits scene YAML to add them, every world rebuild wipes them, and `MVP_Casino` gets four authors.
Instead every sound is played from code through the API below, and looping emitters are attached **at runtime** by
the owning slot's own component. Consequence: **no scene file changes in this entire milestone**, so there is
nothing to merge. If you think you need an `AudioSource` in a scene, you want a runtime emitter — ask D.

**2. No `AudioMixer` asset.**
A mixer is one shared asset that everybody needs a group inside, and its YAML does not merge. We have four
**buses** instead — `Music`, `Ambience`, `Sfx`, `Voice` — as a fixed enum in D's code, with volumes saved per
headset in `PlayerPrefs`. Pick your bus per cue and you are done. The enum is **append-only**; a fifth bus needs
all four owners' OK, like a Core contract.

**3. No shared list of sounds.**
This is the one that would have bitten us hardest. There is **no** single "all the cues" asset. Each slot has its
own `SoundCueSet` asset, in its own `Resources/Audio/` folder, and the game discovers all of them at load. Adding a
sound therefore never touches a file another slot owns.

**4. Cue ids are namespaced, and the namespace is yours.**
Every sound has a string id. The prefix says who owns it, so two slots can never name the same thing:

| Prefix | Owner | Example |
|---|---|---|
| `table.` `cage.` | A | `table.chip.clink`, `cage.cash.out` |
| `sec.` `item.` | B | `sec.reader.ok`, `item.pickup` |
| `ui.` `vo.` | C | `ui.notebook.page`, `vo.guide.first_contact` |
| `env.` `mus.` `amb.` `dark.` | D | `env.step.carpet`, `amb.vault`, `dark.sonar.ping` |

**High Stakes > Audio > Validate Cues** fails if an id is duplicated, sits in a namespace you don't own, points at
a missing clip, or breaks the memory budget. Run it before you push.

**5. One `.wav` folder per slot, and one Python generator per slot.**
Nobody has recorded audio, so we synthesise it, the same way every texture in this repo was made
(`tools/generate_casino_carpet.py` and friends). You get your own script under `tools/audio/`. The shared helper
library `tools/audio/synth.py` is D's and is **append-only** — if you need a new synth primitive, ask and it gets
added rather than edited. Generated `.wav` files go in **your** content folder, never in `Assets/Content/StyleKit/`.

**6. You sound your own interactions.**
If you own the code that makes a thing happen, you own the line of code that makes it audible. D does not go
sprinkling `Play` calls through your files — that is exactly the trespass that caused the fast-travel/watch
awkwardness last milestone. One call is all it takes:

```csharp
using HighStakes.Environment.Audio;

Soundscape.PlayAt("table.chip.clink", chip.position);   // 3D one-shot where the thing happened
Soundscape.Play("ui.press");                            // 2D, for UI and anything "in your head"
Soundscape.PlayOn("table.roulette.ball", ball);         // 3D one-shot that follows a moving transform
var hum = Soundscape.Loop("sec.cctv.hum", wall);        // looping emitter; hum.Stop() when done
```

Safe to call from anywhere, including when no `Soundscape` exists yet (it no-ops, so your EditMode tests and your
solo scene don't need one). It never throws on an unknown id — it logs once and carries on.

**7. Shared files stay D's.**
`ProjectSettings/AudioManager.asset`, the bus enum, the API, the spatial defaults and the blackout mode are Slot D's
single-owner files, same as `Packages/` and `Assets/XR/`. Need a change? Open question in `Progress_Vol1.md`.

## The files we genuinely do share (there are three)

The split above partitions all the **new** code and content perfectly — every `.wav`, every cue set, every
generator, every builder has exactly one owner. But three existing files are touched by more than one of us, and
pretending otherwise is how we'd find out the hard way. Here is each one and the rule.

**1. `Assets/Scripts/TableGames/PressableButton.cs` — the contested one.**
It is Ben's file, in Ben's folder, and it is also the button component used by Pak's notebook tabs and watch rows,
Ashton's keypads and the start screen — 13 files across three other slots. All three of us would otherwise add a
`Play` call to it, in the same method, on the same line. So:

> **The owner of a file adds the line. The cue it plays may belong to someone else's namespace.**

Authoring a sound and calling a sound are different jobs. **Ben adds one line** to `PressableButton.cs` —
`Soundscape.Play("ui.press")` — even though `ui.` is **Pak's** namespace and Pak authors the clip. Nobody else
edits that file. If a particular button needs a different sound, don't branch in the shared file: put a
`SoundCue`/cue-id field on the component and set it per prefab, which is per-slot content and conflicts with nobody.

**2. `Assets/Scripts/Dialogue/SpeechBubble.cs`** — Pak's file, used by Ashton's and Ben's puzzle feedback. Same
rule: **Pak** adds the one line; if you want a different sound on your bubble, it comes from a field on the
component, not an edit to hers.

**3. `Progress_Vol1.md` and `docs/GameDesignDocument.md`** — everyone must edit both (the commit rule and the GDD
Sound Design section). These already have rules, in Merge-conflict rules 6 and 7: **your own log sub-section only,
your own checklist rows only, your own GDD row only, and never reflow a section you don't own.** Markdown still
conflicts on *adjacent* lines even when you each edit "your own" row, so the practical habit that actually saves
you is: `git pull --rebase origin dev` immediately before you commit these two, and keep the edit to the lines you
own. A conflict here is thirty seconds to resolve; a conflict in a scene file is an evening.

Everything else — all 60-odd cues, four generators, four cue sets, four builders, every wav — is single-owner, and
**no scene file changes in this entire milestone**.

## How to add a sound (the whole workflow, five minutes)

1. Add it to **your** generator: `python tools/audio/generate_<yours>_audio.py` writes a `.wav` into your content folder.
2. Add the cue to **your** builder menu (e.g. **High Stakes > Audio > Build Table Audio**) — it imports the clip with
   the Quest settings and writes your `SoundCueSet`.
3. Call it from your own code: one `Soundscape.PlayAt(...)` line.
4. Run **High Stakes > Audio > Validate Cues**, then listen to it in the simulator.
5. Commit: your `.wav` + its `.meta`, your cue set, your script, your generator, and your line in `Progress_Vol1.md`.

Files touched that anyone else owns: **none**. That is the whole point.

## Spatial audio, in one box

D sets these once as the project default; you normally override nothing.

| Setting | Value | Why |
|---|---|---|
| `spatialBlend` | 1.0 (full 3D) for everything except `ui.` and `vo.` | A sound you can't locate is a sound you can't navigate by |
| Rolloff | Logarithmic, custom curve, min 0.6 m / max 18 m (point props: 6 m) | Linear rolloff makes distance unreadable; 0.6 m stops a prop going painfully loud when you lean in |
| Doppler | 0 | Doppler on a teleporting player is nausea, not realism |
| Spread | 15° near, widening under 1 m | Keeps a close sound from collapsing into one ear |
| Reverb | One reverb zone per area from the bed, no per-prop reverb | Quest CPU |
| Voice count | 32 real / 512 virtual, music + beds streamed | §7 budget |

**HRTF is an open question, not a done deal.** Unity's built-in 3D audio gives panning and distance only. True
binaural positioning on Quest needs the Meta XR Audio SDK package, which is a package add (D's call, and it needs an
on-device test before it goes in). Everything above works either way; HRTF would improve it, not enable it.
Tracked in Open questions.

## Why the blackout is not a gimmick

The brief asks us to "engage the player in a meaningful way to consider nonvisual elements." A dark room with a
scored timer would technically satisfy it and would mean nothing. Ours leans on something the game already has:
**the Guide is already a character with no body** — a voice in your ear, the one presence in High Stakes that was
never visual. The blackout is the moment the player's only reliable sense becomes the one the game has been
training them on since the first tier-up.

Three things make it a real part of the game rather than a side mode:

- **It is the heist's power cut.** Ashton's breaker panel in the security office already cuts the vault camera
  (`CctvOutage`). Pulling it now cuts the lights too. The darkness is a thing the player *chose* and caused.
- **You have to act blind, not just walk blind.** The vault keypad gives each key its own pitch, so the code can be
  entered in the dark by ear — the same four digits the lit game asks for. Readers, bolts and drawers all answer.
- **You orient by what rooms sound like.** Every area already has its own room tone; in the dark that tone is the
  map. Testers who played the lit build know where they are before the Guide tells them.

And one thing that outlives the milestone: the same cues, exposed as an opt-in **Sound Cues** setting, are a real
accessibility feature for low-vision players in the *lit* game. That is worth a paragraph in the GDD and is the
honest answer to "meaningful."

## Who does what, so nobody waits

```
D first (the gate) ──> everyone else in parallel
   buses, API, spatial defaults,            A: table sounds
   cue discovery, validator,                B: back-of-house sounds
   music + ambience beds,                   C: Guide VO + UI sounds
   blackout mode, BLACKOUT RUN entry        (all three only need the API to exist)
                                                     │
                                            D runs round 3 testing ──> all fix
```

D's gate is the only blocking item. Until it lands you can still write your generator script and synthesise your
`.wav` files — that is pure Python and depends on nothing.
