# CHRYSALIS: ZERO

Unity project for the mobile action-RPG prototype, with a playable Rusthollow combat scene and a standalone browser prototype.

## Unity project

- Unity version: 6000.6.9f1
- Open this repository root in Unity Hub.
- Open `Assets/Scenes/RusthollowDemo.unity` and press **Play**.

The scene is an original 2D isometric interior of the Rusted Halo in Rusthollow. It includes an angled room and diamond-tile floor, hand-drawn rear/profile/front character animation, separated enemy positions, layered furniture with collision, a mechanic NPC, visible rifle shots and impact effects, and a five-raider bounty.

Controls:

- WASD or arrow keys: move in isometric directions (click the Game view first to give it keyboard focus)
- Mouse: aim
- Left mouse button: fire
- Right mouse button or Space: melee attack
- Shift: run
- Q: Tactical Focus (slow time)
- E: Bio-Boost
- F: talk to Mara when nearby
- R: restart after being downed

Original editable SVG artwork is in `ArtSource/Rusthollow/`; the Unity-ready PNGs are in `Assets/Resources/RusthollowArt/`.

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
