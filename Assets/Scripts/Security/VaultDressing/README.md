# Vault key

The brass key lies flat on the right operator counter in the security office. Grab it, or store it with the notebook's usual B / F pickup.

At the vault, bring the key within 30 cm of the keyhole. It snaps into place from any orientation and makes a short assisted turn. Alternatively, select the keyhole to use the key directly from the notebook. Select or grab the wheel to make its short assisted turn and open the door. Selecting the wheel with an owned key also handles insertion automatically. There is no timed hold or precision wrist gesture. The keyhole light and mechanical movement provide feedback; the door has no written instructions.

Keyboard simulator: with the notebook closed, stand near the lock and press Enter to use the collected key, then Right Arrow to turn the wheel. Right Arrow can perform both steps. These shortcuts never grant a missing key.

`VaultLockLogic` validates the key and short turns. `VaultKeySession` places the key on the office counter and retains unlock state for the play session. `VaultKeyLock` handles forgiving insertion, selection-driven turns, bolt animation and portal gating. Both door faces swing open, and the room-side exit stays unlocked.

Rebuild with **High Stakes > Vault > Build Custodian Key System** and **Build Treasury Dressing**. Run `HighStakes.Security.Tests` and **Run Custodian Key Play Check** for logic and integration verification. The play check uses XR selection events for insertion and turning instead of setting the angles directly. Results and screenshots go to `Temp/VaultKeyQA`. Headset comfort still needs a human playtest.
