# Last Fare: agent notes

First-person horror game about a rideshare driver on night shifts. Unity 6 (6000.6.5f1), URP, Input System. Design lives in `docs/`; read `docs/GAME_DESIGN.md` before proposing features.

## Conventions
- All game content under `Assets/_Project/`. Don't edit Unity template files (`Assets/Settings`, `Assets/Scenes/SampleScene`) without a reason.
- Code in namespace `LastFare.*`, one assembly per folder: `LastFare.Runtime`, `LastFare.Editor`, `LastFare.Tests.EditMode`.
- Gameplay logic that doesn't need the engine (e.g. `DriverRating`) is plain C# so it can be unit tested. MonoBehaviours stay thin.
- Systems communicate through C# events (see `RideController.StateChanged`), not direct references.
- Passenger and ride content is data-driven via ScriptableObjects (`PassengerDefinition`, `RideDefinition`).
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`. Always commit `.meta` files.
- Large binaries (png, fbx, wav, ...) go through Git LFS per `.gitattributes`.

## Ideas workflow
New ideas go in `docs/IDEAS.md`. Accepted mechanics get a row in `docs/MECHANICS.md`; passengers get a file in `docs/passengers/` from the template. Significant technical choices get a short ADR in `docs/decisions/`.

## Tooling
The Unity MCP package (`com.ivanmurzak.unity.mcp`) is installed, so scene and asset edits can be driven from the editor when it is open.
