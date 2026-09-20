# Unity Project Scaffold + Stage 1 (Player Movement + Camera Follow)

## Goal
Create the Unity project itself (currently only CLAUDE.md/SESSION_PLAN.md exist) and
implement Stage 1 from SESSION_PLAN.md: a controllable square that moves around an
empty arena with a camera that follows it smoothly, per CLAUDE.md's architecture rules.

## Scope
This spec covers project bootstrap + Stage 1 only. Stages 2-7 in SESSION_PLAN.md are
out of scope and will each get their own session/spec later.

## Project setup
- Unity project created at repo root (`D:\proj\top-down-arena-survivor`), using the
  locally installed Unity **6000.6.2f1**.
- Render pipeline: **URP with 2D Renderer**. CLI project creation defaults to the
  Built-in pipeline, so URP is added as a package and configured (URP asset + 2D
  Renderer Data created and assigned in Graphics/Quality settings) via an Editor script.
- Input: **Input System package**, active input handling set to "Input System Package
  (New)" only (not both).
- A Unity-appropriate `.gitignore` is added (Library/, Temp/, obj/, Build/, Logs/,
  UserSettings/, etc).
- Folder structure created exactly per CLAUDE.md:
  ```
  Assets/Scripts/{Player,Enemies,Weapons,Systems,UI,Data}
  Assets/ScriptableObjects/{Enemies,Weapons,Upgrades}
  Assets/Prefabs
  Assets/Scenes
  Assets/Tests
  ```
- CLAUDE.md's `<FILL IN>` placeholders (Unity version, render pipeline, current status)
  are filled in once the project exists.

## Stage 1 — plain classes (unit-testable, no MonoBehaviour)
Namespace `ArenaSurvivor.Player`:

- **`PlayerMovement`**: `Vector2 ComputeNextPosition(Vector2 currentPosition,
  Vector2 moveInput, float speed, float deltaTime)`. Normalizes `moveInput` when its
  magnitude exceeds 1 so diagonal movement isn't faster than axis-aligned movement.

- **`CameraFollow`**: `Vector2 ComputeNextPosition(Vector2 currentPosition,
  Vector2 targetPosition, float followSpeed, float deltaTime)`. Exponential/SmoothDamp-
  style interpolation towards the target; never overshoots the target in a single call.
  Optional `Bounds?` clamp parameter to keep the camera within arena limits.

## Stage 1 — MonoBehaviour glue (thin, forwards to plain classes)
- **`PlayerMovementComponent`**: builds a 2D composite `InputAction` in code in `Awake`
  (WASD + arrow keys + gamepad left stick bound into a single Vector2 action — no
  `.inputactions` asset to hand-wire in the Inspector), enables/disables it in
  `OnEnable`/`OnDisable`, reads it in `Update`, calls `PlayerMovement.ComputeNextPosition`,
  applies the result with `Rigidbody2D.MovePosition` in `FixedUpdate`.
- **`CameraFollowComponent`**: holds a `target` Transform reference (wired by the
  scene-build Editor script, not by hand), calls `CameraFollow.ComputeNextPosition`
  each `LateUpdate`.
- **Player object**: white square sprite generated at edit time (1x1 texture, tinted
  blue), `Rigidbody2D` (gravity scale 0, no rotation), `BoxCollider2D`,
  `PlayerMovementComponent`.
- **Arena**: a large flat square sprite as the floor, plus four thin `BoxCollider2D`
  boundary walls positioned at the arena edges so the player can't walk off.
- All of the above (GameObjects, components, prefab, scene) are constructed by an
  Editor script run via `-executeMethod`, not hand-edited scene/prefab YAML — consistent
  with CLAUDE.md rule 3.

## Testing
`Assets/Tests/EditMode`:
- **`PlayerMovementTests`**: diagonal input is normalized (speed matches axis-aligned
  movement), straight-line movement covers `speed * deltaTime` distance, zero input
  produces zero displacement.
- **`CameraFollowTests`**: position moves toward target each call, distance to target
  strictly decreases (no overshoot) for a single step, reaches the target after enough
  steps.

## Verification
- Unity batchmode run forces a compile; any compile errors fail the session before
  anything else is claimed done.
- Unity batchmode `-runTests -testPlatform EditMode` executes the tests above; results
  are read from the output XML.
- Play-feel (does WASD movement actually feel smooth, does the camera follow nicely,
  does the arena boundary work) can't be verified headlessly — the user is asked to
  open the project in the Editor, press Play, and report back per CLAUDE.md's
  "Verifying changes" section.

## Out of scope
- Enemies, weapons, XP, upgrades, spawner, polish (Stages 2-7).
- Gamepad vibration, rebindable controls UI, save systems.
- Any hand-tuned art beyond placeholder colored squares.
