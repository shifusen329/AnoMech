# Changelog

Notable changes to AnoMech. Versions match the plugin version shown in `/xlplugins`.

## [0.4.4.1] - 2026-10-08

### Changed

- **Black Hole: Modified DSA now matches a logged run of the strat:**
  - The players holding both tethers of a pair (DPS 1st in Line in Black Hole 1, Support 3rd in Line in Black Hole 4) stand about 12 y out.
  - For Thunder III, the off-tank holds Exdeath about 11 y out on the intercardinal just clockwise of the first tether's black hole.

### Fixed

- **Black Hole:** at the second Damning Edict and Kefka's body slam, the bots could run far off the platform, taking Chaos with the main tank. They now stand behind Chaos, outside the body slam's line and inside the arena. Every Black Hole strat had this.
- **Limit Cut:** before the second Decisive Battle, bots no longer follow a boss off the edge of the arena.

## [0.4.4.0] - 2026-10-08

### Added

- **Black Hole: Modified DSA (double tethers).** A third bot strat for Phase 3 Black Hole, from the "P3: Modified DSA (Double Tethers)" deck:
  - Black Hole 1: Support 1st in Line takes the lone first tether. DPS 1st in Line takes both tethers of the next pair and holds them between the two black holes. The off-tank drags Exdeath toward the first tether for Thunder III.
  - Black Holes 2 and 3 play out as in D>S>A.
  - Black Hole 4: Support 3rd in Line takes both tethers of the first pair, and DPS 3rd in Line takes the last tether.
  - The death recap explains each player's laser sets under this strat.

### Changed

- **Mitigation practice now moves HP in Phases 3, 4 and 5.** Each hit's damage comes from a logged full clear, so a missed press can now show a would-be death in every Dancing Mad scenario.
  - Hits that deal no damage aren't counted any more: Vacuum Wave, The Decisive Battle, Flood of Naught and Celestriad itself.
  - Black Hole's raidwides are the Earthquakes set off by a popped Accretion or a broken crust, landing about 1.5 s after the black hole's shot.
  - Reprisal, Feint and Addle only reduce hits Kefka or a boss deals himself, such as Ultima Upsurge and Phase 5's Forsaken. The rest come from helpers, so for those hits the plan no longer calls a missing debuff a cause of death.
  - The Decisive Battle barriers in Limit Cut are now treated as setup for Black Hole's Accretions. Black Hole puts them up at its start.
- **HP heals back between hits.** It now refills by about 15% of max HP each second after a hit, as it did in the logs, and is still full again 3 s after the last hit. Quick chains such as the Ultima Blasters and Chaotic Flood no longer warn you when the plan is followed.

### Fixed

Placement in the Dancing Mad scenarios now matches the strategy diagrams and real pulls:

- **Phase 1:**
  - Revolting Ruin III is now a 120° cone, as the diagrams draw it.
  - In Tele-trouncing, a player with two matching arrows now puts the short one on the cardinal first.
  - The Mystery Magic stack can now step in toward the middle to clear a line.
- **Phase 2 (Forsaken):**
  - The clones now step out toward their End target before All Things Ending, and fire it from there. Kefka and the clones aim it at the party, not at you.
  - Tower sets always rotate clockwise.
  - The cone soaker stands a little further inside the tower.
  - The opening lineup starts outside Kefka's hitbox.
- **Phase 3, Limit Cut:**
  - The wind crystal now sits on the corner the bosses are held at.
  - Bots split to Chaos or Exdeath before the second Decisive Battle.
- **Phase 3, Black Hole:**
  - Chaos stands still while casting Implosion, so its cones can no longer swing onto the party at slower speeds.
  - Tether holders stretch their tether about 10 y out instead of at the wall.
  - A black hole on Kefka's own bearing always counts as the first tether.
  - A stray second Knock Down that hit four fixed roles is gone.
  - Bots wait 9 y out before Stomp-a-Mole.
  - A bot whose tether's line moved while it was stepping in now steps back into it. Before, it gave up and the laser could hit the whole stack.
  - Exdeath starts on the west side, as in real pulls.
- **Phase 4 (Kefka Says):**
  - When Inferno is real, bots run straight to their next stack or spread spot instead of across the arena.
  - Stacks sit at max melee.
- **Phase 5:**
  - Flood's waves kill along the whole lane they telegraph, not only its far half.
  - Celestriad's towers resolve on the real timing, slightly later than before.
  - The Catastrophic Choice names were swapped. Wind (green) is the donut: step toward Kefka. Earth (brown) is the point-blank: step out. The settings label and the bots now agree.
  - Ranged and healers spread to the wall for Stray Entropy.
  - Forsaken Null now starts its enrage cast.

## [0.4.3.0] - 2026-10-05

### Added

- **Mitigation practice for every Dancing Mad scenario.** Turn it on with the new "Mitigation" setting in Setup (Off by default; not shown for solo runs or multiplayer guests).
  - The bots press the party mitigation from the Ikuya Mitty mit sheet (P1–P5) at the sheet's timings, for the comp you're actually in.
  - Your own presses are checked against the sheet for your job and slot. When the run ends, a chat summary lists each planned press as on time, early, late, missed, or not reached.
  - In P1 and P2 Forsaken, raidwides lower HP bars. Sim damage never kills; a hit that would have killed someone posts a warning naming the missing presses.
  - Red Mage Magick Barrier, Pictomancer Tempera Coat → Tempera Grassa and Machinist Dismantle are planned wherever the sheet marks Extras.
  - Grading needs "Resolve your own actions". In multiplayer the host runs the practice and only the host's presses are graded.
- **Healer and DPS party mitigation applies its buffs and barriers in the sim,** practice on or off.
- **Dancer Improvisation works:** Rising Rhythm builds while you stand still, and Improvised Finish gives a party barrier sized by your stacks.

## [0.4.2.4] - 2026-10-05

### Fixed

- **Death recap:** it now shows the spot the mechanic that killed you needed, not the spot for the next one.

## [0.4.2.3] - 2026-10-05

### Added

- **Bot timing setting:** bots can move at the last possible moment, or earlier and more naturally like real players.

### Fixed

- **Graven Image 3:** every player now shows a tether.
- **Forsaken:** bots bait Future's and Past's End at max melee, buddies side by side.
- **Auto-restart** keeps a collapsed window collapsed.

## [0.4.2.2] - 2026-10-05

### Added

- **Graven Image 1 and Graven Image 2 scenarios** in Dancing Mad Phase 1.
- **Death recap:** where you died, where the strat had you, and why, for Phases 2 to 5.
- **Forsaken:** a death shows how many tower sets you cleared.

### Fixed

- **Graven Image 2:** Gravitas wipes unless it lands on the whole party.
