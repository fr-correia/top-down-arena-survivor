# Arena Survivor

A top-down 2D "arena survivor" (Vampire Survivors–style) built in Unity. The
player stands in an open arena, enemies spawn in escalating waves and converge
on them, the player auto-fires at the nearest enemy, kills drop XP, and
leveling up offers a choice of upgrades. Runs to a game-over screen with a
saved high score.

## Status: complete (Stages 1–7)

All 7 stages from [SESSION_PLAN.md](SESSION_PLAN.md) are implemented, tested,
and merged to `main`:

1. **Player movement + camera follow** — WASD/gamepad movement via the new
   Input System, smoothed camera follow, arena boundary.
2. **First enemy** — enemy chase AI, contact damage, `Health`/`OnDeath`.
3. **Attack + projectiles** — auto-fire at the nearest enemy on a cooldown.
4. **XP and leveling** — XP orb pickups, a leveling curve, an on-screen XP bar.
5. **Upgrade choice screen** — leveling up pauses the game and offers 3 random
   upgrades (damage, move speed, cooldown, max HP).
6. **Spawner + waves + pooling** — wave-based `EnemySpawner`, object pooling
   for enemies and projectiles so the game holds up with 100+ enemies on
   screen.
7. **Polish pass** — screen shake on player hit/death, hit-flash feedback,
   main menu, game-over screen with a run timer, and a `PlayerPrefs`-backed
   high score.

## Playing it

Open the project in Unity Hub (see version below), open
`Assets/Scenes/Arena.unity`, and press Play. WASD or a gamepad left stick to
move; the player attacks automatically. Survive as long as you can — your
best time is saved as the high score.

## Architecture

See [CLAUDE.md](CLAUDE.md) for the full rules this project follows. In short:

- Gameplay logic lives in plain, unit-tested C# classes (`Assets/Scripts`);
  MonoBehaviours are thin wrappers that forward Unity lifecycle calls into
  them.
- Designer-tunable data (enemy types, weapon stats, upgrades, waves) is
  ScriptableObject assets under `Assets/ScriptableObjects`.
- Every non-trivial plain class has a matching EditMode test under
  `Assets/Tests/EditMode` — 75 tests, all green.
- Scene/prefab wiring is done via self-verifying Editor scripts
  (`Assets/Editor`) rather than hand-edited YAML.

Design specs and implementation plans for each stage are under
[docs/superpowers](docs/superpowers).

## Environment

- Unity 6000.6.2f1
- Universal Render Pipeline (2D Renderer)
- New Input System (keyboard + gamepad)
- Physics2D for all movement/collision

## Running tests

```bash
./run_unity.sh -runTests -testPlatform EditMode -testResults Logs/results.xml -quit
```

## Docs

- [CLAUDE.md](CLAUDE.md) — architecture rules and conventions for this repo
- [SESSION_PLAN.md](SESSION_PLAN.md) — stage-by-stage build order
- [docs/superpowers](docs/superpowers) — per-stage design specs and plans
