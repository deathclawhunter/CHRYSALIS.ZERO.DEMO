# CHRYSALIS: ZERO

Unity project for the mobile action-RPG prototype, with a playable Rusthollow combat scene and a standalone browser prototype.

## Unity project

- Unity version: 6000.6.9f1
- Open this repository root in Unity Hub.
- Open `Assets/Scenes/RusthollowDemo.unity` and press **Play**.

The scene includes a generated Rusthollow scrapyard arena, a playable third-person operator, raiders, fuel-cell pickups, a HUD, and three bounty objectives.

Controls:

- WASD or arrow keys: move
- Mouse: aim
- Left mouse button: fire
- Q: Tactical Focus (slow time)
- E: Bio-Boost
- Shift: sprint
- Right mouse drag: orbit the camera

The Rustline Rifle gains proficiency from hits, and Core Ability upgrades are available in the HUD when skill points are earned.

## Milestone 1 browser demo

The browser prototype is in [`Prototype/BrowserDemo`](./Prototype/BrowserDemo/). It explores the initial Rusthollow combat and progression loop independently of the Unity scene.

To run it locally:

```bash
cd Prototype/BrowserDemo
python3 -m http.server 8000
```

Open `http://localhost:8000`.

Controls:

- WASD or arrow keys: move
- Mouse: aim
- Left mouse button: fire
- Shift or Space: Tactical Focus
- E: Bio-Boost
- Upgrade Core Abilities from the side panel after earning skill points
