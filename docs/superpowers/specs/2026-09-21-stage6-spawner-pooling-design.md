# Stage 6 — Spawner + Waves + Pooling

> Self-approved by Claude Code per the user's standing authorization to work
> autonomously through the remaining stages.

## Goal
Per SESSION_PLAN.md Stage 6: enemies spawn continuously in escalating waves (instead
of the single hand-placed enemy from Stage 2) via a `WaveDefinition` ScriptableObject
(spawn timing, difficulty ramp); enemies and projectiles are object-pooled so
performance holds up with 100+ enemies on screen. This directly acts on two things
Stage 5's final review flagged as needed here: an enemy self-registry (replacing
`AutoAttackComponent`'s per-frame `FindObjectsByType` scan) and pooling for both
enemies and projectiles.

## Pooling: use Unity's built-in `UnityEngine.Pool.ObjectPool<T>`, don't reinvent it
Unity ships a generic, engine-tested object pool (`UnityEngine.Pool.ObjectPool<T>`,
no extra package). Writing a bespoke pool class would just be re-testing what the
engine already guarantees — using the built-in is the "don't build what's already
provided" call, not scope-avoidance.

## `PooledObject` (new, generic, `Assets/Scripts/Systems/`)
A GameObject fetched from a pool needs a way to return itself without knowing which
pool it came from. `PooledObject` (MonoBehaviour): `void Initialize(Action<GameObject>
releaseAction)` (called once by the pool's `createFunc`) and `void ReturnToPool()`
(invokes the stored action). Both `Enemy.prefab` and `Projectile.prefab` get this
component added. Existing self-destruct call sites (`EnemyDeathReaction`,
`ProjectileComponent`) change from `Destroy(gameObject)` to "return to pool if
`PooledObject` is present, otherwise `Destroy`" — falls back gracefully for any
non-pooled context (e.g. a hand-placed instance in a test scene).

## Enemy health must reset on reuse
`Health.TakeDamage` latches `IsDead` permanently once true — necessary for Stage 2's
single-death-event guarantee, but it means a pooled-and-reused enemy would come back
inert (still "dead", immune to further damage) unless reset. New `Health.ResetHealth()`
(sets `CurrentHealth = MaxHealth`, `IsDead = false`) + `HealthComponent.ResetHealth()`
forwarder, called by the spawner right after fetching an enemy from the pool.

## `EnemyRegistry` (new, static, `Assets/Scripts/Enemies/`)
Replaces `AutoAttackComponent.Update()`'s `Object.FindObjectsByType<EnemyChaseComponent>()`
scan — flagged by Stage 5's review as a real cost once enemy counts grow, which this
stage's own goal (100+ enemies) makes immediate rather than hypothetical. A static
`List<EnemyChaseComponent>` that `EnemyChaseComponent` adds/removes itself to/from in
`OnEnable`/`OnDisable` — since `SetActive(false)` (the pool's release path) reliably
fires `OnDisable`, and `SetActive(true)` (the pool's get path) reliably fires
`OnEnable`, the registry stays correct across pooled reuse automatically, with no
special-casing needed in the spawner.

## `SpawnScheduler` (new plain class, `Assets/Scripts/Enemies/`) + `WaveDefinition` (new ScriptableObject, `Assets/Scripts/Data/`)
CLAUDE.md rule 2: wave definitions are designer-tunable data, so `WaveDefinition` is a
`[CreateAssetMenu]` ScriptableObject holding the ramp's raw numbers (initial/min spawn
interval, interval decrease per minute, initial enemies-per-spawn, its own
increase-per-minute, and a `maxActiveEnemies` safety cap — 100, matching the stage's
own stated target). `SpawnScheduler` is the actual ramp *logic*, kept fully
engine-independent aside from `Mathf` (matching `Cooldown`'s precedent): constructed
with plain floats (extracted from a `WaveDefinition` by the MonoBehaviour, not holding
a `ScriptableObject` reference itself, so it's trivially unit-testable with
hand-picked numbers), `ComputeSpawnInterval(float elapsedTime)`,
`ComputeEnemiesPerSpawn(float elapsedTime)`, `TryConsumeSpawnTick(float currentTime,
float elapsedTime)` (mirrors `Cooldown`'s gate-and-consume shape, but with a
time-varying interval instead of a fixed one).

## `EnemySpawnerComponent` (new, `Assets/Scripts/Enemies/`)
Owns the enemy `ObjectPool<GameObject>` and a `SpawnScheduler`. Each `Update`, advances
elapsed time, and on a scheduler tick spawns `ComputeEnemiesPerSpawn` enemies (capped
by `WaveDefinition.MaxActiveEnemies`, checked against the pool's own `CountActive`) at
random points on a circle just inside the arena walls (radius 9, walls are at ±10),
each wired with `SetTarget(player)` and `ResetHealth()`.

## Projectiles get their own pool inside `AutoAttackComponent`
Same `ObjectPool<GameObject>` pattern, replacing the raw `Instantiate` call.
`ProjectileComponent.Launch` switches its delayed self-return from
`Destroy(gameObject, lifetime)` (which can't cooperate with pooling) to
`Invoke(nameof(ReturnToPoolOrDestroy), lifetime)`, with a `CancelInvoke()` both at the
top of `Launch` (clears any stale pending invoke from a previous life before scheduling
a new one) and inside the return path itself (a hit-triggered early return must not
leave the delayed invoke armed to prematurely return whatever new shot later occupies
that same pooled instance — a real correctness bug this design deliberately avoids,
not a hypothetical one).

## Scene changes (Editor script, re-runnable safely, self-verifying per Stage 5's lesson)
A new `Stage6SceneBuilder`:
- Adds `PooledObject` to `Enemy.prefab` and `Projectile.prefab` (existing
  `LoadPrefabContents`/edit/`SaveAsPrefabAsset` pattern).
- Creates `Assets/ScriptableObjects/Enemies/WaveDefinition.asset`.
- Removes the Stage 2 hand-placed `Enemy` instance from `Assets/Scenes/Arena.unity`
  (SESSION_PLAN: spawning replaces hand-placement, not adds to it).
- Adds `EnemySpawnerComponent` to the scene, wired to the player, the enemy prefab,
  and the `WaveDefinition` asset.
- Verifies its own wiring before saving (the `VerifyWiring`-style self-check Stage 5
  added, extended to this stage's new references) rather than relying on a later
  manual diff read to catch a silent null reference — this is now a standing pattern,
  not a one-off.

## Testing
- **`SpawnScheduler` tests**: interval decreases correctly with elapsed time and
  clamps at the floor; enemies-per-spawn increases correctly and never drops below 1;
  `TryConsumeSpawnTick` gates correctly against the *current* (ramped) interval, not
  the initial one.
- **`Health` tests (extended)**: `ResetHealth` restores `CurrentHealth` to `MaxHealth`
  and clears `IsDead`, and a damage call after reset behaves like a fresh `Health`
  (i.e. `TakeDamage` isn't still blocked by the old `IsDead` latch).
- `PooledObject`, `EnemyRegistry`, `EnemySpawnerComponent`, and the
  `AutoAttackComponent`/`ProjectileComponent` pooling changes are
  MonoBehaviour/static-registry wiring — no dedicated tests, consistent with this
  project's established convention (verified via compile + full regression +
  play-test note instead).

## Out of scope
No boss/elite enemy variants (not requested). No difficulty presets/selection UI. No
pooling for XP orbs (SESSION_PLAN names only "enemies and projectiles"). No
`EnemyContactDamage`'s per-instance `Cooldown` reset on reuse — its `lastTriggerTime`
carries over from a previous life, but since real elapsed time between a death and a
later reuse is virtually always far longer than the contact-damage interval, this
self-resolves in practice and isn't worth the extra complexity of a reset hook.
