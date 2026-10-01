# Wrist display

A chunky watch on the left wrist that shows **BANKROLL** (chips) and the current **tier**, in cyan text on a dark
face (docs/reference/VisualReference.md). It lights up only when you raise your wrist and look at it.

## Pieces

- **WristDisplay.cs**: writes the bankroll and tier text. It only observes `IChipWallet` and `ICharacterTier` and
  never changes them. Leave its sources empty and it finds the active wallet and tier in the loaded scenes.
- **WristAttach.cs**: finds the one active `XROrigin` at runtime and attaches the watch to `Left Controller`
  (StyleGuide §5c: never put it inside the rig in a scene). Position and rotation are Inspector fields.
- **WristRaiseVisibility.cs**: turns the face on when it points at your eyes (within 35°, closer than 0.6 m) and
  off past 50°. Tick **Always Visible** to keep it on while testing.
- **Logic/WristDisplayLogic.cs**: pure formatting (`$25,000`) and show/hide rules, covered by `Tests/`.
- **../Mocks/**: `MockChipWallet` and `MockCharacterTier` stand in for Slot A and Slot B until their code lands.
  They sit under `_SoloTest`, so they switch off inside `MVP_Main`.
- **../Editor/WristDisplayBuilder.cs**: the **High Stakes > UI > Build Wrist Display** menu.

## Build and test

1. Menu **High Stakes > UI > Build Wrist Display**. It creates `Assets/Content/UI/UI_WristDisplay.prefab`, three
   mock tiers in `Assets/Content/UI/Mocks/`, and `Assets/Scenes/MVP/MVP_UI.unity` (copied from `MVP_Template`
   the first time), then selects `_SoloTest/UI_Mocks`.
2. Press **Play**, click the Game view, press **[** to make the left controller active, and move the mouse to
   point it to your **left**. That turns the back of the wrist toward you and the face lights up.
   If that's fiddly, tick **Always Visible** on the `UI_WristDisplay` instance.
3. In the Inspector of `UI_Mocks`, press **+1,000** / **+5,000**. The bankroll counts up, and the tier changes at
   1,000 (Known High Roller) and 5,000 (Valued Associate).
4. Tune the watch position on the headset: change **Local Position / Local Euler Angles** on `WristAttach` in Play
   mode, note the values, and set them on the prefab after stopping.

Re-running the menu rebuilds the prefab (so set tuned values in `WristDisplayBuilder` too) and only adds what's
missing to the scene.
