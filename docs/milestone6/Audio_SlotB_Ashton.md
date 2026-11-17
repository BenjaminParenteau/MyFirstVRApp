# Milestone 6 — Slot B (Ashton): the sound of locks, security and the back of house

**You own every noise that means "this is locked", "you are allowed", or "you are somewhere you shouldn't be".**
Readers, keypads, bolts, drawers, the wire panel, the CCTV wall, the vault, the lockdown alarm, and the pickup
sound every collectable in the game uses.

You also own the single most important sound in the blackout mode: **the vault keypad's per-key tones.** Without
them the dark mode is only walking; with them the player can actually *act* blind. See
[below](#your-part-in-the-blackout-mode).

Read [README.md](README.md) once for the seven no-conflict rules. Everything below is yours alone; **no file in
this brief is shared with another slot**, and nothing here touches a scene file.

## Your namespace

`sec.` and `item.`. Nobody else may use those prefixes, and you may not use theirs. **High Stakes > Audio >
Validate Cues** enforces it.

## The cue list

Every call site below is a file you already own.

### Access: readers, cards, doors

| Cue id | Fires when | Call it from | Bus | 3D? |
|---|---|---|---|---|
| `sec.reader.ok` | Card accepted, light goes green | `KeycardReader` | Sfx | 3D at the reader |
| `sec.reader.deny` | Card rejected / no card | `KeycardReader` | Sfx | 3D at the reader |
| `sec.reader.idle` | Reader standby blip, every few seconds (loop) | runtime emitter, see step 5 | Ambience | 3D, 5 m |
| `sec.bolt.unlock` | A door unlocks | `KeycardAccess` / `DoorPortal` grant | Sfx | 3D at the door |
| `sec.bolt.locked` | Player tries a locked door | your locked branch | Sfx | 3D at the door |
| `sec.door.heavy` | A steel back-of-house door swings | your door code | Sfx | 3D at the door |
| `item.pickup` | **Any** collectable goes into the notebook | `Keycard`, `KeycardGiver`, note pickups | Sfx | 3D at the item |
| `item.pickup.key` | A keycard specifically (more important) | `Keycard` | Sfx | 3D at the item |

`item.pickup` is used by Pak's notebook and Zach's note props too — it is yours to author, theirs to call. Keep the
id stable once it ships.

### Keypads and puzzles

| Cue id | Fires when | Call it from | Bus | 3D? |
|---|---|---|---|---|
| `sec.keypad.key.1` … `.0` | **One cue per digit, each a distinct pitch** | `VaultKeypad`, `BarPuzzle` | Sfx | 3D at the pad |
| `sec.keypad.ok` | Correct code | `VaultKeypad`, `BarPuzzle` | Sfx | 3D at the pad |
| `sec.keypad.bad` | Wrong code | `VaultKeypad`, `BarPuzzle` | Sfx | 3D at the pad |
| `sec.keypad.lockout` | Three wrong → 15 s lockdown | `VaultKeypad` | Sfx | 3D at the pad |
| `sec.wire.seat` | A plug seats in a socket | `WirePlug` | Sfx | 3D at the socket |
| `sec.wire.wrong` | Plug in the wrong socket | `WirePlug` | Sfx | 3D at the socket |
| `sec.wire.solved` | Panel complete | `WirePanelPuzzle` | Sfx | 3D at the panel |
| `sec.breaker.throw` | The breaker is pulled — **this is the power cut** | `WirePanelPuzzle` | Sfx | 3D at the panel |
| `sec.tape.insert` | A tape goes in the deck | `VideoTape` | Sfx | 3D at the deck |
| `sec.tape.play` | Deck running (loop) | `TapeDeckPuzzle` | Sfx | 3D at the deck |
| `sec.printer` | The CAM 04 still prints | `TapeDeckPuzzle` | Sfx | 3D at the printer |

The **ten digit tones** are the one thing in this brief that is not optional. Use a clean musical interval set
(e.g. a pentatonic run) so the four-digit vault code becomes a little melody — players will remember `5813` as a
tune, which is exactly the "nonvisual element" the milestone is asking for.

### Back-of-house atmosphere and alarms

| Cue id | Fires when | Call it from | Bus | 3D? |
|---|---|---|---|---|
| `sec.cctv.hum` | The monitor wall (loop) | `SecurityMonitorWall` | Ambience | 3D, 8 m |
| `sec.cctv.static` | A feed cuts to NO SIGNAL | `SecurityMonitorWall` + `CctvOutage` | Sfx | 3D at the wall |
| `sec.radio.chatter` | Distant guard radio, occasional (loop) | runtime emitter in the hall | Ambience | 3D, 10 m |
| `sec.fluorescent` | Strip-light buzz (loop) | runtime emitter per room | Ambience | 3D, 4 m |
| `sec.vault.door` | The treasury door motion | `VaultDoorMotion` | Sfx | 3D at the door |
| `sec.vault.hum` | Vault electrical hum (loop) | runtime emitter | Ambience | 3D, 10 m |
| `sec.alarm.lockdown` | Lockdown (loop) | `VaultKeypad` lockout | Sfx | 3D, 20 m |
| `sec.counting` | Bill counter in the count room (loop) | runtime emitter | Ambience | 3D, 5 m |

**Do not** author the room tone beds themselves (`amb.back_of_house`, `amb.vault`) — those are Zach's, and yours
layer on top of them. If a bed and your emitter fight, say so in Open questions rather than turning yours up.

**Two files you use but must not add sound to.** Your keypads are built on Ben's `PressableButton.cs`
(`Scripts/TableGames/`) and your puzzle feedback uses Pak's `SpeechBubble.cs` (`Scripts/Dialogue/`). Three of us
use each of those, so the rule is **the owner of the file adds the one line** — Ben does `ui.press`, Pak does her
bubble. You get the generic press sound for free. Your **per-digit keypad tones are different and are yours**:
they fire from `VaultKeypad` / `BarPuzzle`, which are your files, in response to *which* key was pressed — that
is your logic, not the button component's.

## Your files

```
tools/audio/generate_security_audio.py            your generator (new, yours)
Assets/Content/Progression/Audio/*.wav            your generated clips (+ .meta)
Assets/Content/Progression/Resources/Audio/
    CueSet_Security.asset                         your cue set, auto-discovered at runtime
Assets/Scripts/Security/Editor/
    SecurityAudioBuilder.cs                       High Stakes > Audio > Build Security Audio
Assets/Scripts/Security/**                         the one-line Play calls in your own files
Assets/Scripts/Inventory/**                        same
Progress_Vol1.md                                   your Slot B log sub-section only
docs/GameDesignDocument.md                         the security sentence in Sound Design, your row only
```

Note `Assets/Content/Progression/Resources/` — a `Resources` folder already exists next to your vault dressing, so
this pattern is nothing new in your area.

## How to start

**You are not blocked on D.** Steps 1–2 are pure Python.

1. **Synthesise your clips:**
   ```
   python tools/audio/generate_security_audio.py
   ```
   Import from the shared helpers — `from synth import tone, click, noise_burst, hum, buzz, save_wav`. Your palette
   is mostly electrical and mechanical: a reader beep is a 2 kHz sine blip, a deny is two low square blips, a bolt
   is a noise burst with a thunk, a hum is a 50/100 Hz stack with a slow wobble, fluorescent buzz is 120 Hz plus
   hiss. `tools/audio/synth.py` is Zach's and **append-only** — ask rather than edit.
2. **Do the ten digit tones first** and listen to them as a sequence. If `5813` doesn't sound like a phrase, retune.
3. **Write `SecurityAudioBuilder.cs`** — your Editor menu that imports each wav and writes
   `CueSet_Security.asset`. Don't write the import settings yourself; **call Slot D's shared helper**
   (`Environment/Audio/Editor/AudioImport.cs`, append-only — ask rather than edit):
   ```csharp
   AudioImport.Apply(assetPath, AudioImport.Kind.OneShot);   // or .Loop for your hums, .Bed for long tones
   ```
4. **Add the `Play` calls**, one line each:
   ```csharp
   using HighStakes.Environment.Audio;
   Soundscape.PlayAt("sec.reader.ok", reader.transform.position);
   ```
   Safe no-op when no `Soundscape` is loaded, so your existing play checks
   (`BarPuzzlePlayCheck`, `SecurityOfficeVerification`) keep passing untouched.
5. **Looping emitters** — rule 1 says no `AudioSource` in a scene, and your rooms are generated anyway, so this is
   natural for you: write one small component in your folder that loops a cue while enabled
   (`Soundscape.Loop(id, transform)` in `OnEnable`, `handle.Stop()` in `OnDisable`) and have
   `SecurityOfficeBuilder` / `VaultDressingBuilder` attach it to the racks, strip lights, counters and vault door
   they already generate. Re-running your builders then re-places the sound automatically, which is the whole
   reason we don't put it in scenes.
6. **Validate:** **High Stakes > Audio > Validate Cues**, then walk the back of house in the simulator.

## Your part in the blackout mode

Yours is the half that makes the dark mode a *game* rather than a walk. Three deliverables:

1. **The breaker is the trigger — and ✅ you don't have to do anything for it (updated 2026-11-17).** This
   originally asked you to expose `WirePanelPuzzle`'s solved state. **You don't need to:** solving it already calls
   `CctvOutage.Cut(cutFeedScene)`, which raises the public static `CctvOutage.WentDown` event, and that is a
   complete signal that the breaker has been thrown. `BlackoutTrigger` listens to that from Slot D's folder, so
   **`WirePanelPuzzle.cs` needs no change, and no new contract had to be agreed.** The only thing left here is the
   sound: `sec.breaker.throw` is the noise of the lights dying and deserves to be the best clip you make.
2. **The keypad must be playable blind.** Per-key tones, a spoken-pitch confirmation per press, and `sec.keypad.ok`
   / `.bad` loud and unmistakable. A tester who learned the code in the lit game should be able to enter it in the
   dark on the first try. This is the milestone's "act by sound" requirement, and it lives in your file.
3. **Readers and doors answer in the dark.** `sec.reader.idle` is how a blind player *finds* a reader at all — a
   quiet blip every 3–4 s, audible from 5 m. Same for the vault door's hum. In the dark these are promoted
   automatically (the blackout mixes ambience up and music down), so you don't need a special case; you just need
   them to exist.

Optional and high-value for the video: a **lockdown chase** in the dark. Three wrong codes already trigger a 15 s
lockdown with blinking lamps — in the blackout those lamps are invisible, so `sec.alarm.lockdown` becomes the only
signal, and the player has to stand still and wait it out by ear.

## Definition of done

- Every moment in the cue list makes a sound, positioned in 3D, in the simulator and on an APK.
- The ten digit tones are distinct and ordered, and the vault code is enterable with the screen off.
- `WirePanelPuzzle` exposes its solved state so Zach's trigger can watch it without editing your file.
- Every emitter is attached by your builders at runtime — **no `AudioSource` in `MVP_SecurityOffice`,
  `MVP_CameraRoom`, `MVP_SecurityHall` or `MVP_Vault`**, and no scene file in your commit.
- **High Stakes > Audio > Validate Cues** passes with no `sec.`/`item.` findings.
- Your line in the Slot B log, and the security sentence in the GDD's Sound Design section.

## Still outstanding from Milestone 4 (not audio, but it blocks testing)

The three lightmap bakes — `MVP_SecurityOffice`, `MVP_CameraRoom`, `MVP_SecurityHall` — are still open and Zach
still can't do them (no GPU). Round 3 of user testing runs on the same build as the audio, and the blackout mode is
only meaningful if the *lit* version of those rooms looks finished first. Please land the bakes before the round 3
APK.

## Round 3 user testing (your part)

- *"When a door refused you, did you know why?"* — `sec.reader.deny` vs `sec.bolt.locked` should mean different
  things to the player, not just to us.
- *"In the dark, could you enter the vault code?"* — the headline question for your slice. If the answer is no, the
  digit tones need to be further apart.

Log fixes as `Playtest R3 → change` under Decisions in `Progress_Vol1.md`.
