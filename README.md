# Last Fare

A first-person horror game about driving rideshare at night and picking up passengers you really shouldn't.

Unity 6 (`6000.6.5f1`), URP, Input System.

## Getting started

1. Install Unity Hub and editor version `6000.6.5f1`.
2. Install [Git LFS](https://git-lfs.com) and run `git lfs install` once.
3. Clone, then open the folder in Unity Hub.
4. Run the edit-mode tests: **Window > General > Test Runner > EditMode**.

## Where things live

| Path | Purpose |
|---|---|
| `Assets/_Project/` | All game content. Don't put game assets anywhere else. |
| `Assets/_Project/Scripts/Runtime` | Game code (`LastFare.Runtime` assembly) |
| `Assets/_Project/ScriptableObjects` | Passenger and ride data assets |
| `docs/` | Design, roadmap, ideas, decisions. **Start with [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md).** |

`Assets/Scenes`, `Assets/Settings` and `InputSystem_Actions.inputactions` are the Unity template defaults and are left in place.

## Managing ideas

Ideas go in [`docs/IDEAS.md`](docs/IDEAS.md) or a GitHub issue using the *Idea* template. Accepted ones get promoted to the mechanics or passenger catalogs. See [`docs/ROADMAP.md`](docs/ROADMAP.md).
