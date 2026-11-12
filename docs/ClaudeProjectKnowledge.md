# High Stakes — project knowledge for Claude

Context file for a Claude Project (claude.ai) so conversations about the game, the code and the GDT-120 course
assignments start from the facts. Snapshot as of **2026-11-12**, `dev` branch commit `2d2fcaa` **plus uncommitted Slot D
work** (fast travel + the reachability fix, section 8). When this file and the repository disagree, the repository wins.

**Read section 8 before claiming anything works.** A lot is built; much less has been verified, and nothing at all has
been verified on the headset.

## 1. The game

- **Title / genre:** High Stakes, a VR adventure / heist simulation: casino simulation with stealth and puzzle elements.
- **Platform:** Meta Quest 3, standalone Android build (primary); Meta Quest Link / PC VR optional.
- **Pitch:** free-roam casino floor (The Meridian) with real, playable table games as the core loop. A voice-only handler,
  **the Guide**, reveals the casino launders money for a syndicate. The player balances building chips and trust with
  gathering keycards, passcode fragments and intel, under a **suspicion meter**, to reach the vault and the **Black
  Ledger** (the record of laundered funds). Two endings: expose the operation, or disappear with evidence and money.
- **Antagonist:** the Pit Boss (charming in public, runs the laundering at night). Supporting: dealers and floor staff
  (some are informants), security guards (patrol obstacles).
- **Evolutionary character (Milestone 2 theme):** the player's own visible body (hands, wrist, accessories) and abilities
  upgrade with chip tiers: Ordinary Night → Known High Roller → Valued Associate (placeholder thresholds 1,000 / 5,000).
  **Note:** the tier system that drives this is still a test mock — see section 5.
- **Moods:** warm gold/green main floor (chandeliers, brass, green felt); cold blue-teal concrete and steel back-of-house
  and vault. All UI is diegetic (wrist display, notebook, floating prompts, the Guide's earpiece). No magic: realism is
  the twist.
- **Team (slots own folders/scenes to avoid merge conflicts):**

| Slot | Member | Owns |
| --- | --- | --- |
| A | Ben (Benjamin Parenteau) | `Scripts/TableGames/`, `Scripts/Economy/`, `Content/Tables/`, `MVP_VIP` (VIP room builder) |
| B | Ashton (Ashton Calkins) | `Scripts/Inventory/`, `Scripts/Security/`, tiers, keycards, access; built `MVP_Casino` |
| C | Pak (Pakrinha Sim) | `Scripts/UI/`, diegetic UI, wrist display, notebook, tier visuals, the Guide's voice |
| D | Zach (Zachary Scheer) | `Scripts/Environment/`, `Scripts/Core/`, locomotion, style kit, `MVP_Main`, security hub/wings, shared settings, Quest builds |

- **Hardware:** Zach and Pak have no dedicated GPU (no Quest Link, no GPU lightmapper, no Meta XR Simulator). Test path:
  standalone APK on Quest 3 + XR Interaction Simulator in Play mode. Ben and Ashton can use Link and bake lightmaps.
  Building an APK needs no GPU — it is CPU work, and installing is `adb` over USB.

## 2. Tech stack

Unity `6000.3.23f1` (pinned), URP 17.3 (Simple Lit shaders), OpenXR (Meta Quest, AndroidXR loaders), XR Interaction
Toolkit 3.5.1, XR Hands (installed, not yet used for gameplay), Unity Test Framework (EditMode tests), Git LFS for
images/models. No CLI build: everything is built from the Editor, mostly through **High Stakes >** menu items that
generate scenes and prefabs from code.

Android build settings are already correct for Quest 3 and should not need changing: ARM64 only, IL2CPP, ASTC, Vulkan,
min/target SDK 34, seven scenes in the build list with `MVP_Main` at index 0.

## 3. Scenes and flow

- `Assets/Scenes/MVP/MVP_Main.unity` (build index 0) stays loaded: the one player rig (`Kit_PlayerRig`), the `ChipWallet`,
  the wrist display, the notebook, the start screen, `SceneFlow` and the `Locomotion` object. `SceneFlow` loads exactly
  one area scene on top: `MVP_Casino` at start, then `MVP_VIP`, `MVP_SecurityHall`, `MVP_SecurityOffice`,
  `MVP_CameraRoom`, `MVP_Vault`.
- `DoorPortal` (with `portalId`, `targetId`, `targetScene`) fades out, swaps the area and places the rig at the target
  door's `arrival` transform. Area scenes keep a `_SoloTest` rig that `SoloTestRoot` disables when loaded into `MVP_Main`.
- Layout: casino floor → **STAFF ONLY** door → security hub (`MVP_SecurityHall`) with SECURITY / CAMERAS / VAULT doors →
  each wing is a warm casino-style hallway leading to a cool room. The security office and camera room are **12 × 12 m**
  (rebuilt 2026-10-29); the vault antechamber stays 8 × 8. The VIP door in the casino is keycard-locked; the VIP lounge
  has the Stake-style games.
- **Critical layout fact — each wing scene contains two disconnected islands.** `HallwayBuilder.BuildHallway` places the
  hallway at the origin and the room at `roomOrigin = (40, 0, 0)`, with the comment "their own islands so nothing is ever
  physically connected". They are joined by an in-scene `DoorPortal` jump, **not** by floor. This has already caused one
  false bug report (section 8) and anything that walks or maps an area must account for it. The vault's doorway is an
  exception: Ashton made it a real walk-through (`DoorPortal.physicalPassage`).
- `MVP_Template` = start point for new areas; `MVP_UI` = wrist-display test scene; `TheReturn.unity` = a standalone
  side assignment, not in the build list. `BasicScene` / `SampleScene` are vendored template scenes: never edited.

## 4. Code map (`Assets/Scripts/`)

| Folder | What is in it |
| --- | --- |
| `Core/Contracts/` | Interfaces and data every slot codes against: `IChipWallet`, `ICharacterTier`, `IInventoryEvents`, `IGuideVoice`, `TierDefinition`, `ItemData`. No logic; append-only. **Two of the four have no real implementation — section 5.** |
| `Economy/` | `ChipWallet` (the single chip balance), Unity-free `Logic/ChipLedger` + tests. |
| `TableGames/` | Unity-free rules in `Logic/` (Blackjack, Roulette, Dice, Mines, Plinko, `IRng`, `ITableWallet`, `WagerAmount`) with EditMode tests in `Tests/` (~190 across the games, including odds/return tests). Presenters per game, `PressableButton`, `TableHologram`, `ChipWalletTableAdapter`. `Editor/` builders make each table prefab and the VIP room. |
| `Inventory/` | `Keycard`, `KeycardReader`, `KeycardAccess` (static session access list, ids `"VIP"` and `"STAFF"`), `KeycardGiver`, `BarPuzzle`, `ItemLedger`. |
| `Security/` | `PatrolWalker` + `PitBossBuilder`; `SecurityMonitorWall` (live CCTV); the back-room puzzle chain (`WirePanelPuzzle`, `TapeDeckPuzzle`, `VaultKeypad`, `VaultClearance`, `PuzzleSession`, `CctvOutage`) with Unity-free logic + tests. Suspicion meter and guard AI **not built**. |
| `Dialogue/` | `GuideVoice` (the real `IGuideVoice`), `GuideLine`, `GuideTrigger`, Unity-free `Logic/GuideQueue` + tests, `SpeechBubble`. A shared dialogue box with reply choices is **not built**. |
| `UI/` | Wrist display (`WristDisplay`, `WatchHologram`, `WristAttach`, `WristRaiseVisibility`, `GuideCommLink`, `FastTravelWatch`, Unity-free `WristDisplayLogic` + tests); the notebook (`NotebookCaseFile`, `NotebookDisplay`, `NotebookPages`, `NotebookSettings`, Unity-free `MapLogic` / `LeadsLogic` / `NotebookLogic` / `SettingsLogic` + tests); `StartScreen`; `TierVisibility`; mocks. |
| `Environment/` | `SceneFlow`, `DoorPortal`, `SoloTestRoot`, `ComfortSettings`, `SharedStyleKit`, `TheReturn/`, the `Locomotion/` subfolder (below) and the editor builders. |
| `Sandbox/` | Throwaway tests only. |

**Locomotion (`Environment/Locomotion/`, Slot D, the only place movement lives):** `TeleportSurfaces` (maps the walkable
floor of whichever area is loaded by flooding out from the rig's feet on a 0.5 m grid, then covers it with a few
`TeleportSurface` rectangles on physics layer 31 — so **no teleport object exists in any scene file**), `TeleportSurface` +
`TeleportClearance` (the "does a standing player fit here?" test), `TeleportBlink` (0.12 s arrival fade), `MoveRamp`
(0.3 s ease-in on smooth movement), `FastTravel` (watch travel list), Unity-free `Logic/WalkableGridLogic` and
`Logic/FastTravelLogic` + tests.

**Environment editor builders (`Environment/Editor/`):** `StyleKitBuilder`, `WorldBuilder` (**Build MVP World**;
batch `BuildAndBake`), `HallwayBuilder` (hub + wings), `BackOfHouseBuilder`, `CountRoomBuilder`, `CasinoEntranceBuilder`,
`ExteriorDistrictBuilder`, the texture builders, `RenderingOptimizer` (LOD + occlusion), `LocomotionBuilder`
(**Build Locomotion & Comfort**, **Validate Fast Travel**), `LocomotionPlayCheck`, `ReachabilityCheck`.

## 5. Contract status — what is real and what is still a mock

This is the most commonly misunderstood thing in the project. Of the four Day-0 contracts:

| Contract | Real implementation | Status |
| --- | --- | --- |
| `IChipWallet` | `Economy/ChipWallet` (Slot A) | ✅ real and in `MVP_Main` |
| `IGuideVoice` | `Dialogue/GuideVoice` (Slot C) | ✅ real |
| `ICharacterTier` | **none** — only `UI/Mocks/MockCharacterTier` | ❌ Slot B never built it |
| `IInventoryEvents` | **none** — only `UI/Mocks/MockInventoryEvents` and the prototype `UI/Notebook/NotebookPickups` | ❌ Slot B never built it |

Consequences: the watch's TIER line, the tier accessory visuals (steel → gold case → VIP stripe), and `GuideTrigger`'s
"tier reached" lines are all driven by a Slot C test mock. Since the evolutionary-character answer is the centrepiece of
the GDD, **the project's headline feature is propped up by a mock.** Do not describe tier progression as working.

## 6. Performance facts

- Budget (StyleGuide §7): 72 fps (90 if it holds), under ~100k visible triangles, ~100 draw calls, ASTC textures max
  1024 px with mipmaps, no realtime shadows, minimal post effects.
- In use: baked lighting (Progressive CPU lightmapper); one area scene loaded at a time; the street district combined
  into one mesh per material with static batching; LOD groups and baked occlusion culling via `RenderingOptimizer`;
  ASTC 6x6 / 1024 import rules; Simple Lit everywhere.
- **Pending lightmap bakes:** `MVP_SecurityOffice`, `MVP_CameraRoom`, `MVP_SecurityHall`. Ben or Ashton must do these —
  Zach and Pak have no GPU. Until then those rooms are lit by flat ambient and emissive strips only.
- **No on-device frame-rate measurements exist. No APK has ever been built.** The Quest 3 budget check has been open
  since Day 0. Any performance number must be measured (OVR Metrics Tool, Unity Profiler over ADB), never invented.

## 7. Status — built vs not built

**Built and working:** casino floor; blackjack and roulette on the floor; VIP lounge with dice, mines and plinko (all
five games have full Unity-free rule logic and tests); chip wallet; Pit Boss NPC who patrols and hands over the VIP
keycard; keycard readers and the VIP door; bar PIN-pad puzzle; staff-door puzzle; security hub and three wings; the
back-room puzzle chain ending at the vault keypad; live CCTV wall; door portals with fades; entrance with a 3D
night-time street; textures throughout; the wrist watch with hologram HUD and Guide comm link; the journal notebook with
ITEMS / MAP / SETTINGS spreads; the start screen; the Guide's voice with triggers and subtitles; teleport + smooth
movement with the full comfort pass in all six areas.

**Not built:** suspicion meter; guard AI; `CharacterTier`; the real inventory backend; a shared dialogue box with reply
choices; cashier cage interactions; grabbable chip stacks; endings.

## 8. Verification status — what has actually been run

| Thing | State |
| --- | --- |
| `LocomotionPlayCheck` | ✅ **PASS: 12 checks** over all six areas, `Logs/LocomotionQA/play-check.txt`, 2026-11-11 |
| EditMode test suite | ✅ passing as of 2026-11-01 (~277 + locomotion 13); **the 11 new fast-travel tests have never been run** |
| `ReachabilityCheck` | ⚠️ ran 2026-11-11 but **its back-room findings were wrong** (below); fixed 2026-11-12 and **not re-run** |
| Fast travel (this session) | ❌ compiles, nothing else. Never play-tested, never validated, never on device |
| APK on Quest 3 | ❌ **never built, by anyone** |
| On-device frame rate | ❌ never measured |

**The withdrawn finding (important context).** On 2026-11-11 `ReachabilityCheck` reported that the wire panel, tape
archive and HOLD button were "stranded ~40 m outside their rooms" and that the security office's 12 × 12 m room was
unmapped. **This was a bug in the audit, not in Ashton's content.** The check mapped each area once, from where its tour
arrived, and `TeleportSurfaces` floods from the player's feet — so in a wing scene it only ever saw the hallway island
(section 3). The rooms are deliberately 40 m away. The runtime was never affected, because `TeleportSurfaces.Update`'s
stray check already remaps when the player lands off-map after an in-scene door jump. The check now maps every island
before judging anything. The entry to Ashton has been withdrawn. **Do not re-raise this as a content bug.**

## 9. Team rules (from CLAUDE.md / Progress_Vol1.md)

- Edit only your slot's folders and scenes; shared files (`Packages/`, `ProjectSettings/`, `Assets/XR/`, `Core/`) are
  Slot D's. Cross-slot edits happen occasionally but must be flagged in Open questions. Never hand-merge scene YAML.
- Branches: `slot-<x>/<topic>` → PR into `dev`; Zach tests `dev` on the headset and promotes to `main`.
- Every commit adds a line to the author's slot section of `Progress_Vol1.md`.
- **No AI attribution** (no "Co-Authored-By: Claude") in commits or PRs.
- Don't commit `Assets/XR/Settings/OpenXRPackageSettings.asset`, or the `Assets/Samples/.../Lightmap-*.meta` churn that a
  platform switch produces. Commit files by name; never `git add .`.
- Movement is Slot D's alone: never add a locomotion provider, a teleport area or a second way of moving to a scene.

## 10. GDT-120 course: assignments and where the answers live

The course project is a VR adventure quest game; each milestone adds sections to the "GDT-120 Game Design Document"
(repo copy: `docs/GameDesignDocument.md`) and needs an individual professional video per member, linked in the document.
**All four individual videos are still outstanding for the current milestone, and none of them depends on any code.**

| Assignment | What it asks for | Our material |
| --- | --- | --- |
| Project Proposal and Concept Development | GDD Storyline + Characters, project plan, concept video | GDD Storyline, Characters, Project Plan |
| Re-imagining an Experience in VR | Technical report + Loom video | Separate report (not in this repo) |
| Milestone 2 — Platform Compatibility | Movement, interaction, UI on headset; the evolutionary-character question; GDD Platform Compatibility section | `Progress_Vol1.md` Milestone 2, `docs/StyleGuide.md` §9 |
| Environment Creation | Optimised environments, textures, quantitative before/after on three objects | GDD Key Features; §6 — before/after numbers still need measuring |
| Milestone 3 — UI, inventory, dialogue | Menus, inventory, dialogue boxes + two rounds of user testing | Notebook, start screen, SETTINGS spread, Guide voice; dialogue box still missing |
| **Milestone 4 — movement and comfort (current)** | Teleport + smooth movement in every area, motion-sickness mitigations, **two rounds of user testing with ≥ 5 testers each** and fixes from the feedback | `Progress_Vol1.md` Milestone 4, `docs/StyleGuide.md` §10, `docs/UserTesting.md` (protocol + empty result tables) |

## 11. What the team has left to do

**Blocking round 1 of user testing:**
- **Pak (C)** — a first-time "how to move" line, so Q1 isn't answered by the observer's hint for every tester.
- **Ashton (B)** — the three lightmap bakes (longest lead time; Zach cannot do them).
- **Ben (A)** — decide the roulette reach (1.10 m from the nearest teleport-landable spot): seat anchor, move the layout,
  or let round 1 answer.
- **Zach (D)** — verify this session's code, build APK #1, run the sessions.

**Bigger, not milestone-blocking:**
- **Ashton (B)** — `CharacterTier` and the real `Inventory` (section 5). Open since Day 0.
- **Pak (C)** — the shared dialogue box with reply choices; move the Pit Boss onto it.
- **Ben (A)** — cashier cage buy-in / cash-out, grabbable chip stacks.

**Everyone:** individual videos, then the rubric check against the GDD.

## 12. Key documents in the repo

`CLAUDE.md` (working rules — read first), `Progress_Vol1.md` (team plan, per-slot logs, decisions, open questions —
the live source of truth), `docs/GameDesignDocument.md`, `docs/StyleGuide.md` (kit, scale, §7 budget, §9 testing without
a GPU, §10 locomotion), `docs/UserTesting.md` (test protocol and result tables), `docs/reference/VisualReference.md`
(approved look), `docs/SimulatorControls.md`, `docs/plans/`.
