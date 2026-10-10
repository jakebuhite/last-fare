# 0003: Open map with free driving and an always-on GPS

**Status**: accepted (supersedes [0001](0001-semi-on-rails-driving.md))

## Context
0001 put the car on rails so the team could focus on the passenger. In practice "find the pickup, then get them home" is the rideshare fantasy, and it needs the player to choose turns. A GPS that is always on screen also sets up later mechanics (M10 Lying GPS, M11 Route demands).

## Decision
- The world is a street grid (`CityMap`, `RoadNetwork`). The player steers, accelerates, brakes and reverses (`CarController` with `ArcadeDrive` handling). A Rigidbody only resolves collisions with buildings.
- `GpsNavigator` keeps a shortest route from the car to the current stop and reroutes as the player drives. U-turns cost a 60 m penalty so it prefers carrying on. `GpsView` always shows a heading-up minimap and the next turn.
- A stop counts once the car is stationary within 7 m of the pin. Hazards are placed along the GPS route when the passenger boards.

## Consequences
- Players can get lost, take detours and avoid hazards by steering around them. That adds tension but makes it harder to time scripted moments.
- Authored scares can't rely on a fixed track. Anchor them to the GPS route, to stops, or to the passenger.
- Driving needs a playtest pass for feel (speeds, steering, collisions). The prototype has no traffic or pedestrians.
- The grid router is plain C# and unit tested. Irregular maps only need a different `RoadNetwork` constructor.
