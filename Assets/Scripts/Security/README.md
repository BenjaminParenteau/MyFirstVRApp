Guard patrol AI and the suspicion meter. Patrols report detection events; the suspicion meter subscribes to them rather than being polled directly.

Also the security office's wire-matching puzzle (`WirePanelPuzzle`, `WirePlug`, pure rules in `Logic/WirePanelLogic.cs`): four colour-coded plugs, four sockets, built by **High Stakes > Puzzles > Build Security Wire Panel** (`Editor/WirePanelBuilder.cs`) into `MVP_SecurityOffice`.

Back-room puzzle chain to the vault (**High Stakes > Puzzles > Build Back Room Puzzles + Vault**, `Editor/BackRoomPuzzlesBuilder.cs`): the camera room's tape archive (`TapeDeckPuzzle`, `VideoTape`) and the breaker panel each give half of the vault code; the hub's VAULT door (`VaultClearance`) stays locked until both notes are in the notebook; the vault's keypad (`VaultKeypad`, rules in `Logic/VaultCodeLogic.cs`) opens the Black Ledger drawer. `PuzzleSession` pays each reward once and puts a reward note back until it is collected.
