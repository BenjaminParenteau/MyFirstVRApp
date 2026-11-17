# Milestone 6 — Slot C (Pak): the voice, the interface, and the mix

**You own every sound with a voice in it or a finger on it** — the Guide, the notebook, the watch, every button
press and page turn — plus the **AUDIO settings page** and, because it is your page, the final call on how the
whole game is balanced.

## First, a note about the team split

The team table in `Progress_Vol1.md` lists **"sound design"** under Slot C — i.e. all of it, yours. For this
milestone we have split it four ways instead, and you should know why before you read the rest: there are roughly
sixty cues across four people's systems, and the person who owns a table's code is the only one who knows the frame
on which its sound should fire. One person authoring all sixty would mean editing all four slots' files, which is
the one thing our merge rules exist to prevent.

So the *authoring* is split, but **the creative lead on sound is still yours**:

- The **mix** is yours. You own the bus volumes, the defaults everyone ships with, and the right to tell any of us
  that our cue is too loud or too frequent. Say it in Open questions and it gets changed.
- The **palette** is yours. If you want the whole game warmer, darker, more muffled, say so and we retune our
  generators.
- The **voice** is entirely yours — nobody else writes a `vo.` cue.

If you would rather own more of it than this brief gives you, say so and we'll move the line.

Read [README.md](README.md) once for the seven no-conflict rules. Everything below is yours alone; **no file in
this brief is shared with another slot**, and nothing here touches a scene file.

## Your namespace

`ui.` and `vo.`. Nobody else may use those prefixes, and you may not use theirs. **High Stakes > Audio >
Validate Cues** enforces it.

## The cue list

### The Guide (your biggest win this milestone)

The Guide's seven lines currently play **placeholder macOS `say` audio** (`Assets/Content/UI/Guide/Audio/`,
`GuideAudioImporter.cs`). He is the only character in the game with no body, the narrative spine of the whole
heist, and he currently sounds like a screen reader. Replacing those seven clips with a real performance is the
single highest-impact audio change available to anyone on this team — it costs no code at all, because
`GuideVoice` already plays whatever clip the `GuideLine` points at.

| Cue / clip | Line | Bus | 3D? |
|---|---|---|---|
| `vo.guide.first_contact` | First contact, on the first chip change | Voice | **2D** — he is in your ear |
| `vo.guide.tier_1` / `tier_2` | On each tier-up | Voice | 2D |
| `vo.guide.vip_card` / `staff_card` / `shift_note` | On key pickups | Voice | 2D |
| `vo.guide.back_of_house` | Entering the security hall | Voice | 2D |
| `vo.guide.blackout.*` | The blackout set — see [below](#your-part-in-the-blackout-mode) | Voice | 2D |
| `vo.pitboss.*` | The Pit Boss's lines (he has a body — these are 3D) | Voice | **3D** at his head |

Record them yourself, or get someone with a good mic to; a phone in a quiet room beats synthesis for voice every
time. Keep the ids and the file names identical and `GuideVoice` picks them up with no code change.

The Guide is **2D on purpose** — an earpiece should not pan when you turn your head. The Pit Boss is 3D for the
same reason inverted: you should be able to hear which way he is.

### Interface

| Cue id | Fires when | Call it from | Bus | 3D? |
|---|---|---|---|---|
| `ui.press` | Any `PressableButton` | **Ben's `PressableButton.cs`** — see the note below | Sfx | 2D |
| `ui.press.deny` | A disabled row is pressed | same | Sfx | 2D |
| `ui.notebook.open` / `.close` | Notebook opens / closes | `NotebookOpen` | Sfx | 2D |
| `ui.notebook.page` | Page or tab change | `NotebookPages`, `NotebookRow` | Sfx | 2D |
| `ui.notebook.item` | An item is taken out / put back | `NotebookCaseFile` | Sfx | 2D |
| `ui.watch.raise` | Watch face lights up | `WristRaiseVisibility` | Sfx | 2D |
| `ui.watch.alert` | New item / new lead pulse | `GuideCommLink` | Sfx | 2D |
| `ui.travel` | A fast-travel row is pressed | `FastTravelWatch` | Sfx | 2D |
| `ui.start.select` / `.confirm` | Start-screen console rows | `StartScreen` | Sfx | 2D |
| `ui.lead.written` | The notebook writes down a Guide lead | `LeadsLogic` caller | Sfx | 2D |

All 2D: interface sound belongs to the player, not to the room. The one exception is if you decide the notebook
should sound like it is in your hands rather than in your head — your call, and the cue supports it.

**`ui.press` is the one cue you author but don't call.** `PressableButton.cs` lives in Ben's folder
(`Scripts/TableGames/`) and is the button behind your notebook tabs, your watch rows, Ashton's keypads and the
start screen — if each of us added a `Play` call to it, three of us would be editing the same line. So **Ben adds
the single line** `Soundscape.Play("ui.press")` in his file, and **you author the clip** — the namespace and the
sound stay yours, only the call site is his. If you want a *different* sound on a specific button, it comes from a
cue-id field on the component set per prefab, not from an edit to his file.

The mirror of that rule protects you: **you** own the one line in your own `SpeechBubble.cs`, even though Ashton's
puzzles and Ben's Pit Boss both use it. If either of them wants a different bubble sound, they ask you or set a
field — they don't edit `Dialogue/`.

### The AUDIO settings page (yours alone)

The notebook SETTINGS spread is your file (`Notebook/Logic/SettingsLogic.cs`, `NotebookSettings.cs`). It needs an
**AUDIO** section with four rows, one per bus:

| Row | Bus | Default |
|---|---|---|
| MUSIC | `Music` | 0.7 |
| AMBIENCE | `Ambience` | 0.8 |
| EFFECTS | `Sfx` | 1.0 |
| VOICE | `Voice` | 1.0 |

Wire each to `Soundscape.SetBusVolume(bus, v)` / `GetBusVolume(bus)`. Volumes persist in PlayerPrefs automatically.
Defaults above are a starting guess from Zach — **they are yours to set**, and round 3 will argue with them.

Also yours, and worth doing: a **SOUND CUES** toggle (default off) that keeps the blackout mode's navigation aids
running in the *lit* game. It started as a dark-mode mechanic and turns out to be a genuine low-vision
accessibility option — see the README's last section. It is one row on your page and it is the strongest thing any
of us can put in the GDD about "considering nonvisual elements."

## Your files

```
tools/audio/generate_ui_audio.py                  your generator (new, yours)
Assets/Content/UI/Audio/*.wav                     your generated UI clips (+ .meta)
Assets/Content/UI/Guide/Audio/*.wav               the Guide's real VO — replaces the `say` placeholders
Assets/Content/UI/Resources/Audio/
    CueSet_UI.asset                               your cue set, auto-discovered at runtime
Assets/Scripts/UI/Editor/UiAudioBuilder.cs        High Stakes > Audio > Build UI Audio
Assets/Scripts/UI/**                               the one-line Play calls, and the AUDIO settings rows
Assets/Scripts/Dialogue/**                         GuideVoice, GuideLine, GuideTrigger, SpeechBubble — all yours
Progress_Vol1.md                                   your Slot C log sub-section only
docs/GameDesignDocument.md                         the UI/voice sentences in Sound Design, your row only
```

## How to start

**Only step 5 needs D's gate; everything else you can do today.**

1. **The Guide's real voice, first.** Seven clips, same names, same folder, drop them in, re-run
   **High Stakes > UI > Build Guide Voice**. No code. Biggest payoff in the milestone.
2. **Synthesise the UI clips:**
   ```
   python tools/audio/generate_ui_audio.py
   ```
   `from synth import click, tone, chime, paper, save_wav`. Your palette is small and soft: a brass button is a
   short click with a woody body, a page turn is filtered noise with a rustle envelope, the watch alert is a
   two-note chime. UI sound should be quiet and quick — anything over 200 ms will grate by the tenth press.
   `tools/audio/synth.py` is Zach's and **append-only** — ask rather than edit.
3. **Write `UiAudioBuilder.cs`**, importing each clip with the Quest settings and writing `CueSet_UI.asset`. Copy
   the import block out of `Environment/Editor/AudioBuilder.cs`.
4. **Add the `Play` calls** — `PressableButton` is the big one; sounding it once covers most of the game's buttons:
   ```csharp
   using HighStakes.Environment.Audio;
   Soundscape.Play("ui.press");
   ```
5. **The AUDIO page rows** against `Soundscape.SetBusVolume`. Add four `SettingsLogic` rows the same way the COMFORT
   rows work, plus tests — your existing `SettingsLogicTests` pattern covers it.
6. **Validate:** **High Stakes > Audio > Validate Cues**, then press everything in the simulator.

## Your part in the blackout mode

**In the dark, your voice is the entire interface.** Subtitles are invisible, the notebook is invisible, the watch
is invisible — the Guide is all the player has. That makes this the most important slice in the dark mode after the
mode itself.

Write and record a **blackout set** of Guide lines (`vo.guide.blackout.*`):

| When | What he does |
|---|---|
| The lights die | Reacts — this is the moment the player knows it was deliberate, not a bug |
| First 10 s | Teaches the mechanic in character: that the player can *ping* for a read on the room |
| Orientation | Gives a bearing in words, not a marker: "the cage is behind you, bar on your left" |
| Stuck for 30 s | Nudges, escalating, without ever just saying "go north" |
| Near the objective | Confirms by ear so the player knows they're close |
| Out | Pays it off |

Two rules for this set:

- **He may describe, but never navigate for the player.** The moment he says "turn left now", the mode stops being
  about sound and becomes about following instructions. He is a man watching bad camera feeds, not a GPS.
- **Keep the lines short.** A 6-second line is 6 seconds of masking everything the player is straining to hear.

Also yours: the watch and notebook should **refuse gracefully** in the dark. A player will raise the watch out of
habit; `ui.press.deny` plus one Guide line ("screen's dead too") is better than silence, and better than a glowing
watch face that ruins the darkness. **Important:** nothing of yours may emit light during the blackout — the watch
hologram, the notebook pages and the start console are all emissive, and if they stay lit the mode is broken. Zach's
`BlackoutMode` switches off renderers it knows about; tell him which of your objects need to be in that list, or
subscribe to `BlackoutMode.Changed` from your own component and hide them yourself.

## Definition of done

- The Guide's seven existing lines are a real performance, not `say`.
- Every row in the interface cue list makes a sound; nothing is louder or longer than it needs to be.
- The AUDIO page's four bus rows work and persist across a restart; the SOUND CUES toggle exists.
- The blackout set is written, recorded and triggered; nothing of yours glows in the dark.
- **High Stakes > Audio > Validate Cues** passes with no `ui.`/`vo.` findings.
- No scene file in your commit. No file outside the list above.
- Your line in the Slot C log, and the UI/voice sentences in the GDD's Sound Design section.

## Round 3 user testing (your part)

- *"Did the Guide feel like a person?"* — the question the placeholder voice has been failing for two milestones.
- *"Was the interface quieter or louder than you wanted?"* — you own the fix either way.
- *"In the dark, did you ever feel abandoned?"* — if yes, the stuck-nudge line needs to come sooner.

Log fixes as `Playtest R3 → change` under Decisions in `Progress_Vol1.md`.
