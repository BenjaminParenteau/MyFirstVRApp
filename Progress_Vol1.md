# Progress — Vol. 1

Shared project memory for the whole team. **Every commit must update this file** (see [CLAUDE.md](CLAUDE.md#progress-log-rule)).

- **Assignment:** Vol. 1 — "High Stakes" VR casino-heist game (15-week assignment). Design reference: [docs/GameDesignDocument.md](docs/GameDesignDocument.md).
- **Next assignment:** start a new file, `Progress_Vol2.md`, and leave this one as an archive. Update the pointer in CLAUDE.md.
- **Personal notes:** each member keeps a git-ignored `Personal.md` locally (copy [docs/Personal.template.md](docs/Personal.template.md) to `Personal.md` in the repo root).

## Team split

Four members, each owning distinct script folders so merges stay clean. Replace the placeholder names.

| Slot | Member | Owns (code) | Owns (content / deliverables) |
|---|---|---|---|
| A | _(name)_ | `Scripts/TableGames/`, `Scripts/Economy/` | Blackjack, roulette, chip wallet, cashier cage |
| B | _(name)_ | `Scripts/Security/`, `Scripts/Inventory/` | Guard patrols, suspicion meter, keycards, vault passcode puzzle |
| C | _(name)_ | `Scripts/Dialogue/`, `Scripts/UI/` | The Guide's voice lines, NPC dialogue, wrist chip counter, notebook, sound design |
| D | _(name)_ | `Scripts/Environment/`, locomotion, scenes, builds | Casino floor / VIP / back-of-house layout, XR Origin & comfort, Quest 3 builds, playtests, GDD upkeep |

Rules:
- Stay in your own folders. If you need a change in someone else's area, ask the owner or open a note under **Open questions** below.
- Scenes (`Assets/Scenes/`) are shared and conflict-prone — only Slot D edits them directly; others work in prefabs or a personal test scene and hand off.
- Update this file in every commit (one line in the Log is enough).

## Weekly plan

Mirrors the milestone table in the GDD. Status: ⬜ not started · 🟨 in progress · ✅ done.

| Week | Deliverable | Lead | Status |
|---|---|---|---|
| 1 | GDD draft (Storyline & Characters), concept videos | All | ⬜ |
| 2 | Environment concept art, casino floor layout | D | ⬜ |
| 3 | Free-roam movement prototype (XR Origin, teleport, smooth locomotion, comfort) | D | ⬜ |
| 4 | Blackjack prototype | A | ⬜ |
| 5 | Roulette prototype | A | ⬜ |
| 6 | Chip economy and cash-out | A | ⬜ |
| 7 | Keycard and inventory system | B | ⬜ |
| 8 | Guard patrol AI and suspicion meter | B | ⬜ |
| 9 | Vault passcode puzzle | B | ⬜ |
| 10 | Narrative integration (Guide dialogue triggers) | C | ⬜ |
| 11 | VIP area and restricted zones | D (+B for gating) | ⬜ |
| 12 | Internal playtest build | D | ⬜ |
| 13 | Bugfix and polish | All | ⬜ |
| 14 | Near-final build, sound pass, ending variations | All (C sound/narrative) | ⬜ |
| 15 | Final Quest 3 build, final GDD, presentation video | All | ⬜ |

## Log

Newest entries first. One entry per commit: date, member, what changed, anything the team needs to know.

Format: `- YYYY-MM-DD · <member/slot> · <what changed> (<commit or branch>)`

- 2026-09-29 · _(setup)_ · Added `Progress_Vol1.md`, git-ignored `Personal.md` template, and the progress-update rule in CLAUDE.md.
- 2026-09-29 · _(setup)_ · Stripped Claude co-author trailers from earlier commits (history rewritten + force-pushed; teammates must `git fetch && git reset --hard origin/main`) and added a no-AI-attribution rule to CLAUDE.md.
- 2026-09-29 · _(setup)_ · Added `docs/reference/` (concept-art descriptions from the submitted GDD PDF), linked from CLAUDE.md; added Benjamin Parenteau to the GDD team line.

## Decisions

Things the team agreed on that aren't obvious from the code.

- _(none yet)_

## Open questions / blockers

- Assign the four members (Zachary Scheer, Ashton Calkins, Pakrinha Sim, Benjamin Parenteau) to slots A–D in the Team split table.
- Drop `WHOLEDOCCLAUDEGDIT.pdf` into `docs/reference/` and extract its images into `docs/reference/images/`.
