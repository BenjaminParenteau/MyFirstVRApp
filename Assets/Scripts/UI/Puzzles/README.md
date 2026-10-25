# Staff door puzzle: the housekeeping cart

Locks the casino's **STAFF ONLY** door (into the security hub) until the player finds the staff keycard that
housekeeping left on a cleaning cart, the lead in the GDD. Slot C prototype; Slot B may move it into `Inventory/`.

## Solution

1. **Shift note** on the cashier counter: "Cart lockbox: clean the tables in order CARDS > WHEEL > DICE. - M."
   Look at it, press F (right B): it goes into the notebook.
2. **Table number stands** (brass posts): blackjack **4**, roulette **7** on the casino floor, and the dice table **2**
   in the high-roller room (so you need the VIP card from the Pit Boss first).
3. **Cleaning cart** by the STAFF ONLY door: enter **472**, then OK, on its lockbox (point and grip, like the bar PIN
   pad). Wrong codes just clear. The lid opens on a steel **STAFF keycard**.
4. Pick the card up (F), open the notebook (N), select it (B), take it (F), and touch it to the reader beside the
   door: green, the door opens, and stays open for the session.

## Pieces

- **CartLockbox.cs**: the 12-key pad (reuses `PressableButton`), lid and card. Solved once per session; the card
  comes back only if it was lost (no STAFF card anywhere, including hidden in the notebook, and the door not yet open).
- **Logic/LockboxLogic.cs**: typing, checking and the `4 7 _` display, covered by `Tests/`.
- **Editor/StaffDoorPuzzleBuilder.cs**: **High Stakes > Puzzles > Build Staff Door Puzzle**. Builds the prefabs and
  items in `Assets/Content/UI/Puzzles/` (`Item_StaffKeycard` is a variant of Slot B's VIP card), places them in
  `MVP_Casino` and `MVP_VIP`, and puts Slot B's reader (via `AccessBuilder.AddVipReader`, retargeted to `STAFF`) on
  the door. Re-running replaces only its own objects.

Re-run the menu after **Build MVP World** or **Build Security Hub**: those rebuild the casino doors and drop the reader.
