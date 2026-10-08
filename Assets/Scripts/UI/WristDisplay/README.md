# Wrist display

A round watch on the back of the **left wrist** (round case, crown, lugs and a strap around the wrist, just behind the left controller's grip) that shows **BANKROLL**
(chips) and the current **tier**, in cyan text on a dark face (docs/reference/VisualReference.md). It lights up when
you raise your wrist and look at it. The case shows the tier: steel → gold (Known High Roller) → gold with a white VIP stripe
(Valued Associate).

## Pieces

- **WristDisplay.cs**: writes the bankroll and tier text. It only observes `IChipWallet` and `ICharacterTier` and
  never changes them. Leave its sources empty and it finds the active wallet and tier in the loaded scenes.
- **WristAttach.cs**: finds the one active `XROrigin` at runtime and attaches the watch to the left wrist,
  relative to `Left Controller` (StyleGuide §5c: never put it inside the rig in a scene). Position and rotation are Inspector fields.
- **../TierVisuals/TierVisibility.cs**: shows a part only within a tier range (used for the steel/gold case and stripe).
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
2. Press **Play**. The watch is on the back of the left controller. If the face is dark, tick **Always Visible** on
   the `UI_WristDisplay` instance (under the left controller).
3. In the Inspector of `UI_Mocks`, press **+1,000** / **+5,000**. The bankroll counts up, the tier changes at
   1,000 (Known High Roller) and 5,000 (Valued Associate), and the case turns gold, then gets the VIP stripe.
4. Tune the watch position on the headset: change **Local Position / Local Euler Angles** on `WristAttach` in Play
   mode, note the values, and set them on the prefab after stopping.

Re-running the menu rebuilds the prefab (so put tuned values in `WristAttach`'s defaults too) and only adds what's
missing to the scene.
