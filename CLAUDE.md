# Arena Survivor — Project Guide for Claude Code

## What this is
A top-down 2D "arena survivor" (Vampire Survivors–style) built in Unity. Player stands
in an open arena, enemies spawn in waves and converge on them, player auto-attacks or
aims a weapon, kills drop XP, leveling up offers a choice of upgrades. Built in stages —
see `SESSION_PLAN.md` for the build order.

## Environment
- Unity version: <FILL IN — e.g. 6000.0.x LTS>
- Render pipeline: <FILL IN — URP 2D recommended for a 2D project>
- Platform target: PC (Windows/Mac), keyboard + gamepad via the new Input System
- Input: use Unity's Input System package (not the legacy Input Manager)
- Physics: Physics2D (Rigidbody2D, Collider2D) for all movement/collision

## Architecture rules (read this before writing scripts)
1. **Gameplay logic lives in plain C# classes, not MonoBehaviours.** A MonoBehaviour's
   job is to sit in the scene, hold Inspector-exposed references, and forward Unity
   lifecycle calls (Update, OnCollisionEnter2D, etc.) into a plain class that does the
   actual work. This keeps logic unit-testable and keeps the Unity-specific glue thin.
   Example: `PlayerHealth` (plain class, has `TakeDamage(int)`, fires a C# event on
   death) vs. `PlayerHealthComponent` (MonoBehaviour, owns a `PlayerHealth`, calls it
   from `OnTriggerEnter2D`).
2. **Data goes in ScriptableObjects, not hardcoded in scripts.** Enemy types, weapon
   stats, upgrade definitions — anything a designer (future you) would want to tweak —
   should be a ScriptableObject asset, not a number buried in code.
3. **No logic in scenes/prefabs beyond wiring.** Scene and prefab files are Editor-owned;
   Claude Code can read them but should avoid hand-editing the YAML directly. When a
   change requires touching a prefab/scene (adding a component, assigning a reference),
   say so explicitly and give clear step-by-step Editor instructions instead of trying
   to hand-edit the file, unless working via an Editor script.
4. **Prefer editor scripts / `[CreateAssetMenu]` tooling over manual Inspector busywork**
   where it saves real time (e.g. a script to batch-generate enemy ScriptableObjects).

## Folder structure
```
Assets/
  Scripts/
    Player/
    Enemies/
    Weapons/
    Systems/       (spawner, XP/leveling, game state)
    UI/
    Data/          (ScriptableObject class definitions)
  ScriptableObjects/
    Enemies/
    Weapons/
    Upgrades/
  Prefabs/
  Scenes/
  Tests/           (EditMode/PlayMode NUnit tests)
```

## Coding conventions
- Namespace root: `ArenaSurvivor.*` matching the folder (e.g. `ArenaSurvivor.Player`)
- PascalCase for classes/methods, camelCase for private fields, no `m_` or `_` prefixes
- Public API on plain classes should be small and event-driven (C# `event Action<T>`)
  rather than requiring callers to poll state every frame
- Every plain (non-MonoBehaviour) gameplay class should have a matching EditMode test
  in `Assets/Tests` where the logic is non-trivial (damage calc, leveling curve, etc.)

## Verifying changes
After making script changes, check for compile errors before considering a task done.
If Unity batch-mode compilation is set up (`Unity -batchmode -quit -projectPath . 
-executeMethod <BuildMethod>`), run it. Otherwise, ask the user to report any errors
from the Unity Console after they reload the project, and treat that as the test result.

## Current status
<FILL IN as you go — e.g. "Stage 1 (player movement + camera) in progress">
