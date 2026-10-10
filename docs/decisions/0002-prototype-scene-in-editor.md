# 0002: Build the prototype scene in the editor, not at runtime

**Status**: accepted

## Context
The first-fare prototype was generated entirely at runtime by a `PrototypeBuilder` MonoBehaviour. The scene held one empty GameObject, so nothing could be inspected, moved or tuned without editing code, and art could not be swapped in piece by piece.

## Decision
`Last Fare > Build First-Fare Prototype Scene` (`PrototypeSceneBuilder`, editor-only) builds the greybox into `Main.unity` as ordinary GameObjects. Components reference each other through `[SerializeField]` fields instead of runtime `Init()` calls. Generated materials, the road mesh and the mirror render texture are saved to `Assets/_Project/Art/Generated/`. The street grid lives on a `CityMap` component.

## Consequences
- The scene is the source of truth: hand-edit it, replace greybox with real art, turn pieces into prefabs.
- Rebuilding overwrites `Main.unity` and `Art/Generated/`. Once the scene has hand edits, stop rebuilding and treat the builder as a reference.
- Stops are the `PickupPoint` / `DropoffPoint` transforms under `Stops`; move them in the scene view. Hazards are placed at runtime along the GPS route.
- `LastFareAudio` still generates its clips at runtime until there's authored audio.
