# Stage 3 — Attack + Projectiles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Player auto-fires at the nearest enemy on a cooldown; enemies take
projectile damage and are destroyed at 0 HP.

**Architecture:** Same plain-class + thin-MonoBehaviour split. `Cooldown` (renamed
from Stage 2's `DamageCooldown`, now reused for attack fire-rate too),
`NearestTargetSelector` (pure target-picking math), `Projectile` (pure movement math)
are plain and tested. `ProjectileComponent`, `AutoAttackComponent`,
`EnemyDeathReaction` are thin MonoBehaviours.

**Tech Stack:** Unity 6000.6.2f1, Physics2D triggers. No new packages.

## Global Constraints

- Namespace root `ArenaSurvivor.*` matching folder; PascalCase/camelCase, no `m_`/`_`.
- Plain classes hold logic, MonoBehaviours are thin forwarders.
- Every plain class with non-trivial logic gets an EditMode test.
- No hand-edited scene/prefab YAML — `Assets/Prefabs/Enemy.prefab` (edit),
  `Assets/Prefabs/Projectile.prefab` (create), `Assets/Scenes/Arena.unity` (edit) are
  all produced via an Editor script run with `-executeMethod`.
- Unity CLI: wrap invocations in a `.sh` script and run with `bash wrapper.sh` (a
  worktree session's safety checker can misflag inline quoted absolute Windows
  paths). Always `-nographics` + `-logFile`; `cygpath -w` for Windows paths. Exit 0 =
  pass, 2 = fail; always confirm a batchmode log actually reached script
  compilation/test execution, not an early licensing-handshake abort.
- Existing state before this plan: `Assets/Scripts/Systems/{Health,HealthComponent,
  DamageCooldown}.cs` + `ArenaSurvivor.Systems.asmdef` (references: []);
  `Assets/Scripts/Enemies/{EnemyChase,EnemyChaseComponent,EnemyContactDamage}.cs` +
  `ArenaSurvivor.Enemies.asmdef` (references: ["ArenaSurvivor.Systems"]);
  `Assets/Scripts/Player/PlayerDeathReaction.cs`; `ArenaSurvivor.Player.asmdef`
  (references: ["Unity.InputSystem", "ArenaSurvivor.Systems"]);
  `ArenaSurvivor.Tests.EditMode.asmdef` (references: ["ArenaSurvivor.Player",
  "ArenaSurvivor.Systems", "ArenaSurvivor.Enemies", "UnityEngine.TestRunner",
  "UnityEditor.TestRunner"]). `Assets/Prefabs/Enemy.prefab` exists (from Stage 2) with
  `EnemyChaseComponent`, `EnemyContactDamage`, `HealthComponent`(30).
  `Assets/Sprites/PlaceholderSquare.png` exists (1-unit square sprite, 100 px/unit).

---

## Task 1: Rename `DamageCooldown` → `Cooldown`; add `NearestTargetSelector` + tests

**Files:**
- Create: `Assets/Scripts/Systems/Cooldown.cs`
- Delete: `Assets/Scripts/Systems/DamageCooldown.cs`, `.meta`
- Modify: `Assets/Scripts/Enemies/EnemyContactDamage.cs` (use `Cooldown` instead of
  `DamageCooldown`)
- Rename+modify: `Assets/Tests/EditMode/DamageCooldownTests.cs` →
  `Assets/Tests/EditMode/CooldownTests.cs`
- Create: `Assets/Scripts/Weapons/ArenaSurvivor.Weapons.asmdef`
- Create: `Assets/Scripts/Weapons/NearestTargetSelector.cs`
- Create: `Assets/Tests/EditMode/NearestTargetSelectorTests.cs`
- Modify: `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef` (add
  `"ArenaSurvivor.Weapons"`)

**Interfaces:**
- Produces: `ArenaSurvivor.Systems.Cooldown` — `Cooldown(float interval)`,
  `bool TryConsume(float currentTime)` (identical behavior to the old
  `DamageCooldown`, only the name changed). Consumed by Task 2's
  `AutoAttackComponent`.
- Produces: `ArenaSurvivor.Weapons.NearestTargetSelector.FindNearestIndex(Vector2
  origin, IReadOnlyList<Vector2> candidatePositions)` → `int` (-1 if empty). Consumed
  by Task 2's `AutoAttackComponent`.

- [ ] **Step 1: Rename `DamageCooldown` to `Cooldown`**

Create `Assets/Scripts/Systems/Cooldown.cs`:
```csharp
namespace ArenaSurvivor.Systems
{
    public class Cooldown
    {
        private readonly float interval;
        private float lastTriggerTime = float.NegativeInfinity;

        public Cooldown(float interval)
        {
            this.interval = interval;
        }

        public bool TryConsume(float currentTime)
        {
            if (currentTime - lastTriggerTime < interval)
            {
                return false;
            }

            lastTriggerTime = currentTime;
            return true;
        }
    }
}
```
Delete `Assets/Scripts/Systems/DamageCooldown.cs` and its `.meta` file.

Rename `Assets/Tests/EditMode/DamageCooldownTests.cs` to `CooldownTests.cs` (`git mv`
so the `.meta` GUID follows), and inside it replace every `DamageCooldown` with
`Cooldown` (class name and namespace usage only — the test bodies/assertions/method
names stay exactly the same, since the behavior is unchanged).

Modify `Assets/Scripts/Enemies/EnemyContactDamage.cs`: replace the field type
`DamageCooldown cooldown;` with `Cooldown cooldown;` and `cooldown = new
DamageCooldown(damageInterval);` with `cooldown = new Cooldown(damageInterval);`.
Everything else in that file is unchanged.

- [ ] **Step 2: Verify the rename compiles and all existing tests still pass**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task1_rename_compile.log")"
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_rename_tests.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_rename_tests.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task1_rename_tests.xml | head -1
```
Expected: both exit 0, `total="19" passed="19" failed="0"` (same 19 as before — the
rename changes no behavior).

- [ ] **Step 3: Create the Weapons assembly definition**

Create `Assets/Scripts/Weapons/ArenaSurvivor.Weapons.asmdef`:
```json
{
    "name": "ArenaSurvivor.Weapons",
    "references": ["ArenaSurvivor.Systems", "ArenaSurvivor.Enemies"],
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
(References `ArenaSurvivor.Enemies` now because Task 2's `AutoAttackComponent` will
look up `EnemyChaseComponent` to find targets.)

Modify `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef`: add
`"ArenaSurvivor.Weapons"` to `references`.

- [ ] **Step 4: Write the failing test for `NearestTargetSelector`**

Create `Assets/Tests/EditMode/NearestTargetSelectorTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Weapons;

namespace ArenaSurvivor.Tests.EditMode
{
    public class NearestTargetSelectorTests
    {
        [Test]
        public void EmptyList_ReturnsNegativeOne()
        {
            var candidates = new List<Vector2>();

            int result = NearestTargetSelector.FindNearestIndex(Vector2.zero, candidates);

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void SingleCandidate_ReturnsIndexZero()
        {
            var candidates = new List<Vector2> { new Vector2(5f, 5f) };

            int result = NearestTargetSelector.FindNearestIndex(Vector2.zero, candidates);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void MultipleCandidates_ReturnsClosest()
        {
            var candidates = new List<Vector2>
            {
                new Vector2(10f, 0f),
                new Vector2(2f, 0f),
                new Vector2(5f, 0f)
            };

            int result = NearestTargetSelector.FindNearestIndex(Vector2.zero, candidates);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void TiedDistances_ReturnsFirstOccurrence()
        {
            var candidates = new List<Vector2>
            {
                new Vector2(3f, 0f),
                new Vector2(0f, 3f)
            };

            int result = NearestTargetSelector.FindNearestIndex(Vector2.zero, candidates);

            Assert.AreEqual(0, result);
        }
    }
}
```

- [ ] **Step 5: Run to verify it fails**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_red.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_red.log")"
```
Expected: nonzero exit; `NearestTargetSelector` does not exist.

- [ ] **Step 6: Implement `NearestTargetSelector`**

Create `Assets/Scripts/Weapons/NearestTargetSelector.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvivor.Weapons
{
    public static class NearestTargetSelector
    {
        public static int FindNearestIndex(Vector2 origin, IReadOnlyList<Vector2> candidatePositions)
        {
            int nearestIndex = -1;
            float nearestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < candidatePositions.Count; i++)
            {
                float sqrDistance = (candidatePositions[i] - origin).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }
    }
}
```

- [ ] **Step 7: Run to verify it passes**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_green.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task1_green.xml | head -1
```
Expected: exit 0, `total="23" passed="23" failed="0"` (19 existing + 4 new
`NearestTargetSelectorTests`).

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Systems/Cooldown.cs Assets/Scripts/Enemies/EnemyContactDamage.cs Assets/Tests/EditMode/CooldownTests.cs Assets/Scripts/Weapons Assets/Tests/EditMode/NearestTargetSelectorTests.cs Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef
git rm Assets/Scripts/Systems/DamageCooldown.cs Assets/Scripts/Systems/DamageCooldown.cs.meta Assets/Tests/EditMode/DamageCooldownTests.cs.meta 2>/dev/null
git status --short
git commit -m "$(cat <<'EOF'
Rename DamageCooldown to Cooldown for reuse; add NearestTargetSelector

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```
Check `git status --short` carefully — a rename via `git mv` may show as `R` (a
rename), which is fine; make sure no leftover `DamageCooldown` file remains tracked.

---

## Task 2: `Projectile`, `ProjectileComponent`, `AutoAttackComponent`, `EnemyDeathReaction`

**Files:**
- Create: `Assets/Scripts/Weapons/Projectile.cs`
- Create: `Assets/Scripts/Weapons/ProjectileComponent.cs`
- Create: `Assets/Scripts/Weapons/AutoAttackComponent.cs`
- Create: `Assets/Scripts/Enemies/EnemyDeathReaction.cs`
- Create: `Assets/Tests/EditMode/ProjectileTests.cs`

**Interfaces:**
- Consumes: `ArenaSurvivor.Systems.Cooldown` and `HealthComponent.ApplyDamage(int)`
  (existing); `ArenaSurvivor.Weapons.NearestTargetSelector.FindNearestIndex` (Task 1);
  `ArenaSurvivor.Enemies.EnemyChaseComponent` (existing, just read `.transform`).
- Produces: `ArenaSurvivor.Weapons.Projectile.ComputeNextPosition(Vector2, Vector2,
  float)`; `ProjectileComponent.Launch(Vector2 direction)` — Task 3's scene script
  doesn't call this directly (it's called at runtime by `AutoAttackComponent`), but
  needs to know the prefab has a `ProjectileComponent` to reference.

- [ ] **Step 1: Write the failing test for `Projectile`**

Create `Assets/Tests/EditMode/ProjectileTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Weapons;

namespace ArenaSurvivor.Tests.EditMode
{
    public class ProjectileTests
    {
        [Test]
        public void StraightLineMovement_MatchesVelocityTimesDeltaTime()
        {
            var projectile = new Projectile();

            Vector2 result = projectile.ComputeNextPosition(Vector2.zero, new Vector2(10f, 0f), 0.5f);

            Assert.AreEqual(new Vector2(5f, 0f), result);
        }

        [Test]
        public void ZeroVelocity_ProducesNoMovement()
        {
            var projectile = new Projectile();

            Vector2 result = projectile.ComputeNextPosition(new Vector2(3f, 3f), Vector2.zero, 1f);

            Assert.AreEqual(new Vector2(3f, 3f), result);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_red.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_red.log")"
```
Expected: nonzero exit; `Projectile` does not exist.

- [ ] **Step 3: Implement `Projectile`**

Create `Assets/Scripts/Weapons/Projectile.cs`:
```csharp
using UnityEngine;

namespace ArenaSurvivor.Weapons
{
    public class Projectile
    {
        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 velocity, float deltaTime)
        {
            return currentPosition + velocity * deltaTime;
        }
    }
}
```

- [ ] **Step 4: Run to verify it passes**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_green.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task2_green.xml | head -1
```
Expected: exit 0, `total="25" passed="25" failed="0"` (23 + 2 new `ProjectileTests`).

- [ ] **Step 5: Implement the MonoBehaviours (no dedicated tests — need a running scene)**

Create `Assets/Scripts/Weapons/ProjectileComponent.cs`:
```csharp
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ProjectileComponent : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private int damage = 10;

        private Rigidbody2D rb;
        private Projectile projectile;
        private Vector2 velocity;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            projectile = new Projectile();
        }

        public void Launch(Vector2 direction)
        {
            velocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            Vector2 nextPosition = projectile.ComputeNextPosition(rb.position, velocity, Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HealthComponent healthComponent = other.GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                return;
            }

            healthComponent.ApplyDamage(damage);
            Destroy(gameObject);
        }
    }
}
```

Create `Assets/Scripts/Weapons/AutoAttackComponent.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Weapons
{
    public class AutoAttackComponent : MonoBehaviour
    {
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private float range = 8f;
        [SerializeField] private GameObject projectilePrefab;

        private Cooldown cooldown;

        private void Awake()
        {
            cooldown = new Cooldown(attackInterval);
        }

        private void Update()
        {
            if (!cooldown.TryConsume(Time.time))
            {
                return;
            }

            EnemyChaseComponent[] enemies = Object.FindObjectsByType<EnemyChaseComponent>(FindObjectsSortMode.None);
            if (enemies.Length == 0)
            {
                return;
            }

            var positions = new List<Vector2>(enemies.Length);
            foreach (EnemyChaseComponent enemy in enemies)
            {
                positions.Add(enemy.transform.position);
            }

            Vector2 origin = transform.position;
            int nearestIndex = NearestTargetSelector.FindNearestIndex(origin, positions);
            Vector2 targetPosition = positions[nearestIndex];
            if (Vector2.Distance(origin, targetPosition) > range)
            {
                return;
            }

            Vector2 direction = (targetPosition - origin).normalized;
            GameObject projectileInstance = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectileInstance.GetComponent<ProjectileComponent>().Launch(direction);
        }
    }
}
```

Create `Assets/Scripts/Enemies/EnemyDeathReaction.cs`:
```csharp
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyDeathReaction : MonoBehaviour
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
            Destroy(gameObject);
        }
    }
}
```

- [ ] **Step 6: Verify compile + full regression**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task2_compile.log")"
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_regression.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_regression.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task2_regression.xml | head -1
```
Expected: both exit 0, `total="25" passed="25" failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Weapons Assets/Scripts/Enemies/EnemyDeathReaction.cs Assets/Tests/EditMode/ProjectileTests.cs
git status --short
git commit -m "$(cat <<'EOF'
Add projectile, auto-attack, and enemy death reaction

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Wire attack + enemy death into the scene/prefabs via an Editor script

**Files:**
- Create: `Assets/Editor/Stage3SceneBuilder.cs`
- Modify (via script, not by hand): `Assets/Prefabs/Enemy.prefab`
- Generate (via script): `Assets/Prefabs/Projectile.prefab`, updated
  `Assets/Scenes/Arena.unity`

**Interfaces:**
- Consumes: `ArenaSurvivor.Enemies.EnemyDeathReaction` (Task 2),
  `ArenaSurvivor.Weapons.{ProjectileComponent,AutoAttackComponent}` (Task 2). Relies
  on `Assets/Prefabs/Enemy.prefab` and the `Player` GameObject in
  `Assets/Scenes/Arena.unity` existing from Stage 2, and
  `Assets/Sprites/PlaceholderSquare.png` from Stage 1.

- [ ] **Step 1: Write the Editor script**

Create `Assets/Editor/Stage3SceneBuilder.cs`:
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaSurvivor.Weapons;
using ArenaSurvivor.Enemies;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage3SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string SpritePath = "Assets/Sprites/PlaceholderSquare.png";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";

        [MenuItem("Arena Survivor/Build Stage 3 Attack")]
        public static void AddAttackAndEnemyDeath()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            AddDeathReactionToEnemyPrefab();
            GameObject projectilePrefab = CreateProjectilePrefab();

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Arena Survivor: Stage3SceneBuilder could not find a GameObject named 'Player' in " + ScenePath);
                return;
            }

            if (player.GetComponent<AutoAttackComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: Player already has an AutoAttackComponent — Stage 3 wiring already applied, skipping.");
                return;
            }

            AutoAttackComponent autoAttack = player.AddComponent<AutoAttackComponent>();
            var serialized = new SerializedObject(autoAttack);
            serialized.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serialized.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 3 attack wired into " + ScenePath);
        }

        private static void AddDeathReactionToEnemyPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

            if (prefabRoot.GetComponent<EnemyDeathReaction>() == null)
            {
                prefabRoot.AddComponent<EnemyDeathReaction>();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static GameObject CreateProjectilePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (existing != null)
            {
                return existing;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            var projectile = new GameObject("Projectile");
            var renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 0.95f, 0.2f);
            renderer.sortingOrder = 8;
            projectile.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            BoxCollider2D collider = projectile.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            projectile.AddComponent<ProjectileComponent>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, ProjectilePrefabPath);
            Object.DestroyImmediate(projectile);

            return prefab;
        }
    }
}
```

- [ ] **Step 2: Run the script via batchmode**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage3SceneBuilder.AddAttackAndEnemyDeath -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage3.log")"
grep "Stage 3 attack wired" Logs/build_stage3.log
ls Assets/Prefabs/Projectile.prefab
```
Expected: exit 0, the log line present, the prefab file exists.

- [ ] **Step 3: Verify the changes actually landed**

```bash
grep -c "EnemyDeathReaction" Assets/Prefabs/Enemy.prefab
grep -c "AutoAttackComponent" Assets/Scenes/Arena.unity
grep -c "ProjectileComponent" Assets/Prefabs/Projectile.prefab
```
Expected: all greater than 0.

- [ ] **Step 4: Confirm idempotency (safe to click twice)**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage3SceneBuilder.AddAttackAndEnemyDeath -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage3_rerun.log")"
grep "already has an AutoAttackComponent" Logs/build_stage3_rerun.log
grep -c "AutoAttackComponent" Assets/Scenes/Arena.unity
```
Expected: the warning line present in the rerun log; the `AutoAttackComponent` count
in the scene is unchanged from Step 3 (no duplicate added).

- [ ] **Step 5: Re-open headless to confirm no compile/scene errors**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task3_verify.log")"
```
Expected: exit 0, full compile/shutdown cycle in the log (not an early abort).

- [ ] **Step 6: Run the full EditMode suite one final time**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task3_tests.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task3_tests.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task3_tests.xml | head -1
```
Expected: exit 0, `total="25" passed="25" failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Editor/Stage3SceneBuilder.cs Assets/Prefabs/Enemy.prefab Assets/Prefabs/Projectile.prefab Assets/Scenes/Arena.unity
git status --short
git commit -m "$(cat <<'EOF'
Wire auto-attack and enemy death reaction into scene and prefabs

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 8: Note for later manual play-test**

Can't verify headlessly: does the player actually auto-fire at the enemy, do
projectiles visibly travel and hit, does the enemy actually disappear at 0 HP, does
firing stop/resume correctly as the enemy moves in and out of range. Leave a note for
the user.

---

## Self-Review Notes

- **Spec coverage:** `Cooldown` rename + reuse (Task 1), `NearestTargetSelector`
  (Task 1), `Projectile`/`ProjectileComponent`/`AutoAttackComponent`/
  `EnemyDeathReaction` (Task 2), prefab/scene wiring (Task 3) — all spec bullets
  covered.
- **Placeholder scan:** none — every step has literal runnable code/commands.
- **Type consistency:** `Cooldown.TryConsume(float)` used identically in
  `EnemyContactDamage` (Task 1) and `AutoAttackComponent` (Task 2).
  `NearestTargetSelector.FindNearestIndex` signature matches its Task 2 call site
  exactly. `ProjectileComponent.Launch(Vector2)` matches its Task 2
  `AutoAttackComponent` call site.
