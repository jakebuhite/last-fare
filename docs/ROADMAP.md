# Roadmap and Implementation Approach

## Guiding principle: vertical slice first
Build **one complete fare** end to end (request > drive > passenger event > drop-off > result) with placeholder art before widening. If one ride isn't scary and fun, ten won't be. Everything else is a content multiplier on that loop.

## Recommended technical approach
- **Driving**: free driving on an open city grid with arcade handling (ADR 0003). The always-on GPS tells the player where to go; set pieces anchor to the GPS route rather than a fixed track.
- **Data-driven content**: passengers and rides are ScriptableObjects (`PassengerDefinition`, `RideDefinition`). New fares shouldn't need new code unless they introduce a new behaviour.
- **State machines**: `RideController` owns the ride lifecycle; UI, audio and passenger behaviour subscribe to its events.
- **Scripted scares**: Timeline plus signal receivers for authored moments; code for systemic ones (mirror checks, rules).
- **Dialogue**: adopt Yarn Spinner or Ink once M07 starts. Don't build a dialogue system.
- **Look**: URP, low-poly with a dithering/PS1 post effect, fog, one or two light sources per scene.
- **Audio** is half the horror. Budget time for it from the start: engine hum, rain, radio bleed, 3D passenger sounds.

## Milestones
| # | Name | Goal | Exit criteria |
|---|---|---|---|
| 0 | Foundation | Repo, folders, docs, core data and state code | Done |
| 1 | Greybox car | Player seated in car, look around, mirror, phone prop | Can look at the mirror and phone in a blockout car |
| 2 | One road | Car travels a spline loop at night with fog and audio | Driving feels tense with no passenger |
| 3 | One fare | Request > pickup > ride > drop-off with **The Quiet One** | A tester can finish the ride and be scared at least once. **Built (greybox), awaiting playtest.** Milestones 1-2 were folded into it. |
| 4 | Rules and journal | Passenger rules, journal, rating consequences | A second playthrough is different because the player learned something |
| 5 | Three passengers | The Chatterbox and The Wrong Address added | Content added using mostly data, not new systems |
| 6 | Shift structure | Several fares, dawn timer, results screen, save | A full night is playable |
| 7 | Vertical slice polish | Art pass, audio pass, playtest feedback | Showable to outsiders |

## Managing ideas (how we avoid losing them or drowning in them)
1. **Capture**: anything goes into `docs/IDEAS.md` (one line) or a GitHub issue using the *Idea* template. No judging at capture time.
2. **Triage** (weekly, 15 min): each idea becomes *promote*, *park*, or *cut*.
3. **Promote**: add a row in `docs/MECHANICS.md` (or a file in `docs/passengers/`) with status `planned` and a priority.
4. **Build**: planned work becomes a GitHub issue assigned to a milestone above. Use labels `mechanic`, `passenger`, `tech`, `art`, `audio`, `bug`.
5. **Decide**: any choice that's expensive to reverse gets a short ADR in `docs/decisions/` so the reasoning survives.
6. **Scope guard**: nothing marked P1 or P2 gets built before the milestone 3 exit criteria are met.

## Team workflow
- Branch per task, small PRs into `main`. Scenes and prefabs are the main conflict risk: avoid two people editing the same scene, and prefer prefabs.
- Enable Unity's smart merge: `git config merge.unityyamlmerge.driver "'<UnityInstall>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"` (optional).
- In Unity: **Project Settings > Editor > Asset Serialization = Force Text** and **Version Control = Visible Meta Files**.
- Run the EditMode tests before pushing.
