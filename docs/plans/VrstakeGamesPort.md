# Plan: casino games

Two rooms, two kinds of game (Ben, 2026-10-13):

- **Main casino floor (`MVP_Casino`): the classic casino games.** Real tables with a felt layout and the dealer
  hologram board.
- **High-roller room (`MVP_VIP`): the Stake-style and virtual games.** Fast electronic games ported from Ben's vrstake
  prototype (`C:\Users\admin\dev\vrstake\vrstake`): **Mines, Plinko and Dice** only (Limbo and Crash are dropped), plus
  new virtual machines.

## Status

| Game | Room | Status |
|---|---|---|
| Blackjack | Main floor (left front table) | Done: ported from vrstake, moved from the VIP room to the floor |
| Roulette | Main floor (right front table) | Done: European single zero, straight-up and outside bets |
| Mines | High-roller room | Port from vrstake |
| Plinko | High-roller room | Port from vrstake |
| Dice | High-roller room | Port from vrstake |
| Slots (recommended) | Main floor | New: make the slot machines already on the floor playable |
| Baccarat (recommended) | Main floor | New: reuses blackjack's cards |
| Three Card Poker (recommended) | Main floor | New: the GDD's "poker", against the house |
| Video Poker (recommended) | High-roller room | New: virtual machine, reuses the cards |
| Keno (recommended) | High-roller room | New: virtual board, shares the Mines tile grid |
| Craps (later) | Main floor | New: iconic but many bet types; after the others |

## Shared pieces (already in place)

- **Rules:** Unity-free logic per game in `TableGames/Logic/<Game>/`, EditMode tests in `TableGames/Tests/`.
- **Money:** every game goes through `ITableWallet` -> `ChipWalletTableAdapter` -> the one `ChipWallet`, so wins
  anywhere raise the tier, the watch and the VIP door. Stakes are taken before the outcome is drawn; winnings are
  credited when the result is revealed (roulette settles when the ball lands), so the chip count never gives a result
  away early.
- **Table building:** `TableKit` (editor) builds every table from the style kit: body, felt buttons, flat text, light
  probes, and the dealer hologram board (`TableHologram`, with a per-table name and prompt; wins count up).
- **Placement:** `WorldBuilder.AddCasinoTables` puts the floor games in place of Ashton's decorative round tables;
  `VipRoomBuilder` lays out the high-roller room.

## Next phases

### Phase 1: shared pieces the vrstake games need
1. Port vrstake's `WagerGameContract<TGame>` test base (debit before the draw, refusals touch nothing, same seed same
   session, measured return matches the edge) and move blackjack and roulette's return tests onto it.
2. `PressableButton`: hold-to-repeat (slider steps) and swipe (Mines tiles), both left out of the blackjack port.
3. `TableSlider` from vrstake's `DiceSlider` (the Dice target).
4. vrstake's synthesised win/lose sounds (`SoundBuilder`, no licence issue) on `TableHologram`.
5. A machine body in `TableKit` for the high-roller games: a standing cabinet with the hologram as its screen.

### Phase 2-4: the Stake games, one commit each (high-roller room)
| Phase | Game | Main new piece |
|---|---|---|
| 2 | **Dice** | Target slider 1-99, roll readout that ticks up to the result |
| 3 | **Mines** | 5 x 5 tile grid, swipe-reveal, gem / mine reveal, cash out; `BigInteger` payouts; walking away refunds or cashes out |
| 4 | **Plinko** | Peg board behind the machine, the ball following the path the game already chose, buckets; pegs as one mesh |

Each: port logic + tests (vrstake's come over almost verbatim, plus the contract subclass), port the presenter onto our
wallet/hologram/buttons, build it from the kit, place it, then a headless Play check that presses its real buttons and
checks the wallet and the board, plus a render.

### Phase 5+: new games (recommended order)
1. **Slots** (main floor): the floor already has 18 slot machine bodies; give each a spin handle/button, three reels on
   its screen, and a paytable. Cheapest new game, makes the floor feel alive.
2. **Baccarat** (main floor): Player / Banker / Tie, no decisions, fixed drawing rules; reuses `Card` and `CardView`.
3. **Three Card Poker** (main floor): Ante / Play against the dealer plus Pair Plus; the GDD's poker without needing
   other AI players; reuses the cards.
4. **Video Poker** (high-roller room): Jacks or Better on a machine; hold / draw buttons; reuses the cards.
5. **Keno** (high-roller room): pick up to 10 of 80 numbers, 20 drawn; reuses the Mines grid.
6. **Craps** (main floor, later): throwable dice in VR are a great fit, but the bet layout is large; Pass / Don't Pass /
   Field / Place first.

## Design and documentation impact

- **GDD:** update the game list (blackjack, roulette, slots, baccarat, three card poker on the floor; Mines, Plinko,
  Dice, video poker, keno in the high-roller room). Roulette is week 5 in the GDD plan and is now built.
- **Character evolution:** the high-roller room is behind the VIP door, which opens at Known High Roller, so the Stake
  games are a reward for climbing the tiers.
- **Suspicion (later):** each presenter already knows its net per round; that is where "high-value win" events for the
  suspicion meter come from.

## Risks

- **Aiming:** small targets (roulette numbers are 9 x 8.5 cm, Mines tiles) need to work with the ray; splits, streets
  and corners in roulette are left out for that reason.
- **Quest 3 budget:** tables are kit primitives and text; Plinko's pegs should be one combined mesh.
- **Repo size:** no textures or models; all visuals are kit primitives and text.
