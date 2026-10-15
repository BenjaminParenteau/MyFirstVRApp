# Bar float PIN puzzle

A compact brass and black keypad sits on the front edge of the casino bar at
(-4.35, 1.105, 8.57) in MVP_Casino. The keypad is 42 cm wide and 50 cm tall,
angled toward a standing player. The adjacent cream closeout slip provides the
clue: reverse its listed counts to get **8642**.

Enter four digits and press OK to transfer **$500** into the existing chip wallet.
CLR clears the entry. Incorrect PINs clear the entry without costing chips.
The reward can be collected once per wallet/game session, including return visits
to the casino. If a wallet is unavailable or full, the correct PIN remains claimable.
All twelve buttons remain visible after completion, with input locked and a
TRANSFER COMPLETE message. There is no independent saved balance.

The keypad uses the existing XRI PressableButton component and Interactable layer.
Controller rays and direct selection work the same way as the casino table buttons.
Keys use bright kit chip-white material with black numerals; the screen uses cyan
text on black. Brass and dark wood match the existing casino palette. No new
textures, shaders, lights, input actions or player rigs are added.

Under High Stakes > Puzzles:

- Build Bar PIN Pad rebuilds the prefab and replaces its existing casino instance.
- Verify Bar PIN Pad checks incorrect PINs, exact payout, repeat and revisit
  protection, missing-wallet recovery and a fresh wallet.
- Run Bar PIN Pad Play Check enters the PIN through actual button events in
  MVP_Main, checks the shared bankroll, then returns to the casino in Edit mode.
- Render Bar PIN Pad Preview writes Temp/BarPuzzleQA/preview.png.

Editor and Play checks passed. A Quest 3 check is still needed for physical reach,
reading comfort and device performance.
