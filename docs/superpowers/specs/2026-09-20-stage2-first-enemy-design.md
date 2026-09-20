# Stage 2 — First Enemy

> Design decisions in this spec were self-approved by Claude Code, per the user's
> explicit authorization to work autonomously overnight (2026-09-20 session) and
> decide reasonable design points without waiting for review. Flagged for the user
> to skim when they're back, not blocking.

## Goal
Per SESSION_PLAN.md Stage 2: one enemy type that spawns (manually placed, no spawner
yet), chases the player, and deals contact damage. Player health reacts to 0 HP by
logging and disabling the player.

## Scope
This spec covers Stage 2 only. No spawner/waves (Stage 6), no way to damage/kill
enemies yet (Stage 3's projectiles), no XP drops (Stage 4).

## New shared system: `Health`
`Health` is explicitly called out in SESSION_PLAN.md as used by both player and
enemies — so it's a generic, reusable plain class + MonoBehaviour pair, not a
player-specific one (CLAUDE.md's own `PlayerHealth`/`PlayerHealthComponent` example is
illustrative, not literal — genericizing it avoids a near-duplicate `EnemyHealth` in
Stage 3 when enemies start taking projectile damage).

- **`ArenaSurvivor.Systems.Health`** (plain class): `int CurrentHealth`, `int MaxHealth`,
  constructor `Health(int maxHealth)`, `void TakeDamage(int amount)` (ignores
  non-positive amounts, clamps `CurrentHealth` at 0), `bool IsDead`,
  `event Action OnDeath` (fires exactly once, the instant `CurrentHealth` reaches 0).
- **`ArenaSurvivor.Systems.HealthComponent`** (MonoBehaviour): `[SerializeField] int
  maxHealth`, owns a `Health`, exposes `public void ApplyDamage(int amount)` (forwards
  to the plain class) and re-exposes `event Action OnDeath`. Generic — attached to both
  the player and the enemy.
- **`ArenaSurvivor.Player.PlayerDeathReaction`** (MonoBehaviour, player-specific):
  `[RequireComponent(typeof(HealthComponent))]`, subscribes to `OnDeath` in `OnEnable`,
  unsubscribes in `OnDisable`. On death: `Debug.Log("Player died")` and
  `gameObject.SetActive(false)` — literal reading of SESSION_PLAN's "for now, just log
  and disable the player." Keeping this reaction in its own small script (rather than
  in `HealthComponent` itself) keeps `HealthComponent` reusable for the enemy, which
  will want a different reaction (destroy/deactivate + drop XP) starting Stage 3-4.

## New enemy logic
- **`ArenaSurvivor.Enemies.EnemyChase`** (plain class): `Vector2 ComputeNextPosition
  (Vector2 currentPosition, Vector2 targetPosition, float speed, float deltaTime)` —
  `Vector2.MoveTowards(currentPosition, targetPosition, speed * deltaTime)`. Using
  `MoveTowards` (not a raw direction*speed step) means the enemy never overshoots past
  the player in one step, mirroring `CameraFollow`'s "never overshoots" guarantee.
- **`ArenaSurvivor.Enemies.EnemyChaseComponent`** (MonoBehaviour):
  `[RequireComponent(typeof(Rigidbody2D))]`, `[SerializeField] float chaseSpeed = 3f`
  (slower than the player's 5, so the player can outrun it), `[SerializeField]
  Transform target`, `public void SetTarget(Transform newTarget)` for scene-builder
  wiring (mirrors `CameraFollowComponent.SetTarget`), `FixedUpdate` calls
  `EnemyChase.ComputeNextPosition` then `Rigidbody2D.MovePosition`.
- **`ArenaSurvivor.Enemies.EnemyContactDamage`** (MonoBehaviour):
  `[SerializeField] int damageAmount = 10`, `[SerializeField] float damageInterval =
  1f`. Tracks `lastHitTime` (initialized so the first touch damages immediately).
  `OnCollisionStay2D(Collision2D collision)`: if `Time.time - lastHitTime >=
  damageInterval`, look up a `HealthComponent` on `collision.gameObject` and call
  `ApplyDamage`, then reset `lastHitTime`. Living on the enemy (the damage source)
  rather than the player (the target) is the more scalable pattern — Stage 3's
  `Projectile` will deal damage the same way, and neither script needs to know the
  other side's type, just that it carries a `HealthComponent`.

## Scene changes (via Editor script, not hand-edited YAML)
A new Editor script extends the existing `Assets/Scenes/Arena.unity` (opens it with
`EditorSceneManager.OpenScene`, does not recreate it from scratch):
- Adds `HealthComponent` (maxHealth 100) + `PlayerDeathReaction` to the existing
  `Player` GameObject.
- Builds an `Enemy` prefab (`Assets/Prefabs/Enemy.prefab` via
  `PrefabUtility.SaveAsPrefabAsset`): the placeholder square sprite tinted red
  `(1f, 0.2f, 0.2f)` at 0.8x scale (visually smaller/distinct from the player),
  `Rigidbody2D` (gravityScale 0, `FreezeRotation`, `RigidbodyInterpolation2D.Interpolate`),
  `BoxCollider2D`, `HealthComponent` (maxHealth 30 — unused for now since nothing
  damages the enemy until Stage 3, but present per the spec's "used by both"),
  `EnemyChaseComponent`, `EnemyContactDamage`. `SpriteRenderer.sortingOrder = 5`
  (between the walls' 0 and the player's 10, per the sorting convention CLAUDE.md
  documents from Stage 1).
- Instantiates one `Enemy` instance into the scene at a fixed offset from center
  (`(5, 5, 0)`) — "manually placed... no spawner yet" — and calls
  `enemyChaseComponent.SetTarget(player.transform)`.
- Saves the scene.

## Testing
`Assets/Tests/EditMode`:
- **`HealthTests`**: `TakeDamage` reduces `CurrentHealth`; clamps at 0 (never
  negative); `OnDeath` fires exactly once when health first reaches 0; `OnDeath` does
  not fire again on further `TakeDamage` calls after death; non-positive damage amounts
  are ignored (health unchanged, no event).
- **`EnemyChaseTests`**: moves toward the target each call; never overshoots past the
  target in one step (distance-to-target strictly decreases, clamped at 0); zero speed
  produces no movement; reaches the target exactly when the remaining distance is
  smaller than one step.

## Asmdef plan (known upfront this time, not discovered mid-task)
- New `Assets/Scripts/Systems/ArenaSurvivor.Systems.asmdef` (`references: []`,
  `autoReferenced: true`) — `Health`/`HealthComponent` need only core `UnityEngine`.
- New `Assets/Scripts/Enemies/ArenaSurvivor.Enemies.asmdef` (`references:
  ["ArenaSurvivor.Systems"]`, `autoReferenced: true`) — `EnemyContactDamage` needs
  `HealthComponent`.
- Modify `Assets/Scripts/Player/ArenaSurvivor.Player.asmdef`: add
  `"ArenaSurvivor.Systems"` to `references` (for `PlayerDeathReaction`).
- Modify `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef`: add
  `"ArenaSurvivor.Systems"` and `"ArenaSurvivor.Enemies"` to `references`.

## Verification
Same pattern as Stage 1: Unity batchmode compile check, `-runTests -testPlatform
EditMode` for the new tests plus full regression of the existing 7, and a batchmode
`-executeMethod` run of the scene-extension script with before/after checks that the
Player gained the new components and the Enemy was placed. Play-feel (does the enemy
actually chase, does contact damage tick, does the player vanish on death) needs a
human eye — noted for the user to check when they're back, same as Stage 1's hand-off.

## Out of scope
No way to kill the enemy yet (Stage 3). No spawner/waves (Stage 6). No XP drop on
enemy death (Stage 4). No UI/health bar (Stage 4-5).
