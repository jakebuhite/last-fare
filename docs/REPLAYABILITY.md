# Replayability: Story, Runs and Competition

**Recommendation:** an authored story spine, played as roguelite-style nights, with asynchronous competition on top. Build in that order; each layer only works if the one below is fun.

## Layer 1: Story spine (gives the game a reason to exist)
A fixed arc across ~7 nights: the previous driver, the app, the recurring passenger (M18), and the real meaning of "last fare".
- Certain **anchor fares** are authored and appear on set nights (the story beats).
- Everything between anchors is drawn from the passenger pool, so no two playthroughs have the same night.
- Multiple endings decided by what you learned and what you did on the final fare (e.g. take it, refuse it, get out of the car).

## Layer 2: Replayable nights (the roguelite part)
- **Passenger pool + night generator**: each night picks 4-6 fares from the pool with a difficulty budget that rises per night.
- **Mutators**: a random rule bends the whole night (fog, dead radio, GPS lies, one extra passenger, no phone light).
- **Knowledge as the real progression**: passenger rules are fixed and learned (journal). Experienced players beat nights that wreck first-timers. This is what makes replays feel like mastery, not grind. Do not randomise the rules themselves.
- **Variant passengers**: the same archetype with a twist (The Quiet One who *only* moves while you look at him).
- **Secrets and alternate outcomes**: decline someone and they show up later; a ride "ending" that changes a later fare.
- **Unlocks that add variety, not power**: new cars, charms, radio stations, cosmetic air fresheners.

## Layer 3: Competition (asynchronous, cheap to build)
In priority order:
1. **Daily shift**: the same seeded night for everyone. Compare score (earnings x rating x survival) on a friends or global leaderboard. Needs only a seed and a score endpoint (Steam leaderboards, no custom server).
2. **Personal bests and ghost stats**: "Only 12% of drivers survived The Quiet One."
3. **Dashcam sharing**: export a clip of your worst ride (M09). Strong for streamers.
4. **Challenge modifiers**: score multipliers for self-imposed handicaps (no mirror hold, no decline).

Skipped for now: live PvP or co-op. A second player in the car (one drives, one is the passenger) is a fun later idea, but it's a separate game's worth of netcode.

## Why this order
- The story spine and passenger pool are mostly content on top of the ride loop we're already building.
- Seeded nights make the daily shift nearly free once the generator exists.
- Competition only matters once a night has a score, so design the score early (rating, earnings, fares survived, "still in the car" count).

## Impacts on current work
- Keep fares data-driven (`RideDefinition`, `PassengerDefinition`) so a night generator can pick them.
- Make all randomness go through one seeded `System.Random` owned by the shift, not `UnityEngine.Random`.
- Record a per-ride result struct (outcome, rating delta, fare, highest creep stage) for scoring and the journal.
