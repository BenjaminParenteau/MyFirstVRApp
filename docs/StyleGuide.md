# High Stakes — Style Guide (standards)

The rules every scene, prop and light must follow so all four slices look like one game. Owned by Slot D (Zach). The values here are what `High Stakes > Build Style Kit` generates (see [StyleKitBuilder.cs](../Assets/Scripts/Environment/Editor/StyleKitBuilder.cs)); change a number in both places or ask Zach. Target look: [reference/VisualReference.md](reference/VisualReference.md).

## 1. How to use the kit

1. Pull the `day-zero` / `stylekit-vN` tag from `main`.
2. **Start every scene by duplicating `Assets/Scenes/MVP/MVP_Template.unity`** (Save As into your own MVP scene). Never start from `BasicScene` / `SampleScene`.
3. Build only from prefabs in `Assets/Content/StyleKit/Prefabs/` and materials in `.../Materials/`. The `SharedStyleKit.asset` in the same folder references all of them.
4. Before opening a PR run **High Stakes > Validate Open Scene Against Style Kit** (fixes any warnings it lists), then check the scene against the visual reference in the XR Interaction Simulator or on the headset as an APK (see [§9](#9-testing-without-a-gpu)).

## 2. Two moods

| | Warm (main floor, VIP lounge) | Cool (back-of-house, vault) |
|---|---|---|
| Feel | Gold, brass, green felt, dark wood, chandelier glow | Blue-teal, concrete, steel, dim fluorescent |
| Light preset | `Light_WarmFloor` — `#FFC88A`, point, range 9, intensity 2 | `Light_CoolCorridor` — `#8FB8FF`, point, range 6, intensity 1.5 |
| Floor / wall prefabs | `Kit_FloorTile_Warm`, `Kit_Wall_Warm`, `Kit_Doorway_Warm` | `Kit_FloorTile_Cool`, `Kit_Wall_Cool`, `Kit_Doorway_Cool` |
| Ambient | Flat `#3A3026` | Flat, cooler; ask Zach for a cool template variant |

## 3. Palette / materials

All materials use URP **Simple Lit** (cheapest lit shader; Quest-friendly). Names are `M_<Name>`.

| Material | Hex | Smoothness | Use |
|---|---|---|---|
| CarpetRed | `#5A1420` | 0.10 | Casino floor |
| FeltGreen | `#0F5A3A` | 0.05 | Table felt |
| WoodDark | `#3B2416` | 0.35 | Tables, trim |
| BrassGold | `#B8893A` | 0.65 | Fixtures, bases, accents |
| PlasterCream | `#CDBB98` | 0.10 | Warm walls, ceilings |
| FabricBurgundy | `#5A1A2B` | 0.05 | Chairs, upholstery |
| ConcreteCool | `#6E7780` | 0.10 | Back-of-house walls |
| SteelCool | `#8FA3B0` | 0.70 | Metal, vault, readers |
| TileCool | `#4E5A63` | 0.35 | Back-of-house floor |
| ScreenGlow | `#38D6E8` | 0.50 | Emissive screens / readers |
| ChipWhite / Red / Green / Black | `#E8E4D8` / `#B3202A` / `#1E8A4C` / `#1A1A1A` | 0.30 | Chip denominations |
| KeycardPlastic | `#E9EDF0` | 0.40 | Keycards |

UI/accent colours (in `SharedStyleKit`): cyan `#38D6E8` (wrist display, Guide comm link), alert red `#D8342C`, safe green `#3CC86E` (suspicion dial, later).

Need a new look? Make a **Material Variant** of a kit material in your own `Assets/Content/<Area>/` folder. Do not create new base materials or use other shaders.

## 4. Scale and grid (1 unit = 1 m)

| Item | Size |
|---|---|
| Grid snap | 0.5 m |
| Floor / ceiling tile | 2 × 2 m |
| Corridor width | 2 m |
| Door opening | 1.0 × 2.1 m |
| Wall | 2 m wide, 3 m tall, 0.15 m thick |
| Ceiling height | 3 m |
| Table top height | 0.75 m (round table Ø 1.8 m) |
| Seat height | 0.45 m |
| Chip | Ø 39 mm |
| Keycard | 85.6 × 54 mm |
| Player reference figure | 1.8 m |
| Max step height (player can walk up onto) | 0.3 m |

`Kit_ScaleReference` (in the template scene, under `_SoloTest`) has a 1.8 m figure, a 1 m cube, a door, a table and a chair to sanity-check against. It switches off automatically with `_SoloTest` in `MVP_Main`.

## 5. Lighting

- **Baked lighting only.** Shared Lighting Settings asset: `Assets/Content/StyleKit/Lighting/StyleKit_Lighting.lighting` (progressive CPU, baked GI on, realtime GI off, 20 texels/unit, 1024 max lightmap).
- Max **2** non-baked lights in a scene; **no realtime shadows**.
- Shared post-processing: one global Volume using `StyleKit_Volume` (Neutral tonemapping, +5 saturation). No bloom, depth of field, motion blur or SSAO (too expensive on Quest 3).
- No skybox — interiors use flat ambient.

## 5b. Zones (where each slice builds)

All slice scenes are loaded together by `MVP_Main`, so everyone builds at **agreed world positions**, not at 0,0,0. `MVP_Template` shows each zone as a flat cyan outline on the floor (`Zones` object, tagged EditorOnly so it never ships). Build your content inside your zone and don't move the markers.

| Zone | Owner | Centre (x, z) | Size |
|---|---|---|---|
| Player spawn | D | (0, -2) | — |
| `Zone_A_CashierCage` | A (Ben) | (-3.5, 1.5) | 3 × 3 m |
| `Zone_A_Table` | A (Ben) | (2.5, 1.5) | 3 × 3 m |
| `Zone_B_VIPDoor` | B (Ashton) | (-3.5, 6.5) | 3 × 3 m |
| UI / hand visuals | C (Pak) | on the player rig, no floor zone | — |
| Casino room shell | D (Zach) | built around all zones in `MVP_World` | — |

These are MVP placeholder positions. Zach moves them if the casino floor layout needs it, announces it as a new `stylekit-vN`, and updates this table and `StyleKitBuilder.cs` together.

## 5c. One player rig (XR Origin) — never add another

The **player rig** (Unity's term: **XR Origin**) is the player: the headset camera, both controllers, the body collider and locomotion. The game has **exactly one**: `Kit_PlayerRig` in `Assets/Content/StyleKit/Prefabs/`, a Prefab Variant of the VR Template rig with our settings (e.g. step height 0.3 m). It is the single source of truth: change it there (ask Zach) and every scene gets the change.

**Rules**
- Never add another XR Origin, XR rig, or Camera to your scene, and never edit the rig inside your scene. Two rigs = two cameras fighting over the headset.
- Your scene's rig lives under `_SoloTest` so you can test alone. When `MVP_Main` loads your scene, `_SoloTest` switches itself off (`SoloTestRoot`), so only Zach's rig in `MVP_Main` remains.
- Don't put your content *inside* the rig (e.g. wrist UI under the left controller). It would be switched off with `_SoloTest`. Instead make it a prefab that finds the rig at runtime and attaches itself (look up the active `XROrigin`, then its left/right controller).

**"Different camera angles" (sitting at a table, roulette)**
In VR there are no camera angles: the camera is the player's head and only the player moves it. To change the view, **move the one rig**, never add a camera:
- *Sit at a table:* a seat anchor (empty Transform) per seat; pressing "sit" moves the rig to that anchor and faces it at the table. Same for stepping up to the roulette wheel.
- *Roulette:* spin the **wheel**, not the player. Rotating or moving the player's view without their input causes motion sickness and breaks Quest comfort guidelines.
- Close-ups (e.g. reading the wheel result) are done by placing the player well, or with a diegetic display, never by forcing the view.

## 5d. How to start your scene

1. Pull `main` (tag `day-zero` or newer).
2. Duplicate `Assets/Scenes/MVP/MVP_Template.unity` (select it, **Ctrl+D**), rename to your scene (`MVP_Tables`, `MVP_Progression`, `MVP_UI`, `MVP_World`).
3. Rename `SliceContent (...)` to `<Area>_Content` (e.g. `Tables_Content`) and build **only inside that object, inside your zone** (§5b). Leave `_SoloTest` and `Zones` alone.
4. Test with Play + the simulator ([SimulatorControls.md](SimulatorControls.md)), then on the headset.
5. Run the style check (§1) and open a PR.

## 6. Prefab and naming conventions

- Kit prefabs: `Kit_<Thing>`; your own prefabs: `<Area>_<Thing>` (e.g. `Tables_Chip`, `UI_WristDisplay`).
- Materials `M_<Name>`, scenes `MVP_<Slice>`, ScriptableObjects `SO_<Thing>`.
- Modular greybox pieces are **placeholders**: art can be swapped later without moving anything, because the footprints/pivots stay the same (floor tile pivot top-centre at y = 0, wall pivot at floor centre).
- Colliders: kit floors/walls/tables have box or mesh colliders from the primitives; chips, keycards and the reader are **mesh only** — their owners add rigidbodies and XRI components.

## 7. Quest 3 performance budget

- Target **72 fps** (90 if it holds).
- Under ~100k visible triangles, ~100 draw calls, few unique materials (the kit keeps this low).
- Textures compressed (ASTC), mipmaps on, max 1024 unless justified.
- No realtime shadows, no post effects beyond the shared volume, no transparent overdraw-heavy effects.
- Test on the device (APK) before merging anything that adds meshes or lights. Quest Link is fine too, but only Ashton and Ben can use it. Judge framerate on the APK, since Link renders on the PC GPU and is not representative.

## 8. Change process

Only Zach edits the kit. Ask in `Progress_Vol1.md` → Open questions; changes ship as a new `stylekit-vN` tag and Zach announces it in the Log. Re-running the builder updates existing kit assets in place (GUIDs stay stable), but it **overwrites manual tweaks to kit assets** — put lasting changes in `StyleKitBuilder.cs`.

## 9. Testing without a GPU

Zach and Pak have no dedicated GPU, so they cannot use Quest Link or PC VR; Ashton and Ben can. Every scene must therefore work three ways: in the simulator, as a standalone APK on Quest 3, and (optionally) over Link.

| Path | Who | Use it for |
|---|---|---|
| **XR Interaction Simulator** (Play mode, mouse + keyboard) | Everyone | Fast iteration: movement, grabbing, UI taps, logic. `MVP_Template` includes it (disabled when loaded additively). |
| **Standalone APK on Quest 3** | Everyone | Real headset check: scale, comfort, framerate, the PR visual check. **Primary target.** |
| **Quest Link / PC VR** | Ashton, Ben only | Quick on-headset iteration without building. Optional; never the only test. |

**Keyboard and mouse controls for the simulator:** see [SimulatorControls.md](SimulatorControls.md).

**One-time setup for the simulator:** Package Manager > XR Interaction Toolkit > Samples > import **XR Interaction Simulator** (shared-file change, Zach does this once and commits it). Then Project Settings > XR Plug-in Management > XR Interaction Toolkit > enable the Interaction Simulator settings if you want it auto-added.

**APK workflow:**
1. Quest 3: enable Developer Mode (Meta Horizon phone app), connect by USB-C, accept the debugging prompt.
2. Unity: File > Build Profiles > Android > *Build and Run* (or build the APK and `adb install -r <file>.apk`, or drag it into Meta Quest Developer Hub).
3. Launch from Library > Unknown Sources on the headset.

**Keep the Editor usable on integrated graphics:**
- Never enable the GPU lightmapper; the kit uses Progressive CPU on purpose.
- Do not bake shared-scene lightmaps on a no-GPU laptop. Ben or Ashton bakes and commits them.
- Keep Scene/Game views small, turn off Gizmos and Scene lighting when orbiting heavy scenes, and keep scenes within the §7 budget.
- The Meta XR Simulator is not supported here because it needs a capable GPU; use the XR Interaction Simulator instead.
