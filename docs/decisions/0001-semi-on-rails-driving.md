# 0001: Semi-on-rails driving

**Status**: proposed

## Context
The game is about being trapped in a car with a passenger. Driving needs to feel real but isn't the main challenge. A small team can't afford open-world physics driving plus content.

## Decision
The car follows spline routes; the player controls speed, interactions and junction choices. Revisit if playtests show players want steering.

## Consequences
- Scares and set pieces can be authored along the route.
- Cheaper to build and test; fewer physics bugs.
- Less player freedom, so we must keep the mirror, phone and conversation engaging.
