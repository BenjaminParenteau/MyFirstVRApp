# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Unity VR project (`MyFirstVRApp`) built from Unity's VR Template, targeting OpenXR devices (Meta Quest / AndroidXR). Unity Editor version is pinned in `ProjectSettings/ProjectVersion.txt` (currently `6000.3.23f1`) — open and build with that exact Editor version to avoid asset reserialization churn across the whole project.

Custom project code lives almost entirely under `Assets/Scripts/`; everything else under `Assets/` (`VRTemplateAssets/`, `Samples/`, `XRI/`, `XR/`, `CompositionLayers/`) is template/package content pulled in by the Unity VR Template and the XR Interaction Toolkit — treat those as vendored, not hand-edited.

## Game being built: "High Stakes"

The project is being developed into a VR casino-heist game for Meta Quest 3 — free-roam casino floor with real table games (blackjack, roulette, ...) as the core loop, layered with a stealth/heist metagame (suspicion meter, guard patrols, keycards, a multi-source vault passcode puzzle) and an unseen voice-only handler ("the Guide") driving the narrative. Full design reference: [docs/GameDesignDocument.md](docs/GameDesignDocument.md). **Vision / concept art reference: [docs/reference/](docs/reference/README.md)** — read [docs/reference/VisualReference.md](docs/reference/VisualReference.md) before building any environment, UI, character, or audio so the result matches the approved look (warm gold/green casino floor, cold blue back-of-house, diegetic UI only, suspicion-meter dial, etc.). Team: Zachary Scheer, Ashton Calkins, Pakrinha Sim, Benjamin Parenteau.

### Script folder layout — `Assets/Scripts/`

Each system below has a dedicated subfolder. Put new scripts in the one they belong to instead of dropping everything at the top level of `Scripts/`; each folder's `README.md` restates its one-line purpose in the Editor:

- `TableGames/` — blackjack, roulette, and any future poker/slots. Share a common base/interface for bet placement, payout, and win/loss evaluation instead of reimplementing betting and chip-payout logic per game; each game script should only own its game-specific rules (card values, wheel physics).
- `Economy/` — the single chip wallet/economy source of truth. Every table, the cashier cage, and cosmetic purchases read and write through here; don't let each minigame track its own local chip balance.
- `Security/` — guard patrol AI and the suspicion meter, kept decoupled: patrol scripts just move guards and report player detection; the suspicion meter subscribes to detection / high-value-win / restricted-area-entry events rather than being reached into directly.
- `Inventory/` — keycards, passcode fragments, and intel notes as data (e.g. ScriptableObjects), driven through one shared pickup/interaction system rather than per-item logic.
- `Dialogue/` — NPC dialogue prompts and the Guide's voice-line triggers, routed through one shared dialogue/trigger system rather than scattered per-NPC scripts.
- `UI/` — diegetic UI (wrist chip counter, notebook inventory, floating dialogue prompts). Should only observe/display state from `Economy/`, `Inventory/`, and `Dialogue/` — presentation code shouldn't own gameplay state.
- `Sandbox/` — scratch/throwaway scripts used to test an idea or a Unity feature in isolation (e.g. `TestScript.cs`). Nothing in here is part of a real system; move or delete code once it graduates into one of the folders above instead of leaving it in `Sandbox/`.

If generating custom geometry or using Unity's low-level `GL`/immediate-mode rendering APIs from C#, keep that code in its own renderer/utility scripts inside the relevant system folder rather than mixing it into gameplay logic.

## Team workflow files

- [Progress_Vol1.md](Progress_Vol1.md) — shared project memory for the current (Vol. 1, 15-week) assignment: team split, weekly plan, commit log, decisions, blockers. Read it at the start of a session to see where things stand.
- `Personal.md` — **git-ignored**, one local copy per team member (4 members). Holds that member's identity, owned areas, current focus, and preferences for Claude. Read it if present; never commit it or copy its contents into tracked files. If it's missing, tell the user to create it by copying `docs/Personal.template.md` to `Personal.md` in the repo root.

### Hardware constraint: not everyone has a GPU

Zach (Slot D) and Pak (Slot C) have laptops with no dedicated GPU, so they **cannot use Quest Link / PC VR**; Ashton (B) and Ben (A) can. Never assume Link: the primary test path is a standalone Android APK on Quest 3 plus the XR Interaction Simulator in Play mode. Don't enable the GPU lightmapper or suggest GPU-dependent tools (e.g. Meta XR Simulator) to Zach or Pak. Details: [docs/StyleGuide.md §9](docs/StyleGuide.md#9-testing-without-a-gpu) and the Team hardware table in `Progress_Vol1.md`.

### One player rig

The game has exactly one XR Origin: `Assets/Content/StyleKit/Prefabs/Kit_PlayerRig.prefab` (owned by Slot D, generated by `StyleKitBuilder`). Never add another XR Origin or Camera to a scene, and never attach slice content inside the rig; attach at runtime instead. Slice scenes keep their test rig under `_SoloTest`, which `SoloTestRoot` disables when loaded additively into `MVP_Main`. View changes (sitting at a table, the roulette wheel) move the one rig to an anchor; never rotate or move the player's view without input. See [docs/StyleGuide.md §5c](docs/StyleGuide.md).

### Ownership and merge-conflict rules

Before editing anything, check the **Merge-conflict rules** in `Progress_Vol1.md`: each team slot (A–D) owns specific `Assets/Scripts/<Area>/` and `Assets/Content/<Area>/` folders and its own scene under `Assets/Scenes/MVP/`; shared files (`Packages/`, `ProjectSettings/`, `Assets/XR/`, `Assets/Scripts/Core/`) belong to Slot D. Read the user's `Personal.md` to learn their slot, and don't modify files outside it — flag the need in Open questions instead.

**Current milestone focus (Milestone 2 MVP):** basic movement, environment interaction, and diegetic UI on Quest 3, built around the *evolutionary character* idea — the player's hands/wrist/accessories and abilities upgrade as chips grow (tiers). Do **not** build NPC characters, guards, suspicion, or dialogue trees yet. Environments must follow [docs/StyleGuide.md](docs/StyleGuide.md) and use only the shared style kit (`Assets/Content/StyleKit/`, owned by Slot D) and start from `MVP_Template`; don't add new base materials, lighting, or off-scale geometry. Details in `Progress_Vol1.md`.

### Progress log rule

**Every commit must update the current `Progress_VolN.md`** (currently `Progress_Vol1.md`). Before committing, add a one-line entry at the top of **your own slot's sub-section** of its **Log** (`YYYY-MM-DD · what changed`; never edit another slot's sub-section, so merges don't conflict), and update the Weekly plan status, Decisions, or Open questions if the commit affects them. Include the Progress file in the same commit. Don't commit without it.

**No AI attribution in git history:** never add `Co-Authored-By: Claude` (or any Claude/AI attribution) to commit messages or PR descriptions in this repo — the team does not want it.

Each new class assignment gets its own file: when Vol. 2 starts, create `Progress_Vol2.md`, keep `Progress_Vol1.md` as an archive, and update the file name in this section.

## Working with this repo (no CLI build/test pipeline)

There is no command-line build, lint, or test runner configured — this is a stock Unity project driven through the Editor:
- **Build**: Unity Editor → File > Build Settings (or `Assets/Settings/Project Configuration/Android Preset.asset` / `Standalone Preset.asset` for platform-specific settings).
- **Tests**: `com.unity.test-framework` is a dependency but no `Tests` assembly/folder exists yet in `Assets/`. If you add tests, use the Unity Test Runner (Window > General > Test Runner) with EditMode/PlayMode test assemblies.
- **Scenes**: `Assets/Scenes/BasicScene.unity` and `Assets/Scenes/SampleScene.unity` are the entry points; corresponding scene templates live in `Assets/Settings/Project Configuration/`.

## Key package dependencies (`Packages/manifest.json`)

- `com.unity.xr.interaction.toolkit` 3.5.1 (XRI) — primary interaction framework; sample content is copied into `Assets/Samples/XR Interaction Toolkit/`.
- `com.unity.xr.openxr` / `com.unity.xr.androidxr-openxr` / `com.unity.xr.meta-openxr` — OpenXR runtime + vendor loaders, configured under `Assets/XR/`.
- `com.unity.xr.hands` — hand tracking.
- `com.unity.render-pipelines.universal` (URP) 17.3.0 — rendering pipeline; global/quality configs in `Assets/Settings/Project Configuration/`.
- `com.unity.purchasing` — IAP; `Assets/Resources/BillingMode.json` configures the Android store backend.

## Conventions to follow when adding code

- **DRY**: before adding a new MonoBehaviour or utility, check `Assets/Scripts/` and the XRI/VRTemplateAssets sample code for an existing component that already does it — XRI ships many reusable interactors/affordances; prefer composing those over writing new ones.
- **Separation of concerns**: keep gameplay/app-specific scripts in `Assets/Scripts/`, out of the vendored `Samples/`, `VRTemplateAssets/`, and `XRI/` trees. Don't modify vendored/sample assets in place — if template behavior needs to change, subclass or wrap it from `Assets/Scripts/` instead, so future template/package updates don't silently overwrite custom logic.
- Every asset needs its paired `.meta` file committed alongside it (standard Unity requirement) — when adding files outside the Editor, generate or let Unity generate the `.meta` before committing.

## Git notes specific to this project

- `.gitattributes` is configured for Unity (LF line endings, LFS filters, `.cs`/shader/asset text handling) — don't hand-edit line-ending behavior per file.
- Opening the project in a different Editor version than `ProjectSettings/ProjectVersion.txt` will reserialize large numbers of `.meta`/`.fbx`/`.png`/package-lock files even without real changes; avoid committing that churn mixed in with feature work when possible.
