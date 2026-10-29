# Progress — Vol. 1

Shared project memory for the whole team. **Every commit must update this file** (see [CLAUDE.md](CLAUDE.md#progress-log-rule)).

- **Assignment:** Vol. 1 — "High Stakes" VR casino-heist game (15-week assignment). Design reference: [docs/GameDesignDocument.md](docs/GameDesignDocument.md).
- **Next assignment:** start a new file, `Progress_Vol2.md`, and leave this one as an archive. Update the pointer in CLAUDE.md.
- **Personal notes:** each member keeps a git-ignored `Personal.md` locally (copy [docs/Personal.template.md](docs/Personal.template.md) to `Personal.md` in the repo root).

## Team split

Four members, each owning distinct script folders so merges stay clean.

| Slot | Member | Owns (code) | Owns (content / deliverables) |
|---|---|---|---|
| A | Ben (Benjamin Parenteau) | `Scripts/TableGames/`, `Scripts/Economy/` | Chip wallet, cashier cage, grabbable chips, one table interaction (later: full blackjack, roulette) |
| B | Ashton (Ashton Calkins) | `Scripts/Inventory/`, `Scripts/Security/` | Character-tier progression logic, keycards and access gates (later: guard patrols, suspicion meter, vault passcode puzzle) |
| C | Pak (Pakrinha Sim) | `Scripts/UI/`, `Scripts/Dialogue/` | Diegetic UI, **character evolution visuals** (hands, wrist, accessories), the Guide's voice lines, sound design |
| D | Zach (Zachary Scheer) | `Scripts/Environment/`, `Scripts/Core/`, locomotion, scenes, builds | **Shared style kit**, casino floor / VIP / back-of-house layout, XR Origin & comfort, Quest 3 builds, playtests, GDD upkeep |

Rules: see [Merge-conflict rules](#merge-conflict-rules) under the Milestone 2 plan below.

### Team hardware (affects how each person tests)

| Member | Slot | Dedicated GPU | How they test on the headset |
|---|---|---|---|
| Zach | D | **No** | Standalone Android **APK** on Quest 3 + XR Interaction Simulator in Play mode. Cannot use Quest Link. |
| Pak | C | **No** | Same as Zach: APK + simulator. Cannot use Quest Link. |
| Ashton | B | Yes | Quest Link, APK, or simulator. |
| Ben | A | Yes | Quest Link, APK, or simulator. |

Consequences: the **standalone Android build is the primary target and test path** (it is also the real Quest 3 performance test); Quest Link / PC VR is a secondary, optional path that only Ashton and Ben can use. Shared lightmap bakes are done by Ben or Ashton, never Zach or Pak. See [docs/StyleGuide.md §9](docs/StyleGuide.md#9-testing-without-a-gpu).

## Milestone 2 — MVP prototype (current assignment)

**Assignment (CLC, group):** implement and test the game's core features **on the headset** to catch platform-compatibility issues early. The prototype must include **basic player movement, environment interaction, and user-interface navigation**. Also address: *what storytelling, worldbuilding and mechanics can be communicated through the evolutionary design of the character?*

**Submit:**
1. The GDT-120 Game Design Document with a complete **Platform Compatibility Design** section (in the Tasks Breakdown section).
2. **One individual video per member** (core concept, storyline, characters, unique features; clean setting, professional dress; can be recorded in-headset or cast to laptop). Every member's video link goes in the GDD's team table.
3. Solid academic writing (APA not required). Uses a rubric — review it before starting. No LopesWrite submission.

### MVP focus and what is out of scope

The assignment grades three things on the headset — **basic movement, environment interaction, UI navigation** — plus the question **"what storytelling, worldbuilding and mechanics can be communicated through the evolutionary design of the character?"**. That question is the centerpiece, so the MVP is built around one idea:

> **The player's own body evolves as their bankroll grows.** In first-person VR the "character" is what you see of yourself — hands, wrist, sleeves, accessories — plus what the world lets you do. As chips rise, the hero visibly upgrades (plain gloves and a basic watch → tailored cuffs, VIP lanyard → gold card case and chip-count overlay) and new abilities unlock.

What each layer of that evolution communicates (this is the GDD answer, Slot C writes it up):
- **Storytelling:** the rise from unknown gambler to trusted insider is *shown*, not told; the Guide's first line plays on the first tier-up.
- **Worldbuilding:** the casino runs on status — staff and doors respond to how you look and what you carry, hinting at the hierarchy and the second economy beneath the floor.
- **Mechanics:** each tier unlocks something concrete (VIP door access, private chip-count overlay, faster cash-out) that gates areas and story beats.

**Out of scope for this MVP (do not build yet):** NPC characters and models (dealers, Pit Boss, guards, waitresses), guard patrol AI, suspicion meter, dialogue trees, full blackjack/roulette rules, vault puzzle. Anything that needs a person in the scene is represented by props or a voice only. These return in later weeks per the Weekly plan.

### MVP scope: four vertical slices

Each slice is playable **on its own in its own scene**, then D stitches them together. Nobody waits on anyone else to start (scripts can begin immediately; scene work starts after the Day-0 gate below).

| Slot | Slice | MVP definition of done (tested on Quest 3 via APK or Link, and in the simulator) | Meets assignment requirement |
|---|---|---|---|
| A (Ben) | **Chips & economy** | Chip wallet; a cashier cage prop where pressing a button hands out starting chips; chips are grabbable stacks; one simple table interaction — drop chips on a bet spot, press "deal", a self-dealing card sequence resolves win/lose, wallet updates. No dealer NPC. | Environment interaction |
| B (Ashton) | **Tier progression & access** | Tier definitions as a ScriptableObject (`Ordinary Night` → `Known High Roller` → `Valued Associate`, thresholds tunable); a `CharacterTier` component that watches the wallet and fires `TierChanged`; a grabbable keycard + door reader; a VIP door that opens when the tier is high enough; one clue note the player can pick up. | Environment interaction |
| C (Pak) | **UI & character evolution visuals** | Wrist display (chips + current tier); notebook holding the clue note; comfort-settings wrist panel (vignette, snap/smooth turn, teleport/smooth toggle); hand/wrist visuals that swap per tier (e.g. plain glove → watch + cuff → lanyard + gold card case); the Guide's first earpiece audio line on first tier-up; writes the GDD's evolutionary-character answer. All diegetic, all hand-tapped. | User-interface navigation + the evolutionary-design question |
| D (Zach) | **Movement, style kit & integration** | XR Origin with teleport + smooth locomotion + comfort hooks; the **shared style kit and `MVP_Template` scene** (see below); blocked-out casino floor, cashier cage area, and a VIP doorway; `MVP_Main` scene that additively loads A/B/C's scenes; Quest 3 build; **Platform Compatibility Design** section of the GDD. | Basic player movement + platform compatibility |

### Day-0 gate (Zach does this first; everyone else starts scripts now, scenes after)

Before anyone builds scene content, D merges **one PR tagged `day-zero`** containing the contracts and the style kit. Everyone then pulls and starts every scene from `MVP_Template`.

> **✅ Day-0 is merged (2026-09-30, tag `day-zero`). Start here, A/B/C:**
> 1. `git checkout main && git pull --rebase origin main` (fetch tags too: `git fetch --tags`). Open the project in Unity `6000.3.23f1` exactly.
> 2. Copy `docs/Personal.template.md` to `Personal.md` in the repo root and fill it in.
> 3. Make your branch: `slot-<a|b|c>/<topic>` (e.g. `slot-a/chips`). Don't work on `main` or on a branch named after yourself.
> 4. Duplicate `Assets/Scenes/MVP/MVP_Template.unity` into your own scene (`MVP_Tables` / `MVP_Progression` / `MVP_UI`) and follow [StyleGuide §5d](docs/StyleGuide.md#5d-how-to-start-your-scene): build only inside your `<Area>_Content` object and your zone (§5b), never add a second XR Origin (§5c).
> 5. Code against `Assets/Scripts/Core/Contracts/` with a mock in your own folder until the real implementation lands. Test with the simulator ([docs/SimulatorControls.md](docs/SimulatorControls.md)), then an APK.
> 6. Every commit: one line at the top of **your slot's** Log sub-section below.

**1. Contracts** — `Assets/Scripts/Core/Contracts/`, interfaces and event channels only, no logic:
- `IChipWallet` — `int Balance`, `bool TrySpend(int)`, `void Add(int)`, `event Action<int> BalanceChanged` (A implements; B and C subscribe).
- `ICharacterTier` — `TierDefinition Current`, `event Action<TierDefinition> TierChanged` (B implements; C's visuals and wrist display subscribe; door readers query it).
- `IInventoryEvents` — `event Action<ItemData> ItemAdded` (B implements; C's notebook subscribes).
- `IGuideVoice` — `void Play(string lineId)` (C implements; B calls it on tier-up).

Until a real implementation lands, each consumer works against a throwaway mock in its own folder and swaps when it merges. Contracts are append-only after Day 0; changing one needs a note in Open questions and all four owners' OK.

**2. Shared style kit** — how we make sure every environment and scene looks the same *before* we begin. Owned by D, read-only for A/B/C, delivered under `Assets/Content/StyleKit/`:

| Piece | What it locks down |
|---|---|
| **Palette + mood** | Taken from [docs/reference/VisualReference.md](docs/reference/VisualReference.md): main floor = warm gold, brass, green felt, dark wood, deep red carpet; back-of-house/vault = cold blue-teal, concrete, steel. Stored as named colours in the kit. |
| **Materials** | One set of URP materials (floor carpet, felt, wood, brass, concrete, steel, fabric, chip plastic) using Quest-friendly shaders (Simple Lit / baked). Everyone uses these. If you need a new one, make a **Material Variant** of a kit material inside your own `Content/` folder — no new base materials. |
| **Lighting** | One shared Lighting Settings asset, one shared post-processing Volume Profile, one skybox, plus two light prefabs: `Light_WarmFloor` and `Light_CoolCorridor`. Lighting is baked; at most 1–2 realtime lights. |
| **Scale + grid** | 1 unit = 1 m. Snap to 0.5 m. Floor tile 2×2 m, corridor width 2 m, door 1.0 × 2.1 m, table top 0.75 m, ceiling 3 m. A `ScaleReference` prefab (1.8 m figure, door, table) is in the template for checking. |
| **Modular greybox prefabs** | Floor tile, wall, doorway, ceiling, table, chair, chip, keycard, door reader — simple meshes with kit materials. Build environments only from these; art gets swapped in later without moving anything. |
| **`MVP_Template.unity`** | Scene with the shared lighting, volume, scale reference, spawn point and a test floor. **Every slice scene starts by duplicating this** (never `BasicScene` / `SampleScene`). Its XR rig is for solo testing and is disabled when the scene is loaded additively. |
| **Quest 3 budget** | Target 72 fps; keep visible triangles low (aim < 100k), few draw calls, no realtime shadows beyond the kit's, textures compressed (ASTC), no heavy post effects. D verifies the budget on device before the gate closes. |
| **`SharedStyleKit.cs`** | The scratch ScriptableObject in `Assets/Scripts/Environment/` gets activated (uncomment + wire) to hold references to the above, so scripts can load the kit rather than hard-coding assets. |

**PR visual check (everyone, before merging a scene change):** run the scene in the XR Interaction Simulator or as an APK on the headset (Quest Link only if you have a GPU), compare it against `VisualReference.md`, and confirm it uses only kit materials, kit lighting and the modular prefabs at the right scale. If the change adds meshes or lights, Ben or Ashton also does a quick Link check for framerate. Add "style check ✔" to the PR description.

Change requests for the style kit go to Zach via Open questions; he ships them as `stylekit-vN` so everyone knows when to pull.

### Merge-conflict rules

Unity's scene and prefab files are the main source of conflicts, so the split is designed around never having two people edit the same file.

1. **One owner per folder.** Code stays in `Assets/Scripts/<Area>/` as in the table above. Non-code content (prefabs, materials, audio, ScriptableObjects, textures) goes in `Assets/Content/<Area>/` — same owner as the matching Scripts folder — with areas `Tables`, `Progression`, `UI`, `Environment`, plus the shared `StyleKit` (D only). Only edit files in your own folders.
2. **One scene per person, never share a scene.** A: `Assets/Scenes/MVP/MVP_Tables.unity`, B: `MVP_Progression.unity`, C: `MVP_UI.unity`, D: `MVP_World.unity` and `MVP_Main.unity`. Slices are loaded **additively** by `MVP_Main`. Never edit another person's scene, and never edit `BasicScene` / `SampleScene` (vendored templates). Every scene starts as a duplicate of `MVP_Template` (see the Day-0 gate) and keeps its XR rig only for solo testing; D's `MVP_Main` provides the real one and slice scenes must disable theirs when loaded additively.
3. **Prefab-first.** Build things as prefabs inside your `Content/` folder and place them in your scene. Don't nest another slot's prefab inside yours; reference it via the contracts instead.
4. **Single-owner shared files (D only):** `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/*`, `Assets/XR/*` (including `OpenXRPackageSettings.asset`), `.gitattributes`, `.gitignore`, and `Assets/Scripts/Core/`. If you need a package, layer, tag, input action, or build setting, add a line to Open questions; D makes the change and announces it.
5. **New layers/tags/input actions** are the classic hidden conflict: D reserves them up front — layers `Interactable`, `PlayerBody`; tags `Chip`, `Keycard`, `Restricted`.
6. **Docs:** in `docs/GameDesignDocument.md` each member edits only their own section or table row (video link row = your own row; Platform Compatibility = D; evolutionary-character answer = C; chips/economy text = A; progression/access text = B). Never reflow or reformat sections you don't own — whitespace edits create conflicts.
7. **This file:** the Log is split into one sub-section per slot (below) so each member only ever appends to their own; the Weekly plan and Milestone 2 checklist rows are edited by the row's owner only.
8. **Git habits:** `main` is always the working, tested build; `dev` is the integration branch where everything is tested first. Work on a branch named `slot-<a|b|c|d>/<topic>`, open a PR into **`dev`** (never straight into `main`), `git pull --rebase origin dev` before pushing, commit small and often, and never force-push `main`. Always commit `.meta` files with their assets. Don't commit `Library/`, `Temp/`, `Logs/`, or `UserSettings/`. Use the pinned Editor version `6000.3.23f1`. Enable Unity's smart merge locally: `git config merge.unityyamlmerge.driver '"<UnityInstall>/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'`.
9. **Resolving a scene conflict:** never hand-merge scene YAML. Take one side, then redo the small change in the Editor.

### Milestone 2 checklist

| Item | Owner | Status |
|---|---|---|
| Day-0 gate merged: contracts + shared style kit + `MVP_Template` (tag `day-zero`) | D | ✅ (on-device fps check still pending, see Open questions) |
| Slice A: chips, cage and one table interaction on headset | A | 🟨 chip wallet, VIP room and full blackjack table done (in PRs); cashier cage and grabbable chips to do |
| Slice B: tier progression, keycard and VIP door on headset | B | ⬜ |
| Slice C: diegetic UI + character evolution visuals on headset | C | 🟨 wrist chip display built (needs headset check); notebook, comfort panel, tier visuals, Guide line to do |
| Slice D: movement + blocked-out world + `MVP_Main` integration | D | 🟨 `MVP_Main` + area scenes through doors done (checked in headless Play mode); comfort panel and teleport floors to do |
| All scenes visually consistent (PR style check passed against `VisualReference.md`) | All | ⬜ |
| Quest 3 build runs the integrated `MVP_Main` | D | ⬜ |
| GDD: Platform Compatibility Design section (Quest 3 specs, Unity `6000.3.23f1` + OpenXR, standalone Android build as primary with Meta Quest Link as optional secondary (two members have no GPU), URP, target framerate, changes needed for other platforms such as AndroidXR / PC VR) | D (all review) | ⬜ |
| GDD: "evolutionary character design" storytelling/worldbuilding/mechanics answer | C | ⬜ |
| Compatibility issues found and fixed (log in Decisions) | All | ⬜ |
| Individual video — Zachary Scheer, link in GDD | Zachary | ⬜ |
| Individual video — Ashton Calkins, link in GDD | Ashton | ⬜ |
| Individual video — Pakrinha Sim, link in GDD | Pakrinha | ⬜ |
| Individual video — Benjamin Parenteau, link in GDD | Benjamin | ⬜ |
| Rubric reviewed and GDD checked against it | All | ⬜ |

## Weekly plan

Mirrors the milestone table in the GDD. Status: ⬜ not started · 🟨 in progress · ✅ done.

| Week | Deliverable | Lead | Status |
|---|---|---|---|
| 1 | GDD draft (Storyline & Characters), concept videos | All | ⬜ |
| 2 | Environment concept art, casino floor layout | D | ⬜ |
| 3 | Free-roam movement prototype (XR Origin, teleport, smooth locomotion, comfort) | D | 🟨 |
| 4 | Blackjack prototype | A | ⬜ |
| 5 | Roulette prototype | A | ⬜ |
| 6 | Chip economy and cash-out | A | ⬜ |
| 7 | Keycard and inventory system | B | ⬜ |
| 8 | Guard patrol AI and suspicion meter | B | ⬜ |
| 9 | Vault passcode puzzle | B | ⬜ |
| 10 | Narrative integration (Guide dialogue triggers) | C | ⬜ |
| 11 | VIP area and restricted zones | D (+B for gating) | ⬜ |
| 12 | Internal playtest build | D | ⬜ |
| 13 | Bugfix and polish | All | ⬜ |
| 14 | Near-final build, sound pass, ending variations | All (C sound/narrative) | ⬜ |
| 15 | Final Quest 3 build, final GDD, presentation video | All | ⬜ |

## Log

One entry per commit, newest first, **inside your own slot's sub-section** so members never edit the same lines (this prevents merge conflicts). Setup/cross-team entries go under "Setup".

Format: `- YYYY-MM-DD · <what changed> (<commit or branch>)`

### Slot A log

- 2026-10-20 · Plan for LOD and occlusion culling (WIP, not started): baseline the Quest 3 APK per area, LODs on the Pit Boss and future NPCs (Unity 6 Mesh LOD, LODGroup fallback), occluder/occludee flags in the world builders with occlusion baked in `BuildAndBake`, then measure again. `docs/plans/LodAndOcclusion.md`.

- 2026-10-15 · VIP door needs a keycard from the Pit Boss: point at him and grip, he stops, turns to you, says a line in a speech bubble and holds out a gold VIP card; grab it and touch it to the reader by the VIP door (light goes red to green, sign KEYCARD REQUIRED to ACCESS GRANTED) and the door unlocks for the rest of the session, also after coming back from the lounge. New: `Inventory/Keycard`, `KeycardReader`, `KeycardAccess` (session access list), `KeycardGiver`, `Dialogue/SpeechBubble`, `PatrolWalker.Pause`; `AccessBuilder` builds `Content/Inventory/Prefabs/Item_VIPKeycard.prefab` and the reader (`WorldBuilder.RebuildCasinoDoors`). Headless check: locked door keeps you in the casino, he hands over the card facing you, the card in hand opens the reader, the door leads to the lounge and stays open on return, a second chat gives no second card.

- 2026-10-13 · Mines, Plinko and Dice ported from vrstake into the high-roller room (in a row facing the bar; the old decorative table sets are gone, chandeliers moved between the games). Same rules and tests as vrstake (Unity-free logic, 1% edge, pricing before the debit, hidden mines); Dice and Plinko now credit winnings only when the roll / ball lands (`Settle()`), Mines on cash out. Dice uses -/+ hold-to-repeat target buttons with vrstake's red/green bar as a display; Mines 5 x 5 tile buttons (no swipe yet); Plinko draws its pegs (one mesh), buckets and ball inside a tall hologram board. Shared: `HoldRepeat` + hold-to-repeat on `PressableButton`, `TableKit.Body` / `RepeatWhileHeld`, taller hologram boards. 195 EditMode tests (194 pass, 1 skipped by design). Headless check in the lounge: each game's bet leaves at its button, nothing is paid before the reveal, and the board announces each result.

- 2026-10-13 · Roulette on the main casino floor and blackjack moved there from the VIP room (classics on the floor, Stake-style games in the high-roller room). Roulette: European single zero, straight-up on 0-36 plus columns, dozens and even-money bets; bets stay on the layout until CLEAR; the wheel spins and the ball lands in the pocket the game drew; winnings are credited when the ball lands, not at SPIN, so the chip count can't spoil the result. Shared `TableKit` builds every table (blackjack moved onto it); `TableHologram` takes a table name and prompt. 15 roulette tests (98 pass). Headless check: bets placed through the buttons, stake leaves at SPIN, payout matches the announced pocket, ball rests in that pocket, blackjack deals on the floor. Also fixed `CasinoVRInteractionSetup` stopping on a removed grabbable prop. Plan updated: `docs/plans/VrstakeGamesPort.md`.

- 2026-10-08 · Plan for porting the other vrstake games (Dice, Limbo, Crash, Mines, Plinko): `docs/plans/VrstakeGamesPort.md` (shared table kit first, then one game per commit, simplest first). Placement and the GDD's roulette plan are open questions.

- 2026-10-08 · Dealer board now as wide as the table (1.6 m) with its bottom edge standing on the table's dealer edge; a brass projector strip along that edge replaces the puck and beam, and the board rises straight up out of it (height grows from the bottom) when you walk up and sinks back when you leave. Headless check: width matches the table top, bottom on the top, win lands on the amount, sinks away after walking off.

- 2026-10-08 · Dealer hologram at the blackjack table: a brass projector puck on the dealer's edge projects a blue holographic board where the dealer would stand (shares the watch's hologram materials). It rises out of the puck when you walk within 1.8 m of the table and folds away past 2.2 m; it replaces the old floating result text: idle prompt, YOU WIN / BLACKJACK! with the amount counting up from +$0 (eased, pulsing, a pop when it lands), DEALER WINS / PUSH, and the hands (YOU 20 · DEALER 18). `TableHologram` is reusable for future tables; `CountUp` + 3 tests (86 pass). Headless check: hidden from the hallway, opens at the table, a win counts up (+$1 on the way to +$10) and lands on the amount won, folds away after walking off.

- 2026-10-06 · Blackjack ported from Ben's vrstake prototype: Unity-free rules + 49 EditMode tests in `TableGames/Logic` and `TableGames/Tests` (natural pays 3:2, dealer stands on all 17s, double, one split, hidden hole card, exact-odds return check), `BlackjackPresenter` + `PressableButton` + our own drawn cards (`CardView`, no third-party art), bets go through `ChipWallet`. **High Stakes > Tables > Build Blackjack Table** builds `Content/Tables/Prefabs/Tables_BlackjackTable.prefab` from the kit; it replaces one table in the VIP room, and `_SoloTest` gets a 1,000-chip wallet so the table plays on its own. Ahead of the week-4 plan. Zach: kit floor tile `Mesh` isn't static, so floors never lightmap (slot-a/blackjack)

- 2026-10-06 · Chip wallet: `Economy/ChipWallet` is the one real `IChipWallet` (balance rules in the Unity-free `ChipLedger`, rejects negative amounts, saturates at int max, logs an error if two wallets are active, Inspector ⋮ menu adds/spends chips in Play mode); 6 EditMode tests pass. Zach: add one `ChipWallet` to `MVP_Main` outside `_SoloTest` (slot-a/chips)
- 2026-10-04 · VIP room: **High Stakes > Tables > Build VIP Room Scene** generates `MVP_VIPRoom.unity` (10 m hallway with a velvet-rope lane and "VIP LOUNGE" sign opening into a 12 x 12 m lounge: two high-stakes tables, bar, slots), style kit only, baked lighting; the hallway's back door is a portal `VIP_HallStart` -> `Casino_VIP`, so the casino needs one `Env_PortalDoor_Warm` with id `Casino_VIP` to connect it (slot-a/vip-room)

### Slot B log

- 2026-10-27 - Added isolated vault treasury dressing: 834 cash bundles, 212 gold bars, 168 deposit drawers, three carts, counting station/open till and a reinforced circular door facade; generated cash texture, feature-owned PBR lighting and material-batched geometry (63,630 triangles, ten renderers). Visual, clear-aisle and room travel/reload checks passed; shared scenes unchanged; headset performance pending.

- 2026-10-22 · Improved security-office readability: fitted terminal/board text, moved CCTV captions above equipment and the title ahead of the cable tray, and increased live/editor screen emission to 2.5x.

- 2026-10-22 · Furnished the existing security office with six live CCTV channels, operator consoles, equipment racks, dispatch terminal, lockers, incident board, evidence storage and baked cool lighting. Actual remote rooms load with isolated physics and dormant interactions; existing patrol animation stays live. Play checks verify feed updates, hold/resume, one rig/wallet and office-hub-revisit cleanup; authored-room style validation passes. Quest 3 APK performance review remains pending.

- 2026-10-15 · Added a photographed starry night-sky dome (Poly Haven Dikhololo Night, CC0) around the 3D exterior district, replacing the flat sky walls while preserving casino lighting.

- 2026-10-15 · Expanded the entrance exterior into a 3D street district with 39 varied buildings, storefronts, a 130 m avenue, sidewalks/crossings, street furniture and eight parked cars. Brick/asphalt color and normal textures, two shadow-free exterior lights, and geometry combined by material; all six entrance glass panels retained. Collision checks and rendered views passed; Quest 3 performance review remains pending.

- 2026-10-15 · Enclosed the casino's open entrance with matching wallpaper, wood panels and brass trim; added a closed door with a small glass window and a 3D nighttime courtyard/city view. CasinoEntranceBuilder regenerates the prefab; 355 collision checks passed, with no new lights or player rigs. Rendered entrance and window views reviewed; headset review remains pending.

- 2026-10-15 · Integrated the latest dev VIP keycard changes with the bar PIN puzzle; rebuilt the puzzle on the updated casino scene to preserve the Pit Boss and reader setup.

- 2026-10-15 · Added a compact bar float PIN pad at the MVP_Casino bar: tilted brass/black housing, high-contrast keys, a closeout-slip clue and a one-time $500 session bonus through IChipWallet. Replaces the earlier recipe concept. Includes a prefab builder, reward/revisit checks and a passing live keypad Play check; headset reach/performance review remains pending.

### Slot C log

- 2026-10-17 · UI textures: notebook covers in burgundy leather (reuses the casino `T_Leather`, +0 MB) and paper pages / shift note (new 512² generated `T_Paper`, ASTC 6x6 on Android ≈ 0.15 MB with mips), as Material Variants of kit materials in `Content/UI/Materials/`; **High Stakes > UI > Apply UI Textures** updates the prefabs only, no scene edits; the open notebook now sits closer (32 cm) with larger, bold page text (slot-c/ui-textures)

- 2026-10-17 · Merged the latest `dev` (VIP/security textures, city-street entrance) into slot-c/notebook-staff-door-puzzle and re-ran **Build Notebook** + **Build Staff Door Puzzle** on the fresh scenes: notebook in `MVP_Main`, cart / shift note / table stands / STAFF reader in `MVP_Casino`, dice stand in `MVP_VIP`. Play-tested the full chain in the simulator (VIP card → note → 472 → staff card → door opens)

- 2026-10-15 · Staff door puzzle ("housekeeping cart", from the GDD): the STAFF ONLY door needs a STAFF keycard from a cleaning cart's lockbox; the code (472) comes from a shift note (CARDS > WHEEL > DICE) and brass table-number stands at blackjack, roulette and the high-roller dice table. Reuses the notebook, Slot B's keycard/reader and the PressableButton keypad; collected notes no longer reappear after a door trip; 12 lockbox test cases. Run **High Stakes > Puzzles > Build Staff Door Puzzle**

- 2026-10-15 · Notebook inventory: N / left Y opens an A5 notebook in front of you as a two-page spread matching the concept art (blue screen with the selected item on the left page, notes list on the right, brass card slot holding the keycard; it follows you as you walk and recentres when you turn away); look at a collectable (VIP keycard) for a "PICK UP" prompt and press F / right B to put it in the notebook; B / left X steps through items, F / right B takes one into the right hand and puts it back. Prototype `NotebookPickups` stands in for Slot B's inventory; 22 EditMode test cases. Run **High Stakes > UI > Build Notebook** (adds it to `MVP_UI` and `MVP_Main`)

- 2026-10-03 · Committed the rebuilt `UI_WristDisplay.prefab` (watch on the back of the left controller); opened the wrist-display PR into `dev` (slot-c/wrist-display)

- 2026-10-03 · Moved the watch from the wrist spot onto the back of the left controller (the rig shows controllers, not hands, so it looked like it floated); dropped the shirt cuff and VIP badge; tier now shows on the case: steel → gold → gold + white VIP stripe. Re-run **High Stakes > UI > Build Wrist Display** (slot-c/wrist-display)

- 2026-10-03 · Rebuilt `UI_WristDisplay.prefab` with the tier parts; checked in the simulator: tier text updates (ORDINARY NIGHT → VALUED ASSOCIATE), gold case, shirt cuff and VIP badge switch on by tier (slot-c/wrist-display)

- 2026-10-03 · Tier hand visuals: new `TierVisibility` component shows an accessory only within a tier range; the watch now upgrades with tier (steel → gold case + white shirt cuff with brass cufflink at Known High Roller → adds a VIP badge at Valued Associate), kit materials only; 8 new EditMode tests (23 total). Re-run **High Stakes > UI > Build Wrist Display** to update the prefab (slot-c/wrist-display)

- 2026-10-01 · Wrist display: ran the builder and added `MVP_UI.unity`, `UI_WristDisplay.prefab` and three mock tiers (`Content/UI/Mocks/`); fixed the builder dropping the tier references (showed `--`), tier line now wraps for long names, builder refuses to run in Play mode. Tested in the simulator: bankroll updates live; watch position still to tune on the headset (slot-c/wrist-display)

- 2026-10-01 · Wrist chip display: `WristDisplay` (bankroll + tier, observes `IChipWallet`/`ICharacterTier`), `WristAttach` (finds the active XROrigin and attaches to the left controller at runtime), `WristRaiseVisibility` (face lights up when the wrist is raised toward the eyes); mock wallet/tier with Inspector +/- chip buttons; **High Stakes > UI > Build Wrist Display** menu builds `UI_WristDisplay.prefab` and `MVP_UI`; 15 EditMode tests (slot-c/wrist-display)

### Slot D log

- 2026-10-27 · Committed Unity's own reserialization of `Assets/XR/Settings/OpenXRPackageSettings.asset` (new Android XR feature entries, all disabled; one entry relabelled Standalone → Android) so it stops reappearing as a local change. No features enabled, Quest build unaffected. (dev)
- 2026-10-27 · Breaker panel fixes + vault camera cut: the shelf, plugs, drawer and note were built inside the back of the cabinet (unreachable from the front, note couldn't be picked up); moved to the front. Solving now also cuts monitor feed **04 / VAULT APPROACH** (black, "NO SIGNAL") for the session via new `Security/CctvOutage.cs` (vault stealth can ask `CctvOutage.IsDown("MVP_Vault")`), shows a floating **TASK COMPLETE** card (+300 chips, vault camera offline; reuses `SpeechBubble`), and the Breaker Log note says the vault is unwatched. Rebuilt with **Build Security Wire Panel**; `MVP_SecurityOffice` otherwise unchanged. (dev)
- 2026-10-27 · Ashton had meanwhile pushed his own merge of the same work to `dev` and then reverted it, so `origin/dev` was byte-identical to the pre-merge `dev` (0 files changed vs `4d6edb9`) and his monitors were **not** on `dev` — only the wire puzzle was. Since his two commits contributed no content that this branch's integration didn't already have, they were brought in with `-s ours` to keep the resolved tree, then pushed. If anyone still has the reverted merge locally, reset to `origin/dev`. (dev)
- 2026-10-27 · Git hygiene: `Assets/_Recovery/` (Unity crash-recovery scenes) is now git-ignored and the one tracked file in it (`0.unity`) is untracked, so it will disappear from everyone's working tree on the next pull — it was never project content. Ashton's branch had committed a second copy of it. (dev)
- 2026-10-27 · Ran **Build Furnished Security Office** on the merged scene: the monitor wall, consoles, racks, lockers and dispatch terminal are now placed in `MVP_SecurityOffice` alongside `Puzzle_WirePanel`, which survived (verified: 0 of Ashton's objects overlap the panel's box, nearest is a locker handle 1.09 m away). Note the panel is a **scene-root** object, a sibling of `WingA_Content`, not a child of `RoomA` — so the wing builders' per-room loops never see it. His builder also switches scene ambient from warm (0.227,0.188,0.149) to cool (0.3,0.36,0.44) via `AmbientMode.Flat`, which is correct for back-of-house but leaves his new furniture unbaked; **the office still needs a lightmap re-bake from Ashton or Ben** (no GPU here, StyleGuide §9). (slot-d/merge-ashton-monitors)
- 2026-10-27 · Integrated Ashton's furnished security office (Slot B) into `dev` without losing the wire panel. His branch was 10 commits behind and had regenerated every area scene and table prefab (all new fileIDs), so the 7 scenes + `OpenXRPackageSettings.asset` all conflicted and his `MVP_SecurityOffice` had no wire puzzle in it. Resolved per merge-conflict rule 9: took `dev`'s side for all 7 scenes, the XR asset and the `MVP_SecurityOffice` lightmaps, reverted the five `Content/Tables/` prefabs to `dev` (ID-regeneration churn only, Slot A content), dropped the committed `Assets/_Recovery/` crash file, and kept all of his new code and CCTV content. His `SecurityOfficeBuilder` is an additive furnishing pass (only removes its own `SecurityOffice_Operations` root plus `Screen`/`Kit_Table_Round*`/`Kit_Chair*` direct children), so re-running **High Stakes > Security > Build Furnished Security Office** on `dev`'s scene restores the monitors with `Puzzle_WirePanel` intact. (slot-d/merge-ashton-monitors)
- 2026-10-25 · Security office wire-matching puzzle: a breaker panel with four colour-coded sockets (red/blue/yellow/green) and four loose plugs that start cross-wired on a shelf; seating every plug in its matching socket pays 300 chips and pops a drawer with a notebook pickup (`SO_Item_BreakerLog`) inside. `WirePanelPuzzle` + `WirePlug` (`Scripts/Security/`), pure match logic in `Logic/WirePanelLogic.cs` (6 EditMode tests), built by **High Stakes > Puzzles > Build Security Wire Panel** (`Editor/WirePanelBuilder.cs`) into `MVP_SecurityOffice`'s near corner (clear of the table/chairs/door). Uses XRSocketInteractor/XRGrabInteractable (first use of sockets in the project); two new Material Variants (`M_WireBlue`, `M_WireYellow`) next to the security wing's other generated materials. Like the staff-door puzzle, HallwayBuilder regenerating the wings wipes this placement; re-run the menu after. (dev)
- 2026-10-25 · Fixed `RenderingOptimizer`'s occlusion bake hanging indefinitely on `MVP_Casino`: the entrance's `ExteriorDistrict` street (190x130 m, added 2026-10-15) was getting flagged occluder/occludee like interior geometry, so Umbra's view-cell grid stretched over the whole city block instead of just the casino interior. The street is sealed behind `ClosedDoorCollision` and only ever seen through glass, so it's excluded from occlusion flags the same way the sky dome already was; lightmaps/batching untouched. Re-run **Optimize All Area Scenes** after pulling. (dev)
- 2026-10-22 · Rendering: LOD + occlusion culling. `RenderingOptimizer` (**High Stakes > Rendering > Optimize Open Scene / All Area Scenes**) gives small static details (< 0.8 m; small prefabs like chairs as one piece) a culling-only LODGroup at 0.5% screen height, marks fixed renderers occludee and big opaque ones occluder, and bakes Umbra occlusion (CPU) per area scene (SceneFlow makes each area active, which is where Unity reads occlusion data). Quality levels' LOD bias raised to at least 1 (Android's was 0.4). `WorldBuilder` runs it after every rebuild; StyleGuide §7 has the rule. (dev)
- 2026-10-17 · Textures everywhere: security rooms (painted cinder block, sealed concrete, vault tread plate, steel doors, generated drop ceiling; **High Stakes > Security Textures > Build And Apply To Security Wings**, `SecurityTextureBuilder`, textures in `Content/Environment/Security/`), casino wallpaper gets a cream-and-gold damask, and casino details (`CasinoDetailTextures`, run by **Build Casino Textures** / **Apply Casino Textures To All Area Scenes**): coffered wood ceiling (also on warm ceiling tiles), rosewood beams, dark wood pilasters and table wood, wool felt, leather seats and padded table rails, brushed brass/steel, black slot cabinets, bar top, glowing lamps, wood staff door. Applied to `MVP_Casino`, `MVP_VIP` (Ben OK'd), hub and wings. Materials-only overrides (lightmaps kept; floor/wall panels no longer swap prefabs); fixed wall panels never getting wallpaper/wood (`Casino_SideWall` name); `WorldBuilder` re-applies after regenerating scenes. CC0 Poly Haven sources + generators in `tools/generate_*_textures.py` / `generate_casino_wallpaper.py`. (dev)
- 2026-10-14 · Security hub: the casino's three staff doors are now one **STAFF ONLY** door (right wall) into a new hub hallway `MVP_SecurityHall`, whose signed SECURITY / CAMERAS / VAULT doors lead into the existing wings (and each wing's start door leads back to the hub). **High Stakes > Build Security Hub (keeps wing lighting)** builds the hub, relinks the wings in place and adds the hub to the build list; **Build MVP World** builds the same layout. (slot-d/casino-textures)
- 2026-10-14 · Casino textures: our own generated casino carpet (`tools/generate_casino_carpet.py`, kit palette) plus CC0 Poly Haven marble, wood-panel and wallpaper textures and an FBX picture frame in `Assets/Content/Environment/Casino/`; **High Stakes > Casino Textures > Build Casino Textures** makes kit Material Variants + Prefab Variants (`Env_CasinoFloor_Carpet/Marble`, `Env_CasinoWallPanel`, `Env_PictureFrame`), and **Apply Casino Textures To Open Scene** swaps them in (also textures `MVP_Casino`'s plain floor/wall boxes, tiled to size). Android import: ASTC 6x6, max 1024, mipmaps. (slot-d/casino-textures)
- 2026-10-01 · Committed the built `MVP_Hallways.unity` (3 hallways + security rooms + stand-in casino with door portals); rebuilt against the restored casino prefabs and checked in the simulator. (slot-d/hallways)
- 2026-10-01 · Added the two portal door prefabs (`Env_PortalDoor_Warm/Cool`) so the casino can place doors with ids `Casino_A/B/C`; `MVP_Hallways.unity` itself is not committed yet (needs a rebuild against the restored casino prefabs). (slot-d/hallways)
- 2026-10-01 · Faster movement: simulator WASD walking set to 3 m/s (was 1) via `SoloTestRoot`; rig smooth-move speed target 3 m/s (was 2.5) as `StyleScale.MoveSpeed`, applied by the style-kit builder and set by hand on `Kit_PlayerRig` in the Editor. (slot-d/hallways)
- 2026-10-01 · Checked every scene has VR + WASD: `MVP_Template`, `Casino_Test/Casino` and `MVP_Hallways` each have the one rig and the XR Interaction Simulator (starts in first-person WASD + mouse mode); `SoloTestRoot` now switches the simulator off in builds and when a headset is connected. `BasicScene`/`SampleScene` are vendored and unused. (slot-d/hallways)
- 2026-10-01 · Hallways rebuilt wider (4 m, double-width portal doors) with the casino look: red carpet, Ashton's casino wall panel, framed pictures, sconces, chandeliers; security rooms enlarged to 8 x 8 m. Still untested in Unity. (slot-d/hallways)
- 2026-10-01 · Hallways: `High Stakes > Build Hallways Scene` generates `MVP_Hallways` (3 hallways, casino-warm lead-in turning cool, each ending at a security room) and `DoorPortal` carries the one rig between doors by id, so nothing is connected in the scene; works with the XR Interaction Simulator (WASD) and the headset. Untested in Unity yet. (slot-d/hallways)
- 2026-10-01 · Told the team not to commit the reserialized OpenXR settings asset (Open questions); verified Android OpenXR features are correct for Quest 3.
- 2026-10-01 · Created the `dev` integration branch; slot PRs now target `dev`, and Zach promotes tested `dev` into `main` (Merge-conflict rule 8, Decisions).
- 2026-10-01 · Let Ashton edit the shared style kit directly (temporary exception, logged in Decisions); added an Open question on where the casino scene lives and the Quest 3 budget check for it.

- 2026-09-30 · Merged the Day-0 gate to `main` and tagged `day-zero`; added a "start here" checklist for A/B/C under the Day-0 gate; brought CLAUDE.md up to date (`Core/` + `Environment/` folders, MVP scenes replace `BasicScene`/`SampleScene` as entry points); marked the GDD PDF as in the repo; discarded Unity reserialization churn to vendored `SampleScene`.

- 2026-09-30 · Style kit: added `Kit_PlayerRig` (the one XR Origin, step height 0.3 m), `_SoloTest` group with `SoloTestRoot` (auto-off in `MVP_Main`), zone markers (StyleGuide §5b), convex mesh colliders on cylinder parts, bigger test floor; documented the one-rig rule and "how to start your scene" (StyleGuide §5c–5d).

- 2026-09-30 · Day-0 gate: generated the style kit (`Assets/Content/StyleKit/`) and `Assets/Scenes/MVP/MVP_Template.unity` with the XR Interaction Simulator; added contracts in `Assets/Scripts/Core/Contracts/` (`IChipWallet`, `ICharacterTier`, `IInventoryEvents`, `IGuideVoice`, `TierDefinition`, `ItemData`); reserved layers `Interactable` (6), `PlayerBody` (7) and tags `Chip`, `Keycard`, `Restricted`; created `Assets/Content/{Tables,Progression,UI,Environment}/`; added `docs/SimulatorControls.md`.

### Setup

- 2026-10-08 · Pit Boss walk no longer snaps backwards: the Rocketbox walk still moved the hips 1.20 m forward per 1.47 s loop and jumped back each loop; the hips (`Bip01`) are now the clip's root node so that travel becomes root motion and is dropped, and the builder sets the patrol speed from the clip's own stride (0.82 m/s) so the feet don't slide. Headless check: over 19 s of walking the hips never left the feet (0 m drift, 0 m jump).

- 2026-10-08 · Pit Boss walks the casino floor: Microsoft Rocketbox `Business_Male_04` (MIT, 7.3k triangles, textures cut to 1024 PNG) in `Content/Characters/PitBoss/`, slow-walk and look-around clips from the same library, `PatrolWalker` (Security) loops four waypoints around the centre and left aisles, turning on the spot and looking around at each corner; capsule collider so you bump into him. **High Stakes > Characters > Build Pit Boss** rebuilds the prefab and his `PitBoss_Patrol` in `MVP_Casino`. Headless check in `MVP_Main`: he walks up the aisle facing forward, plays both clips, turns the corner.

- 2026-10-08 · Watch hologram: looking at the raised watch projects a blue holographic HUD above it on a beam of light (bankroll, tier, session +/-), growing out of the face in 0.2 s, always facing the eyes, folding away in 0.12 s when you look away (`WatchHologram`, built by **High Stakes > UI > Build Wrist Display**; transparent unlit `UI_Hologram*.mat`). 5 new logic tests (83 pass); headless check: hidden, opens above the watch while looked at, folds away after.

- 2026-10-08 · The wrist display now looks like a watch: round case (steel, gold from Known High Roller) whose rim frames a round dark face, crown at 3 o'clock, lugs, and a strap wrapped around the wrist; worn so it reads upright when you turn the wrist to look (`WristAttach` (90, 0, 90), behind the grip). Rebuilt with **High Stakes > UI > Build Wrist Display**; checked in headless renders.

- 2026-10-08 · Watch back on the left wrist (Ben's call): `WristAttach` defaults and `UI_WristDisplay.prefab` use Pak's original wrist pose again, (-0.035, -0.01, -0.09) with the face turned toward the back of the hand, just behind the controller grip; checked in a headless render (shows BANKROLL $1,000).

- 2026-10-06 · Blackjack buttons pressable and watch on the controller: the starter rig's near grab only hit layer Default and its far ray Default/UI/Teleport, so nothing on the reserved `Interactable` layer (table buttons, Ashton's props) could be pointed at or grabbed; `Kit_PlayerRig` now adds `Interactable` to both hands' casters. `WristAttach` also finds the left controller while the rig still has it switched off (untracked), so the watch rides along from the start. Headless check: watch on Left Controller, right ray hovers DEAL, pressing it takes the wager.

- 2026-10-06 · Simulator off with a real headset: `SoloTestRoot.SimulatorAllowed()` also treats a running XR loader as a headset (over Quest Link `XRSettings.isDeviceActive` can still be false at startup, so the simulator's fake HMD ran next to the real one: view on the floor, wrong head tracking) and keeps checking each frame; the casino setup uses the same check.

- 2026-10-06 · Movement now matches Ben's vrstake prototype: `Kit_PlayerRig` rebuilt as a variant of the plain XRI starter rig (**High Stakes > Rebuild Player Rig**), dropping the VR Template's tunneling vignette on every move/turn and the floating controller callouts; move speed back to the starter default; removed `RoomPlayerCollision` (fought XRI's own CharacterController every frame). Stick locomotion still collides with walls; walking for real (or simulator WASD) passes through them, as in vrstake.

- 2026-10-06 · One scene per area, loaded through doors: new `MVP_Main` (build index 0: the one rig, `ChipWallet`, wrist display, `SceneFlow` with a fade) loads one area at a time; `DoorPortal.targetScene` swaps areas. Casino moved to `MVP_Casino` and got its doors (VIP portal on Ashton's VIP door, staff doors SECURITY / CAMERAS / VAULT); `MVP_Hallways` split into `MVP_SecurityOffice`, `MVP_CameraRoom`, `MVP_Vault`; `MVP_VIPRoom` renamed `MVP_VIP`. **High Stakes > Build MVP World (all scenes)** builds it all. Headless Play-mode walk: casino -> VIP -> casino -> security wing, one rig and one wallet in every area, arrivals land on the doors.

- 2026-10-06 · Merged Pak's `slot-c/wrist-display` straight into `dev` (no PR, at Ben's request) on top of Slot A's VIP room, wallet and blackjack; clean merge, all 78 EditMode tests pass (wrist display 23, blackjack 49, wallet 6). The wrist display now finds the real `ChipWallet` once both are in one scene (`MVP_Main`).

- 2026-09-30 · Recorded team hardware (Zach and Pak have no dedicated GPU; Ashton and Ben do). Switched the primary test path to standalone APK + XR Interaction Simulator, made Quest Link optional, added StyleGuide §9 "Testing without a GPU", and made the style-kit builder add the simulator to `MVP_Template`.
- 2026-09-29 · Wrote docs/StyleGuide.md and the style-kit generator (`SharedStyleKit.cs`, `Editor/StyleKitBuilder.cs`). Zach still needs to run **High Stakes > Build Style Kit** in the Editor, review the result on the headset, and commit the generated `Assets/Content/StyleKit/` + `MVP_Template.unity`.
- 2026-09-29 · Refocused MVP on movement / interaction / UI plus the evolutionary-character question; removed NPCs, guards and suspicion from scope; added the Day-0 style-kit gate and PR visual check; assigned slots A–D by name.
- 2026-09-29 · Added the Milestone 2 (MVP) plan: four vertical slices, Day-0 contracts, merge-conflict rules, per-slot log sections.
- 2026-09-29 · Added `Progress_Vol1.md`, git-ignored `Personal.md` template, and the progress-update rule in CLAUDE.md.
- 2026-09-29 · _(setup)_ · Stripped Claude co-author trailers from earlier commits (history rewritten + force-pushed; teammates must `git fetch && git reset --hard origin/main`) and added a no-AI-attribution rule to CLAUDE.md.
- 2026-09-29 · _(setup)_ · Added `docs/reference/` (concept-art descriptions from the submitted GDD PDF), linked from CLAUDE.md; added Benjamin Parenteau to the GDD team line.

## Decisions

- **2026-10-08 · NPCs come from Microsoft Rocketbox (MIT).** Free, safe in the public repo, semi-realistic, shared skeleton across 115 avatars and 400+ animations. Cast plan: Pit Boss `Business_Male_04`, dealers `Business_Male_06` / `Business_Female_01` (+ black vest), informant `Business_Male_07`, guards `Security_Male_01` / `Security_Female_01`, server `Female_Adult_13`, guests from Adults/Party. Rocketbox's own `FixRocketboxMaxImport` postprocessor is not used (it rewrites every material in the project); the builders apply its fixes per character. The Pit Boss was added ahead of the MVP plan at Ben's request (walking only; no detection or suspicion yet).

- **2026-10-08 · Holograms get their own materials.** The kit has no transparent glow material, so the watch HUD uses three transparent unlit blue materials in `Content/UI/` (`UI_Hologram`, `UI_HologramEdge`, `UI_HologramBeam`). Reuse them for any future holographic UI instead of adding more.

- **2026-10-06 · Player rig = plain XRI starter rig.** Same base as vrstake, which plays better: no tunneling vignette by default (the comfort panel can offer it), no controller callouts, starter move speed, no custom tracked-movement collision. Keep our settings on top only (0.3 m step, black clear).

Things the team agreed on that aren't obvious from the code.

- **2026-09-30 · Standalone APK is the primary test target.** Zach and Pak have no dedicated GPU and cannot use Quest Link; Ashton and Ben can. Everything must work as a standalone Android build and in the XR Interaction Simulator; Link/PC VR is optional. Shared lightmaps are baked by Ben or Ashton with the CPU lightmapper only.

- **2026-10-01 · `dev` is the integration branch; `main` is always working.** Slot branches (`slot-<x>/<topic>`) merge by PR into `dev`. Zach tests `dev` (simulator + APK on Quest 3) and only then promotes `dev` into `main` by PR, so `main` is always a build that runs on the headset. No direct pushes to `main` after this (Zach's Day-0 and Progress commits were the last exceptions).
- **2026-10-01 · Ashton may edit the shared style kit directly (temporary exception).** He built the full casino and needs kit changes (materials, prefabs, lighting). He edits `Assets/Content/StyleKit/` on a `slot-b/<topic>` branch; Zach does not touch the kit while he does, and nobody reruns **High Stakes > Build Style Kit** (it may overwrite prefabs, since it saves them with `SaveAsPrefabAsset`). Anything not specific to the casino still follows the normal rule (request to Zach, shipped as `stylekit-vN`). Ashton announces each kit change in Open questions so everyone knows when to pull.
- **2026-10-14 · Casino textures are CC0 Material Variants in `Assets/Content/Environment/Casino/`.** They inherit from the kit materials (no new base materials) and the prefabs are variants of the kit floor tile and casino wall panel, so sizes and colliders match. Nothing in `Assets/Content/StyleKit/` changes, so this doesn't clash with Ashton's kit exception. Sources and licenses are in that folder's README.
- **2026-10-17 · Slot D may texture `MVP_VIP` (Ben's OK, texture-only).** Zach applies the casino materials to the VIP room (floor, walls, ceiling, the dice/mines/plinko tables' felt, wood and rails, slot machines, chandeliers, stools) so it matches the main floor. Only material overrides in the scene: no changes to Ben's table prefabs, `VipRoomBuilder` or gameplay. If Ben regenerates the VIP with **Build VIP Room Scene**, re-run **High Stakes > Casino Textures > Apply Casino Textures To Open Scene** on it (Build MVP World does this automatically).

## Open questions / blockers

- **Ashton:** Zach edited your `Security/SecurityMonitorWall.cs` (2026-10-27): feeds whose scene is in `CctvOutage` show black / NO SIGNAL and stop rendering (the breaker panel cuts `MVP_Vault`'s). Small, additive; shout if you'd rather own it differently. Also: **Build Security Hub** / **Build MVP World** regenerate the security wings and wipe the furnished office and the wire panel. If that happens, restore the four security scenes from git, or re-run **Build Furnished Security Office** then **Build Security Wire Panel** (lightmaps then need a re-bake).

- **Ashton / Ben:** heads-up, **High Stakes > Rendering > Optimize All Area Scenes** (Zach, 2026-10-22) also touches `MVP_Casino` and `MVP_VIP`: it adds LODGroups to small static props, adjusts their Occluder/Occludee static flags and stores an `OcclusionCullingData.asset` next to each scene. No geometry or materials change. After you add or move static geometry, re-run **Optimize Open Scene** on your scene (CPU only, safe on any laptop).

- **Ashton, Zach, Ben (from Pak): staff door puzzle.** **High Stakes > Puzzles > Build Staff Door Puzzle** (`Scripts/UI/Puzzles/`) edits `MVP_Casino` (cart by the staff door, a shift note on the cashier counter, number stands by blackjack and roulette, a STAFF reader on `Door_Casino_Staff`) and `MVP_VIP` (a number stand by the dice table). **Placements are included** (built on top of the 2026-10-17 `dev` scenes, additions only, plus the staff door now starts locked). **Ashton:** please check the placements in your scenes. **Zach:** **Build MVP World** / **Build Security Hub** rebuild the casino doors and drop the STAFF reader; either re-run my menu after them or call `StaffDoorPuzzleBuilder.Build` from `WorldBuilder`. **Slot B:** it reuses your `Keycard`, `KeycardReader`, `KeycardAccess` and `AccessBuilder.AddVipReader` unchanged (the staff card is a variant of your VIP card); happy to move the puzzle into `Inventory/` if you'd rather own it.

- **Ben / Ashton (Slot B) and Zach (Slot D), from Pak:** the notebook (`Scripts/UI/Notebook/`, `UI_Notebook.prefab`) is a working inventory prototype: look at the VIP keycard → "F / B PICK UP" prompt → it goes into the notebook; N / left Y opens the notebook in front of you, B / left X steps through items, F / right B takes one into the right hand (and puts it back). It lists pickups from any `IInventoryEvents`, and a prototype `NotebookPickups` raises `ItemAdded` for now (the keycard is matched by its `Keycard` component, no code dependency). **Slot B:** when your inventory raises `ItemAdded` itself, tell me and I'll drop the prototype; item assets should then move to `Content/Progression/`. Also, once the card is in the notebook the Pit Boss still says "your card's right there" (his `handedOut` is still set). **Zach:** I added `UI_Notebook` to `MVP_Main` (your scene) via **High Stakes > UI > Build Notebook**; please have `WorldBuilder` instantiate `Assets/Content/UI/UI_Notebook.prefab` next to the wrist display, or **Build MVP World** drops it. Controls use inline Input System bindings in my scripts (left X/Y, right B; keyboard N/B/F), no shared input asset changes.

- **Ben / Ashton:** please bake `MVP_SecurityHall` (CPU lightmapper, like the wings). Its lights are Baked-only, so until then only the flat ambient lights it. Zach can't bake on his laptop.

- **Ashton:** heads-up, `MVP_Casino` was textured by Zach (2026-10-14) with **High Stakes > Casino Textures > Apply Casino Textures To All Area Scenes**: `Casino_Carpet` is now the generated casino carpet, the side/back walls wallpaper, the wainscot/back paneling (and bar front, cashier counter, VIP door) wood panels, all Material Variants in `Content/Environment/Casino/Materials/Tiled/`. The three staff doors are now one STAFF ONLY door (`Casino_Doors`, from WorldBuilder). Pull `dev` before editing the casino again; re-running the menu is safe (it updates in place, Undo works).

- **GDD:** update the game list to the two-room plan in `docs/plans/VrstakeGamesPort.md` (classics on the floor; Mines, Plinko, Dice and virtual games in the high-roller room).


- **Everyone: don't commit `Assets/XR/Settings/OpenXRPackageSettings.asset`.** Unity keeps reserializing it (renamed/duplicated Android XR feature entries, no functional change; checked 2026-10-01: Meta Quest Support, Touch controllers and hand tracking are on for Android, Android XR Support is off). Commit files by name, never `git add .`; if a pull is blocked by it, `git stash` first. Zach owns this file and only commits it after a deliberate OpenXR settings change. Proper cleanup is after the Milestone 2 submission.

- **Ashton / Zach:** where does the full casino live (`MVP_World` is Slot D's, `MVP_Progression` is Slot B's)? Agree on one scene so nobody edits the same scene file, and run **High Stakes > Validate Open Scene Against Style Kit** plus the Quest 3 budget check (72 fps, < 100k triangles) on it before the APK build.

- Slots assigned: A Ben, B Ashton, C Pak, D Zach. Confirm the Day-0 tier thresholds (placeholder: 1,000 / 5,000 chips) and the three tier names with the team.
- `WHOLEDOCCLAUDEGDIT.pdf` is now in `docs/reference/`; its concept images still need extracting into `docs/reference/images/` and linking from `VisualReference.md`.
- **Zach:** run `MVP_Template` as an APK on Quest 3 and confirm the §7 budget (72 fps) — the last Day-0 item, not yet done on device.
- **Ashton:** the remote branch `Ashton` doesn't follow the `slot-b/<topic>` naming rule (it has no new commits yet); please start from the new `main` on `slot-b/<topic>` and delete `Ashton`.
