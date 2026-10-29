# Reserve 03 custodian key

The security office has a burgundy-lined key custody pedestal in the clear aisle beside the equipment racks. Its serial-matched double-bit key opens the vault's hallway entrance. The room-side exit stays usable.

## Playing

1. Find the key in Security Operations. Grab it physically, or look at it and use **B / F** to store it in the notebook. Take it back out of the notebook at the vault.
2. Insert the key straight into the numbered **03** cylinder, shaft toward the door and bow upright. A held, matching key within 4.5 cm of the seat and aligned within about 20 degrees snaps into place.
3. Grip the key at the cylinder, rotate the wrist **clockwise 90 degrees**, and hold at the stop for **0.6 seconds**. Releasing early lets the spring return it. The safety indicator turns brass and the key stays captive.
4. Grip the handwheel rim and sweep the hand clockwise through **270 degrees**. Bolts visibly withdraw with detent sounds. The door swings clear before the portal unlocks. Progress and captive-key state survive area changes during the session.

Simulator: use **F** and the notebook's take-out action for the key. With the key in hand, stand within 2 m and look at the lock, then press **Enter** to seat it. Hold **Right Arrow** for the key turn and dwell, then continue holding to turn the wheel. **Left Arrow** reverses unfinished travel. These shortcuts are inactive while the notebook is open. They cannot supply a missing key or bypass its interlock.

## Implementation and verification

`VaultLockLogic` owns the pure interlock rules. `VaultKeySession` resets once per Play session and spawns the custody station when the office loads. `VaultKeyItem` survives office unloads through the notebook/main scene, transfers ownership to the lock on seating, and recovers a key dropped below the floor. `VaultRotaryHandle` reads relative hand motion without moving the mechanism off its bearings. `VaultKeyLock` connects visuals, input, audio and the hallway portal. `VaultDoorMotion` animates the leaf on both sides of the portal.

**High Stakes > Vault > Build Custodian Key System** regenerates the three prefabs and notebook item. **Build Treasury Dressing** regenerates the entrance frame and room dressing. Neither rebuild changes the source scenes. Resources are instantiated at runtime, matching the existing treasury dressing workflow.

Run the EditMode tests in `HighStakes.Security.Tests`, then **High Stakes > Vault > Run Custodian Key Play Check**. The Play check exercises the actual notebook transfer, held-key alignment, entry gating, leaf motion, room revisits and free exit. It writes its result and screenshots to `Temp/VaultKeyQA`. It programmatically drives the mechanism after insertion; final headset comfort, hand pose and audio balance still need a human VR playtest.
