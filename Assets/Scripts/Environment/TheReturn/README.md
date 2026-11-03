# TheReturn

Runtime scripts for `Assets/Scenes/TheReturn.unity`, the standalone Christian-worldview scene ("The Return"): the
player pays back by hand the staff wages the syndicate skimmed, and the room warms from vault blue to sunrise as they
do. Nothing here is loaded by the MVP world — the scene is opened and played on its own.

- `RestitutionManager.cs` — singleton that counts filled envelopes and drives the dark-to-dawn lighting, the window
  light shafts and dust motes, and the `OnAllReturned` event.
- `RestitutionEnvelope.cs` — one named pay envelope: a socket that takes only a cash bundle, seals it in place, lights
  the name in gold, chimes, buzzes the hand that placed it and reports to the manager.
- `BlackLedger.cs` — marker on the grabbable ledger, holding its red title label.
- `LedgerHandoff.cs` — the investigator's evidence tray: a socket that takes only the ledger, fades its title from red
  to white over two seconds and sets the Restoration ending.
- `ReturnEnding.cs` — which way it ended (`Restoration` / `Kept`) and the world-space ending card. Leaving the room has
  the last word, so it can override an early handoff.
- `ReturnExitTrigger.cs` — the volume in the doorway; walking out with anything unfinished takes the Kept ending.
- `GuideQuestion.cs` — the Guide's closing question on a world-space canvas clicked with the UI ray, with an optional
  voice clip; either answer plays a closing line and fades to the end screen.

Built into the scene by **High Stakes > Build The Return Content** (`Environment/Editor/TheReturnContentBuilder.cs`).
