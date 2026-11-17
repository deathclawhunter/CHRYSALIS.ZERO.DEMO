# CHRYSALIS: ZERO

Unity project for the mobile action-RPG prototype, plus a standalone browser demo for the first milestone.

## Unity project

- Unity version: 6000.6.9f1
- Open this repository root in Unity Hub.

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
