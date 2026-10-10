# Last Fare: Game Design

## Pitch
You're a rideshare driver working the night shift. Every request pays, and every passenger is a little wrong. Survive until dawn, keep your rating up, and don't ask where the last driver went.

## Pillars
1. **The car is the whole game.** Claustrophobic, one interior, one windshield, one rearview mirror. Fear comes from being trapped with someone.
2. **Observation over reflexes.** Players survive by noticing things (clues, rules, inconsistencies), not by shooting or fast twitch play.
3. **Politeness is the trap.** The rating system rewards friendliness, but friendliness gets you killed. Every ride is a tension between customer service and self-preservation.
4. **Dread, not gore.** Slow-building wrongness. Mundane app UI colliding with the impossible.

## Core loop (one fare, ~4-8 min)
1. **Request**: a phone card shows pickup, dropoff, fare, passenger name and rating. Red flags are visible if you look (1-star history, pickup at a closed cemetery). Accept or decline (declining costs acceptance rate).
2. **Pickup drive**: night driving to the pin. Radio, GPS and road events set the mood.
3. **Boarding**: passenger enters. First impression and a chance to refuse the ride.
4. **The ride**: conversation, mirror checks, route demands, escalating events. The passenger has hidden rules you can learn.
5. **Drop-off**: the ending state is one of: delivered, escaped, hurt, or *still in the car*.
6. **Result**: rating change, pay, journal entry, dashcam footage saved.

## Meta loop (a shift = a night, ~5 fares until dawn)
- Earn money to cover fuel, car repairs and rent.
- Rating is your health. Too low and you're deactivated (run over).
- Rules and clues discovered are kept in the **journal**, so knowledge is the persistent progression, not stats.
- A background mystery (the previous driver, the app itself) unfolds across nights.

## Tone and look
Low-poly, slightly PS1 and dithered, heavy fog and sodium streetlight orange. Cheap to make, reads as horror, hides rough art.

## Open questions
Track these in `docs/IDEAS.md` and resolve with an ADR in `docs/decisions/`.
- ~~Free driving or semi-on-rails along splines?~~ Free driving on an open map with an always-on GPS (ADR 0003).
- Run-based roguelite (random fares each night) or authored story nights?
- Is the player ever able to leave the car?
