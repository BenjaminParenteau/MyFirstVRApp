# Plan: port the other vrstake games

Blackjack is already ported (see `Assets/Scripts/TableGames/`). This plan brings over the other five games from Ben's
vrstake prototype (`C:\Users\admin\dev\vrstake\vrstake`, local commit `2ab4b04`): **Dice, Limbo, Crash, Mines,
Plinko**. Each one is a "crypto-casino original": quick rounds against the house, a provably fixed 1% edge, and logic
that is already Unity-free and fully tested in vrstake.

## What each game is

| Game | Round | vrstake size | Needs from the shared kit |
|---|---|---|---|
| **Dice** | Pick a target 1-99, the house rolls 1-100, win if the roll is over the target. Pays `wager x 0.99 x 100 / (100 - target)`. | ~690 lines | Target slider, hold-to-repeat buttons |
| **Limbo** | Pick a target multiplier (1.01x-1,000,000x); one draw decides how high the round reaches; win if it reaches the target. | ~660 lines | `ReachedMultiplier` draw, log slider |
| **Crash** | A multiplier climbs from 1.00x until it crashes; cash out before it does. Optional auto cash-out. | ~820 lines | `ReachedMultiplier`, a per-frame climb, walk-away settles |
| **Mines** | 5 x 5 tiles, pick 1-24 mines, flip safe tiles to raise the payout, cash out any time; a mine loses. | ~890 lines | Tile grid, swipe-to-reveal buttons, `BigInteger` payouts |
| **Plinko** | Drop a ball through 8-16 rows of pegs; the bucket pays its multiplier (low/medium/high risk). | ~850 lines | Tall board, ball path animation, peg click sound |

Every game already follows the rules our Blackjack port relies on: the wager is debited before the outcome is drawn,
a bet too large to pay is refused with the wallet untouched, hidden information (crash point, mines) can't be read
early, and walking away settles the round fairly.

## What changes in the port (same as Blackjack)

- **Wallet:** `IWallet` (long, with reasons) becomes our `ITableWallet`, adapted onto the one `ChipWallet`
  (`ChipWalletTableAdapter`). No game keeps its own balance.
- **Services lookup:** replaced by finding the wallet by interface, as `BlackjackPresenter` does.
- **RoundFeedback:** replaced by our `TableHologram` board (the result, and a count-up on wins).
- **Namespaces / folders:** `HighStakes.TableGames.<Game>`; logic in `TableGames/Logic/<Game>/`, tests in
  `TableGames/Tests/`, presenters in `TableGames/<Game>/`, builders in `TableGames/Editor/`.
- **Look:** built from the shared style kit (materials, brass, felt), not vrstake's table kit. No third-party art is
  needed: only Blackjack used the licensed ithappy card image, and that is already replaced.
- **Sounds:** vrstake's win/lose stings are synthesised by code (`SoundBuilder`, no licence issue) and its click is the
  VR Template click we already have. Both come over.

## Phase 0: shared table kit (do once, before any game)

1. **Logic:** port `ReachedMultiplier` (Limbo + Crash draw) and the per-game `*Bet` value types' shared shape.
2. **Tests:** port vrstake's `WagerGameContract<TGame>` into `TableGames/Tests/`. Every game then gets the same
   house-rule checks (debit before the draw, refused bets touch nothing, same seed = same session, the measured return
   matches the declared edge) for a few lines each. Move Blackjack's return tests onto it.
3. **Table builder kit:** pull the table body, wager row (1/2, amount, 2x, BET), balance readout, button and flat-text
   helpers out of `BlackjackTableBuilder` into one `TableKit` editor class. Rebuild Blackjack on it, unchanged.
4. **PressableButton:** port the two features the Blackjack port left out: hold-to-repeat (slider steps) and swipe
   (Mines tiles).
5. **Slider:** port `DiceSlider` as a general `TableSlider` (linear for Dice, logarithmic for Limbo).
6. **TableHologram:** make the title a field (it says "BLACKJACK" today) so every table reuses the board.
7. **Sounds:** port `SoundBuilder` (Win/Lose WAVs) and give `TableHologram` win/lose sounds.

Done when: Blackjack still passes all 49 tests and its headless play check on the new kit, and the contract base runs.

## Phases 1-5: one game each, simplest first

Each game is one commit on `dev`, in this order, each done the same way:

1. Port logic + tests (vrstake tests come over almost verbatim, plus the contract subclass).
2. Port the presenter onto `ITableWallet`, `TableHologram`, `TableSlider`, `PressableButton`.
3. A builder makes its table prefab from the kit and places it (see Placement).
4. Verify: EditMode tests, then a headless Play check that walks to the table, presses its real buttons, plays rounds
   until a win and a loss, and checks the chip balance and the board; a render of the table.

| Phase | Game | Main new piece |
|---|---|---|
| 1 | **Dice** | Slider-driven target; a roll readout that ticks up to the result |
| 2 | **Limbo** | `ReachedMultiplier`; log slider; climbing readout to the reached multiplier |
| 3 | **Crash** | Real-time climb (presenter owns the clock, the round decides); auto cash-out ladder; graph on the board |
| 4 | **Mines** | 5 x 5 tile grid on the felt, swipe-reveal, gem/mine reveal, cash-out; `BigInteger` payout table |
| 5 | **Plinko** | Peg board standing behind the table (like the dealer board), ball following the chosen path, buckets |

## Placement (decision needed)

The casino floor already has five decorative round tables from Ashton's casino, the same count as these games.

- **Option A (recommended): the five floor tables become the five games.** The casino floor fills with things to play
  as soon as you arrive, no extra loading, and the Pit Boss already patrols between them. Blackjack stays the VIP game.
- **Option B: a separate "Electronic Games" room** behind a new casino door (one more area scene). Keeps the main floor
  classic but adds a load and hides the games.

## Design and documentation impact

- **GDD:** these games are not in it. The GDD plans blackjack, **roulette**, poker and slots. Add them as "Meridian
  Originals", the casino's electronic games, and decide whether roulette (week 5 in the plan) is still built; vrstake
  has no roulette to port.
- **Character evolution:** every game pays into the same `ChipWallet`, so wins anywhere raise the tier, the watch and
  the VIP door, with no extra work.
- **Suspicion (later):** the presenters are the natural place to raise "high-value win" events for the suspicion
  meter; each game can report its net per round the way Blackjack's `Announce` already does.
- **Scope:** the Milestone 2 plan lists full table games as out of scope; this is ahead of schedule (weeks 4-5 work).

## Risks

- **Comfort / readability:** sliders and the Mines grid need to work with the ray (grip to press) and in the simulator.
  The headless checks press through the same interactor path the headset uses.
- **Crash timing on Quest:** the climb is frame-rate independent in vrstake (the round, not the frame, decides); keep it.
- **Quest 3 budget:** each table is a few hundred triangles plus text; Plinko's pegs (up to 16 rows, about 150 pegs)
  should be one combined mesh.
- **Repo size:** no textures or models are added; all visuals are kit primitives and text.
