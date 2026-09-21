# Stage 5 — Upgrade Choice Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Leveling up pauses the game and offers 3 random upgrades (+damage, +move
speed, +attack cooldown reduction, +max HP) via `Upgrade` ScriptableObjects and a
simple button UI.

**Architecture:** `Upgrade` (abstract ScriptableObject) + 4 concrete subclasses in a
new `Assets/Scripts/Data/` folder hold designer-tunable magnitudes and a one-line
`Apply` forwarder into an existing component's new public mutator method. A new plain
`UpgradeSelector` picks 3 unique random upgrades. `UpgradeChoiceComponent` (UI)
listens for `PlayerLevelingComponent.OnLevelUp`, pauses (`Time.timeScale = 0`), shows
3 buttons, and resumes on pick.

**Tech Stack:** Unity 6000.6.2f1, uGUI, `UnityEngine.InputSystem.UI.InputSystemUIInputModule`
(the new-Input-System-compatible replacement for uGUI's legacy `StandaloneInputModule`
— required because this project has no legacy Input Manager active). No new packages.

## Global Constraints

- Namespace root `ArenaSurvivor.*` matching folder; PascalCase/camelCase, no `m_`/`_`.
- Plain classes hold logic, MonoBehaviours/ScriptableObjects with only a forwarding
  call are thin wiring (no dedicated test), matching this project's existing
  convention of only testing the plain-class layer.
- Data goes in ScriptableObjects (CLAUDE.md rule 2) — the 4 upgrade magnitudes are
  serialized fields on their respective `[CreateAssetMenu]` assets, not hardcoded.
- No hand-edited scene/prefab/asset YAML — `Assets/ScriptableObjects/Upgrades/*.asset`
  (create) and `Assets/Scenes/Arena.unity` (edit) are produced via an Editor script
  run with `-executeMethod`.
- **Idempotency-check lesson from Stage 4's operational incident:** never use a
  `GetComponent<T>()` check as an "already exists / already applied" guard when `T`
  carries `[RequireComponent]` — Unity auto-injects required components in memory on
  load even when they're not yet serialized to disk, making such a check always true.
  Use plain asset-existence checks (`AssetDatabase.LoadAssetAtPath` returning
  non-null) or checks against a type that has no `[RequireComponent]` dependency of
  its own instead.
- Unity CLI: wrap invocations in a `.sh` script and run with `bash wrapper.sh`,
  always including `-quit` on any run that isn't itself starting a test run (a
  previous stage's fix hung for 90+ minutes from a batchmode call missing `-quit`).
  Always `-nographics` + `-logFile`; `cygpath -w` for Windows paths. Exit 0 = pass, 2
  = fail for test runs; always confirm a batchmode log actually reached script
  compilation/test execution.
- Existing state before this plan: `Assets/Scripts/Systems/{Health,HealthComponent,
  Cooldown,PlayerLeveling,PlayerLevelingComponent,ExperienceOrb,ExperienceOrbComponent,
  UpgradeSelector(new-this-plan)}.cs`; `Assets/Scripts/Player/PlayerMovementComponent.cs`;
  `Assets/Scripts/Weapons/{AutoAttack,AutoAttackComponent,Projectile,ProjectileComponent,
  NearestTargetSelector}.cs`; `ArenaSurvivor.Tests.EditMode.asmdef` references
  `["ArenaSurvivor.Player", "ArenaSurvivor.Systems", "ArenaSurvivor.Enemies",
  "ArenaSurvivor.Weapons", "ArenaSurvivor.UI", "UnityEngine.TestRunner",
  "UnityEditor.TestRunner"]`. Current EditMode test count: 41.
- **Exact current content of files this plan modifies** (read before editing, don't
  guess — these are believed accurate as of this plan's writing but the file on disk
  is the source of truth):
  - `Assets/Scripts/Systems/Cooldown.cs`: has `private readonly float interval;`,
    constructor sets it, `IsReady(float)` and `TryConsume(float)` methods.
  - `Assets/Scripts/Systems/Health.cs`: `MaxHealth { get; }` (get-only).
  - `Assets/Scripts/Weapons/ProjectileComponent.cs`: has
    `[SerializeField] private int damage = 10;` and `public void Launch(Vector2 direction)`.
  - `Assets/Scripts/Weapons/AutoAttackComponent.cs`: has
    `[SerializeField] private float attackInterval = 1f;`, constructs
    `cooldown = new Cooldown(attackInterval);` in `Awake`, and calls
    `projectileInstance.GetComponent<ProjectileComponent>().Launch(direction);`.

---

## Task 1: Plain-class extensions — `Cooldown.SetInterval`, `Health.IncreaseMaxHealth`, new `UpgradeSelector`

**Files:**
- Modify: `Assets/Scripts/Systems/Cooldown.cs`
- Modify: `Assets/Scripts/Systems/Health.cs`
- Modify: `Assets/Tests/EditMode/CooldownTests.cs` (append tests, don't remove existing)
- Modify: `Assets/Tests/EditMode/HealthTests.cs` (append tests, don't remove existing)
- Create: `Assets/Scripts/Systems/UpgradeSelector.cs`
- Create: `Assets/Tests/EditMode/UpgradeSelectorTests.cs`

**Interfaces:**
- Produces: `Cooldown.Interval` (public float property, was private), `Cooldown.SetInterval(float
  newInterval)` (clamps to a 0.1f floor). Consumed by Task 2's `AutoAttackComponent.ReduceAttackInterval`.
- Produces: `Health.IncreaseMaxHealth(int amount)` (raises `MaxHealth` and
  `CurrentHealth` by the same amount; non-positive is a no-op). Consumed by Task 2's
  `HealthComponent.IncreaseMaxHealth`.
- Produces: `ArenaSurvivor.Systems.UpgradeSelector` — constructor `(System.Random
  random)`, `List<T> SelectRandomUnique<T>(IReadOnlyList<T> pool, int count)`.
  Consumed by Task 3's `UpgradeChoiceComponent`.

- [ ] **Step 1: Read the current files first**

```bash
cat Assets/Scripts/Systems/Cooldown.cs
cat Assets/Scripts/Systems/Health.cs
cat Assets/Tests/EditMode/CooldownTests.cs
cat Assets/Tests/EditMode/HealthTests.cs
```
Confirm they match this plan's "Exact current content" assumptions above (field/method
names). If they differ meaningfully, adapt the following steps to the real structure
— the interfaces this task must produce (listed above) are what matters, not the
exact internal field names.

- [ ] **Step 2: Rewrite `Cooldown.cs`**

Replace the full contents of `Assets/Scripts/Systems/Cooldown.cs` with:
```csharp
namespace ArenaSurvivor.Systems
{
    public class Cooldown
    {
        private const float MinInterval = 0.1f;

        private float lastTriggerTime = float.NegativeInfinity;

        public float Interval { get; private set; }

        public Cooldown(float interval)
        {
            Interval = interval;
        }

        public void SetInterval(float newInterval)
        {
            Interval = newInterval < MinInterval ? MinInterval : newInterval;
        }

        public bool IsReady(float currentTime)
        {
            return currentTime - lastTriggerTime >= Interval;
        }

        public bool TryConsume(float currentTime)
        {
            if (!IsReady(currentTime))
            {
                return false;
            }

            lastTriggerTime = currentTime;
            return true;
        }
    }
}
```
(This preserves `IsReady`/`TryConsume` behavior exactly — only `interval` becomes the
public `Interval` property, and `SetInterval` is new.)

- [ ] **Step 3: Append new tests to `CooldownTests.cs`**

Add these two `[Test]` methods inside the existing `CooldownTests` class (don't
remove or change any existing test method):
```csharp
[Test]
public void SetInterval_ChangesFutureTiming()
{
    var cooldown = new Cooldown(1f);
    cooldown.TryConsume(0f);

    cooldown.SetInterval(0.5f);

    Assert.IsFalse(cooldown.IsReady(0.3f));
    Assert.IsTrue(cooldown.IsReady(0.5f));
}

[Test]
public void SetInterval_ClampsToFloor_NeverZeroOrNegative()
{
    var cooldown = new Cooldown(1f);

    cooldown.SetInterval(-5f);

    Assert.AreEqual(0.1f, cooldown.Interval, 0.0001f);
}
```

- [ ] **Step 4: Modify `Health.cs`**

Change `public int MaxHealth { get; }` to `public int MaxHealth { get; private set; }`.
Add this method to the class:
```csharp
public void IncreaseMaxHealth(int amount)
{
    if (amount <= 0)
    {
        return;
    }

    MaxHealth += amount;
    CurrentHealth += amount;
}
```

- [ ] **Step 5: Append new tests to `HealthTests.cs`**

Add these two `[Test]` methods inside the existing `HealthTests` class:
```csharp
[Test]
public void IncreaseMaxHealth_RaisesMaxAndCurrentByAmount()
{
    var health = new Health(50);
    health.TakeDamage(20);

    health.IncreaseMaxHealth(10);

    Assert.AreEqual(60, health.MaxHealth);
    Assert.AreEqual(40, health.CurrentHealth);
}

[Test]
public void IncreaseMaxHealth_NonPositiveAmount_IsNoOp()
{
    var health = new Health(50);

    health.IncreaseMaxHealth(0);
    health.IncreaseMaxHealth(-5);

    Assert.AreEqual(50, health.MaxHealth);
    Assert.AreEqual(50, health.CurrentHealth);
}
```

- [ ] **Step 6: Write the failing test for `UpgradeSelector`**

Create `Assets/Tests/EditMode/UpgradeSelectorTests.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class UpgradeSelectorTests
    {
        [Test]
        public void PoolSmallerThanCount_ReturnsWholePool()
        {
            var selector = new UpgradeSelector(new Random(1));
            var pool = new List<string> { "A", "B" };

            List<string> result = selector.SelectRandomUnique(pool, 5);

            Assert.AreEqual(2, result.Count);
            CollectionAssert.AreEquivalent(pool, result);
        }

        [Test]
        public void RequestingFewerThanPoolSize_ReturnsExactCountAllUniqueFromPool()
        {
            var selector = new UpgradeSelector(new Random(2));
            var pool = new List<string> { "A", "B", "C", "D", "E" };

            List<string> result = selector.SelectRandomUnique(pool, 3);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, result.Distinct().Count());
            foreach (string item in result)
            {
                Assert.Contains(item, pool);
            }
        }

        [Test]
        public void ManySeeds_NeverProducesDuplicateInOneCall()
        {
            var pool = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };

            for (int seed = 0; seed < 100; seed++)
            {
                var selector = new UpgradeSelector(new Random(seed));
                List<int> result = selector.SelectRandomUnique(pool, 4);

                Assert.AreEqual(4, result.Distinct().Count(), "Seed " + seed + " produced a duplicate");
            }
        }
    }
}
```

- [ ] **Step 7: Run to verify it fails**

Create `run_unity.sh` in the worktree root if it doesn't already exist:
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
Expected: nonzero exit; `UpgradeSelector` does not exist (the `Cooldown`/`Health`
tests you appended should compile-fail too until Step 8/9, since those methods don't
exist yet either — that's expected, this is one combined RED state for the whole task).

- [ ] **Step 8: Implement `UpgradeSelector`**

Create `Assets/Scripts/Systems/UpgradeSelector.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace ArenaSurvivor.Systems
{
    public class UpgradeSelector
    {
        private readonly Random random;

        public UpgradeSelector(Random random)
        {
            this.random = random;
        }

        public List<T> SelectRandomUnique<T>(IReadOnlyList<T> pool, int count)
        {
            var poolCopy = new List<T>(pool);
            var result = new List<T>();
            int selectCount = Math.Min(count, poolCopy.Count);

            for (int i = 0; i < selectCount; i++)
            {
                int index = random.Next(poolCopy.Count);
                result.Add(poolCopy[index]);
                poolCopy.RemoveAt(index);
            }

            return result;
        }
    }
}
```

- [ ] **Step 9: Run to verify everything passes**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_green.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task1_green.xml | head -1
```
Expected: exit 0, `total="48" passed="48" failed="0"` (41 existing + 2 `Cooldown`
tests + 2 `Health` tests + 3 `UpgradeSelector` tests).

- [ ] **Step 10: Commit**

```bash
git add Assets/Scripts/Systems/Cooldown.cs Assets/Scripts/Systems/Health.cs Assets/Scripts/Systems/UpgradeSelector.cs Assets/Tests/EditMode/CooldownTests.cs Assets/Tests/EditMode/HealthTests.cs Assets/Tests/EditMode/UpgradeSelectorTests.cs
git status --short
git commit -m "$(cat <<'EOF'
Add mutable Cooldown.Interval, Health.IncreaseMaxHealth, and UpgradeSelector

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: MonoBehaviour mutators + `Upgrade` ScriptableObject hierarchy

**Files:**
- Modify: `Assets/Scripts/Player/PlayerMovementComponent.cs`
- Modify: `Assets/Scripts/Weapons/ProjectileComponent.cs`
- Modify: `Assets/Scripts/Weapons/AutoAttackComponent.cs`
- Modify: `Assets/Scripts/Systems/HealthComponent.cs`
- Create: `Assets/Scripts/Data/ArenaSurvivor.Data.asmdef`
- Create: `Assets/Scripts/Data/Upgrade.cs`
- Create: `Assets/Scripts/Data/DamageUpgrade.cs`
- Create: `Assets/Scripts/Data/MoveSpeedUpgrade.cs`
- Create: `Assets/Scripts/Data/AttackCooldownUpgrade.cs`
- Create: `Assets/Scripts/Data/MaxHealthUpgrade.cs`
- Modify: `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef` (add
  `"ArenaSurvivor.Data"`)

**Interfaces:**
- Consumes: `Cooldown.Interval`/`SetInterval` and `Health.IncreaseMaxHealth` (Task 1).
- Produces: `PlayerMovementComponent.IncreaseSpeed(float amount)`;
  `ProjectileComponent.Launch(Vector2 direction, int damage)` (signature CHANGE —
  update the only call site, in `AutoAttackComponent`, in this same task);
  `AutoAttackComponent.IncreaseDamage(int amount)`,
  `AutoAttackComponent.ReduceAttackInterval(float amount)`;
  `HealthComponent.IncreaseMaxHealth(int amount)`. All four consumed by Task 2's own
  `Upgrade` subclasses, and by Task 3's UI (indirectly, via `Upgrade.Apply`).
- Produces: `ArenaSurvivor.Data.Upgrade` (abstract) — `string DisplayName`,
  `string Description`, `abstract void Apply(GameObject player)`. Four concrete
  subclasses consumed by Task 3's scene-builder script (which creates one asset per
  subclass) and `UpgradeChoiceComponent` (which calls `.Apply`).

- [ ] **Step 1: Read the current files first**

```bash
cat Assets/Scripts/Player/PlayerMovementComponent.cs
cat Assets/Scripts/Weapons/ProjectileComponent.cs
cat Assets/Scripts/Weapons/AutoAttackComponent.cs
cat Assets/Scripts/Systems/HealthComponent.cs
```

- [ ] **Step 2: Add `IncreaseSpeed` to `PlayerMovementComponent`**

Add this method to the `PlayerMovementComponent` class (anywhere in the class body):
```csharp
public void IncreaseSpeed(float amount)
{
    moveSpeed += amount;
}
```
(`moveSpeed` is the existing private `[SerializeField] float` field — if its exact
name differs from `moveSpeed` in the actual file, use the real field name.)

- [ ] **Step 3: Change `ProjectileComponent.Launch`'s signature**

Replace:
```csharp
[SerializeField] private int damage = 10;
```
with:
```csharp
private int damage;
```
Replace:
```csharp
public void Launch(Vector2 direction)
{
    velocity = direction.normalized * speed;
    Destroy(gameObject, lifetime);
}
```
with:
```csharp
public void Launch(Vector2 direction, int damage)
{
    this.damage = damage;
    velocity = direction.normalized * speed;
    Destroy(gameObject, lifetime);
}
```
(Field/parameter names as in the actual file — adapt if they differ from this plan's
assumption.)

- [ ] **Step 4: Update `AutoAttackComponent`**

Add these fields to the class (alongside the existing `[SerializeField]` fields):
```csharp
[SerializeField] private int baseDamage = 10;

private int damageBonus;
```
Add these two public methods:
```csharp
public void IncreaseDamage(int amount)
{
    damageBonus += amount;
}

public void ReduceAttackInterval(float amount)
{
    cooldown.SetInterval(cooldown.Interval - amount);
}
```
Change the projectile-launch call site from:
```csharp
projectileInstance.GetComponent<ProjectileComponent>().Launch(direction);
```
to:
```csharp
projectileInstance.GetComponent<ProjectileComponent>().Launch(direction, baseDamage + damageBonus);
```

- [ ] **Step 5: Add `IncreaseMaxHealth` to `HealthComponent`**

Add this method to the `HealthComponent` class:
```csharp
public void IncreaseMaxHealth(int amount)
{
    health.IncreaseMaxHealth(amount);
}
```
(`health` is the existing private `Health` field — use its real name if different.)

- [ ] **Step 6: Verify these four files compile and the full suite still passes**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task2_compile1.log")"
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_regression1.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_regression1.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task2_regression1.xml | head -1
```
Expected: both exit 0, `total="48" passed="48" failed="0"` (no new tests yet, no
regressions from the signature change since the only call site was updated in the
same step).

- [ ] **Step 7: Create the Data assembly definition**

Create `Assets/Scripts/Data/ArenaSurvivor.Data.asmdef`:
```json
{
    "name": "ArenaSurvivor.Data",
    "references": ["ArenaSurvivor.Player", "ArenaSurvivor.Weapons", "ArenaSurvivor.Systems"],
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

Modify `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef`: add
`"ArenaSurvivor.Data"` to `references` (per CLAUDE.md's asmdef convention, even
though this task adds no dedicated Data tests — the rule applies whenever a new
asmdef is added).

- [ ] **Step 8: Implement the `Upgrade` base class**

Create `Assets/Scripts/Data/Upgrade.cs`:
```csharp
using UnityEngine;

namespace ArenaSurvivor.Data
{
    public abstract class Upgrade : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] [TextArea] private string description;

        public string DisplayName => displayName;
        public string Description => description;

        public abstract void Apply(GameObject player);
    }
}
```

- [ ] **Step 9: Implement the four concrete upgrades**

Create `Assets/Scripts/Data/DamageUpgrade.cs`:
```csharp
using UnityEngine;
using ArenaSurvivor.Weapons;

namespace ArenaSurvivor.Data
{
    [CreateAssetMenu(fileName = "DamageUpgrade", menuName = "Arena Survivor/Upgrades/Damage")]
    public class DamageUpgrade : Upgrade
    {
        [SerializeField] private int damageIncrease = 5;

        public override void Apply(GameObject player)
        {
            AutoAttackComponent autoAttack = player.GetComponent<AutoAttackComponent>();
            if (autoAttack == null)
            {
                Debug.LogError("DamageUpgrade: player has no AutoAttackComponent");
                return;
            }

            autoAttack.IncreaseDamage(damageIncrease);
        }
    }
}
```

Create `Assets/Scripts/Data/MoveSpeedUpgrade.cs`:
```csharp
using UnityEngine;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.Data
{
    [CreateAssetMenu(fileName = "MoveSpeedUpgrade", menuName = "Arena Survivor/Upgrades/Move Speed")]
    public class MoveSpeedUpgrade : Upgrade
    {
        [SerializeField] private float speedIncrease = 1f;

        public override void Apply(GameObject player)
        {
            PlayerMovementComponent movement = player.GetComponent<PlayerMovementComponent>();
            if (movement == null)
            {
                Debug.LogError("MoveSpeedUpgrade: player has no PlayerMovementComponent");
                return;
            }

            movement.IncreaseSpeed(speedIncrease);
        }
    }
}
```

Create `Assets/Scripts/Data/AttackCooldownUpgrade.cs`:
```csharp
using UnityEngine;
using ArenaSurvivor.Weapons;

namespace ArenaSurvivor.Data
{
    [CreateAssetMenu(fileName = "AttackCooldownUpgrade", menuName = "Arena Survivor/Upgrades/Attack Cooldown")]
    public class AttackCooldownUpgrade : Upgrade
    {
        [SerializeField] private float cooldownReduction = 0.1f;

        public override void Apply(GameObject player)
        {
            AutoAttackComponent autoAttack = player.GetComponent<AutoAttackComponent>();
            if (autoAttack == null)
            {
                Debug.LogError("AttackCooldownUpgrade: player has no AutoAttackComponent");
                return;
            }

            autoAttack.ReduceAttackInterval(cooldownReduction);
        }
    }
}
```

Create `Assets/Scripts/Data/MaxHealthUpgrade.cs`:
```csharp
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Data
{
    [CreateAssetMenu(fileName = "MaxHealthUpgrade", menuName = "Arena Survivor/Upgrades/Max Health")]
    public class MaxHealthUpgrade : Upgrade
    {
        [SerializeField] private int healthIncrease = 20;

        public override void Apply(GameObject player)
        {
            HealthComponent health = player.GetComponent<HealthComponent>();
            if (health == null)
            {
                Debug.LogError("MaxHealthUpgrade: player has no HealthComponent");
                return;
            }

            health.IncreaseMaxHealth(healthIncrease);
        }
    }
}
```

- [ ] **Step 10: Verify compile + full regression**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task2_compile2.log")"
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_regression2.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_regression2.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task2_regression2.xml | head -1
```
Expected: both exit 0, `total="48" passed="48" failed="0"`.

- [ ] **Step 11: Commit**

```bash
git add Assets/Scripts/Player/PlayerMovementComponent.cs Assets/Scripts/Weapons/ProjectileComponent.cs Assets/Scripts/Weapons/AutoAttackComponent.cs Assets/Scripts/Systems/HealthComponent.cs Assets/Scripts/Data Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef
git status --short
git commit -m "$(cat <<'EOF'
Make player stats upgradeable; add Upgrade ScriptableObject hierarchy

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: `UpgradeChoiceComponent` + scene/asset wiring via Editor script

**Files:**
- Create: `Assets/Scripts/UI/UpgradeChoiceComponent.cs`
- Create: `Assets/Editor/Stage5SceneBuilder.cs`
- Generate (via script): `Assets/ScriptableObjects/Upgrades/{Damage,MoveSpeed,
  AttackCooldown,MaxHealth}Upgrade.asset`, updated `Assets/Scenes/Arena.unity`

**Interfaces:**
- Consumes: `ArenaSurvivor.Data.Upgrade` and its 4 subclasses (Task 2);
  `ArenaSurvivor.Systems.{UpgradeSelector,PlayerLevelingComponent}` (Task 1 /
  existing). Relies on the `Player` GameObject in `Assets/Scenes/Arena.unity` already
  having `PlayerLevelingComponent` (from Stage 4).

- [ ] **Step 1: Implement `UpgradeChoiceComponent`**

Create `Assets/Scripts/UI/UpgradeChoiceComponent.cs`:
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArenaSurvivor.Data;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.UI
{
    public class UpgradeChoiceComponent : MonoBehaviour
    {
        [SerializeField] private Upgrade[] availableUpgrades;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private Text[] optionTitles;
        [SerializeField] private Text[] optionDescriptions;

        private UpgradeSelector selector;
        private PlayerLevelingComponent playerLeveling;
        private Upgrade[] currentOptions;

        private void Awake()
        {
            selector = new UpgradeSelector(new Random());
            playerLeveling = Object.FindFirstObjectByType<PlayerLevelingComponent>();
            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (playerLeveling != null)
            {
                playerLeveling.OnLevelUp += HandleLevelUp;
            }
        }

        private void OnDisable()
        {
            if (playerLeveling != null)
            {
                playerLeveling.OnLevelUp -= HandleLevelUp;
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            List<Upgrade> options = selector.SelectRandomUnique(availableUpgrades, optionButtons.Length);
            currentOptions = options.ToArray();

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < currentOptions.Length)
                {
                    Upgrade upgrade = currentOptions[i];
                    optionTitles[i].text = upgrade.DisplayName;
                    optionDescriptions[i].text = upgrade.Description;
                    optionButtons[i].gameObject.SetActive(true);

                    int optionIndex = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => SelectUpgrade(optionIndex));
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            panelRoot.SetActive(true);
            Time.timeScale = 0f;
        }

        private void SelectUpgrade(int optionIndex)
        {
            currentOptions[optionIndex].Apply(playerLeveling.gameObject);
            panelRoot.SetActive(false);
            Time.timeScale = 1f;
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task3_compile1.log")"
```
Expected: exit 0.

- [ ] **Step 3: Write the Editor script**

Create `Assets/Editor/Stage5SceneBuilder.cs`:
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ArenaSurvivor.Data;
using ArenaSurvivor.UI;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage5SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string UpgradesFolder = "Assets/ScriptableObjects/Upgrades";

        [MenuItem("Arena Survivor/Build Stage 5 Upgrade Screen")]
        public static void AddUpgradeScreen()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            Upgrade[] upgrades = CreateUpgradeAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            if (Object.FindFirstObjectByType<UpgradeChoiceComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: UpgradeChoiceComponent already present — Stage 5 wiring already applied, skipping.");
                return;
            }

            EnsureEventSystem();
            BuildUpgradeChoiceUI(upgrades);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 5 upgrade screen wired into " + ScenePath);
        }

        private static Upgrade[] CreateUpgradeAssets()
        {
            System.IO.Directory.CreateDirectory(UpgradesFolder);

            var upgrades = new Upgrade[4];
            upgrades[0] = CreateUpgradeAsset<DamageUpgrade>("DamageUpgrade", "More Firepower", "Increases projectile damage.");
            upgrades[1] = CreateUpgradeAsset<MoveSpeedUpgrade>("MoveSpeedUpgrade", "Quick Feet", "Increases movement speed.");
            upgrades[2] = CreateUpgradeAsset<AttackCooldownUpgrade>("AttackCooldownUpgrade", "Rapid Fire", "Reduces time between shots.");
            upgrades[3] = CreateUpgradeAsset<MaxHealthUpgrade>("MaxHealthUpgrade", "Vitality", "Increases max health and heals.");

            return upgrades;
        }

        private static T CreateUpgradeAsset<T>(string fileName, string displayName, string description) where T : Upgrade
        {
            string path = UpgradesFolder + "/" + fileName + ".asset";
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T upgrade = ScriptableObject.CreateInstance<T>();
            var serialized = new SerializedObject(upgrade);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(upgrade, path);
            return upgrade;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static void BuildUpgradeChoiceUI(Upgrade[] upgrades)
        {
            var canvasObject = new GameObject("UpgradeChoiceCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var panelObject = new GameObject("UpgradePanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelBackground = panelObject.AddComponent<Image>();
            panelBackground.color = new Color(0f, 0f, 0f, 0.7f);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var buttons = new Button[3];
            var titles = new Text[3];
            var descriptions = new Text[3];

            for (int i = 0; i < 3; i++)
            {
                float xOffset = (i - 1) * 350f;
                CreateOptionButton(panelObject.transform, xOffset, out buttons[i], out titles[i], out descriptions[i]);
            }

            UpgradeChoiceComponent upgradeChoice = canvasObject.AddComponent<UpgradeChoiceComponent>();
            var serialized = new SerializedObject(upgradeChoice);
            SetObjectArray(serialized, "availableUpgrades", upgrades);
            serialized.FindProperty("panelRoot").objectReferenceValue = panelObject;
            SetObjectArray(serialized, "optionButtons", buttons);
            SetObjectArray(serialized, "optionTitles", titles);
            SetObjectArray(serialized, "optionDescriptions", descriptions);
            serialized.ApplyModifiedProperties();

            panelObject.SetActive(false);
        }

        private static void CreateOptionButton(Transform parent, float xOffset, out Button button, out Text title, out Text description)
        {
            var buttonObject = new GameObject("UpgradeOption");
            buttonObject.transform.SetParent(parent, false);
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.25f, 0.3f);
            button = buttonObject.AddComponent<Button>();
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(xOffset, 0f);
            buttonRect.sizeDelta = new Vector2(300f, 200f);

            var titleObject = new GameObject("Title");
            titleObject.transform.SetParent(buttonObject.transform, false);
            title = titleObject.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 20;
            title.alignment = TextAnchor.UpperCenter;
            title.color = Color.white;
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.6f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var descriptionObject = new GameObject("Description");
            descriptionObject.transform.SetParent(buttonObject.transform, false);
            description = descriptionObject.AddComponent<Text>();
            description.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            description.fontSize = 14;
            description.alignment = TextAnchor.UpperCenter;
            description.color = Color.white;
            RectTransform descriptionRect = descriptionObject.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 0.6f);
            descriptionRect.offsetMin = Vector2.zero;
            descriptionRect.offsetMax = Vector2.zero;
        }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
```

- [ ] **Step 4: Run the script via batchmode**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage5SceneBuilder.AddUpgradeScreen -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage5.log")"
grep "Stage 5 upgrade screen wired" Logs/build_stage5.log
ls Assets/ScriptableObjects/Upgrades/
```
Expected: exit 0, the success log line genuinely present (not a skip — if it's
missing, do NOT assume success; check whether the idempotency guard tripped for an
unexpected reason and investigate before proceeding, per the lesson from Stage 3/4's
final reviews), all 4 `.asset` files exist.

- [ ] **Step 5: Verify the changes actually landed**

```bash
grep -c "UpgradeChoiceComponent" Assets/Scenes/Arena.unity
grep -c "InputSystemUIInputModule" Assets/Scenes/Arena.unity
grep -c "m_DisplayName\|displayName" Assets/ScriptableObjects/Upgrades/DamageUpgrade.asset
```
Expected: all greater than 0.

- [ ] **Step 6: Confirm idempotency (safe to click twice)**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage5SceneBuilder.AddUpgradeScreen -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage5_rerun.log")"
grep "already present" Logs/build_stage5_rerun.log
grep -c "UpgradeChoiceComponent" Assets/Scenes/Arena.unity
```
Expected: the warning line present; the `UpgradeChoiceComponent` count unchanged
from Step 5 (no duplicate).

- [ ] **Step 7: Re-open headless to confirm no compile/scene errors**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task3_verify.log")"
```
Expected: exit 0, full compile/shutdown cycle.

- [ ] **Step 8: Run the full EditMode suite one final time**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task3_tests.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task3_tests.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task3_tests.xml | head -1
```
Expected: exit 0, `total="48" passed="48" failed="0"`.

- [ ] **Step 9: Commit**

```bash
git add Assets/Scripts/UI/UpgradeChoiceComponent.cs Assets/Editor/Stage5SceneBuilder.cs Assets/ScriptableObjects/Upgrades Assets/Scenes/Arena.unity
git status --short
git commit -m "$(cat <<'EOF'
Wire upgrade-choice UI, EventSystem, and upgrade assets into the scene

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 10: Note for later manual play-test**

Can't verify headlessly: does leveling up actually pause the game and show 3
buttons, do the buttons actually respond to mouse clicks and/or gamepad navigation
(via `InputSystemUIInputModule`'s auto-assigned default actions — this is the one
thing most likely to need a manual nudge if `Reset()` didn't auto-populate actions as
expected), does picking an upgrade actually change player behavior (faster movement,
faster fire rate, more damage, higher max HP with a partial heal), does the game
correctly resume at normal speed after picking. Leave a note for the user.

---

## Self-Review Notes

- **Spec coverage:** `Cooldown`/`Health` extensions + `UpgradeSelector` (Task 1),
  mutators + `Upgrade` hierarchy (Task 2), UI + scene wiring (Task 3) — all spec
  bullets covered.
- **Placeholder scan:** none — every step has literal runnable code/commands, except
  Step 1 of Tasks 1/2 which explicitly instructs reading the real file first and
  adapting to it if it differs from this plan's stated assumptions (not a
  placeholder — a deliberate verify-before-edit step, since these are modifications
  to existing files this plan didn't just create).
- **Type consistency:** `ProjectileComponent.Launch(Vector2, int)`'s new signature is
  updated at its only call site in the same task (Task 2, Step 4).
  `Cooldown.Interval`/`SetInterval` used identically in `AutoAttackComponent`.
  `Upgrade.Apply(GameObject)` signature matches its call site in
  `UpgradeChoiceComponent.SelectUpgrade` exactly. Field names used in
  `Stage5SceneBuilder`'s `SerializedObject.FindProperty` calls
  (`displayName`/`description`/`availableUpgrades`/`panelRoot`/`optionButtons`/
  `optionTitles`/`optionDescriptions`) match the actual private field names declared
  in `Upgrade.cs`/`UpgradeChoiceComponent.cs` exactly.
- **Idempotency-check safety:** `Stage5SceneBuilder`'s guards use either a plain
  `AssetDatabase.LoadAssetAtPath` check (upgrade assets) or a `GetComponent` check
  against a type (`UpgradeChoiceComponent`) that itself carries no
  `[RequireComponent]` — avoiding Stage 4's exact failure mode.
