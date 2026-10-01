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
| Slice A: chips, cage and one table interaction on headset | A | ⬜ |
| Slice B: tier progression, keycard and VIP door on headset | B | ⬜ |
| Slice C: diegetic UI + character evolution visuals on headset | C | ⬜ |
| Slice D: movement + blocked-out world + `MVP_Main` integration | D | ⬜ |
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

### Slot B log

### Slot C log

### Slot D log

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

- 2026-09-30 · Recorded team hardware (Zach and Pak have no dedicated GPU; Ashton and Ben do). Switched the primary test path to standalone APK + XR Interaction Simulator, made Quest Link optional, added StyleGuide §9 "Testing without a GPU", and made the style-kit builder add the simulator to `MVP_Template`.
- 2026-09-29 · Wrote docs/StyleGuide.md and the style-kit generator (`SharedStyleKit.cs`, `Editor/StyleKitBuilder.cs`). Zach still needs to run **High Stakes > Build Style Kit** in the Editor, review the result on the headset, and commit the generated `Assets/Content/StyleKit/` + `MVP_Template.unity`.
- 2026-09-29 · Refocused MVP on movement / interaction / UI plus the evolutionary-character question; removed NPCs, guards and suspicion from scope; added the Day-0 style-kit gate and PR visual check; assigned slots A–D by name.
- 2026-09-29 · Added the Milestone 2 (MVP) plan: four vertical slices, Day-0 contracts, merge-conflict rules, per-slot log sections.
- 2026-09-29 · Added `Progress_Vol1.md`, git-ignored `Personal.md` template, and the progress-update rule in CLAUDE.md.
- 2026-09-29 · _(setup)_ · Stripped Claude co-author trailers from earlier commits (history rewritten + force-pushed; teammates must `git fetch && git reset --hard origin/main`) and added a no-AI-attribution rule to CLAUDE.md.
- 2026-09-29 · _(setup)_ · Added `docs/reference/` (concept-art descriptions from the submitted GDD PDF), linked from CLAUDE.md; added Benjamin Parenteau to the GDD team line.

## Decisions

Things the team agreed on that aren't obvious from the code.

- **2026-09-30 · Standalone APK is the primary test target.** Zach and Pak have no dedicated GPU and cannot use Quest Link; Ashton and Ben can. Everything must work as a standalone Android build and in the XR Interaction Simulator; Link/PC VR is optional. Shared lightmaps are baked by Ben or Ashton with the CPU lightmapper only.

- **2026-10-01 · `dev` is the integration branch; `main` is always working.** Slot branches (`slot-<x>/<topic>`) merge by PR into `dev`. Zach tests `dev` (simulator + APK on Quest 3) and only then promotes `dev` into `main` by PR, so `main` is always a build that runs on the headset. No direct pushes to `main` after this (Zach's Day-0 and Progress commits were the last exceptions).
- **2026-10-01 · Ashton may edit the shared style kit directly (temporary exception).** He built the full casino and needs kit changes (materials, prefabs, lighting). He edits `Assets/Content/StyleKit/` on a `slot-b/<topic>` branch; Zach does not touch the kit while he does, and nobody reruns **High Stakes > Build Style Kit** (it may overwrite prefabs, since it saves them with `SaveAsPrefabAsset`). Anything not specific to the casino still follows the normal rule (request to Zach, shipped as `stylekit-vN`). Ashton announces each kit change in Open questions so everyone knows when to pull.

## Open questions / blockers

- **Everyone: don't commit `Assets/XR/Settings/OpenXRPackageSettings.asset`.** Unity keeps reserializing it (renamed/duplicated Android XR feature entries, no functional change; checked 2026-10-01: Meta Quest Support, Touch controllers and hand tracking are on for Android, Android XR Support is off). Commit files by name, never `git add .`; if a pull is blocked by it, `git stash` first. Zach owns this file and only commits it after a deliberate OpenXR settings change. Proper cleanup is after the Milestone 2 submission.

- **Ashton / Zach:** where does the full casino live (`MVP_World` is Slot D's, `MVP_Progression` is Slot B's)? Agree on one scene so nobody edits the same scene file, and run **High Stakes > Validate Open Scene Against Style Kit** plus the Quest 3 budget check (72 fps, < 100k triangles) on it before the APK build.

- Slots assigned: A Ben, B Ashton, C Pak, D Zach. Confirm the Day-0 tier thresholds (placeholder: 1,000 / 5,000 chips) and the three tier names with the team.
- `WHOLEDOCCLAUDEGDIT.pdf` is now in `docs/reference/`; its concept images still need extracting into `docs/reference/images/` and linking from `VisualReference.md`.
- **Zach:** run `MVP_Template` as an APK on Quest 3 and confirm the §7 budget (72 fps) — the last Day-0 item, not yet done on device.
- **Ashton:** the remote branch `Ashton` doesn't follow the `slot-b/<topic>` naming rule (it has no new commits yet); please start from the new `main` on `slot-b/<topic>` and delete `Ashton`.
