# Last Fare

A first-person horror game about driving rideshare at night and picking up passengers you really shouldn't.

Unity 6 (`6000.6.5f1`), URP, Input System.

## Getting started

1. Install Unity Hub and editor version `6000.6.5f1`.
2. Install [Git LFS](https://git-lfs.com) and run `git lfs install` once.
3. Clone, then open the folder in Unity Hub.
4. Run the edit-mode tests: **Window > General > Test Runner > EditMode**.

## Playing the first fare
1. Open the project in Unity and wait for scripts to compile.
2. Menu **Last Fare > Create First-Fare Prototype Scene** (creates `Assets/_Project/Scenes/Main.unity` and the content assets).
3. Press **Play**.

| Control | Action |
|---|---|
| Mouse | Look (turn around to see the back seat) |
| Hold right mouse | Look at the rear-view mirror |
| W / S | Raise / lower cruise speed |
| Space | Brake hard |
| E / Q | Accept / decline the ride request |
| R | After a ride, take another fare |

The car stops itself at the pickup and drop-off. Details on the passenger: [`docs/passengers/the-quiet-one.md`](docs/passengers/the-quiet-one.md).

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
