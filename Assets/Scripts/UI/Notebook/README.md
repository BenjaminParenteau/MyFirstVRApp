# Notebook

An A5 leather notebook that holds what the player picks up: keycards, passcode clues and intel notes. It matches the
concept art (docs/reference/VisualReference.md, "Notebook & card case"): press a button and it opens in front of you
as a two-page spread, with a **blue screen on the left page** showing the selected item, the **notes list on the
right page**, and a **brass card slot** that holds a keycard while one is in the notebook. Step through your items and
take one into your hand.

## Controls

| Action | Quest 3 controller | Simulator / keyboard |
|---|---|---|
| Open / close the notebook | Left **Y** | **N** |
| Next item (while open) | Left **X** | **B** |
| Pick up the item you're looking at | Right **B** | **F** |
| Take the selected item into your hand (while open; closes the notebook) | Right **B** | **F** |
| Put the held item back (notebook closed, or select it and press again) | Right **B** | **F** |

E isn't used because the simulator already uses it to move up. The bindings are built in code (not in a shared
`.inputactions` asset), so nothing in `ProjectSettings/` or `Assets/XR/` changes.

## Pieces

- **NotebookOpen.cs**: the open/close button and animation. On open it appears ~40 cm ahead and 15 cm below your
  eyes, the spine centred and the pages turned toward you, and follows you while open (it trails smoothly as you walk and swings back in front
  only once you turn your head more than 30° away, so glancing around doesn't drag it); the cover swings a full 180° about the spine (0.45 s open,
  0.3 s close) to become the left page, and both pages' text appears once it is mostly open. Reuses the hologram's `StepOpen` / `EaseOut` easing.
- **NotebookPickups.cs** + **PickupItem.cs**: prototype `IInventoryEvents` until Slot B's inventory exists. It looks
  where you look (a 10 cm-wide check, up to 2.5 m) for anything with a `PickupItem`, or with a component listed in
  **By Component** (the builder lists `Keycard` → `Content/UI/Items/SO_Item_VIPKeycard`), and shows a
  "F  PICK UP ..." prompt above it. Picking up hides the object and moves it into `MVP_Main`'s scene so it survives
  area swaps. Taking it out puts it in the right hand with XRI's manual interaction: it stays held without the grip
  and can't be dropped and lost; put it back with the same button.
- **NotebookDisplay.cs**: both pages. The right page lists every pickup from any `IInventoryEvents` in the loaded
  scenes (`>` on the selected one, `(IN HAND)` on the held one, control hints); the left screen shows the selected
  item's name and note; the slot card shows while a keycard is in the notebook. Rechecks when an area
  scene loads.
- **Logic/NotebookLogic.cs**: pure rules (no duplicate pickups, next/wrap, page text), covered by `Tests/`.
- **../Mocks/MockInventoryEvents.cs**: Inspector "Pick up" buttons in `MVP_UI`. Mock items are listed but can't be
  taken into the hand (there's no object).
- **../Editor/NotebookBuilder.cs**: the **High Stakes > UI > Build Notebook** menu.

## Build and test

1. Run **High Stakes > UI > Build Wrist Display** first if `MVP_UI` doesn't exist yet.
2. Menu **High Stakes > UI > Build Notebook** (not in Play mode). It rebuilds `Assets/Content/UI/UI_Notebook.prefab`
   and makes sure the notebook is in `MVP_UI` and `MVP_Main`.
3. Play `MVP_Main`: talk to the Pit Boss (point and grip), look at the card he holds out: **F PICK UP VIP KEYCARD**.
   Press F: the card disappears. Press N: the notebook opens with VIP KEYCARD selected. Press F: it's in your right
   hand; touch it to the VIP door reader. Press F again to put it back.

To make another object collectable, give it a collider and an XR Grab Interactable, add `PickupItem`, and set its
Item to an Item Data asset (**Create > High Stakes > Item Data**).
