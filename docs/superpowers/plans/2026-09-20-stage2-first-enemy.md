# Stage 2 — First Enemy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `Health` system shared by player and enemies, an enemy that chases the
player and deals contact damage, and wire one enemy instance into the existing
`Assets/Scenes/Arena.unity` scene.

**Architecture:** Same plain-class + thin-MonoBehaviour split as Stage 1. `Health`
(plain) + `HealthComponent` (generic MonoBehaviour) are shared; `PlayerDeathReaction`
is the player-specific reaction to death; `EnemyChase` (plain) + `EnemyChaseComponent`
+ `EnemyContactDamage` are the enemy side. The existing Arena scene is extended (not
rebuilt) via an Editor script that opens it, adds components/an enemy, and saves it.

**Tech Stack:** Unity 6000.6.2f1, Physics2D, Unity Test Framework (NUnit) EditMode
tests. No new packages.

## Global Constraints

- Namespace root `ArenaSurvivor.*` matching folder.
- PascalCase classes/methods, camelCase private fields, no `m_`/`_` prefixes.
- Plain classes hold logic; MonoBehaviours are thin forwarders (CLAUDE.md rule 1).
- Every plain class with non-trivial logic gets an EditMode test.
- No hand-edited scene/prefab YAML — `Assets/Scenes/Arena.unity` and
  `Assets/Prefabs/Enemy.prefab` are produced by running an Editor script via
  `-executeMethod`, never edited by hand.
- Unity CLI invocations: put the command in a small `.sh` wrapper file and run it with
  `bash wrapper.sh` rather than invoking `Unity.exe` inline — a worktree-isolated
  session's safety checker sometimes misflags an inline quoted absolute Windows path
  as an unverifiable command. Always pass `-nographics` and a `-logFile`; convert
  Windows paths with `cygpath -w`.
- Test runner exit codes: 0 = all passed, 2 = at least one failed. Always open the log
  and confirm it reached script compilation / test execution (not an early
  licensing-handshake abort that exits right after "Successfully changed project
  path" with nothing further — that run proved nothing; rerun it).
- Existing asmdefs before this plan: `Assets/Scripts/Player/ArenaSurvivor.Player.asmdef`
  (`references: ["Unity.InputSystem"]`), `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef`
  (`references: ["ArenaSurvivor.Player", "UnityEngine.TestRunner", "UnityEditor.TestRunner"]`,
  `precompiledReferences: ["nunit.framework.dll"]`, `autoReferenced: true`).

---

## Task 1: `Health` system (plain class + generic component) + tests

**Files:**
- Create: `Assets/Scripts/Systems/Health.cs`
- Create: `Assets/Scripts/Systems/HealthComponent.cs`
- Create: `Assets/Scripts/Systems/ArenaSurvivor.Systems.asmdef`
- Create: `Assets/Tests/EditMode/HealthTests.cs`
- Modify: `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef` (add
  `"ArenaSurvivor.Systems"` to `references`)

**Interfaces:**
- Produces: `ArenaSurvivor.Systems.Health` — constructor `Health(int maxHealth)`,
  `int MaxHealth { get; }`, `int CurrentHealth { get; }`, `bool IsDead { get; }`,
  `void TakeDamage(int amount)`, `event Action OnDeath`.
- Produces: `ArenaSurvivor.Systems.HealthComponent` (MonoBehaviour) —
  `public void ApplyDamage(int amount)`, `public event Action OnDeath`. Used by Task 2
  (`EnemyContactDamage` looks it up on the object it collides with) and Task 3's scene
  script (adds it to both Player and Enemy with different `maxHealth`).

- [ ] **Step 1: Create the Systems assembly definition**

Create `Assets/Scripts/Systems/ArenaSurvivor.Systems.asmdef`:

```json
{
    "name": "ArenaSurvivor.Systems",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing tests**

Create `Assets/Tests/EditMode/HealthTests.cs`:

```csharp
using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class HealthTests
    {
        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            var health = new Health(100);

            health.TakeDamage(30);

            Assert.AreEqual(70, health.CurrentHealth);
        }

        [Test]
        public void TakeDamage_ClampsAtZero_NeverNegative()
        {
            var health = new Health(10);

            health.TakeDamage(999);

            Assert.AreEqual(0, health.CurrentHealth);
        }

        [Test]
        public void OnDeath_FiresExactlyOnce_WhenHealthReachesZero()
        {
            var health = new Health(10);
            int deathCount = 0;
            health.OnDeath += () => deathCount++;

            health.TakeDamage(10);

            Assert.AreEqual(1, deathCount);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void OnDeath_DoesNotFireAgain_AfterDeath()
        {
            var health = new Health(10);
            int deathCount = 0;
            health.OnDeath += () => deathCount++;

            health.TakeDamage(10);
            health.TakeDamage(5);

            Assert.AreEqual(1, deathCount);
        }

        [Test]
        public void TakeDamage_IgnoresNonPositiveAmounts()
        {
            var health = new Health(100);

            health.TakeDamage(0);
            health.TakeDamage(-5);

            Assert.AreEqual(100, health.CurrentHealth);
        }
    }
}
```

Modify `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef`: add
`"ArenaSurvivor.Systems"` to the `references` array (keep the existing entries).

- [ ] **Step 3: Run the tests to verify they fail**

Write a wrapper `run_unity.sh` in the repo root of the worktree:
```bash
#!/bin/bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
WT="$(pwd)"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$WT")" "$@"
echo "EXIT CODE: $?"
```
Run:
```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_red.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_red.log")"
```
Expected: nonzero exit; `grep -i "error CS" Logs/task1_red.log` shows `Health` does
not exist.

- [ ] **Step 4: Implement `Health`**

Create `Assets/Scripts/Systems/Health.cs`:

```csharp
using System;

namespace ArenaSurvivor.Systems
{
    public class Health
    {
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event Action OnDeath;

        public Health(int maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            CurrentHealth = Math.Max(0, CurrentHealth - amount);

            if (CurrentHealth == 0)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }
    }
}
```

- [ ] **Step 5: Implement `HealthComponent`**

Create `Assets/Scripts/Systems/HealthComponent.cs`:

```csharp
using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 10;

        private Health health;

        public event Action OnDeath;

        private void Awake()
        {
            health = new Health(maxHealth);
            health.OnDeath += HandleDeath;
        }

        public void ApplyDamage(int amount)
        {
            health.TakeDamage(amount);
        }

        private void HandleDeath()
        {
            OnDeath?.Invoke();
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_green.log")"
grep -o 'result="[A-Za-z]*"' Logs/task1_green.xml | head -1
```
Expected: exit 0, `result="Passed"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Systems/Health.cs Assets/Scripts/Systems/HealthComponent.cs Assets/Scripts/Systems/ArenaSurvivor.Systems.asmdef Assets/Tests/EditMode/HealthTests.cs Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef
git status --short
git commit -m "$(cat <<'EOF'
Add Health plain class + generic HealthComponent with EditMode tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```
Check `git status --short` first for any `.meta` files Unity generated for the new
`.cs`/`.asmdef` files and include them.

---

## Task 2: `EnemyChase`, `EnemyContactDamage`, `PlayerDeathReaction` + tests

**Files:**
- Create: `Assets/Scripts/Enemies/EnemyChase.cs`
- Create: `Assets/Scripts/Enemies/EnemyChaseComponent.cs`
- Create: `Assets/Scripts/Enemies/EnemyContactDamage.cs`
- Create: `Assets/Scripts/Enemies/ArenaSurvivor.Enemies.asmdef`
- Create: `Assets/Scripts/Player/PlayerDeathReaction.cs`
- Create: `Assets/Tests/EditMode/EnemyChaseTests.cs`
- Modify: `Assets/Scripts/Player/ArenaSurvivor.Player.asmdef` (add
  `"ArenaSurvivor.Systems"` to `references`)
- Modify: `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef` (add
  `"ArenaSurvivor.Enemies"` to `references`)

**Interfaces:**
- Consumes: `ArenaSurvivor.Systems.HealthComponent.ApplyDamage(int)` and
  `.OnDeath` (Task 1).
- Produces: `ArenaSurvivor.Enemies.EnemyChase.ComputeNextPosition(Vector2, Vector2,
  float, float)`; `ArenaSurvivor.Enemies.EnemyChaseComponent.SetTarget(Transform
  newTarget)` — Task 3's scene script calls this to point the enemy at the player,
  exactly mirroring `CameraFollowComponent.SetTarget` from Stage 1.

- [ ] **Step 1: Create the Enemies assembly definition**

Create `Assets/Scripts/Enemies/ArenaSurvivor.Enemies.asmdef`:

```json
{
    "name": "ArenaSurvivor.Enemies",
    "references": ["ArenaSurvivor.Systems"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Modify `Assets/Scripts/Player/ArenaSurvivor.Player.asmdef`: add
`"ArenaSurvivor.Systems"` to `references` (alongside the existing
`"Unity.InputSystem"`).

Modify `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef`: add
`"ArenaSurvivor.Enemies"` to `references`.

- [ ] **Step 2: Write the failing test for `EnemyChase`**

Create `Assets/Tests/EditMode/EnemyChaseTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Enemies;

namespace ArenaSurvivor.Tests.EditMode
{
    public class EnemyChaseTests
    {
        [Test]
        public void MovesTowardTarget()
        {
            var chase = new EnemyChase();

            Vector2 result = chase.ComputeNextPosition(Vector2.zero, new Vector2(10f, 0f), 3f, 1f);

            Assert.AreEqual(new Vector2(3f, 0f), result);
        }

        [Test]
        public void NeverOvershootsTarget()
        {
            var chase = new EnemyChase();

            Vector2 result = chase.ComputeNextPosition(Vector2.zero, new Vector2(1f, 0f), 100f, 1f);

            Assert.AreEqual(new Vector2(1f, 0f), result);
        }

        [Test]
        public void ZeroSpeed_ProducesNoMovement()
        {
            var chase = new EnemyChase();

            Vector2 result = chase.ComputeNextPosition(new Vector2(5f, 5f), new Vector2(10f, 10f), 0f, 1f);

            Assert.AreEqual(new Vector2(5f, 5f), result);
        }

        [Test]
        public void ReachesTargetExactly_WhenCloseEnough()
        {
            var chase = new EnemyChase();

            Vector2 result = chase.ComputeNextPosition(new Vector2(9.5f, 0f), new Vector2(10f, 0f), 3f, 1f);

            Assert.AreEqual(new Vector2(10f, 0f), result);
        }
    }
}
```

- [ ] **Step 3: Run to verify it fails**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_red.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_red.log")"
```
Expected: nonzero exit; `EnemyChase` does not exist.

- [ ] **Step 4: Implement `EnemyChase`**

Create `Assets/Scripts/Enemies/EnemyChase.cs`:

```csharp
using UnityEngine;

namespace ArenaSurvivor.Enemies
{
    public class EnemyChase
    {
        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 targetPosition, float speed, float deltaTime)
        {
            return Vector2.MoveTowards(currentPosition, targetPosition, speed * deltaTime);
        }
    }
}
```

- [ ] **Step 5: Run to verify it passes**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_green.log")"
grep -o 'result="[A-Za-z]*"' Logs/task2_green.xml | head -1
```
Expected: exit 0, `result="Passed"`.

- [ ] **Step 6: Implement the MonoBehaviours (no dedicated test — MonoBehaviours need a running scene)**

Create `Assets/Scripts/Enemies/EnemyChaseComponent.cs`:

```csharp
using UnityEngine;

namespace ArenaSurvivor.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyChaseComponent : MonoBehaviour
    {
        [SerializeField] private float chaseSpeed = 3f;
        [SerializeField] private Transform target;

        private Rigidbody2D rb;
        private EnemyChase enemyChase;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            enemyChase = new EnemyChase();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector2 nextPosition = enemyChase.ComputeNextPosition(rb.position, target.position, chaseSpeed, Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);
        }
    }
}
```

Create `Assets/Scripts/Enemies/EnemyContactDamage.cs`:

```csharp
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    public class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] private int damageAmount = 10;
        [SerializeField] private float damageInterval = 1f;

        private float lastHitTime = float.NegativeInfinity;

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (Time.time - lastHitTime < damageInterval)
            {
                return;
            }

            HealthComponent healthComponent = collision.gameObject.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                return;
            }

            healthComponent.ApplyDamage(damageAmount);
            lastHitTime = Time.time;
        }
    }
}
```

Create `Assets/Scripts/Player/PlayerDeathReaction.cs`:

```csharp
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Player
{
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerDeathReaction : MonoBehaviour
    {
        private HealthComponent healthComponent;

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            healthComponent.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            healthComponent.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            Debug.Log("Player died");
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 7: Verify the project compiles and the full EditMode suite still passes**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task2_compile.log")"
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_regression.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_regression.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task2_regression.xml | head -1
```
Expected: both exit 0; regression shows `total="16" passed="16" failed="0"` (7 from
Stage 1 + 5 `HealthTests` + 4 `EnemyChaseTests`).

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Enemies Assets/Scripts/Player/PlayerDeathReaction.cs Assets/Scripts/Player/ArenaSurvivor.Player.asmdef Assets/Tests/EditMode/EnemyChaseTests.cs Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef
git status --short
git commit -m "$(cat <<'EOF'
Add enemy chase/contact-damage logic and player death reaction

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Wire the enemy into the Arena scene via an Editor script

**Files:**
- Create: `Assets/Editor/Stage2SceneBuilder.cs`
- Generate (by running the script): `Assets/Prefabs/Enemy.prefab`, and an updated
  `Assets/Scenes/Arena.unity`

**Interfaces:**
- Consumes: `ArenaSurvivor.Systems.HealthComponent` (Task 1),
  `ArenaSurvivor.Player.PlayerDeathReaction` (Task 2),
  `ArenaSurvivor.Enemies.EnemyChaseComponent.SetTarget(Transform)` (Task 2),
  `ArenaSurvivor.Enemies.EnemyContactDamage` (Task 2). Also relies on the `Player`
  GameObject existing in `Assets/Scenes/Arena.unity` under the exact name `"Player"`
  (created by Stage 1's `ArenaSceneBuilder`) and the sprite asset at
  `Assets/Sprites/PlaceholderSquare.png` (also from Stage 1).

- [ ] **Step 1: Write the Editor script**

Create `Assets/Editor/Stage2SceneBuilder.cs`:

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.Systems;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage2SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string SpritePath = "Assets/Sprites/PlaceholderSquare.png";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private static readonly Vector3 EnemySpawnPosition = new Vector3(5f, 5f, 0f);

        [MenuItem("Arena Survivor/Build Stage 2 Enemy")]
        public static void AddEnemyAndPlayerHealth()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            GameObject player = GameObject.Find("Player");
            HealthComponent playerHealth = player.AddComponent<HealthComponent>();
            SetMaxHealth(playerHealth, 100);
            player.AddComponent<PlayerDeathReaction>();

            GameObject enemyPrefab = CreateEnemyPrefab();

            var enemyInstance = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
            enemyInstance.transform.position = EnemySpawnPosition;
            EnemyChaseComponent chase = enemyInstance.GetComponent<EnemyChaseComponent>();
            chase.SetTarget(player.transform);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 2 enemy added to " + ScenePath);
        }

        private static GameObject CreateEnemyPrefab()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            var enemy = new GameObject("Enemy");
            var renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 0.2f, 0.2f);
            renderer.sortingOrder = 5;
            enemy.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            enemy.AddComponent<BoxCollider2D>();
            HealthComponent health = enemy.AddComponent<HealthComponent>();
            SetMaxHealth(health, 30);

            enemy.AddComponent<EnemyChaseComponent>();
            enemy.AddComponent<EnemyContactDamage>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            Object.DestroyImmediate(enemy);

            return prefab;
        }

        private static void SetMaxHealth(HealthComponent healthComponent, int maxHealth)
        {
            var serialized = new SerializedObject(healthComponent);
            serialized.FindProperty("maxHealth").intValue = maxHealth;
            serialized.ApplyModifiedProperties();
        }
    }
}
```

- [ ] **Step 2: Run the script via batchmode**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage2SceneBuilder.AddEnemyAndPlayerHealth -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage2.log")"
grep "Stage 2 enemy added" Logs/build_stage2.log
ls Assets/Prefabs/Enemy.prefab
```
Expected: exit 0, the log line present, the prefab file exists.

- [ ] **Step 3: Verify the scene actually changed as expected**

```bash
grep -c "PlayerDeathReaction" Assets/Scenes/Arena.unity
grep -c "m_Name: Enemy" Assets/Scenes/Arena.unity
```
Expected: both greater than 0 (PlayerDeathReaction attached to Player; an Enemy
instance present in the scene, distinct from the `Enemy` prefab asset itself which
lives in a separate file).

- [ ] **Step 4: Re-open headless to confirm no compile/scene errors**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task3_verify.log")"
```
Expected: exit 0, log shows a full compile/shutdown cycle (not an early abort).

- [ ] **Step 5: Run the full EditMode suite one final time**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task3_tests.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task3_tests.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task3_tests.xml | head -1
```
Expected: exit 0, `total="16" passed="16" failed="0"`.

- [ ] **Step 6: Commit**

```bash
git add Assets/Editor/Stage2SceneBuilder.cs Assets/Prefabs/Enemy.prefab Assets/Scenes/Arena.unity
git status --short
git commit -m "$(cat <<'EOF'
Wire one enemy into the Arena scene (chase + contact damage) via Editor script

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 7: Note for later manual play-test**

Can't verify headlessly: does the enemy visibly chase the player, does contact damage
actually reduce player health and eventually disable the player object, does the
player's sprite disappear on death. Leave a note for the user to check next time they
open the Editor and press Play.

---

## Self-Review Notes

- **Spec coverage:** `Health`/`HealthComponent` (Task 1), `EnemyChase` +
  `EnemyChaseComponent` + `EnemyContactDamage` + `PlayerDeathReaction` (Task 2), scene
  wiring + enemy prefab (Task 3) — all of the Stage 2 spec's bullet points are covered.
- **Placeholder scan:** none — every step has literal runnable code/commands.
- **Type consistency:** `HealthComponent.ApplyDamage(int)`/`OnDeath` used identically
  in Task 2/3; `EnemyChaseComponent.SetTarget(Transform)` matches its Task 3 call site
  exactly, mirroring `CameraFollowComponent.SetTarget` from Stage 1.
