# Stage 6 — Spawner + Waves + Pooling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enemies spawn continuously in escalating waves (replacing Stage 2's single
hand-placed enemy); enemies and projectiles are object-pooled.

**Architecture:** Uses Unity's built-in `UnityEngine.Pool.ObjectPool<T>` (no bespoke
pool class). `PooledObject` (generic, `Systems`) lets a pooled instance return itself
without knowing which pool it came from. `SpawnScheduler` (plain, `Enemies`) is the
testable difficulty-ramp math; `WaveDefinition` (ScriptableObject, `Data`) holds its
tunable numbers. `EnemyRegistry` (static, `Enemies`) replaces a per-frame
`FindObjectsByType` scan with self-registration via `OnEnable`/`OnDisable`.

**Tech Stack:** Unity 6000.6.2f1, `UnityEngine.Pool`. No new packages.

## Global Constraints

- Namespace root `ArenaSurvivor.*` matching folder; PascalCase/camelCase, no `m_`/`_`.
- Plain classes hold logic, MonoBehaviours are thin forwarders.
- Wave definitions are ScriptableObject data (CLAUDE.md rule 2), not hardcoded.
- No hand-edited scene/prefab/asset YAML — `Enemy.prefab`/`Projectile.prefab` (edit),
  `WaveDefinition.asset` (create), `Arena.unity` (edit) all via Editor script.
- **Idempotency-check safety (Stage 4's lesson):** never use `GetComponent<T>()` as
  an existence guard when `T` has `[RequireComponent]`. `EnemySpawnerComponent` (this
  stage) has none, so a `GetComponent` guard against it is safe.
- **Self-verify wiring before saving (Stage 5's lesson, now a standing pattern):**
  after wiring a new component's serialized references in an Editor script, walk them
  with `SerializedObject`/`SerializedProperty` and refuse to save if anything is null.
- **Pooled-object return, not `Destroy`:** anywhere existing code called
  `Destroy(gameObject)` on something that might now be pooled, check for a
  `PooledObject` component first and return to its pool instead; fall back to
  `Destroy` if none is present (keeps non-pooled contexts, like tests, working).
- Unity CLI: wrap invocations in a `.sh` script (`run_unity.sh`), run with `bash`,
  always `-quit` on any call that isn't `-runTests` (a past stage hung 90+ minutes
  from a missing `-quit`). `-nographics` + `-logFile` always; `cygpath -w` for Windows
  paths. Exit 0 = pass, 2 = fail for test runs; confirm logs actually reached
  compilation/test execution, not an early licensing-handshake abort.
- Existing state before this plan: `Assets/Scripts/Enemies/{EnemyChase,
  EnemyChaseComponent,EnemyContactDamage,EnemyDeathReaction,EnemyXpDrop}.cs` +
  `ArenaSurvivor.Enemies.asmdef` (references: `["ArenaSurvivor.Systems"]`);
  `Assets/Scripts/Weapons/{AutoAttack,AutoAttackComponent,Projectile,
  ProjectileComponent,NearestTargetSelector}.cs`; `Assets/Scripts/Data/*` +
  `ArenaSurvivor.Data.asmdef` (references: `["ArenaSurvivor.Player",
  "ArenaSurvivor.Weapons", "ArenaSurvivor.Systems"]`); `ArenaSurvivor.Tests.EditMode.asmdef`
  references `["ArenaSurvivor.Player", "ArenaSurvivor.Systems", "ArenaSurvivor.Enemies",
  "ArenaSurvivor.Weapons", "ArenaSurvivor.UI", "ArenaSurvivor.Data",
  "UnityEngine.TestRunner", "UnityEditor.TestRunner"]`. `Assets/Prefabs/Enemy.prefab`
  and `Assets/Prefabs/Projectile.prefab` exist. Current EditMode test count: 48.
- **Read the actual current content of any file this plan modifies before editing it**
  (`EnemyChaseComponent.cs`, `EnemyDeathReaction.cs`, `AutoAttackComponent.cs`,
  `ProjectileComponent.cs`, `Health.cs`, `HealthComponent.cs`) — this plan's inline
  code assumes their Stage 2/3/5 shape; adapt to the real file if it differs, but
  still produce the exact interfaces/behavior each step specifies.

---

## Task 1: `PooledObject` + `Health.ResetHealth`/`HealthComponent.ResetHealth`

**Files:**
- Create: `Assets/Scripts/Systems/PooledObject.cs`
- Modify: `Assets/Scripts/Systems/Health.cs`
- Modify: `Assets/Scripts/Systems/HealthComponent.cs`
- Modify: `Assets/Tests/EditMode/HealthTests.cs` (append, don't remove existing)

**Interfaces:**
- Produces: `ArenaSurvivor.Systems.PooledObject` — `void Initialize(Action<GameObject>
  releaseAction)`, `void ReturnToPool()`. Consumed by Task 3/4 (added to
  `Enemy.prefab`/`Projectile.prefab`, used by `EnemyDeathReaction`/
  `ProjectileComponent`/the spawner/`AutoAttackComponent`'s pools).
- Produces: `Health.ResetHealth()` (sets `CurrentHealth = MaxHealth`, `IsDead =
  false`), `HealthComponent.ResetHealth()` (forwards). Consumed by Task 3's
  `EnemySpawnerComponent`.

- [ ] **Step 1: Implement `PooledObject`**

Create `Assets/Scripts/Systems/PooledObject.cs`:
```csharp
using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class PooledObject : MonoBehaviour
    {
        private Action<GameObject> releaseAction;

        public void Initialize(Action<GameObject> releaseAction)
        {
            this.releaseAction = releaseAction;
        }

        public void ReturnToPool()
        {
            releaseAction?.Invoke(gameObject);
        }
    }
}
```

- [ ] **Step 2: Read `Health.cs` and `HealthComponent.cs` first**

```bash
cat Assets/Scripts/Systems/Health.cs
cat Assets/Scripts/Systems/HealthComponent.cs
```

- [ ] **Step 3: Add `ResetHealth` to `Health`**

Add this method to the `Health` class:
```csharp
public void ResetHealth()
{
    CurrentHealth = MaxHealth;
    IsDead = false;
}
```
(`CurrentHealth`/`IsDead` are the existing `{ get; private set; }` properties — from
inside the class, setting them directly is fine.)

- [ ] **Step 4: Add `ResetHealth` to `HealthComponent`**

Add this method to the `HealthComponent` class:
```csharp
public void ResetHealth()
{
    health.ResetHealth();
}
```
(`health` is the existing private `Health` field — use its real name if different.)

- [ ] **Step 5: Append new tests to `HealthTests.cs`**

Add these two `[Test]` methods inside the existing `HealthTests` class:
```csharp
[Test]
public void ResetHealth_RestoresCurrentHealthAndClearsIsDead()
{
    var health = new Health(50);
    health.TakeDamage(50);

    health.ResetHealth();

    Assert.AreEqual(50, health.CurrentHealth);
    Assert.IsFalse(health.IsDead);
}

[Test]
public void ResetHealth_AllowsTakeDamageToWorkAgain()
{
    var health = new Health(50);
    health.TakeDamage(50);
    health.ResetHealth();

    health.TakeDamage(20);

    Assert.AreEqual(30, health.CurrentHealth);
    Assert.IsFalse(health.IsDead);
}
```

- [ ] **Step 6: Verify**

Create `run_unity.sh` in the worktree root if missing:
```bash
#!/bin/bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
WT="$(pwd)"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$WT")" "$@"
echo "EXIT CODE: $?"
```
Run:
```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_green.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task1_green.xml | head -1
```
Expected: exit 0, `total="50" passed="50" failed="0"` (48 existing + 2 new).

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Systems/PooledObject.cs Assets/Scripts/Systems/Health.cs Assets/Scripts/Systems/HealthComponent.cs Assets/Tests/EditMode/HealthTests.cs
git status --short
git commit -m "$(cat <<'EOF'
Add PooledObject and Health.ResetHealth/HealthComponent.ResetHealth

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: `SpawnScheduler` + `WaveDefinition` + `EnemyRegistry`

**Files:**
- Create: `Assets/Scripts/Enemies/SpawnScheduler.cs`
- Create: `Assets/Tests/EditMode/SpawnSchedulerTests.cs`
- Create: `Assets/Scripts/Data/WaveDefinition.cs`
- Create: `Assets/Scripts/Enemies/EnemyRegistry.cs`
- Modify: `Assets/Scripts/Enemies/EnemyChaseComponent.cs`

**Interfaces:**
- Produces: `ArenaSurvivor.Enemies.SpawnScheduler` — constructor `(float
  initialSpawnInterval, float minSpawnInterval, float spawnIntervalDecreasePerMinute,
  int initialEnemiesPerSpawn, float enemiesPerSpawnIncreasePerMinute)`,
  `float ComputeSpawnInterval(float elapsedTime)`, `int
  ComputeEnemiesPerSpawn(float elapsedTime)`, `bool TryConsumeSpawnTick(float
  currentTime, float elapsedTime)`. Consumed by Task 3's `EnemySpawnerComponent`.
- Produces: `ArenaSurvivor.Data.WaveDefinition` (ScriptableObject) — read-only
  properties matching the constructor params above, plus `int MaxActiveEnemies`.
  Consumed by Task 3/4.
- Produces: `ArenaSurvivor.Enemies.EnemyRegistry` (static) —
  `IReadOnlyList<EnemyChaseComponent> ActiveEnemies`, `void
  Register(EnemyChaseComponent)`, `void Unregister(EnemyChaseComponent)`. Wired via
  `EnemyChaseComponent.OnEnable`/`OnDisable` in this task; consumed by Task 3's
  `AutoAttackComponent` rewrite.

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/SpawnSchedulerTests.cs`:
```csharp
using NUnit.Framework;
using ArenaSurvivor.Enemies;

namespace ArenaSurvivor.Tests.EditMode
{
    public class SpawnSchedulerTests
    {
        [Test]
        public void ComputeSpawnInterval_DecreasesWithElapsedTime()
        {
            var scheduler = new SpawnScheduler(2f, 0.3f, 0.2f, 1, 0.5f);

            float atStart = scheduler.ComputeSpawnInterval(0f);
            float afterOneMinute = scheduler.ComputeSpawnInterval(60f);

            Assert.AreEqual(2f, atStart, 0.0001f);
            Assert.AreEqual(1.8f, afterOneMinute, 0.0001f);
        }

        [Test]
        public void ComputeSpawnInterval_ClampsAtFloor()
        {
            var scheduler = new SpawnScheduler(2f, 0.3f, 0.2f, 1, 0.5f);

            float afterTenMinutes = scheduler.ComputeSpawnInterval(600f);

            Assert.AreEqual(0.3f, afterTenMinutes, 0.0001f);
        }

        [Test]
        public void ComputeEnemiesPerSpawn_IncreasesWithElapsedTime_NeverBelowOne()
        {
            var scheduler = new SpawnScheduler(2f, 0.3f, 0.2f, 1, 0.5f);

            int atStart = scheduler.ComputeEnemiesPerSpawn(0f);
            int afterTwoMinutes = scheduler.ComputeEnemiesPerSpawn(120f);

            Assert.AreEqual(1, atStart);
            Assert.AreEqual(2, afterTwoMinutes);
        }

        [Test]
        public void TryConsumeSpawnTick_GatesOnCurrentRampedInterval()
        {
            var scheduler = new SpawnScheduler(2f, 0.3f, 0.2f, 1, 0.5f);

            bool first = scheduler.TryConsumeSpawnTick(0f, 0f);
            bool tooSoon = scheduler.TryConsumeSpawnTick(1f, 1f);
            bool readyAtInitialInterval = scheduler.TryConsumeSpawnTick(2f, 2f);

            Assert.IsTrue(first);
            Assert.IsFalse(tooSoon);
            Assert.IsTrue(readyAtInitialInterval);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_red.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_red.log")"
```
Expected: nonzero exit; `SpawnScheduler` does not exist.

- [ ] **Step 3: Implement `SpawnScheduler`**

Create `Assets/Scripts/Enemies/SpawnScheduler.cs`:
```csharp
using UnityEngine;

namespace ArenaSurvivor.Enemies
{
    public class SpawnScheduler
    {
        private readonly float initialSpawnInterval;
        private readonly float minSpawnInterval;
        private readonly float spawnIntervalDecreasePerMinute;
        private readonly int initialEnemiesPerSpawn;
        private readonly float enemiesPerSpawnIncreasePerMinute;

        private float lastSpawnTime = float.NegativeInfinity;

        public SpawnScheduler(float initialSpawnInterval, float minSpawnInterval, float spawnIntervalDecreasePerMinute, int initialEnemiesPerSpawn, float enemiesPerSpawnIncreasePerMinute)
        {
            this.initialSpawnInterval = initialSpawnInterval;
            this.minSpawnInterval = minSpawnInterval;
            this.spawnIntervalDecreasePerMinute = spawnIntervalDecreasePerMinute;
            this.initialEnemiesPerSpawn = initialEnemiesPerSpawn;
            this.enemiesPerSpawnIncreasePerMinute = enemiesPerSpawnIncreasePerMinute;
        }

        public float ComputeSpawnInterval(float elapsedTime)
        {
            float minutesElapsed = elapsedTime / 60f;
            float interval = initialSpawnInterval - spawnIntervalDecreasePerMinute * minutesElapsed;
            return Mathf.Max(minSpawnInterval, interval);
        }

        public int ComputeEnemiesPerSpawn(float elapsedTime)
        {
            float minutesElapsed = elapsedTime / 60f;
            int count = initialEnemiesPerSpawn + Mathf.FloorToInt(enemiesPerSpawnIncreasePerMinute * minutesElapsed);
            return Mathf.Max(1, count);
        }

        public bool TryConsumeSpawnTick(float currentTime, float elapsedTime)
        {
            float interval = ComputeSpawnInterval(elapsedTime);
            if (currentTime - lastSpawnTime < interval)
            {
                return false;
            }

            lastSpawnTime = currentTime;
            return true;
        }
    }
}
```

- [ ] **Step 4: Run to verify it passes**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_green.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task2_green.xml | head -1
```
Expected: exit 0, `total="54" passed="54" failed="0"` (50 + 4 new
`SpawnSchedulerTests`).

- [ ] **Step 5: Implement `WaveDefinition` (no dedicated test — plain data holder)**

Create `Assets/Scripts/Data/WaveDefinition.cs`:
```csharp
using UnityEngine;

namespace ArenaSurvivor.Data
{
    [CreateAssetMenu(fileName = "WaveDefinition", menuName = "Arena Survivor/Wave Definition")]
    public class WaveDefinition : ScriptableObject
    {
        [SerializeField] private float initialSpawnInterval = 2f;
        [SerializeField] private float minSpawnInterval = 0.3f;
        [SerializeField] private float spawnIntervalDecreasePerMinute = 0.2f;
        [SerializeField] private int initialEnemiesPerSpawn = 1;
        [SerializeField] private float enemiesPerSpawnIncreasePerMinute = 0.5f;
        [SerializeField] private int maxActiveEnemies = 100;

        public float InitialSpawnInterval => initialSpawnInterval;
        public float MinSpawnInterval => minSpawnInterval;
        public float SpawnIntervalDecreasePerMinute => spawnIntervalDecreasePerMinute;
        public int InitialEnemiesPerSpawn => initialEnemiesPerSpawn;
        public float EnemiesPerSpawnIncreasePerMinute => enemiesPerSpawnIncreasePerMinute;
        public int MaxActiveEnemies => maxActiveEnemies;
    }
}
```

- [ ] **Step 6: Implement `EnemyRegistry`**

Create `Assets/Scripts/Enemies/EnemyRegistry.cs`:
```csharp
using System.Collections.Generic;

namespace ArenaSurvivor.Enemies
{
    public static class EnemyRegistry
    {
        private static readonly List<EnemyChaseComponent> activeEnemies = new List<EnemyChaseComponent>();

        public static IReadOnlyList<EnemyChaseComponent> ActiveEnemies => activeEnemies;

        public static void Register(EnemyChaseComponent enemy)
        {
            activeEnemies.Add(enemy);
        }

        public static void Unregister(EnemyChaseComponent enemy)
        {
            activeEnemies.Remove(enemy);
        }
    }
}
```

- [ ] **Step 7: Read `EnemyChaseComponent.cs` first, then wire it to the registry**

```bash
cat Assets/Scripts/Enemies/EnemyChaseComponent.cs
```

Add these two methods to the `EnemyChaseComponent` class:
```csharp
private void OnEnable()
{
    EnemyRegistry.Register(this);
}

private void OnDisable()
{
    EnemyRegistry.Unregister(this);
}
```

- [ ] **Step 8: Verify compile + full regression**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task2_compile.log")"
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_regression.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_regression.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task2_regression.xml | head -1
```
Expected: both exit 0, `total="54" passed="54" failed="0"`.

- [ ] **Step 9: Commit**

```bash
git add Assets/Scripts/Enemies/SpawnScheduler.cs Assets/Tests/EditMode/SpawnSchedulerTests.cs Assets/Scripts/Data/WaveDefinition.cs Assets/Scripts/Enemies/EnemyRegistry.cs Assets/Scripts/Enemies/EnemyChaseComponent.cs
git status --short
git commit -m "$(cat <<'EOF'
Add SpawnScheduler, WaveDefinition, and EnemyRegistry

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: `EnemySpawnerComponent` + pooling conversion for enemies and projectiles

**Files:**
- Create: `Assets/Scripts/Enemies/EnemySpawnerComponent.cs`
- Modify: `Assets/Scripts/Enemies/EnemyDeathReaction.cs`
- Modify: `Assets/Scripts/Weapons/AutoAttackComponent.cs`
- Modify: `Assets/Scripts/Weapons/ProjectileComponent.cs`
- Modify: `Assets/Scripts/Enemies/ArenaSurvivor.Enemies.asmdef` (add
  `"ArenaSurvivor.Data"`)

**Interfaces:**
- Consumes: `ArenaSurvivor.Systems.PooledObject`, `SpawnScheduler`, `WaveDefinition`,
  `EnemyRegistry`, `Health.ResetHealth`/`HealthComponent.ResetHealth` (Tasks 1-2).
- Produces: `ArenaSurvivor.Enemies.EnemySpawnerComponent` — `[SerializeField]
  WaveDefinition wave`, `[SerializeField] GameObject enemyPrefab`, `[SerializeField]
  Transform target`, `[SerializeField] float spawnRadius = 9f`. Consumed by Task 4's
  scene-wiring script (which sets the three serialized references).

- [ ] **Step 1: Add `"ArenaSurvivor.Data"` to the Enemies asmdef**

Modify `Assets/Scripts/Enemies/ArenaSurvivor.Enemies.asmdef`: add
`"ArenaSurvivor.Data"` to `references` (needed for `EnemySpawnerComponent` to use
`WaveDefinition`). Keep the existing `"ArenaSurvivor.Systems"` entry.

- [ ] **Step 2: Implement `EnemySpawnerComponent`**

Create `Assets/Scripts/Enemies/EnemySpawnerComponent.cs`:
```csharp
using UnityEngine;
using UnityEngine.Pool;
using ArenaSurvivor.Data;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    public class EnemySpawnerComponent : MonoBehaviour
    {
        [SerializeField] private WaveDefinition wave;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform target;
        [SerializeField] private float spawnRadius = 9f;

        private SpawnScheduler scheduler;
        private ObjectPool<GameObject> pool;
        private float elapsedTime;

        private void Awake()
        {
            scheduler = new SpawnScheduler(wave.InitialSpawnInterval, wave.MinSpawnInterval, wave.SpawnIntervalDecreasePerMinute, wave.InitialEnemiesPerSpawn, wave.EnemiesPerSpawnIncreasePerMinute);
            pool = new ObjectPool<GameObject>(CreateEnemy, OnGetEnemy, OnReleaseEnemy, OnDestroyEnemy);
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;

            if (!scheduler.TryConsumeSpawnTick(Time.time, elapsedTime))
            {
                return;
            }

            int count = scheduler.ComputeEnemiesPerSpawn(elapsedTime);
            for (int i = 0; i < count; i++)
            {
                if (pool.CountActive >= wave.MaxActiveEnemies)
                {
                    break;
                }

                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            GameObject enemy = pool.Get();

            float angle = Random.value * Mathf.PI * 2f;
            Vector2 spawnPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            enemy.transform.position = spawnPosition;

            enemy.GetComponent<EnemyChaseComponent>().SetTarget(target);
            enemy.GetComponent<HealthComponent>().ResetHealth();
        }

        private GameObject CreateEnemy()
        {
            GameObject enemy = Instantiate(enemyPrefab);
            enemy.GetComponent<PooledObject>().Initialize(go => pool.Release(go));
            return enemy;
        }

        private void OnGetEnemy(GameObject enemy)
        {
            enemy.SetActive(true);
        }

        private void OnReleaseEnemy(GameObject enemy)
        {
            enemy.SetActive(false);
        }

        private void OnDestroyEnemy(GameObject enemy)
        {
            Destroy(enemy);
        }
    }
}
```

- [ ] **Step 3: Read and modify `EnemyDeathReaction.cs`**

```bash
cat Assets/Scripts/Enemies/EnemyDeathReaction.cs
```
Replace its `HandleDeath` method body from `Destroy(gameObject);` to:
```csharp
private void HandleDeath()
{
    PooledObject pooled = GetComponent<PooledObject>();
    if (pooled != null)
    {
        pooled.ReturnToPool();
    }
    else
    {
        Destroy(gameObject);
    }
}
```

- [ ] **Step 4: Read and modify `ProjectileComponent.cs`**

```bash
cat Assets/Scripts/Weapons/ProjectileComponent.cs
```
Replace the `Launch` method:
```csharp
public void Launch(Vector2 direction, int damage)
{
    CancelInvoke();
    this.damage = damage;
    velocity = direction.normalized * speed;
    Invoke(nameof(ReturnToPoolOrDestroy), lifetime);
}
```
Replace the `OnTriggerEnter2D` method's final two lines (`healthComponent.ApplyDamage(damage);` /
`Destroy(gameObject);`) with:
```csharp
healthComponent.ApplyDamage(damage);
ReturnToPoolOrDestroy();
```
Add this new private method to the class:
```csharp
private void ReturnToPoolOrDestroy()
{
    CancelInvoke();
    PooledObject pooled = GetComponent<PooledObject>();
    if (pooled != null)
    {
        pooled.ReturnToPool();
    }
    else
    {
        Destroy(gameObject);
    }
}
```
Add `using ArenaSurvivor.Systems;` at the top if not already present (it should
already be there for `HealthComponent`).

- [ ] **Step 5: Read and rewrite `AutoAttackComponent.cs`**

```bash
cat Assets/Scripts/Weapons/AutoAttackComponent.cs
```
Replace the full contents with:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Weapons
{
    public class AutoAttackComponent : MonoBehaviour
    {
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private float range = 8f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int baseDamage = 10;

        private Cooldown cooldown;
        private AutoAttack autoAttack;
        private int damageBonus;
        private ObjectPool<GameObject> projectilePool;

        private void Awake()
        {
            cooldown = new Cooldown(attackInterval);
            autoAttack = new AutoAttack();
            projectilePool = new ObjectPool<GameObject>(CreateProjectile, OnGetProjectile, OnReleaseProjectile, OnDestroyProjectile);
        }

        private void Update()
        {
            if (!cooldown.IsReady(Time.time))
            {
                return;
            }

            IReadOnlyList<EnemyChaseComponent> enemies = EnemyRegistry.ActiveEnemies;
            var positions = new List<Vector2>(enemies.Count);
            foreach (EnemyChaseComponent enemy in enemies)
            {
                positions.Add(enemy.transform.position);
            }

            Vector2 origin = transform.position;
            if (!autoAttack.TryGetShotDirection(origin, positions, range, out Vector2 direction))
            {
                return;
            }

            cooldown.TryConsume(Time.time);

            if (projectilePrefab == null)
            {
                Debug.LogError("AutoAttackComponent: projectilePrefab is not assigned on " + name);
                return;
            }

            GameObject projectileInstance = projectilePool.Get();
            projectileInstance.transform.position = origin;
            projectileInstance.GetComponent<ProjectileComponent>().Launch(direction, baseDamage + damageBonus);
        }

        public void IncreaseDamage(int amount)
        {
            damageBonus += amount;
        }

        public void ReduceAttackInterval(float amount)
        {
            cooldown.SetInterval(cooldown.Interval - amount);
        }

        private GameObject CreateProjectile()
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.GetComponent<PooledObject>().Initialize(go => projectilePool.Release(go));
            return projectile;
        }

        private void OnGetProjectile(GameObject projectile)
        {
            projectile.SetActive(true);
        }

        private void OnReleaseProjectile(GameObject projectile)
        {
            projectile.SetActive(false);
        }

        private void OnDestroyProjectile(GameObject projectile)
        {
            Destroy(projectile);
        }
    }
}
```
(If the real file's field names for `IncreaseDamage`/`ReduceAttackInterval` differ
from this plan's assumption, keep the real names — those are Stage 5's public API and
other code depends on them unchanged.)

- [ ] **Step 6: Verify compile + full regression**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task3_compile.log")"
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task3_regression.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task3_regression.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task3_regression.xml | head -1
```
Expected: both exit 0, `total="54" passed="54" failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Enemies/EnemySpawnerComponent.cs Assets/Scripts/Enemies/EnemyDeathReaction.cs Assets/Scripts/Weapons/AutoAttackComponent.cs Assets/Scripts/Weapons/ProjectileComponent.cs Assets/Scripts/Enemies/ArenaSurvivor.Enemies.asmdef
git status --short
git commit -m "$(cat <<'EOF'
Add EnemySpawnerComponent; convert enemies and projectiles to pooling

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Wire spawner + pooling into scene and prefabs via an Editor script

**Files:**
- Create: `Assets/Editor/Stage6SceneBuilder.cs`
- Modify (via script): `Assets/Prefabs/Enemy.prefab`, `Assets/Prefabs/Projectile.prefab`
- Generate (via script): `Assets/ScriptableObjects/Enemies/WaveDefinition.asset`,
  updated `Assets/Scenes/Arena.unity` (hand-placed `Enemy` instance removed,
  `EnemySpawner` GameObject added)

**Interfaces:**
- Consumes: `ArenaSurvivor.Systems.PooledObject`,
  `ArenaSurvivor.Enemies.EnemySpawnerComponent`, `ArenaSurvivor.Data.WaveDefinition`
  (Tasks 1-3). Relies on `Assets/Prefabs/Enemy.prefab`, the `Player` GameObject, and
  the Stage 2 hand-placed `Enemy` instance (named exactly `"Enemy"`) all existing in
  `Assets/Scenes/Arena.unity`.

- [ ] **Step 1: Write the Editor script**

Create `Assets/Editor/Stage6SceneBuilder.cs`:
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaSurvivor.Data;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage6SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";
        private const string WaveDefinitionPath = "Assets/ScriptableObjects/Enemies/WaveDefinition.asset";

        [MenuItem("Arena Survivor/Build Stage 6 Spawner And Pooling")]
        public static void AddSpawnerAndPooling()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            AddPooledObjectToPrefab(EnemyPrefabPath);
            AddPooledObjectToPrefab(ProjectilePrefabPath);
            WaveDefinition wave = CreateWaveDefinitionAsset();

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            if (Object.FindFirstObjectByType<EnemySpawnerComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: EnemySpawnerComponent already present — Stage 6 wiring already applied, skipping.");
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Arena Survivor: Stage6SceneBuilder could not find a GameObject named 'Player' in " + ScenePath);
                return;
            }

            RemoveHandPlacedEnemy();

            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            EnemySpawnerComponent spawner = BuildSpawner(wave, enemyPrefab, player.transform);

            if (!VerifyWiring(spawner))
            {
                Debug.LogError("Arena Survivor: Stage6SceneBuilder wiring verification failed — scene NOT saved.");
                return;
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 6 spawner and pooling wired into " + ScenePath);
        }

        private static void AddPooledObjectToPrefab(string prefabPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            if (prefabRoot.GetComponent<PooledObject>() == null)
            {
                prefabRoot.AddComponent<PooledObject>();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static WaveDefinition CreateWaveDefinitionAsset()
        {
            WaveDefinition existing = AssetDatabase.LoadAssetAtPath<WaveDefinition>(WaveDefinitionPath);
            if (existing != null)
            {
                return existing;
            }

            System.IO.Directory.CreateDirectory("Assets/ScriptableObjects/Enemies");

            WaveDefinition wave = ScriptableObject.CreateInstance<WaveDefinition>();
            AssetDatabase.CreateAsset(wave, WaveDefinitionPath);
            return wave;
        }

        private static void RemoveHandPlacedEnemy()
        {
            GameObject existingEnemy = GameObject.Find("Enemy");
            if (existingEnemy != null)
            {
                Object.DestroyImmediate(existingEnemy);
            }
        }

        private static EnemySpawnerComponent BuildSpawner(WaveDefinition wave, GameObject enemyPrefab, Transform target)
        {
            var spawnerObject = new GameObject("EnemySpawner");
            EnemySpawnerComponent spawner = spawnerObject.AddComponent<EnemySpawnerComponent>();

            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("wave").objectReferenceValue = wave;
            serialized.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            serialized.FindProperty("target").objectReferenceValue = target;
            serialized.ApplyModifiedProperties();

            return spawner;
        }

        private static bool VerifyWiring(EnemySpawnerComponent spawner)
        {
            var serialized = new SerializedObject(spawner);
            bool ok = true;

            ok &= VerifyField(serialized, "wave");
            ok &= VerifyField(serialized, "enemyPrefab");
            ok &= VerifyField(serialized, "target");

            return ok;
        }

        private static bool VerifyField(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == null)
            {
                Debug.LogError("Arena Survivor: Stage6SceneBuilder verification failed — '" + propertyName + "' is null.");
                return false;
            }

            return true;
        }
    }
}
```

- [ ] **Step 2: Run the script via batchmode**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage6SceneBuilder.AddSpawnerAndPooling -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage6.log")"
grep "Stage 6 spawner and pooling wired" Logs/build_stage6.log
```
Expected: exit 0, the success line genuinely present (not a skip or a verification
failure — if you see "wiring verification failed" instead, investigate why a field
came back null before assuming anything succeeded, same discipline as prior stages).

- [ ] **Step 3: Verify the changes actually landed**

```bash
grep -c "PooledObject" Assets/Prefabs/Enemy.prefab
grep -c "PooledObject" Assets/Prefabs/Projectile.prefab
ls Assets/ScriptableObjects/Enemies/WaveDefinition.asset
grep -c "EnemySpawnerComponent" Assets/Scenes/Arena.unity
grep -c 'm_Name: Enemy$' Assets/Scenes/Arena.unity
```
Expected: first four greater than 0; the last one (exact match on the hand-placed
`Enemy` GameObject's name) returns 0 — it should be gone.

- [ ] **Step 4: Confirm idempotency (safe to click twice)**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage6SceneBuilder.AddSpawnerAndPooling -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage6_rerun.log")"
grep "already present" Logs/build_stage6_rerun.log
grep -c "EnemySpawnerComponent" Assets/Scenes/Arena.unity
```
Expected: the warning line present; the `EnemySpawnerComponent` count unchanged from
Step 3 (no duplicate).

- [ ] **Step 5: Re-open headless to confirm no compile/scene errors**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task4_verify.log")"
```
Expected: exit 0, full compile/shutdown cycle.

- [ ] **Step 6: Run the full EditMode suite one final time**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task4_tests.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task4_tests.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task4_tests.xml | head -1
```
Expected: exit 0, `total="54" passed="54" failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Editor/Stage6SceneBuilder.cs Assets/Prefabs/Enemy.prefab Assets/Prefabs/Projectile.prefab Assets/ScriptableObjects/Enemies/WaveDefinition.asset Assets/Scenes/Arena.unity
git status --short
git commit -m "$(cat <<'EOF'
Wire enemy spawner, wave definition, and pooling into scene and prefabs

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 8: Note for later manual play-test**

Can't verify headlessly: do enemies actually spawn continuously and escalate in
frequency/count over time, does the arena stay playable (framerate, no visual
glitches) with 100+ enemies on screen at once, do pooled enemies/projectiles actually
reappear correctly on reuse (health full, chase/contact-damage/attack all working
normally — not stuck in a dead or otherwise stale state from a previous life), does
the old hand-placed enemy's absence look right (no empty gap, just the spawner taking
over). Leave a note for the user.

---

## Self-Review Notes

- **Spec coverage:** `PooledObject`/`Health.ResetHealth` (Task 1), `SpawnScheduler`/
  `WaveDefinition`/`EnemyRegistry` (Task 2), `EnemySpawnerComponent` + pooling
  conversion (Task 3), scene/prefab wiring (Task 4) — all spec bullets covered.
- **Placeholder scan:** none — every step has literal runnable code/commands, aside
  from the explicit "read the real file first, adapt if it differs" instructions,
  which are deliberate verify-before-edit steps for files this plan didn't create.
- **Type consistency:** `SpawnScheduler`'s constructor signature and method names
  match between Task 2 (definition + tests) and Task 3 (`EnemySpawnerComponent`'s
  `Awake`). `PooledObject.Initialize`/`ReturnToPool` used identically across
  `EnemySpawnerComponent`, `AutoAttackComponent`, `EnemyDeathReaction`,
  `ProjectileComponent` (Tasks 1/3). `WaveDefinition`'s property names match both
  `EnemySpawnerComponent`'s `Awake` (Task 3) and `Stage6SceneBuilder`'s
  `SerializedObject.FindProperty` calls (Task 4 — `wave`/`enemyPrefab`/`target` match
  `EnemySpawnerComponent`'s actual private field names exactly).
- **Idempotency/wiring-safety:** `Stage6SceneBuilder` uses a `GetComponent` check
  against `EnemySpawnerComponent` (no `[RequireComponent]`, safe) and a plain
  asset-existence check for `WaveDefinition` — matches the established safe patterns.
  `VerifyWiring` catches a null reference before saving, per Stage 5's lesson.
