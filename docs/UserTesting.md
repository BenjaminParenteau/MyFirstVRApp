# User testing — High Stakes

Where every play test is recorded: what we tested, who tested it, what they said, and what we changed because of it. The before/after goes into the GDD and the individual videos. Round owner: Zach (Slot D); everyone helps run sessions.

Status: **Round 1 not yet run.** Fill the tables in as sessions happen — don't wait until both rounds are done.

- Current focus: **movement and comfort** (Milestone 4 — teleport, smooth movement, motion sickness).
- The menus / notebook / dialogue questions from Milestone 3 ride along in the same sessions (section 7), so testers are only recruited once.
- Plan and checklist: [Progress_Vol1.md](../Progress_Vol1.md). What the mechanics actually are: [StyleGuide.md §10](StyleGuide.md#10-locomotion-and-comfort).

---

## 1. What we are trying to find out

Two rounds, five or more testers each, on the Quest 3 APK.

| # | Question | How we answer it |
|---|---|---|
| Q1 | Can a player who has never used VR move around the casino without being told how? | Tasks T1–T3 with no coaching; count how many need a hint |
| Q2 | Which mode do people actually choose, and does it match what is comfortable for them? | Mode chosen at the start screen vs. their nausea score |
| Q3 | Does anything make people feel sick, and what specifically? | Comfort score after each block + the "when did it feel worst?" question |
| Q4 | Does teleport always have somewhere to go, and does it land where they aimed? | T2, T5, T6 — observer notes any rejected or surprising landing |
| Q5 | Can they find and change the comfort settings when they want to? | T4 |
| Q6 | Does the movement get in the way of playing (betting, talking, reading the notebook)? | T7 + the open questions |

**Success bar for the milestone:** every tester completes T1–T5 unaided or with one hint; no tester scores worse than 2 on the nausea scale; no tester is unable to reach somewhere they wanted to go.

---

## 2. Who tests

- **At least 5 per round**, at least **3 from outside the team**, and ideally at least one who has **never used VR**.
- Record only a first name or initials, whether they have used VR before, and whether they wear glasses (it affects fit and comfort).
- Say up front: this is a student game, it is being recorded in notes only, they can stop at any second for any reason, and stopping is useful data, not a failure.
- **Stop immediately** if they report nausea above 3, go quiet, sweat, or ask to sit down. Note where it happened. That is the single most valuable result we can get.

## 3. Setting up a session (15 min before the first tester)

1. Build and install the APK (`High Stakes > Build MVP World (all scenes)` first if the scenes changed, then File > Build Profiles > Android > Build and Run). Record the commit hash in the round's table.
2. Headset: clean the lenses, set the IPD, fresh battery, guardian/boundary set for a standing play space of at least 1.5 × 1.5 m with a chair behind the tester.
3. Reset the saved comfort choices between testers — they are kept in PlayerPrefs on the headset, so tester 2 would otherwise inherit tester 1's settings. (Uninstall/reinstall the APK, or clear the app's data from the headset's app settings.)
4. Cast the headset view to a laptop so the observer can see what they see.
5. One observer per session, with this page and a blank copy of the sheet in section 6.

## 4. The script (about 12 minutes)

Read the words in quotes and nothing else. **The observer stays quiet**: no coaching, no "try the other stick", no reacting to mistakes. Only note where they hesitate, ask for help, or fail. If they are stuck for **30 seconds**, give the one hint written next to that task, and mark the task as "with help".

> "You're a gambler on their last good night. You'll start in a dark room with a console in front of you. Take as long as you like, and tell me whenever something feels uncomfortable — I'll be asking about that at the end."

| # | Task | What you say | Done when | Your one hint if they stall |
|---|---|---|---|---|
| T1 | Start the night | "Have a look around, then start when you're ready." | They press START NIGHT | "Everything on the console can be pressed by pointing at it and pulling the trigger." |
| T2 | Cross the casino floor | "Get yourself over to the roulette table on the far side." | They are standing at the table | "Both thumbsticks do something." |
| T3 | Turn around | "Have a look at the room behind you." | They turn ~180° | "Try pushing a stick sideways." |
| T4 | Change the movement mode | "Switch to the other way of moving — whichever one you aren't using." | The mode changed (start screen rows, or the notebook's SETTINGS tab) | "Your notebook has a settings tab." |
| T5 | Cross back in the new mode | "Now get back to where you started." | They are at the entrance aisle | — |
| T6 | Through a door | "Find the VIP door and go through it." | They are in the VIP lounge | "Walk into a door to use it." |
| T7 | Move and play at once | "Place a bet at a table, then go and talk to the man in the suit." | A bet is placed and the Pit Boss has spoken | — |
| T8 | Free play (2 min) | "Last two minutes — do whatever you like." | Time up | — |

After T2 and after T5, ask: **"How does your stomach feel, 1 to 5?"** (1 = completely fine, 5 = I need to stop.) Write both numbers down — the comparison between the two modes is the point.

## 5. The questions at the end (2 minutes)

1. Comfort, 1–5 (1 fine, 5 awful) — **overall**, and **when did it feel worst?**
2. Which way of moving would you pick if you played again, and why?
3. Easy to move where you wanted, 1–5 (1 impossible, 5 effortless).
4. Did you ever try to go somewhere and not get there? Where?
5. Did anything make you lose your balance or want to reach for something?
6. What confused you?
7. What would you change?

## 6. Per-tester sheet (copy one per tester)

```
Round __   Tester ____________   Date ________   Build (commit) ________
VR before? none / some / lots      Glasses? y / n     Standing / seated

T1 start screen      done / with help / failed    time ____   notes ______________
T2 cross the floor   done / with help / failed    time ____   mode used: smooth / teleport
   nausea after T2: 1 2 3 4 5
T3 turn around       done / with help / failed    turn used: snap / smooth
T4 change the mode   done / with help / failed    time ____   found it in: start screen / notebook
T5 cross back        done / with help / failed    time ____   mode used: smooth / teleport
   nausea after T5: 1 2 3 4 5
T6 through a door    done / with help / failed
T7 move while playing done / with help / failed
T8 free play         stopped early? y / n   why ______________

Overall nausea 1-5 ____   worst moment ____________________________
Preferred mode: smooth / teleport   why ____________________________
Ease of moving 1-5 ____   Immersion 1-5 ____
Tried to go somewhere and couldn't: _______________________________
Confused by: ______________________________________________________
Would change: _____________________________________________________
Observer noticed: _________________________________________________
```

## 7. Riding along: menus, notebook, dialogue (Milestone 3)

Same session, after T8 if there is time. Ask them to open the notebook (N / left Y), find an item in it, and read the map page. Record ease 1–5 each for **menu**, **notebook**, **dialogue**, plus immersion 1–5. Anything they say about those goes in section 9 tagged `[UI]` so Slot C can pick it up.

---

## 8. Round 1 — results

Build: _(commit)_ · Date(s): _(dd mm)_ · Testers: _(n)_

| Tester | VR before | T1 | T2 | T3 | T4 | T5 | T6 | T7 | Nausea T2 / T5 / overall | Preferred mode | Ease | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| | | | | | | | | | | | | |

**What they said** (verbatim, one line each):

**Totals:** tasks unaided __ / __ · with help __ · failed __ · worst nausea __ · teleport preferred by __ of __

## 9. What we changed because of round 1

| # | What testers hit | Change | Where | Who |
|---|---|---|---|---|
| | | | | |

Every row here also goes under **Decisions** in `Progress_Vol1.md` as `Playtest R1 → change`, and the before/after into the GDD.

## 10. Round 2 — results

Build: _(commit)_ · Date(s): _(dd mm)_ · Testers: _(n)_ · Same script as round 1.

| Tester | VR before | T1 | T2 | T3 | T4 | T5 | T6 | T7 | Nausea T2 / T5 / overall | Preferred mode | Ease | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| | | | | | | | | | | | | |

**Round 1 → round 2 comparison** (this table is what the GDD write-up is built from):

| Measure | Round 1 | Round 2 |
|---|---|---|
| Tasks completed unaided | | |
| Testers needing a hint to move at all (T2) | | |
| Worst nausea score | | |
| Average nausea, smooth / teleport | | |
| Testers who couldn't reach somewhere | | |
| Preferred mode | | |

## 11. What we changed because of round 2

| # | What testers hit | Change | Where | Who |
|---|---|---|---|---|
| | | | | |
