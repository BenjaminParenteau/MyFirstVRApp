# Testing without a headset — keyboard and mouse controls

For anyone on a laptop with no dedicated GPU (Zach, Pak) or anyone who just doesn't have the headset on. No physical controller is needed. The **XR Interaction Simulator** in `MVP_Template` draws two virtual controllers in the Game view and you drive them with the mouse and keyboard. The floating labels (Move, Grab, UI Press, Blink) are the VR template's controller tooltips.

Context and the APK / Link alternatives: [StyleGuide.md §9](StyleGuide.md#9-testing-without-a-gpu).

## Quick start

1. Open `Assets/Scenes/MVP/MVP_Template.unity` (or your own scene duplicated from it) and press **Play**.
2. **Click inside the Game view** so it has keyboard focus.
3. **WASD moves whichever device is active**, not necessarily the player. If WASD moves the controllers, press **Tab** (or **H**) to make the headset active.
4. There are two ways to move:
   - **Walk the headset** (like walking around your play space): headset active, **WASD** + hold **right mouse** to look.
   - **Real locomotion** (what the player does in-game, use this for the movement check): press **[** for the left controller and hold **I J K L** to push its thumbstick (smooth move). Press **]** for the right controller: **J / L** turns, push **I** to aim a teleport and release.
5. The simulator also shows a small controls panel in the Game view while playing.

## Controls (default bindings)

| Do this | Press |
|---|---|
| Move the active device (headset or controller) | **W A S D** (hold) |
| Look around (rotate the headset) | Hold **right mouse button** + move the mouse |
| Point the controller | Move the mouse |
| Select / grab | **Left mouse button** (hold) |
| Trigger | **T** (hold) |
| Grip | **G** (hold) |
| Thumbstick of the active controller (left = smooth move, right = turn / teleport) | **I J K L** (hold) |
| Primary / Secondary button | **1** / **2** (hold) |
| Menu button | **M** (hold) |
| Joystick click | **3** / **4** (hold) |
| Activate left controller / right controller / both | **[** / **]** / **[ ]** |
| Press **[** or **]** twice | Switch between Controller mode and Hand mode |
| First-person mode ⇄ controller/hand mode | **Tab** |
| Toggle headset only | **H** |
| Reset the active device | **R** |
| Hold **Shift** | Hotkeys act on the **left** controller instead of the right |
| Constrain movement to an axis | **Z** (z-axis), **V** (x-axis), **C** (y-axis) (hold) |
| Mouse wheel while holding right mouse | Move forward / backward |

A simulated controller only responds while it is the *active* one, so press **[** or **]** first. A button held on a controller stays held when you switch to the other one, which lets you grab with both hands.

## Things to check in every scene

- Walk around and confirm scale against the door, table and chair.
- Grab something (once your slice has a grabbable) with the left mouse button or **G**.
- Tap any wrist or world UI with the left mouse button.
- Teleport / Blink and smooth move both work.

## Troubleshooting

- **Nothing responds:** click the Game view; make sure exactly one *XR Interaction Simulator* and one XR rig are in the scene.
- **Two simulators:** don't also enable "Use XR Interaction Simulator in scenes" in Project Settings > XR Plug-in Management > XR Interaction Toolkit. The template already has one.
- **Controller isn't moving:** press **[** or **]** to activate it.
- **WASD moves the controllers instead of me:** the controllers are the active device. Press **Tab** or **H** for the headset, or use **I J K L** on the left controller to move with locomotion.
- **Want different keys:** edit the Input Action assets in `Assets/Samples/XR Interaction Toolkit/<version>/XR Interaction Simulator/`. That's a shared change, so ask Zach.

The simulator does **not** show comfort, real-world scale feel or framerate. Check those on the headset with an APK (or Quest Link if you have a GPU).
