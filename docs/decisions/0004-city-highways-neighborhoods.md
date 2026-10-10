# 0004: World layout: downtown, highways, neighborhoods

**Status**: accepted (builds on [0003](0003-open-map-free-driving.md))

## Context
A single city grid made every fare feel the same and gave the "taking someone home" fantasy nowhere to go. Rideshare at night is mostly the long drive out: leave the lit city, take a dark highway, then find the house among identical cul-de-sacs.

## Decision
- `WorldGenerator` (plain C#, deterministic per seed) produces a `WorldPlan`:
  - a 5×5-block downtown where fares start;
  - three highways of about 1 km, built as smoothed splines sampled every 30 m;
  - three neighborhoods, each a main drive with curving side streets ending in cul-de-sacs.
- Houses get real addresses (even numbers on the right, odd on the left), so a `RideDefinition` drop-off label like "114 Aldous Street" resolves to a house. The build fails if it doesn't.
- The road graph is general: `RoadMap` stores nodes, named segments and kinds (street or highway), not a grid. The GPS routes on it and names the roads in its directions.
- **Scene size:** repeated objects are prefabs (16 house variants, 3 trees, 2 lamps, 2 cars), and flat road pieces are merged into a few meshes. The scene file is about 4 MB.
- **Off-road:** grass and pavement cap the car at 9 m/s, so highways and streets are the way to travel.

## Consequences
- New neighborhoods or highways are a few lines in `WorldGenerator` (a style plus control points). Layout tests check connectivity, unique addresses and that nothing overlaps the roads.
- Rebuilding regenerates `Art/Generated/`, including prefab GUIDs, so hand-edit prefab copies outside that folder.
- Every route between districts has exactly one highway, so there are no route choices yet (see IDEAS: county road).
- With about 7,000 renderers and about 260 lights, the scene relies on fog, far-plane culling, static batching and Forward+. Profile before adding much more.
