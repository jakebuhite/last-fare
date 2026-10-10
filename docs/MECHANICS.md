# Mechanics Catalog

Status: `idea` > `planned` > `prototype` > `done` / `cut`. Priority: **P0** vertical slice, **P1** alpha, **P2** nice to have.
Add a detail file in `docs/mechanics/` (copy `_TEMPLATE.md`) once a mechanic is `planned`.

| ID | Mechanic | What it is | Status | Pri |
|---|---|---|---|---|
| M01 | Star rating as health | 5-star rating drops from bad service or scares. Below ~3.5 you're deactivated. | prototype (`DriverRating`) | P0 |
| M02 | Ride request screen | Phone UI with passenger name, rating, pickup and dropoff, fare, countdown. Accept or decline. | prototype (HUD) | P0 |
| M03 | Red flags | Readable clues on the request (odd name, bad reviews, impossible pickup). Skilled players can screen passengers. | prototype | P0 |
| M04 | Rearview mirror | Look up to check the backseat. Mirror can disagree with reality. Some things only happen when you look away, others only when you look. | prototype | P0 |
| M05 | Driving | Free driving: gas, brake/reverse, steering, handbrake; grip limits turning at highway speed; off-road is a crawl; buildings, rails and trees block you. See ADR 0003/0004. | prototype (`CarController`, `ArcadeDrive`) | P0 |
| M06 | Passenger rules | Each passenger has hidden rules (don't answer questions, don't use the radio, never stop). Learn them by observation. | prototype | P0 |
| M07 | Conversation | Short dialogue choices. Engaging, ignoring and lying all have consequences. | planned | P1 |
| M08 | Journal | Discovered rules, clues and passengers recorded. The core progression: knowledge. | planned | P1 |
| M09 | Dashcam | Footage of each ride you can review afterwards. It records things you didn't see. | idea | P1 |
| M10 | Lying GPS | Navigation increasingly disagrees with the road or the passenger's demands. Follow or trust your gut. | idea | P1 |
| M11 | Route demands | Passenger asks for detours or to stop. Complying and refusing both cost something. | idea | P1 |
| M12 | Car controls | Door locks, child locks, windows, radio, heater, glove box, trunk. Each is a tool and a risk. | idea | P1 |
| M13 | Radio | Stations leak broadcasts that react to passengers or foreshadow events. | idea | P2 |
| M14 | Fuel and upkeep | Gas stations are safe-ish spaces and set-piece encounters. Money pressure between fares. | idea | P2 |
| M15 | Decline and cancel | Declining or cancelling hurts acceptance rate. Some passengers punish refusal. | idea | P1 |
| M16 | Fake-out fares | An ordinary rider (no scare) to keep players off balance. | idea | P1 |
| M17 | Dawn timer | Shift ends at sunrise. Time pressure against letting a ride run long. | idea | P1 |
| M19 | GPS navigation | Always-on phone GPS: heading-up minimap that zooms out at speed, route line, turn-by-turn with street names ("Turn left onto Aldous Street"), current road, reroutes when you go off route. Groundwork for M10. | prototype (`GpsNavigator`, `GpsView`) | P0 |
| M18 | Recurring passenger | One figure who appears across nights, getting closer. Ties into the meta mystery. | idea | P2 |
| M19 | Tips and upgrades | Spend money on air freshener (lore items), better dashcam, protective charms. | idea | P2 |
| M20 | Ride endings | Delivered / escaped / still in the car. Each affects rating and the next night. | prototype | P0 |

## Suggested first passengers (see `docs/passengers/`)
- **The Quiet One**: never speaks, only moves when you aren't watching the mirror. (Teaches M04.)
- **The Chatterbox**: friendly, asks personal questions. Answering things you shouldn't gets worse over time. (Teaches M07.)
- **The Wrong Address**: pickup isn't on the map. Nobody is there, but the car door opens. (Teaches M03 and M10.)
- **The Phantom Fare**: fully normal ride. The horror is that nothing happens. (M16.)
