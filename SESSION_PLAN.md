# Build Order — Arena Survivor

Each stage below is sized to be roughly one Claude Code session. Do them in order;
each one leaves you with something playable. Copy the "Prompt to use" line (or adapt
it) as your first message to Claude Code once you're in the project folder.

## Stage 1 — Player movement + camera follow (start here)
**Goal:** a controllable square that moves around an empty arena with a camera that
follows it smoothly.

**Scope:**
- `PlayerMovement` plain class: takes a movement vector + speed, exposes a method to
  compute the next position (easy to unit test)
- `PlayerMovementComponent` (MonoBehaviour): reads input via the Input System, feeds it
  to `PlayerMovement`, applies the result via `Rigidbody2D.MovePosition`
- Simple `CameraFollow` script: smoothly follows a target transform (Lerp or
  SmoothDamp), with configurable follow speed and optional bounds
- A flat-colored square sprite for the player (placeholder art, primitives are fine)
- A large flat-colored plane/quad for the arena floor, with a `BoxCollider2D` boundary
  so the player can't walk off the edge

**Acceptance criteria:** WASD or left stick moves the player smoothly in 8 directions,
camera keeps the player roughly centered, player can't leave the arena.

**Prompt to use:**
> Read CLAUDE.md for project conventions. Let's build Stage 1: player movement and
> camera follow, per SESSION_PLAN.md. Create the plain PlayerMovement class with a
> unit test, the MonoBehaviour wrapper using the Input System, and a CameraFollow
> script. Tell me exactly what GameObjects/components to set up in the Editor
> afterward.

## Stage 2 — First enemy
**Goal:** one enemy type that spawns, chases the player, and deals contact damage.
- `Health` plain class (current/max HP, `TakeDamage`, `OnDeath` event) — used by both
  player and enemies
- `EnemyChase` logic: moves toward the player's position each frame
- Contact damage on collision, player `Health` wired to a MonoBehaviour that reacts to
  0 HP (for now, just log + disable the player)
- One enemy prefab, manually placed in the scene for now (no spawner yet)

## Stage 3 — Attack + projectiles
**Goal:** the player can damage enemies.
- Simplest version: auto-fire toward the nearest enemy on a cooldown, or click-to-shoot
- `Projectile` plain class + pooled MonoBehaviour (see Stage 6 for full pooling, but a
  basic version here is fine)
- Enemy `Health` takes damage from projectile hits and dies (destroy/deactivate) at 0

## Stage 4 — XP and leveling
**Goal:** killing enemies drops XP orbs; collecting enough triggers a level-up.
- `ExperienceOrb` — simple pickup, plain class tracks XP value + magnet radius
- `PlayerLeveling` plain class: XP total, level curve (e.g. XP needed = base * level^1.2),
  fires `OnLevelUp` event
- Basic on-screen XP bar (UI Toolkit or uGUI, your choice)

## Stage 5 — Upgrade choice screen
**Goal:** leveling up pauses the game and offers 3 random upgrades to pick from.
- `Upgrade` ScriptableObject (name, description, apply-effect)
- A few real upgrades: +damage, +move speed, +attack cooldown reduction, +max HP
- Simple UI: 3 buttons, pause `Time.timeScale = 0` while shown

## Stage 6 — Spawner + waves + pooling
**Goal:** enemies spawn continuously in escalating waves instead of being hand-placed;
performance holds up with 100+ enemies on screen.
- `EnemySpawner`: wave definitions (ScriptableObject), spawn timing, difficulty ramp
- Object pooling for enemies and projectiles (reuse instead of Instantiate/Destroy)

## Stage 7 — Polish pass
- Screen shake on hit/death, hit-flash material feedback, simple main menu + game-over
  screen, a run timer, maybe a simple high-score save (PlayerPrefs is fine to start)

---
Once Stage 1–3 are done you already have a playable (if ugly) game loop — that's a
good checkpoint to stop and just enjoy playing your own thing for a bit before
continuing.
