# I'm The Manager!!!!!

*A retail simulator inspired by real (chaotic) retail experience.*

**Play it on itch.io:** https://itch.io/profile/parzykmv *(update this with the direct game page link once it's published)*

## What is this project?

**I'm The Manager!!!!!** is a retail store simulator — think **Ross, Burlington**, and every other discount store you've ever walked into — but with a much more chaotic, comedic twist.

This game is inspired by real experience working in retail. Anyone who's worked a job like this knows: customers can be disastrous, oblivious, and downright ridiculous. **I'm The Manager!!!!!** takes that frustration and flips it into something cathartic — you play as an employee surviving 10 shifts, doing the actual job (scanning products, restocking shelves, cleaning up messes, dealing with complaints)... but this time, when things get bad enough, **you get to fight back.**

A persistent **Sanity Meter** tracks how stressed you are throughout each shift. Every mistake, every complaint, every disaster a customer leaves behind pushes it further. Push it too far, and you snap into **Rage Mode** — a short window of chaos where you can shove displays, throw products, and even grab-and-launch customers across the store with full ragdoll physics, before you're pulled back together and have to keep working. Your performance across all 10 days — sales, customers served, how many times you lost it, and how clean you kept the store — decides your final paycheck at the end of the game.



- Kevin Mejia Vazquez — Programming, Design

## How to install and run

**Easiest way:** play it directly on itch.io (link above) — no setup required.

**From source (for development/contributing):**

1. **Install Unity 6** (Personal license or higher) via [Unity Hub](https://unity.com/download).
2. Clone or download this repository.
3. Open Unity Hub → `Add` → select the project's root folder.
4. Open the project (Unity will import all assets and packages automatically — this can take a few minutes the first time).
5. In the Project window, open the **MainMenu** scene (or the store scene directly, if testing gameplay only).
6. Press **Play** in the Editor.

### Controls

| Action | Key |
|---|---|
| Move | WASD |
| Sprint | Left Shift |
| Jump | Space |
| Interact / Pick up / Fix shelf (hold) | E |
| Throw | Left Mouse Button |
| Pause | Esc |

*(An in-game "employee safety training" computer also covers controls the first time you play.)*

## External libraries and tools required

- **Unity 6** (Personal) — engine
- **Universal Render Pipeline (URP)** — rendering
- **Unity Input System** — all player input (New Input System, not the legacy Input Manager)
- **Unity Behavior** (Behavior Graph package) — customer AI
- **Yarn Spinner for Unity** — branching dialogue (Karen's complaints, customer banter, register interactions)
- **TextMeshPro** — all in-game text/UI
- **Unity AI Navigation (NavMesh)** — customer pathfinding

All of the above are standard Unity Package Manager packages and will be restored automatically when the project is opened (`Window → Package Manager`, or via the project's manifest).



