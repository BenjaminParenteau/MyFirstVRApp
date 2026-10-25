# LOD and occlusion culling (WIP)

Status: **work in progress, not started.** Lightmapping is done (baked per area, light probes over every table). This plan adds the other two Quest 3 optimisations and the measurements to prove they help.

Budget we are measuring against (Meta, Quest 3 at 72 fps): under ~1.5M triangles and under ~200 draw calls per frame. Style guide target: under 100k visible triangles.

## 1. Measure first (baseline)

Nothing has been measured on the headset yet, so every later step compares against this.

- Build the Quest 3 APK (Android, IL2CPP, ARM64) and play through every area: Casino, VIP lounge, Security hall, Security office, Camera room, Vault.
- Record per area: fps (OVR Metrics Tool on the headset), triangles and draw calls (Frame Debugger / Rendering Profiler over Link on Ben's or Ashton's PC; the APK itself is the test for Zach and Pak).
- Log the numbers in a table at the bottom of this file.

Done when: a baseline row exists for every area.

## 2. Level of Detail (LOD)

Most of the world is kit primitives (cubes, cylinders, a few hundred triangles each), and LOD buys nothing there. LOD goes on the meshes that are actually heavy:

| Mesh | Today | Plan |
|---|---|---|
| Pit Boss (`Content/Characters/PitBoss/Business_Male_04.fbx`, Rocketbox) | 7.3k tris, hipoly only | LOD0 7.3k, LOD1 ~3k, LOD2 ~1k, cull past ~25 m |
| Future NPCs (dealers, guards, bartender, crowd) | not built | Same three levels from day one; crowds are where LOD really pays |
| Casino models (`fancy_picture_frame_01.fbx`, any later imported props) | single mesh | LOD1 at half, cull when tiny on screen |
| Plinko peg mesh (runtime, one combined mesh) | full detail | Fewer sides per peg when far from the board |

How:
- Prefer Unity 6's built-in Mesh LOD (model importer: Generate Mesh LODs), so the LODs are made at import and need no extra assets. Fall back to a `LODGroup` with simplified meshes if it doesn't handle the skinned Pit Boss well.
- Set it in the builder or importer that makes the asset (`PitBossBuilder`, `CasinoTextureImporter`), not by hand in scenes, so a world rebuild keeps it.
- Animator culling stays on (`CullUpdateTransforms`), so far-away NPCs also skip animation.

Done when: the Pit Boss swaps levels as you walk away (checked in the Scene view with LOD coloring), and an EditMode test fails if the Pit Boss prefab loses its LODs.

## 3. Occlusion culling

Each area is a closed room or hallway, so walls block most of the level from any point. Occlusion culling stops Unity drawing what is behind them.

- Mark the static environment (walls, floors, bar, tables, the street) **Occluder Static** and **Occludee Static** in `WorldBuilder` / `HallwayBuilder` / `VipRoomBuilder`, next to where they're already marked static for lightmapping. Small props stay Occludee only; glass, holograms and the player rig are neither.
- Bake occlusion per area scene as part of `WorldBuilder.BuildAndBake`, right after the lightmap bake. `SceneFlow` makes the loaded area the active scene, so its own occlusion data is the data in use.
- Expected wins: the hallways and back-of-house rooms (tight walls). On the open casino floor the gain is small, since most of it is in view; the bar and the street entrance are the main occluders there.

Done when: every area scene has occlusion data, and the Occlusion Culling visualization shows the hidden rooms and back walls disappearing.

## 4. Measure again

- Same APK playthrough as step 1; add an "after" row for every area.
- Pass: every area holds 72 fps and stays under the triangle and draw-call budgets.
- If an area still misses, the next suspects are transparent hologram overdraw and texture memory, not LOD/occlusion.

## Milestones

| # | Deliverable | Depends on |
|---|---|---|
| M1 | Baseline numbers for every area (APK) | APK build |
| M2 | Pit Boss LODs + test | - |
| M3 | Occluder/occludee flags in the builders + occlusion baked in `BuildAndBake` | - |
| M4 | After numbers, results table filled in | M1-M3 |

## Results

| Area | Before fps / tris / draw calls | After fps / tris / draw calls |
|---|---|---|
| Casino | | |
| VIP lounge | | |
| Security hall | | |
| Security office | | |
| Camera room | | |
| Vault | | |
