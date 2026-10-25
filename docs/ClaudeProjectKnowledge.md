# High Stakes — project knowledge for Claude

Context file for a Claude Project (claude.ai) so conversations about the game, the code and the GDT-120 course
assignments start from the facts. Snapshot as of 2026-10-18, `dev` branch commit `8574652`. When this file and the
repository disagree, the repository wins.

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
- **Moods:** warm gold/green main floor (chandeliers, brass, green felt); cold blue-teal concrete and steel back-of-house
  and vault. All UI is diegetic (wrist display, notebook, floating prompts, the Guide's earpiece). No magic: realism is
  the twist.
- **Team (slots own folders/scenes to avoid merge conflicts):**

| Slot | Member | Owns |
| --- | --- | --- |
| A | Ben (Benjamin Parenteau) | `Scripts/TableGames/`, `Scripts/Economy/`, `Content/Tables/`, `MVP_VIP` (VIP room builder) |
| B | Ashton (Ashton Calkins) | `Scripts/Inventory/`, `Scripts/Security/`, tiers, keycards, access; built `MVP_Casino` |
| C | Pak (Pakrinha Sim) | `Scripts/UI/`, diegetic UI, wrist display, tier visuals |
| D | Zach (Zachary Scheer) | `Scripts/Environment/`, `Scripts/Core/`, style kit, `MVP_Main`, security hub/wings, shared settings, Quest builds |

- **Hardware:** Zach and Pak have no dedicated GPU (no Quest Link, no GPU lightmapper, no Meta XR Simulator). Test path:
  standalone APK on Quest 3 + XR Interaction Simulator in Play mode. Ben and Ashton can use Link and bake lightmaps.

## 2. Tech stack

Unity `6000.3.23f1` (pinned), URP 17.3 (Simple Lit shaders), OpenXR (Meta Quest, AndroidXR loaders), XR Interaction
Toolkit 3.5.1, XR Hands (installed, not yet used for gameplay), Unity Test Framework (EditMode tests), Git LFS for
images/models. No CLI build: everything is built from the Editor, mostly through **High Stakes >** menu items that
generate scenes and prefabs from code.

## 3. Scenes and flow

- `Assets/Scenes/MVP/MVP_Main.unity` (build index 0) stays loaded: the one player rig (`Kit_PlayerRig`), the `ChipWallet`,
  the wrist display and `SceneFlow`. `SceneFlow` loads exactly one area scene on top: `MVP_Casino` at start, then
  `MVP_VIP`, `MVP_SecurityHall`, `MVP_SecurityOffice`, `MVP_CameraRoom`, `MVP_Vault`.
- `DoorPortal` (with `portalId`, `targetId`, `targetScene`) fades out, swaps the area and places the rig at the target
  door. Area scenes keep a `_SoloTest` rig that `SoloTestRoot` disables when loaded into `MVP_Main`.
- Layout: casino floor → **STAFF ONLY** door → security hub (`MVP_SecurityHall`) with SECURITY / CAMERAS / VAULT doors →
  each wing is a warm casino-style hallway leading to a cool 8 x 8 m room (security office, camera room, vault
  antechamber). The VIP door in the casino is keycard-locked; the VIP lounge has the Stake-style games.
- `MVP_Template` = start point for new areas; `MVP_UI` = wrist-display test scene. `BasicScene` / `SampleScene` are
  vendored template scenes: never edited.

## 4. Code map (`Assets/Scripts/`)

| Folder | What is in it |
| --- | --- |
| `Core/Contracts/` | Interfaces and data every slot codes against: `IChipWallet`, `ICharacterTier`, `IInventoryEvents`, `IGuideVoice`, `TierDefinition`, `ItemData`. No logic; append-only. |
| `Economy/` | `ChipWallet` (the single chip balance), Unity-free `Logic/ChipLedger` + tests. |
| `TableGames/` | Unity-free rules in `Logic/` (Blackjack, Roulette, Dice, Mines, Plinko, `IRng`, `ITableWallet`, `WagerAmount`) with EditMode tests in `Tests/` (blackjack alone has 49; odds/return tests for each game). Presenters per game (`BlackjackPresenter`, `RoulettePresenter`, …), `PressableButton`, `TableHologram` (dealer-side holographic board), `ChipWalletTableAdapter`. `Editor/` builders make each table prefab and the VIP room. |
| `Inventory/` | `Keycard`, `KeycardReader` (trigger collider, locks a `DoorPortal`, red→green light, sign text), `KeycardAccess` (session access list + event), `KeycardGiver` (Pit Boss hands over the card), `BarPuzzle` (bar PIN pad, one-time $500 reward). |
| `Security/` | `PatrolWalker` (Pit Boss walks a loop, can pause); `PitBossBuilder`. Suspicion meter and guards not built yet. |
| `Dialogue/` | `SpeechBubble`. Guide voice lines not built yet. |
| `UI/` | Wrist display (`WristDisplay`, `WatchHologram`, `WristAttach`, `WristRaiseVisibility`, Unity-free `WristDisplayLogic` + tests), `TierVisibility`, mocks for solo testing. The watch sits on the back of the left controller. |
| `Environment/` | `SceneFlow`, `DoorPortal`, `SoloTestRoot`, `SharedStyleKit`, teleport/interaction setup, and the editor builders below. |
| `Sandbox/` | Throwaway tests only. |

**Environment editor builders (`Environment/Editor/`):** `StyleKitBuilder` (shared kit: materials, prefabs, lighting,
player rig), `WorldBuilder` (**Build MVP World**: regenerates VIP, security area, casino doors/tables, `MVP_Main`, build
list; re-applies textures; batch `BuildAndBake`), `HallwayBuilder` (security hub + wings), `CasinoEntranceBuilder` +
`ExteriorDistrictBuilder` (entrance wall, 39-building street district merged by material), `CasinoTextureBuilder` +
`CasinoDetailTextures` (casino textures), `SecurityTextureBuilder` (back-of-house textures), `CasinoTextureImporter`
(Android import rules).

**Content (`Assets/Content/`):** `StyleKit/` (shared materials `M_*`, prefabs `Kit_*`, lighting), `Environment/` (Slot D:
casino and security textures/materials/prefabs, portal doors), `Tables/` (table prefabs), `Inventory/`, `Progression/`
(bar puzzle), `Characters/PitBoss/` (Microsoft Rocketbox model, MIT, 7.3k triangles), `UI/`.

## 5. Look and textures (as of 2026-10-17)

- Shared style kit: one set of URP Simple Lit materials, kit prefabs on a 2 m grid, 3 m ceilings. New looks are
  **Material Variants** of kit materials (no new base materials).
- Textures are CC0 from Poly Haven (plus our own generated ones), recoloured into the kit palette by
  `tools/generate_security_textures.py`, `generate_casino_detail_textures.py`, `generate_casino_wallpaper.py`,
  `generate_casino_carpet.py`. Credits in each `Content/Environment/*/README.md`.
- Casino: generated carpet, cream-and-gold damask wallpaper, wood wainscot, marble, carved coffered wood ceiling,
  rosewood beams, dark wood pilasters and table wood, wool felt, burgundy leather seats and padded table rails, brushed
  brass/steel, black slot cabinets, glowing lamp glass, FBX picture frames. Same in the VIP lounge (Ben approved
  texture-only changes) and hub/hallways.
- Security rooms: painted cinder block, sealed concrete floor, diamond tread plate (vault), steel panel doors, generated
  acoustic drop ceiling.
- Menus: **High Stakes > Casino Textures > Build Casino Textures / Apply Casino Textures To All Area Scenes**,
  **High Stakes > Security Textures > Build And Apply To Security Wings**. They only override materials, so baked
  lightmaps are kept.

## 6. Performance facts

- Budget (StyleGuide §7): 72 fps (90 if it holds), under ~100k visible triangles, ~100 draw calls, ASTC textures max
  1024 px with mipmaps, no realtime shadows, minimal post effects.
- In use: baked lighting (Progressive CPU lightmapper, 20 texels/unit; lightmaps in VIP and the three wings; casino and
  hub still on flat ambient), one area scene loaded at a time, the street district combined into one mesh per material
  with static batching, ASTC 6x6 / 1024 import rules applied automatically, Simple Lit everywhere, emissive lamps flagged
  so they don't affect bakes.
- Not yet: LOD groups, baked occlusion culling. Known issue: kit floor tile is not static, so floors get no lightmaps.
- **No on-device frame-rate measurements exist yet** (the Quest 3 budget check is an open task). Any performance numbers
  must be measured (OVR Metrics Tool, Unity Profiler over ADB, Frame Debugger, Rendering Statistics), never invented.

## 7. Status

Built: casino floor with blackjack and roulette; VIP lounge with dice, mines, plinko; chip wallet; wrist chip display with
tier visuals; Pit Boss NPC who patrols and hands over the VIP keycard; keycard reader on the VIP door; bar PIN-pad puzzle;
security hub + three wings; door portals with fades; entrance with a 3D night-time street outside; textures throughout.

Not built: suspicion meter, guard AI, vault passcode puzzle, the Guide's voice lines and earpiece, notebook inventory,
NPC dialogue options, cashier cage interactions, comfort settings panel, endings.

## 8. Team rules (from CLAUDE.md / Progress_Vol1.md)

- Edit only your slot's folders and scenes; shared files (`Packages/`, `ProjectSettings/`, `Assets/XR/`, `Core/`) are
  Slot D's. Never hand-merge scene YAML.
- Branches: `slot-<x>/<topic>` → PR into `dev`; Zach tests `dev` on the headset and promotes to `main`.
- Every commit adds a line to the author's slot section of `Progress_Vol1.md`.
- No AI attribution (no "Co-Authored-By: Claude") in commits or PRs.
- Don't commit `Assets/XR/Settings/OpenXRPackageSettings.asset` (Unity churn).

## 9. GDT-120 course: assignments and where the answers live

The course project is a VR adventure quest game; each milestone adds sections to the "GDT-120 Game Design Document"
(repo copy: `docs/GameDesignDocument.md`) and needs an individual professional video per member, linked in the document.

| Assignment | What it asks for | Our material |
| --- | --- | --- |
| Project Proposal and Concept Development | GDD Storyline + Characters sections (plot, hero customization, environments, exploration/puzzle mechanics), project plan with milestones, concept video | GDD Storyline, Characters, Project Plan (15-week timeline) |
| Re-imagining an Experience in VR | Technical report: a life experience, a software application and a game, each with an image (credited), 5+ characteristics re-imagined in VR, a Unity mock scene, who benefits; greater purpose of VR, impact on game design, Christian worldview on purposeful design; Loom video | Separate report (not in this repo) |
| Milestone 2 (Platform Compatibility) | Prototype on headset: movement, environment interaction, UI navigation; "what storytelling, worldbuilding and mechanics can the evolutionary character communicate?"; GDD Platform Compatibility Design section in Tasks Breakdown | GDD Tasks Breakdown note, `Progress_Vol1.md` Milestone 2 plan, `docs/StyleGuide.md` §9 |
| Environment Creation (due 2026-10-18) | Create and optimise 3D environments; textures/models for immersion and consistency; performance testing with a **quantitative before/after comparison covering three specific objects**; GDD Exploration, Puzzles and Collectibles sections | GDD Key Features sections; texture work (§5); performance facts (§6) — before/after numbers still need measuring |
| Design responses (five questions: new environment concept sketch, re-imagining a real-world scenario, puzzle evaluation, project plan after a rejected pitch, LOD/occlusion/lightmapping) | Written answers with sketches and code | Claude Doc "High Stakes — VR Adventure Quest Design Responses" (proposes the Counting Room, co-op camera-room heist, mixed-reality table mode; marks LOD/occlusion as planned) |

## 10. Key documents in the repo

`docs/GameDesignDocument.md` (GDD), `docs/StyleGuide.md` (kit, scale, budget, testing without a GPU),
`docs/reference/VisualReference.md` (approved look), `Progress_Vol1.md` (team plan, logs, decisions, open questions),
`CLAUDE.md` (working rules), `docs/plans/VrstakeGamesPort.md`, `docs/SimulatorControls.md`.
