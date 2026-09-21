# Stage 3 — Attack + Projectiles

> Self-approved by Claude Code per the user's standing authorization to work
> autonomously through the remaining stages.

## Goal
Per SESSION_PLAN.md Stage 3: the player can damage enemies. Auto-fire toward the
nearest enemy on a cooldown; a basic (non-pooled) `Projectile`; enemy `Health` takes
damage from hits and dies (destroyed) at 0.

## Design choice: auto-fire, not click-to-shoot
SESSION_PLAN offers either. Auto-fire toward the nearest enemy matches the genre
(CLAUDE.md's own opening line: "player auto-attacks or aims a weapon") and needs no
new input handling — simpler, and consistent with what's already built.

## Reuse + one small refactor
`ArenaSurvivor.Systems.DamageCooldown` (Stage 2) is a generic "has enough time passed
since last trigger" gate — exactly what attack fire-rate needs too. Renaming it to
`ArenaSurvivor.Systems.Cooldown` (it's no longer damage-specific) and reusing it for
both `EnemyContactDamage` and the new `AutoAttackComponent` avoids a near-duplicate
class. This touches Stage 2's `EnemyContactDamage.cs` and its test file — a small,
justified refactor now that a second consumer exists, not scope creep.

## New: `ArenaSurvivor.Weapons` (new folder + asmdef)
- **`NearestTargetSelector`** (plain, static): `int FindNearestIndex(Vector2 origin,
  IReadOnlyList<Vector2> candidatePositions)` — returns the index of the closest
  position, or `-1` if the list is empty. Kept engine-decoupled from `Transform` on
  purpose (just `Vector2`s) so it's trivially unit-testable; the MonoBehaviour side
  keeps a parallel list of `Transform`s and uses the returned index.
- **`Projectile`** (plain): `Vector2 ComputeNextPosition(Vector2 currentPosition,
  Vector2 velocity, float deltaTime)` — straight-line movement. Trivial, but
  SESSION_PLAN explicitly calls for a plain `Projectile` class, so it gets a test.
- **`ProjectileComponent`** (MonoBehaviour): `Rigidbody2D` (kinematic — moved
  manually, not physics-driven), trigger `BoxCollider2D`. `public void
  Launch(Vector2 direction)` sets velocity and self-destructs via `Destroy(gameObject,
  lifetime)` (3s default — no pooling yet, Stage 6 upgrades this). `OnTriggerEnter2D`:
  look up `HealthComponent` on the other collider, `ApplyDamage`, destroy self on hit.
- **`AutoAttackComponent`** (MonoBehaviour, attached to Player): every `Cooldown`
  interval, gathers all `EnemyChaseComponent` instances in the scene
  (`Object.FindObjectsByType`, unambiguous — no custom tag needed, avoiding
  TagManager.asset editing), calls `NearestTargetSelector.FindNearestIndex`, and if
  one is in range, instantiates a `Projectile` prefab aimed at it.

## Enemy death
- **`EnemyDeathReaction`** (MonoBehaviour, mirrors `PlayerDeathReaction`'s shape):
  `[RequireComponent(typeof(HealthComponent))]`, subscribes to `OnDeath`, on death
  calls `Destroy(gameObject)` — no pooling yet, so an outright `Destroy` (not
  deactivate) is correct and simplest; Stage 6 will change this to pool-return.

## Scene/prefab changes (Editor script, re-runnable safely per Stage 2's fix)
A new `Stage3SceneBuilder`:
- Adds `EnemyDeathReaction` to the existing `Assets/Prefabs/Enemy.prefab` via
  `PrefabUtility.LoadPrefabContents`/edit/`SaveAsPrefabAsset` (the correct API for
  editing an existing prefab asset, not recreating it).
- Creates `Assets/Prefabs/Projectile.prefab`: small square (0.3x scale, yellow),
  kinematic `Rigidbody2D`, trigger `BoxCollider2D`, `ProjectileComponent`.
- Opens `Assets/Scenes/Arena.unity`, adds `AutoAttackComponent` to the existing
  `Player` (wiring the projectile prefab reference), saves.
- Follows the same idempotency/null-check/save-prompt guards Stage 2's fix
  established.

## Testing
- **`NearestTargetSelectorTests`**: empty list returns -1; single candidate returns
  index 0; picks the genuinely closest of several; ties broken consistently (first
  occurrence wins — documented, not left to chance).
- **`ProjectileTests`**: straight-line movement matches `velocity * deltaTime`; zero
  velocity produces no movement.
- **`CooldownTests`** (renamed from `DamageCooldownTests`): unchanged behavior, same
  5 cases, just the type name changes.

## Out of scope
No spawner/waves (Stage 6). No pooling (Stage 6). No XP drop on death (Stage 4). No
UI (Stage 4-5). No upgrade to attack stats yet (Stage 5).
