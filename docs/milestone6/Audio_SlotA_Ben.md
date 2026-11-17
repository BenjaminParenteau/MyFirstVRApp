# Milestone 6 — Slot A (Ben): the sound of the games

**You own every noise a game makes.** Chips, cards, the wheel, the dice, the Plinko ball, the Mines tiles, the
cashier cage, and the stings that tell a player they just won or lost. You already own all the code that fires
these moments — this milestone is mostly one line per moment.

Read [README.md](README.md) once for the seven no-conflict rules. Everything below is yours alone; **no file in
this brief is shared with another slot**, and nothing here touches a scene file.

## Your namespace

`table.` and `cage.`. Nobody else may use those prefixes, and you may not use theirs. **High Stakes > Audio >
Validate Cues** enforces it.

## The cue list

Your call sites already exist — the right-hand column is the file and the moment. Volume and pitch are yours to
taste; the bus and 2D/3D column is the part that matters for spatial audio.

### Chips and cards

| Cue id | Fires when | Call it from | Bus | 3D? |
|---|---|---|---|---|
| `table.chip.clink` | A chip stack is grabbed or set down | `PressableButton` bet rows / chip grabbable | Sfx | 3D at the chip |
| `table.chip.bet` | A bet lands on a spot | `RouletteSpot`, `BlackjackPresenter` bet | Sfx | 3D at the spot |
| `table.chip.rake` | Losing bets are swept | each presenter's lose branch | Sfx | 3D at the table |
| `table.chip.payout` | Winnings are credited | where you call `IChipWallet.Add` | Sfx | 3D at the table |
| `table.card.deal` | One card slides out | `CardView` reveal | Sfx | 3D at the card |
| `table.card.flip` | The hole card turns over | `BlackjackPresenter` | Sfx | 3D at the card |
| `table.card.shuffle` | A new shoe | `BlackjackRules` reset | Sfx | 3D at the table |

Three or four variants per cue, picked at random, or a roomful of identical clinks will sound like a machine gun.
`SoundCue` takes a `clips[]` array and pitch jitter for exactly this — one cue, several wavs.

### The games themselves

| Cue id | Fires when | Call it from | Bus | 3D? |
|---|---|---|---|---|
| `table.roulette.spin` | Wheel starts (loop, stop it on settle) | `RoulettePresenter` | Sfx | 3D at the wheel |
| `table.roulette.ball` | Ball running the rim (loop, follows the ball) | `RoulettePresenter` | Sfx | `PlayOn` the ball |
| `table.roulette.drop` | Ball falls into the pocket | `RoulettePresenter` settle | Sfx | 3D at the pocket |
| `table.dice.roll` | Dice thrown | `DicePresenter` | Sfx | 3D at the table |
| `table.dice.settle` | Dice stop | `DicePresenter.Settle` | Sfx | 3D at the table |
| `table.plinko.peg` | Ball hits a peg — pitch up as it descends | `PlinkoPresenter` | Sfx | `PlayOn` the ball |
| `table.plinko.bucket` | Ball lands | `PlinkoPresenter.Settle` | Sfx | 3D at the bucket |
| `table.mines.safe` | A safe tile opens | `MinesPresenter` | Sfx | 3D at the tile |
| `table.mines.boom` | A mine | `MinesPresenter` | Sfx | 3D at the tile |
| `table.slots.idle` | Slot cabinets attract loop | runtime emitter, see below | Ambience | 3D, 6 m |

### Results and the cage

| Cue id | Fires when | Call it from | Bus | 3D? |
|---|---|---|---|---|
| `table.win.small` | Any win under 10× | `TableHologram` win | Sfx | 3D at the hologram |
| `table.win.big` | 10× and over, or a natural blackjack | `TableHologram` | Sfx | 3D at the hologram |
| `table.lose` | Dealer wins / bust | `TableHologram` | Sfx | 3D at the hologram |
| `table.push` | Push | `TableHologram` | Sfx | 3D at the hologram |
| `cage.buyin` | Chips handed out | cashier cage | Sfx | 3D at the window |
| `cage.cash.out` | Cash-out confirmed | cashier cage | Sfx | 3D at the window |
| `cage.bell` | Cage attendant bell / attract | cage emitter | Ambience | 3D, 8 m |

**Do not** sound the Pit Boss's lines or his speech bubble — that is Pak's `vo.` namespace. **Do not** sound the
keycard he hands you (`item.pickup`, Ashton's).

### One line you own on everyone else's behalf

`PressableButton.cs` is **your** file, in `Scripts/TableGames/`, and it is also the button component behind Pak's
notebook tabs and watch rows, Ashton's keypads and the start screen — 13 files across the other three slots. If
each of us added our own `Play` call to it we would all be editing the same method on the same line. So you add
**one** line and the rest of us stay out:

```csharp
Soundscape.Play("ui.press");   // Pak's namespace, Pak's clip — your file, so your line
```

`ui.` is Pak's namespace and she authors the clip; you just own the call site because you own the file. Anyone who
wants a *different* sound on a specific button gets it from a cue-id field on the component, set per prefab — not
from a branch inside your file. Same rule in reverse protects you: Pak owns the one line in her `SpeechBubble.cs`
even when it's your Pit Boss using it.

## Your files

Everything you create for this milestone, and nothing outside this list:

```
tools/audio/generate_tables_audio.py              your generator (new, yours)
Assets/Content/Tables/Audio/*.wav                 your generated clips (+ .meta)
Assets/Content/Tables/Resources/Audio/
    CueSet_Tables.asset                           your cue set, auto-discovered at runtime
Assets/Scripts/TableGames/Editor/
    TableAudioBuilder.cs                          High Stakes > Audio > Build Table Audio
Assets/Scripts/TableGames/**                       the one-line Play calls in your own presenters
Assets/Scripts/Economy/ChipWallet.cs              only if you want a wallet-change cue
Progress_Vol1.md                                   your Slot A log sub-section only
docs/GameDesignDocument.md                         the table-games sentence in Sound Design, your row only
```

## How to start

**You are not blocked on D.** Steps 1–2 are pure Python and need nothing from anyone.

1. **Synthesise your clips.** Copy the shape of `tools/audio/generate_environment_audio.py` and import the shared
   helpers — `from synth import click, noise_burst, tone, metallic, save_wav, rand_variants`. A chip clink is a
   short filtered noise burst with a metallic ring on top; a card deal is a 60 ms noise sweep; the roulette ball is
   a looping rattle whose rate you drive from code. Write 3–4 variants of anything that repeats.
   ```
   python tools/audio/generate_tables_audio.py
   ```
   `tools/audio/synth.py` is Slot D's and is **append-only** — need a primitive it doesn't have, ask Zach and he
   adds it rather than you editing it.
2. **Listen to them outside Unity first.** Much faster than a Play-mode loop. They are plain 48 kHz wavs.
3. **Write `TableAudioBuilder.cs`** — one Editor menu that imports each wav with the Quest settings
   (`AudioImporter`: force-mono for anything 3D, Vorbis for loops, ADPCM for one-shots, `loadInBackground`) and
   writes `CueSet_Tables.asset`. Crib the import block from `Environment/Editor/AudioBuilder.cs`; copy it, don't
   call into it.
4. **Add the `Play` calls** to your presenters, one line each:
   ```csharp
   using HighStakes.Environment.Audio;
   Soundscape.PlayAt("table.chip.bet", spot.transform.position);
   ```
   It no-ops safely if no `Soundscape` is loaded, so your EditMode tests and `MVP_Tables` on its own are unaffected.
5. **Slot-machine and cage loops** are the one case where you need a looping emitter. Do **not** put an
   `AudioSource` in `MVP_Casino` (rule 1) — write a tiny component in your folder that calls
   `Soundscape.Loop("table.slots.idle", transform)` in `OnEnable` and `handle.Stop()` in `OnDisable`, and have
   `VipRoomBuilder` / your table builders attach it to the cabinets they already generate.
6. **Validate and listen:** **High Stakes > Audio > Validate Cues**, then play `MVP_Casino` in the simulator and
   walk the floor.

## Your part in the blackout mode

The player will be crossing the casino floor with the lights dead, and **your tables are the landmarks.** You don't
build the mode — Zach does — but two things are yours:

- **Your attract loops are the map.** `table.slots.idle` and `cage.bell` are how a blind player knows which end of
  the room they are in. Make sure every cabinet and the cage actually carry one, and keep them quiet enough to live
  under the music in the lit game (they'll be mixed up automatically in the dark).
- **A table must be identifiable by ear.** Standing at roulette should not sound like standing at blackjack. A
  faint distinct idle per table type — the wheel's bearing, a riffle of chips — is enough, and it is the difference
  between the blackout being navigable and being a maze.

Optional, if you have time and want the credit: make one table **playable** in the dark. Blackjack is the
candidate — HIT and STAND are two buttons, and if each announces itself when the ray crosses it and the hologram's
result is spoken rather than shown, a tester can play a hand blind. That is a strong moment for the video.

## Definition of done

- Every moment in the cue list above makes a sound, in the right place in 3D, in the simulator and on an APK.
- Nothing repeats identically twice in a row (variants + pitch jitter on everything that fires often).
- **High Stakes > Audio > Validate Cues** passes with no `table.`/`cage.` findings.
- Standing at each table with your eyes shut, you can tell which table it is.
- No scene file in your commit. No file outside the list above in your commit.
- Your line in the Slot A log, and the table-games sentence in the GDD's Sound Design section.

## Round 3 user testing (your part)

Zach runs the sessions; you get asked two questions about your area, so build for them:

- *"Without looking at the hologram, did you know whether you won?"* — if testers check the screen to find out, the
  win/lose stings are not doing their job.
- *"Was anything too loud or annoying after ten minutes?"* — repetitive one-shots are the usual offender, which is
  what the variants are for.

Log fixes as `Playtest R3 → change` under Decisions in `Progress_Vol1.md`.
