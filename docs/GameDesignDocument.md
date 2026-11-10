# Game Design Document

**Title:** High Stakes
**Genre:** VR Adventure / Heist Simulation (Casino Simulation with Stealth and Puzzle Elements)
**Platform:** Meta Quest 3 (standalone VR; built in Unity as a standalone Android app and tested on the headset, with Meta Quest Link as an optional secondary path)

**Team:** Zachary Scheer, Ashton Calkins, Pakrinha Sim, Benjamin Parenteau

## Storyline

### Plot

- **Overview:** High Stakes is a free roam VR adventure set inside a casino resort. The player is a newcomer to the casino floor who is free to explore and play fully functional casino games for real in-game stakes. As the player wins chips and reputation, a mysterious handler known only as the Guide makes contact and reveals that the casino's ownership is running an illegal operation behind the glittering front-of-house. The central conflict pits the player between two goals that must be balanced simultaneously: build enough wealth and trust to move freely through the casino, while gathering the access and evidence needed to expose and rob the operation before the house's own security catches on.
- **Beginning:** The game opens on the main casino floor on the player's first night at the resort. A short tutorial sequence walks the player through cashing in at the cage, sitting down at a blackjack table, and placing a first bet, establishing the core systems of free-roam movement and the chip economy. Midway through this first session, the Guide makes contact through an earpiece, hinting that the casino is not what it appears and inviting the player to keep listening.
- **Middle:** As the player builds a bankroll at the tables, the Guide begins feeding leads: a staff keycard left unattended on a cleaning cart, a passcode fragment overheard from a careless pit boss conversation, a guard's patrol schedule spotted on a break-room whiteboard. Each lead opens a new restricted area of the casino. Progress is constrained by a suspicion meter: winning too aggressively at the tables draws the Pit Boss's attention and tightens security, so the player must alternate between low-key play, information gathering, and moments of calculated risk. Milestone challenges include sneaking into the back-of-house corridors between guard patrols, bluffing past a floor supervisor, and assembling the full vault passcode from fragments scattered across several NPC conversations.
- **End:** In the final sequence, the player stakes everything on one last high-value hand at the main table to draw security's attention away from the back of the house, then uses the gathered keycards and passcode to reach the vault. Cracking the vault reveals the Black Ledger, the casino's record of laundered funds, and the player must escape the property before the Pit Boss realizes what has happened. The story resolves with the player either turning the Ledger over to expose the operation or disappearing with both the evidence and a share of the money, depending on choices made with the Guide throughout the playthrough.

### Lore and World Building

- **History:** The casino, The Meridian, opened decades ago as a legitimate family-run resort before being quietly bought out by a syndicate that uses its cash flow to launder money from other illegal operations. The culture on the floor is one of old-fashioned hospitality on the surface: dealers memorize regulars' names, cocktail service is constant, and the Pit Boss personally greets high rollers. Beneath that hospitality is a rigid, unspoken hierarchy among staff, some of whom know exactly what happens after hours and say nothing.
- **Mystical Elements:** High Stakes has no magical or fantastical elements; its "twist" replaces fantasy with a grounded, high-stakes secret: an entire second economy of laundered money and hidden information operating invisibly underneath the ordinary rhythms of a working casino floor. The tension the game generates comes from realism rather than supernaturalism.
- **Significance of the Artifact:** In place of a fantasy artifact, the game's central object is the Black Ledger, a physical logbook kept in the vault that records the casino's laundering transactions. Recovering the Ledger is the player's ultimate objective: it is both the proof needed to expose the operation and the leverage the Guide has been maneuvering the player toward all along, tying every exploration and puzzle objective back to the central conflict.

## Characters

### Main Characters

**The Hero**
- **Appearance:** Players choose their avatar's gender presentation, body type, and face/hair options, then select an outfit style that also affects gameplay perception: a business-casual "high roller" look raises staff trust faster but draws more attention from security, while a streetwear "local" look blends into the crowd more easily. Accessories such as watches, jewelry, and card-holder cases can be purchased with in-game chips and are purely cosmetic.
- **Evolution:** As the player accumulates chips and completes heist objectives, their avatar visibly upgrades: better tailoring, a VIP lanyard, and higher-tier chip cases signal rising status to NPCs. Functionally, reaching chip thresholds unlocks new abilities such as VIP room access, a chip-count overlay visible only to the player, and faster cash-out privileges at the cage, all of which gate access to new areas and story beats.
- **Backstory:** The hero is a skilled but relatively unknown gambler arriving at The Meridian looking for a fresh start and a big win. What begins as an ordinary night at the tables changes the moment the Guide makes contact, pulling the player into a role they never expected to play.

**Mysterious Guide**
- **Role:** The Guide is a voice-only handler who contacts the player through an in-world earpiece prop rather than any on-screen UI. They act as the player's informant and conscience throughout the game, surfacing leads on keycards, passcodes, and guard schedules, and periodically checking in with commentary that frames the heist as "leveling the playing field" against a corrupt house.
- **Appearance:** The Guide is never seen in person; their presence is represented only by a soft voice and a small glowing earpiece prop the player wears, giving them a deliberately mysterious, almost ghostly presence despite the game's otherwise grounded, realistic world.
- **Key Moments:** Key moments include the Guide's first contact during the tutorial, a mid-game exchange where the player can push back and ask why the Guide cares about exposing the casino, and a final-act choice in which the Guide reveals their true motive, shaped by whether the player has been playing cautiously or aggressively throughout the run. This choice directly influences which ending the player receives.

### NPCs

**The Pit Boss — Antagonist**
- **Role in Story:** Runs the casino floor by day and quietly oversees the laundering operation by night. Escalates security and personally intervenes whenever the player's winnings or behavior become suspicious.
- **Personality:** Outwardly charming, hospitable, and quick with a compliment for a winning player; privately controlling, image-obsessed, and unforgiving toward anyone who threatens the house.
- **Tasks Provided:** Does not offer tasks directly, but his reactions gate the pace of the game: comps and "friendly" warnings signal rising suspicion and force the player to slow down or change tactics.

**Dealers and Floor Staff — Supporting Cast**
- **Role in Story:** Run the blackjack, roulette, poker, and slot tables that form the game's core loop; a handful moonlight as informants who trade information for good tips or hands won at their table.
- **Personality:** Mostly professional, neutral, and courteous on the floor, though the informants among them are visibly nervous, opportunistic, and eager to trade what they know for a payout.
- **Tasks Provided:** Offer side information such as partial vault passcodes, guard schedules, or keycard locations in exchange for chips, favors, or a run of good tips at their table.

**Security Guards — Obstacles**
- **Role in Story:** Patrol the casino floor and the restricted back-of-house corridors, forming the primary physical obstacle to the player's heist objectives.
- **Personality:** Alert and routine-driven, following predictable patrol patterns once learned, but unforgiving and quick to raise an alarm if the player is caught somewhere they should not be.
- **Tasks Provided:** Create the exploration and stealth puzzle layer of the game; the player must time movement, use distractions, and read patrol routes to avoid detection while reaching restricted areas.

## Key Features

### Exploration

- **Main Casino Floor:** The heart of The Meridian — a sprawling, brightly lit gaming floor packed with blackjack, roulette, poker, and slot tables. Cocktail waitresses circulate on fixed routes, the Pit Boss patrols on a loop, and ambient chatter and clinking chips create a dense, believable crowd. Interactive elements include every table game, the cashier cage, and dozens of NPCs the player can approach for small talk or leads.
- **VIP Lounge and Back-of-House Corridors:** A quieter, tighter space unlocked once the player's chip total crosses a threshold. The VIP Lounge offers private high-stakes tables and informants with better information. Beyond it, the back-of-house corridors are narrow, dimly lit, and guarded — staff lockers, a break room with a whiteboard schedule, and security camera blind spots reward players who explore carefully.
- **The Vault:** The final restricted environment, reached only with the full passcode and keycard access gathered over the course of the game. Cold, steel-lined, and heavily secured, it houses the Black Ledger and represents the point of no return in the story.
- **Navigation:** Players move through the casino using standard VR locomotion (teleport or smooth movement, player's choice) across the open main floor, and use stealth-conscious movement — slower, crouch-capable navigation — once inside restricted areas, where guard sightlines and patrol timing matter. Interaction is hand-based: picking up keycards and clue objects, sitting down at tables, and pressing buttons or dialing combinations all use natural VR grab-and-press mechanics rather than menus.

### Puzzles

- **Logic Puzzles:** The vault passcode puzzle is the game's central logic challenge: the player collects three or four passcode fragments from separate sources (an overheard pit boss conversation, a note on a break-room whiteboard, a tip bought from an informant dealer) and must work out the correct order or fill in a missing digit before the vault will accept the code. A secondary logic puzzle has the player cross-reference a stolen staff roster against guard shift times to figure out which keycard grants access to which door.
- **Pattern Matching:** Security guards patrol on fixed, repeat routes. The player must watch a guard's loop through a corridor over two or three passes, recognize the timing pattern, and use it to predict the exact window when the path is clear. A related puzzle has the player match a sequence of cocktail-service rounds to figure out when a security camera's sweeping angle leaves a blind spot.
- **Environmental Challenges:** Reaching a restricted keycard or door often requires using the casino floor itself as cover — ducking behind a crowd forming around a big win, timing a move between two guards' overlapping patrols, or riding along with a cleaning cart to get past a checkpoint without triggering suspicion. These challenges are solved through positioning and timing rather than menus or dialogue.

### Collectibles

- **Items and Artifacts:** In place of magical items and lore scrolls, players collect grounded heist objects: staff keycards (each opening a specific restricted door), passcode fragments (scraps of overheard numbers, written notes, or traded tips that combine into the vault code), intel notes (guard shift schedules, camera blind-spot maps, informant tips), and chip cases and accessories (cosmetic upgrades purchased with winnings). The Black Ledger itself, recovered at the end of the game, functions as the single central "artifact" the entire collectible system builds toward.
- **Enhancements:** Keycards and passcode fragments unlock new areas, including the VIP lounge, back-of-house corridors, and vault, rather than granting combat or magic abilities. Intel notes reveal guard patterns and camera blind spots, making later stealth puzzles easier. Chip totals raise the hero's visible status, gating story access instead of power.

### User Interface

- **Design Elements:** All UI is diegetic. Chip count shows on a wrist display; inventory is a physical notebook and card case; the Guide speaks through the earpiece with no text box; NPC dialogue options float briefly above their head.
- **Sketches/Mockups:** Mockups to include wrist chip counter, notebook/card-case inventory, NPC dialogue prompt.
- **VR Interaction:** Everything is touched by hand, not menus or gaze. Raise your wrist for chips, open the notebook to check clues, tap dialogue options directly. No fixed overlay keeps the view clear during play.

### Movement

Both ways of moving are available everywhere in the game, in every room, at the same time — the player never has to learn a second scheme or discover that their preferred mode has stopped working somewhere.

- **Locomotion Options:**
  - **Teleportation:** Pushing a thumbstick throws a curved arc out in front of the player; releasing it puts them where the arc lands. The right stick does this at any time; the left one does it too when MOVE is set to TELEPORT. The view arrives black and fades up over about a tenth of a second rather than cutting, which removes the jolt testers associate with teleporting while costing no apparent self-motion.
  - **Smooth Movement:** Continuous left-stick movement, on by default, easing up to full speed over a third of a second instead of starting at it — a hard velocity onset is the strongest sickness trigger in this mode. Forward follows where the head faces. Preferred in the back-of-house corridors and the vault approach, where creeping at a controlled pace and staying aligned with sightlines matters more than speed.
  - **Turning:** Snap turning (45° per flick of the right stick) is the default because it produces no rotational self-motion; smooth turning (60°/s) is available for players who prefer it.
- **Comfort Settings:** On the start screen before the night begins, and at any time afterwards on the notebook's SETTINGS page: move (smooth / teleport), turn (snap / smooth), a peripheral vignette that darkens the edges of the view while moving, and a seated mode that lifts the view to standing height so the whole game can be played sitting down. Each row explains what it does in one line. Choices are saved on the headset, so a player sets them once.
- **Never moving the player's view:** the camera is the player's head. Nothing in the game rotates or pans it — a change of view (sitting at a table, stepping up to the wheel) moves the player to an anchor, and the roulette wheel spins rather than the player.
- **How teleport destinations are decided:** rather than hand-placing teleport pads, the game works out the floor the player can actually stand on when an area loads — it spreads out from the player's feet over a half-metre grid, keeping the squares that have floor beneath them and room for a standing body, and makes those the teleport targets. Consequences for play: the player can never teleport into a wall, under a table, or out of the building, the arc fails rather than lands when they aim somewhere they would not fit, and a newly built room needs no extra work to be traversable.

#### Movement user testing

Two rounds of five or more testers each on the Quest 3 build, at least three from outside the team and at least one who has never used VR, run to the same twelve-minute script: start the night, cross the casino floor, turn around, find the comfort settings and switch modes, cross back in the other mode, go through a door, then place a bet and talk to the Pit Boss while moving. The observer gives no coaching and notes only hesitation, requests for help and failures; each tester rates their stomach 1–5 after each crossing, so the two modes can be compared on the same person. Full protocol, sheets and results: [UserTesting.md](UserTesting.md).

| Measure | Round 1 | Round 2 |
|---|---|---|
| Tasks completed unaided | _(pending)_ | _(pending)_ |
| Testers needing a hint to move at all | _(pending)_ | _(pending)_ |
| Worst reported nausea (1–5) | _(pending)_ | _(pending)_ |
| Average nausea: smooth / teleport | _(pending)_ | _(pending)_ |
| Testers who could not reach somewhere they aimed for | _(pending)_ | _(pending)_ |
| Mode preferred if they played again | _(pending)_ | _(pending)_ |

What changed between the rounds, and why: _(filled in after round 1; each change is logged under Decisions in `Progress_Vol1.md` as `Playtest R1 → change`)_.

### Sound Design

- **Background Music:** A low, moody lounge score on the main floor; the music thins out and grows tenser in the back-of-house corridors; near-silence in the vault, broken only by a faint tone as the passcode is entered.
- **Ambient Sounds:** Clinking chips, shuffling cards, murmured conversation, and a spinning roulette wheel on the floor; footsteps and distant guard radio chatter in the corridors; a low electrical hum in the vault.
- **Interactive Sound Effects:** Cards dealing, chips stacking, the roulette ball settling, a keycard beep at doors, footsteps that shift pitch with movement speed, and a rising suspicion cue when the Pit Boss's attention increases.

## Project Plan

### Timeline Milestones (Ground)

| Week | Deliverables | Comments |
|---|---|---|
| 1 | GDT-120 Game Design Document draft (Storyline and Characters sections) | Finalize storyline, hero customization, and full NPC roster; submit individual concept videos. |
| 2 | Environment concept art and casino floor layout sketch | Block out the main casino floor, cashier cage, and one restricted area using the Unity VR template. |
| 3 | Playable free-roam movement prototype | Implement XR Origin, teleport and smooth locomotion, and comfort settings. |
| 4 | Blackjack minigame prototype | Program card-dealing logic, betting and chip system, and win/loss evaluation. |
| 5 | Roulette minigame prototype | Program wheel physics, betting grid, and payout logic. |
| 6 | Chip economy and cash-out system | Connect table winnings to a persistent chip total and cashier cage interactions. |
| 7 | Keycard and inventory system | Implement pickup and interact systems for keycards and clue objects. |
| 8 | Guard patrol AI and suspicion meter | Build NPC patrol routes and a visible suspicion meter tied to player behavior. |
| 9 | Vault passcode puzzle | Implement the multi-clue passcode puzzle that gates the vault door. |
| 10 | Full narrative integration | Wire the Guide's dialogue triggers to milestones across the casino floor. |
| 11 | VIP area and restricted-zone content | Build the VIP lounge and back-of-house corridors; gate access by chip threshold. |
| 12 | Internal playtest build | Run in-headset playtests; log bugs and comfort issues. |
| 13 | Bugfix and polish pass | Address playtest feedback; tune difficulty, audio, and UI. |
| 14 | Near-final build for instructor review | Complete asset polish, sound design pass, and final narrative branch (ending variations). |
| 15 | Final Quest 3 build, final GDD, and final presentation video | Deploy the signed build; submit final documentation and presentation video. |

### Tasks Breakdown

- **Concept Development:** Assign team members to work on different aspects of the game design document and concept art descriptions.
- **Research and Planning:** Allocate time for researching VR game design principles, 3D modeling techniques, and sound design best practices.
- **Prototyping:** Schedule time for developing and testing initial prototypes on the VR headset.
- **Platform Compatibility Design:** Describes the technical specs of the target platform for your game. Address necessary modifications (design, software, deployment procedure) to make the game compatible with other platforms — as appropriate.
  - *Development hardware constraint (Zach):* two of four team laptops have no dedicated GPU and cannot run Meta Quest Link or PC VR, so the standalone Android build on Quest 3 is the primary target and test path (it also reflects real headset performance), with the XR Interaction Simulator used for quick iteration. PC VR over Link is a secondary path for the two members with GPUs. See [StyleGuide.md §9](StyleGuide.md#9-testing-without-a-gpu).
- **Review and Feedback:** Plan for regular review sessions to gather feedback and ensure the project is on track.
