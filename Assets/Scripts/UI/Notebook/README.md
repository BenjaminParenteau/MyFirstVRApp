# Notebook

An A5 leather-bound journal that holds what the player picks up (keycards, passcode clues and intel notes), a map and
the Guide's leads. Press a button and it opens in front of you as a two-page spread of kraft paper with a spiral
binding, styled after the Humble Pixel journal mockups. Square **side tabs** on the outer edge switch between two
spreads, and a **bookmark ribbon** shows which is open:

- **ITEMS**: the left page ("ITEM") shows the selected item's icon, name, kind and note. The right page lists your
  items as boxed rows above a **brass card slot**, which holds a keycard while one is in the notebook.
- **MAP**: the left page has the map and a "YOU ARE HERE" box. The right page ("LEADS") lists the Guide's leads, the
  newest first and older ones ticked, with what the Guide said.

The concept art (docs/reference/VisualReference.md, "Notebook & card case") had a blue screen on the left page; that
change is in `Progress_Vol1.md` Open questions.

## Controls

| Action | Quest 3 controller | Simulator / keyboard |
|---|---|---|
| Open / close the notebook | Left **Y** | **N** |
| Jump to a spread: press its side tab (keycard = ITEMS, pin = MAP) on the outer edge | Poke it, or point and pull the **trigger** | Point and click |
| Flip between items and the case file (map + leads) | Left **thumbstick click** | **U** (M is the simulator's menu button) |
| Next item (while open, items spread) | Left **X** | **B** |
| Pick up the item you're looking at | Right **B** | **F** |
| Take the selected item into your hand (while open; closes the notebook) | Right **B** | **F** |
| Put the held item back (notebook closed, or select it and press again) | Right **B** | **F** |

E isn't used because the simulator already uses it to move up. The bindings are built in code (not in a shared
`.inputactions` asset), so nothing in `ProjectSettings/` or `Assets/XR/` changes.

## Pieces

- **NotebookOpen.cs**: the open/close button and animation. On open it appears ~32 cm ahead and 10 cm below your
  eyes, the spine centred and the pages turned toward you, and follows you while open (it trails smoothly as you walk and swings back in front
  only once you turn your head more than 30° away, so glancing around doesn't drag it); the cover swings a full 180° about the spine (0.45 s open,
  0.3 s close) to become the left page, and both pages' text appears once it is mostly open. Reuses the hologram's `StepOpen` / `EaseOut` easing.
- **NotebookPickups.cs** + **PickupItem.cs**: prototype `IInventoryEvents` until Slot B's inventory exists. It looks
  where you look (a 10 cm-wide check, up to 2.5 m) for anything with a `PickupItem`, or with a component listed in
  **By Component** (the builder lists `Keycard` → `Content/UI/Items/SO_Item_VIPKeycard`), and shows a
  "F  PICK UP ..." prompt above it. Picking up hides the object and moves it into `MVP_Main`'s scene so it survives
  area swaps. Taking it out puts it in the right hand with XRI's manual interaction: it stays held without the grip
  and can't be dropped and lost; put it back with the same button.
- **NotebookDisplay.cs**: the ITEMS spread. The right page lists every pickup from any `IInventoryEvents` in the
  loaded scenes as rows (selected one inked with its circle filled, `(IN HAND)` on the held one); the left page shows
  the selected item; the slot card shows while a keycard is in the notebook. Rechecks when an area scene loads.
- **NotebookRow.cs**: one boxed list row (pencil box, ink box when highlighted, a circle that fills when ticked),
  shared by the items and leads lists.
- **NotebookPages.cs**: flips the open book between its two spreads (side tabs, left stick click, U), sticks the
  open spread's tab out in brass and swaps the ribbon's icon. Item controls only work on the items spread.
- **NotebookCaseFile.cs**: the MAP spread. Left page: a framed map of the six areas (casino, VIP lounge, security hall
  and its office, camera room and vault) where a room is inked in with its name, and the door to it drawn, once you've
  been there; a red pin marks where you are (from `SceneFlow.CurrentScene`, so the CCTV feed loading the camera room
  doesn't count), and a "YOU ARE HERE" box with the room's note. Right page: **LEADS**, the Guide's newest lead on
  top and older ones ticked and faded, and a GUIDE box quoting the line that gave the current lead. A lead is the
  optional `lead` text on a `GuideLine` (set in `GuideBuilder`'s table), written down when the line plays.
- **Page look** (all from `NotebookBuilder`): each page is one quad with a generated kraft texture (dashed border,
  corner dots), big centred titles, section dividers, pencil boxes and dark-brown ink, plus steel coils over the spine
  and striped page edges. Icons are small kit-material models (keycard, note, pin). Materials (Variants of kit
  materials, made by `UITextures`): `M_UI_Page` (`T_NotebookPage`, 512 x 728), `M_UI_Ink`, `M_UI_Rule` (pencil),
  `M_UI_PageEdge` (`T_PageEdge`, 64 x 64).
- **Logic/NotebookLogic.cs**, **Logic/MapLogic.cs** (rooms, layout, doors, visits), **Logic/LeadsLogic.cs**: pure
  rules, covered by `Tests/`.
- **../Mocks/MockInventoryEvents.cs**: Inspector "Pick up" buttons in `MVP_UI`. Mock items are listed but can't be
  taken into the hand (there's no object).
- **../Editor/NotebookBuilder.cs**: the **High Stakes > UI > Build Notebook** menu.
- **../Editor/UITextures.cs**: burgundy leather covers (reusing the casino's `T_Leather`) and paper pages (a small
  generated `Content/UI/Textures/T_Paper_Albedo.png`), as Material Variants of kit materials in `Content/UI/Materials/`.
  **High Stakes > UI > Apply UI Textures** puts them on the existing notebook and shift note prefabs without touching scenes.

## Build and test

1. Run **High Stakes > UI > Build Wrist Display** first if `MVP_UI` doesn't exist yet.
2. Menu **High Stakes > UI > Build Notebook** (not in Play mode). It rebuilds `Assets/Content/UI/UI_Notebook.prefab`
   and makes sure the notebook is in `MVP_UI` and `MVP_Main`.
3. Play `MVP_Main`: talk to the Pit Boss (point and grip), look at the card he holds out: **F PICK UP VIP KEYCARD**.
   Press F: the card disappears. Press N: the notebook opens with VIP KEYCARD selected. Press F: it's in your right
   hand; touch it to the VIP door reader. Press F again to put it back.

To make another object collectable, give it a collider and an XR Grab Interactable, add `PickupItem`, and set its
Item to an Item Data asset (**Create > High Stakes > Item Data**).
