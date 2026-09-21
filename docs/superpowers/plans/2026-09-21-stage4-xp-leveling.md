# Stage 4 — XP and Leveling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Killing enemies drops XP orbs; collecting enough triggers a level-up; a
basic on-screen XP bar shows progress.

**Architecture:** `PlayerLeveling`/`ExperienceOrb` (plain, tested) live in the
existing `Assets/Scripts/Systems/` folder per CLAUDE.md's own folder-structure
comment ("Systems: spawner, XP/leveling, game state") — no new asmdef needed for
them. Thin MonoBehaviours (`PlayerLevelingComponent`, `ExperienceOrbComponent`,
`EnemyXpDrop`, `XpBarComponent`) forward to them. A new `ArenaSurvivor.UI.asmdef` is
needed only for the uGUI-touching `XpBarComponent`.

**Tech Stack:** Unity 6000.6.2f1, Physics2D triggers, `com.unity.ugui` (already
installed). No new packages.

## Global Constraints

- Namespace root `ArenaSurvivor.*` matching folder; PascalCase/camelCase, no `m_`/`_`.
- Plain classes hold logic, MonoBehaviours are thin forwarders.
- Every plain class with non-trivial logic gets an EditMode test.
- No hand-edited scene/prefab YAML — `Assets/Prefabs/XpOrb.prefab` (create),
  `Assets/Prefabs/Enemy.prefab` (edit), `Assets/Scenes/Arena.unity` (edit) all
  produced via an Editor script run with `-executeMethod`.
- **Trigger-collision lesson from Stage 3's critical bug:** never damage/affect "any
  object with component X" on a trigger hit — always check for the *specific* type
  you expect to interact with first (e.g. `ExperienceOrbComponent`'s pickup check
  looks for `PlayerLevelingComponent` specifically, not any generic marker).
- Unity CLI: wrap invocations in a `.sh` script and run with `bash wrapper.sh`. Always
  `-nographics` + `-logFile`; `cygpath -w` for Windows paths. Exit 0 = pass, 2 = fail;
  always confirm a batchmode log actually reached script compilation/test execution.
- Existing state before this plan: `Assets/Scripts/Systems/{Health,HealthComponent,
  Cooldown}.cs` + `ArenaSurvivor.Systems.asmdef` (references: [], autoReferenced:
  true); `Assets/Scripts/Enemies/{EnemyChase,EnemyChaseComponent,EnemyContactDamage,
  EnemyDeathReaction}.cs` + `ArenaSurvivor.Enemies.asmdef` (references:
  ["ArenaSurvivor.Systems"]); `Assets/Scripts/Weapons/*` +
  `ArenaSurvivor.Weapons.asmdef` (references: ["ArenaSurvivor.Systems",
  "ArenaSurvivor.Enemies"]); `ArenaSurvivor.Tests.EditMode.asmdef` (references:
  ["ArenaSurvivor.Player", "ArenaSurvivor.Systems", "ArenaSurvivor.Enemies",
  "ArenaSurvivor.Weapons", "UnityEngine.TestRunner", "UnityEditor.TestRunner"]).
  `Assets/Prefabs/Enemy.prefab` exists with `EnemyChaseComponent`,
  `EnemyContactDamage`, `EnemyDeathReaction`, `HealthComponent`(30).
  `Assets/Sprites/PlaceholderSquare.png` exists. The `com.unity.ugui` package's
  runtime assembly is named `UnityEngine.UI` (confirmed via package cache).
  Current EditMode test count: 33.

---

## Task 1: `PlayerLeveling` + `ExperienceOrb` (plain classes) + tests

**Files:**
- Create: `Assets/Scripts/Systems/PlayerLeveling.cs`
- Create: `Assets/Scripts/Systems/ExperienceOrb.cs`
- Create: `Assets/Tests/EditMode/PlayerLevelingTests.cs`
- Create: `Assets/Tests/EditMode/ExperienceOrbTests.cs`

**Interfaces:**
- Produces: `ArenaSurvivor.Systems.PlayerLeveling` — parameterless constructor
  (starts at Level 1), `int Level { get; }`, `int CurrentXp { get; }`,
  `int XpToNextLevel { get; }`, `event Action<int> OnLevelUp`,
  `void AddExperience(int amount)`. Curve: `XpToNextLevel = ceil(10 *
  level^1.2)`.
- Produces: `ArenaSurvivor.Systems.ExperienceOrb` — constructor `(int xpValue, float
  magnetRadius)`, `int XpValue { get; }`, `float MagnetRadius { get; }`,
  `Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 playerPosition, float
  magnetSpeed, float deltaTime)`. Both consumed by Task 2's MonoBehaviours.

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/PlayerLevelingTests.cs`:
```csharp
using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class PlayerLevelingTests
    {
        [Test]
        public void BelowThreshold_DoesNotLevelUp()
        {
            var leveling = new PlayerLeveling();
            int levelUpCount = 0;
            leveling.OnLevelUp += _ => levelUpCount++;

            leveling.AddExperience(5);

            Assert.AreEqual(1, leveling.Level);
            Assert.AreEqual(5, leveling.CurrentXp);
            Assert.AreEqual(0, levelUpCount);
        }

        [Test]
        public void CrossingThreshold_LevelsUpOnceWithRemainderCarried()
        {
            var leveling = new PlayerLeveling();
            int newLevel = 0;
            leveling.OnLevelUp += level => newLevel = level;

            leveling.AddExperience(13);

            Assert.AreEqual(2, leveling.Level);
            Assert.AreEqual(2, newLevel);
            Assert.AreEqual(3, leveling.CurrentXp);
        }

        [Test]
        public void LargeXpGain_TriggersMultipleLevelUps()
        {
            var leveling = new PlayerLeveling();
            int levelUpCount = 0;
            leveling.OnLevelUp += _ => levelUpCount++;

            leveling.AddExperience(1000);

            Assert.Greater(levelUpCount, 1);
            Assert.AreEqual(leveling.Level, 1 + levelUpCount);
            Assert.Less(leveling.CurrentXp, leveling.XpToNextLevel);
        }

        [Test]
        public void NonPositiveAmounts_AreIgnored()
        {
            var leveling = new PlayerLeveling();

            leveling.AddExperience(0);
            leveling.AddExperience(-5);

            Assert.AreEqual(0, leveling.CurrentXp);
            Assert.AreEqual(1, leveling.Level);
        }

        [Test]
        public void XpToNextLevel_StrictlyIncreasesWithLevel()
        {
            var leveling = new PlayerLeveling();
            int thresholdAtLevel1 = leveling.XpToNextLevel;

            leveling.AddExperience(thresholdAtLevel1);

            Assert.AreEqual(2, leveling.Level);
            Assert.Greater(leveling.XpToNextLevel, thresholdAtLevel1);
        }
    }
}
```

Create `Assets/Tests/EditMode/ExperienceOrbTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class ExperienceOrbTests
    {
        [Test]
        public void OutsideMagnetRadius_DoesNotMove()
        {
            var orb = new ExperienceOrb(5, 3f);

            Vector2 result = orb.ComputeNextPosition(new Vector2(10f, 0f), Vector2.zero, 8f, 1f);

            Assert.AreEqual(new Vector2(10f, 0f), result);
        }

        [Test]
        public void InsideMagnetRadius_MovesTowardPlayerWithoutOvershooting()
        {
            var orb = new ExperienceOrb(5, 3f);

            Vector2 result = orb.ComputeNextPosition(new Vector2(2f, 0f), Vector2.zero, 100f, 1f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void ExactlyAtMagnetRadius_IsTreatedAsInRange()
        {
            var orb = new ExperienceOrb(5, 3f);

            Vector2 result = orb.ComputeNextPosition(new Vector2(3f, 0f), Vector2.zero, 1f, 1f);

            Assert.AreEqual(new Vector2(2f, 0f), result);
        }
    }
}
```

- [ ] **Step 2: Run to verify they fail**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_red.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_red.log")"
```
Expected: nonzero exit; `PlayerLeveling`/`ExperienceOrb` do not exist.

- [ ] **Step 3: Implement `PlayerLeveling`**

Create `Assets/Scripts/Systems/PlayerLeveling.cs`:
```csharp
using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class PlayerLeveling
    {
        private const float BaseXp = 10f;
        private const float Exponent = 1.2f;

        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }
        public int XpToNextLevel { get; private set; }

        public event Action<int> OnLevelUp;

        public PlayerLeveling()
        {
            XpToNextLevel = ComputeXpToNextLevel(Level);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentXp += amount;

            while (CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                Level++;
                XpToNextLevel = ComputeXpToNextLevel(Level);
                OnLevelUp?.Invoke(Level);
            }
        }

        private static int ComputeXpToNextLevel(int level)
        {
            return Mathf.CeilToInt(BaseXp * Mathf.Pow(level, Exponent));
        }
    }
}
```

- [ ] **Step 4: Implement `ExperienceOrb`**

Create `Assets/Scripts/Systems/ExperienceOrb.cs`:
```csharp
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class ExperienceOrb
    {
        public int XpValue { get; }
        public float MagnetRadius { get; }

        public ExperienceOrb(int xpValue, float magnetRadius)
        {
            XpValue = xpValue;
            MagnetRadius = magnetRadius;
        }

        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 playerPosition, float magnetSpeed, float deltaTime)
        {
            float distance = Vector2.Distance(currentPosition, playerPosition);
            if (distance > MagnetRadius)
            {
                return currentPosition;
            }

            return Vector2.MoveTowards(currentPosition, playerPosition, magnetSpeed * deltaTime);
        }
    }
}
```

- [ ] **Step 5: Run to verify they pass**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_green.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task1_green.xml | head -1
```
Expected: exit 0, `total="41" passed="41" failed="0"` (33 existing + 5
`PlayerLevelingTests` + 3 `ExperienceOrbTests`).

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Systems/PlayerLeveling.cs Assets/Scripts/Systems/ExperienceOrb.cs Assets/Tests/EditMode/PlayerLevelingTests.cs Assets/Tests/EditMode/ExperienceOrbTests.cs
git status --short
git commit -m "$(cat <<'EOF'
Add PlayerLeveling and ExperienceOrb plain classes with EditMode tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: MonoBehaviour glue — `PlayerLevelingComponent`, `ExperienceOrbComponent`, `EnemyXpDrop`, `XpBarComponent`

**Files:**
- Create: `Assets/Scripts/Systems/PlayerLevelingComponent.cs`
- Create: `Assets/Scripts/Systems/ExperienceOrbComponent.cs`
- Create: `Assets/Scripts/Enemies/EnemyXpDrop.cs`
- Create: `Assets/Scripts/UI/ArenaSurvivor.UI.asmdef`
- Create: `Assets/Scripts/UI/XpBarComponent.cs`

**Interfaces:**
- Consumes: `ArenaSurvivor.Systems.PlayerLeveling` / `ExperienceOrb` (Task 1);
  `ArenaSurvivor.Systems.HealthComponent.OnDeath` (existing).
- Produces: `PlayerLevelingComponent` — `public void AddExperience(int)`,
  `public event Action<int> OnLevelUp`, read-only `Level`/`CurrentXp`/
  `XpToNextLevel` properties. Consumed by `ExperienceOrbComponent` (this task) and
  Task 3's scene wiring (added directly to the Player) and `XpBarComponent`.
  `ExperienceOrbComponent`'s pickup check looks for `PlayerLevelingComponent`
  specifically (the Stage-3-bug lesson — see Global Constraints).

No dedicated tests in this task (all four are MonoBehaviours, verified via compile +
full regression).

- [ ] **Step 1: Create the UI assembly definition**

Create `Assets/Scripts/UI/ArenaSurvivor.UI.asmdef`:
```json
{
    "name": "ArenaSurvivor.UI",
    "references": ["ArenaSurvivor.Systems", "UnityEngine.UI"],
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

- [ ] **Step 2: Implement `PlayerLevelingComponent`**

Create `Assets/Scripts/Systems/PlayerLevelingComponent.cs`:
```csharp
using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class PlayerLevelingComponent : MonoBehaviour
    {
        private PlayerLeveling leveling;

        public event Action<int> OnLevelUp;

        public int Level => leveling.Level;
        public int CurrentXp => leveling.CurrentXp;
        public int XpToNextLevel => leveling.XpToNextLevel;

        private void Awake()
        {
            leveling = new PlayerLeveling();
            leveling.OnLevelUp += HandleLevelUp;
        }

        public void AddExperience(int amount)
        {
            leveling.AddExperience(amount);
        }

        private void HandleLevelUp(int newLevel)
        {
            OnLevelUp?.Invoke(newLevel);
        }
    }
}
```

- [ ] **Step 3: Implement `ExperienceOrbComponent`**

Create `Assets/Scripts/Systems/ExperienceOrbComponent.cs`:
```csharp
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class ExperienceOrbComponent : MonoBehaviour
    {
        [SerializeField] private int xpValue = 5;
        [SerializeField] private float magnetRadius = 3f;
        [SerializeField] private float magnetSpeed = 8f;

        private ExperienceOrb orb;
        private PlayerLevelingComponent playerLeveling;

        private void Awake()
        {
            orb = new ExperienceOrb(xpValue, magnetRadius);
            playerLeveling = Object.FindFirstObjectByType<PlayerLevelingComponent>();
        }

        private void Update()
        {
            if (playerLeveling == null)
            {
                return;
            }

            Vector2 nextPosition = orb.ComputeNextPosition(transform.position, playerLeveling.transform.position, magnetSpeed, Time.deltaTime);
            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerLevelingComponent otherLeveling = other.GetComponent<PlayerLevelingComponent>();
            if (otherLeveling == null)
            {
                return;
            }

            otherLeveling.AddExperience(orb.XpValue);
            Destroy(gameObject);
        }
    }
}
```

- [ ] **Step 4: Implement `EnemyXpDrop`**

Create `Assets/Scripts/Enemies/EnemyXpDrop.cs`:
```csharp
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Enemies
{
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyXpDrop : MonoBehaviour
    {
        [SerializeField] private GameObject xpOrbPrefab;

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
            if (xpOrbPrefab != null)
            {
                Instantiate(xpOrbPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
```

- [ ] **Step 5: Implement `XpBarComponent`**

Create `Assets/Scripts/UI/XpBarComponent.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.UI
{
    public class XpBarComponent : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Text levelText;

        private PlayerLevelingComponent playerLeveling;

        private void Awake()
        {
            playerLeveling = Object.FindFirstObjectByType<PlayerLevelingComponent>();
        }

        private void Update()
        {
            if (playerLeveling == null)
            {
                return;
            }

            fillImage.fillAmount = (float)playerLeveling.CurrentXp / playerLeveling.XpToNextLevel;
            levelText.text = "Lv. " + playerLeveling.Level;
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
Expected: both exit 0, `total="41" passed="41" failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Systems/PlayerLevelingComponent.cs Assets/Scripts/Systems/ExperienceOrbComponent.cs Assets/Scripts/Enemies/EnemyXpDrop.cs Assets/Scripts/UI
git status --short
git commit -m "$(cat <<'EOF'
Add leveling/orb MonoBehaviours and a basic XP bar UI component

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Wire XP/leveling into scene and prefabs via an Editor script

**Files:**
- Create: `Assets/Editor/Stage4SceneBuilder.cs`
- Modify (via script): `Assets/Prefabs/Enemy.prefab`
- Generate (via script): `Assets/Prefabs/XpOrb.prefab`, updated
  `Assets/Scenes/Arena.unity` (Player gains `PlayerLevelingComponent`, new
  `XpBarCanvas` hierarchy added)

**Interfaces:**
- Consumes: `ArenaSurvivor.Systems.PlayerLevelingComponent`, `ExperienceOrbComponent`
  (Task 1/2); `ArenaSurvivor.Enemies.EnemyXpDrop` (Task 2); `ArenaSurvivor.UI.XpBarComponent`
  (Task 2). Relies on `Assets/Prefabs/Enemy.prefab` and the `Player` GameObject in
  `Assets/Scenes/Arena.unity` from earlier stages, and
  `Assets/Sprites/PlaceholderSquare.png` from Stage 1.

- [ ] **Step 1: Write the Editor script**

Create `Assets/Editor/Stage4SceneBuilder.cs`:
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ArenaSurvivor.Systems;
using ArenaSurvivor.Enemies;
using ArenaSurvivor.UI;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage4SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string SpritePath = "Assets/Sprites/PlaceholderSquare.png";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string XpOrbPrefabPath = "Assets/Prefabs/XpOrb.prefab";

        [MenuItem("Arena Survivor/Build Stage 4 XP And Leveling")]
        public static void AddXpAndLeveling()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            GameObject xpOrbPrefab = CreateXpOrbPrefab();
            AddXpDropToEnemyPrefab(xpOrbPrefab);

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Arena Survivor: Stage4SceneBuilder could not find a GameObject named 'Player' in " + ScenePath);
                return;
            }

            if (player.GetComponent<PlayerLevelingComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: Player already has a PlayerLevelingComponent — Stage 4 wiring already applied, skipping.");
                return;
            }

            player.AddComponent<PlayerLevelingComponent>();

            BuildXpBarUI();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 4 XP and leveling wired into " + ScenePath);
        }

        private static void AddXpDropToEnemyPrefab(GameObject xpOrbPrefab)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

            if (prefabRoot.GetComponent<EnemyXpDrop>() == null)
            {
                EnemyXpDrop drop = prefabRoot.AddComponent<EnemyXpDrop>();
                var serialized = new SerializedObject(drop);
                serialized.FindProperty("xpOrbPrefab").objectReferenceValue = xpOrbPrefab;
                serialized.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static GameObject CreateXpOrbPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(XpOrbPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            var orb = new GameObject("XpOrb");
            var renderer = orb.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.2f, 0.9f, 0.9f);
            renderer.sortingOrder = 3;
            orb.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            BoxCollider2D collider = orb.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            orb.AddComponent<ExperienceOrbComponent>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(orb, XpOrbPrefabPath);
            Object.DestroyImmediate(orb);

            return prefab;
        }

        private static void BuildXpBarUI()
        {
            var canvasObject = new GameObject("XpBarCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var backgroundObject = new GameObject("XpBarBackground");
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            Image background = backgroundObject.AddComponent<Image>();
            background.color = new Color(0.15f, 0.15f, 0.15f);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 1f);
            backgroundRect.anchorMax = new Vector2(0.5f, 1f);
            backgroundRect.pivot = new Vector2(0.5f, 1f);
            backgroundRect.anchoredPosition = new Vector2(0f, -20f);
            backgroundRect.sizeDelta = new Vector2(300f, 20f);

            var fillObject = new GameObject("XpBarFill");
            fillObject.transform.SetParent(backgroundObject.transform, false);
            Image fill = fillObject.AddComponent<Image>();
            fill.color = new Color(0.2f, 0.9f, 0.3f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var textObject = new GameObject("XpBarLevelText");
            textObject.transform.SetParent(canvasObject.transform, false);
            Text levelText = textObject.AddComponent<Text>();
            levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelText.fontSize = 18;
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.color = Color.white;
            levelText.text = "Lv. 1";
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -45f);
            textRect.sizeDelta = new Vector2(300f, 25f);

            XpBarComponent xpBar = canvasObject.AddComponent<XpBarComponent>();
            var serialized = new SerializedObject(xpBar);
            serialized.FindProperty("fillImage").objectReferenceValue = fill;
            serialized.FindProperty("levelText").objectReferenceValue = levelText;
            serialized.ApplyModifiedProperties();
        }
    }
}
```

- [ ] **Step 2: Run the script via batchmode**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage4SceneBuilder.AddXpAndLeveling -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage4.log")"
grep "Stage 4 XP and leveling wired" Logs/build_stage4.log
ls Assets/Prefabs/XpOrb.prefab
```
Expected: exit 0, the log line present (confirming the actual add-path ran, not an
idempotency skip — this matters, see Step 4), the prefab file exists.

- [ ] **Step 3: Verify the changes actually landed**

```bash
grep -c "EnemyXpDrop" Assets/Prefabs/Enemy.prefab
grep -c "PlayerLevelingComponent" Assets/Scenes/Arena.unity
grep -c "XpBarCanvas" Assets/Scenes/Arena.unity
grep -c "ExperienceOrbComponent" Assets/Prefabs/XpOrb.prefab
```
Expected: all greater than 0.

- [ ] **Step 4: Confirm idempotency (safe to click twice)**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage4SceneBuilder.AddXpAndLeveling -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage4_rerun.log")"
grep "already has a PlayerLevelingComponent" Logs/build_stage4_rerun.log
grep -c "XpBarCanvas" Assets/Scenes/Arena.unity
```
Expected: the warning line present in the rerun log; the `XpBarCanvas` count in the
scene is unchanged from Step 3 (no duplicate UI added).

- [ ] **Step 5: Re-open headless to confirm no compile/scene errors**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task3_verify.log")"
```
Expected: exit 0, full compile/shutdown cycle (not an early abort).

- [ ] **Step 6: Run the full EditMode suite one final time**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task3_tests.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task3_tests.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task3_tests.xml | head -1
```
Expected: exit 0, `total="41" passed="41" failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Editor/Stage4SceneBuilder.cs Assets/Prefabs/Enemy.prefab Assets/Prefabs/XpOrb.prefab Assets/Scenes/Arena.unity
git status --short
git commit -m "$(cat <<'EOF'
Wire XP orbs, enemy XP drop, player leveling, and XP bar UI into scene/prefabs

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 8: Note for later manual play-test**

Can't verify headlessly: does the XP bar visibly fill and reset correctly across
level-ups, does the "Lv. N" text update, do orbs actually get magnetized and picked
up, does killing an enemy actually spawn a visible orb at its position. Leave a note
for the user.

---

## Self-Review Notes

- **Spec coverage:** `PlayerLeveling`/`ExperienceOrb` (Task 1), MonoBehaviour glue +
  XP bar UI (Task 2), prefab/scene wiring (Task 3) — all spec bullets covered.
- **Placeholder scan:** none — every step has literal runnable code/commands.
- **Type consistency:** `PlayerLevelingComponent.AddExperience(int)` used identically
  by `ExperienceOrbComponent` (Task 2) and nowhere else needing it yet.
  `ExperienceOrb.ComputeNextPosition` signature matches its Task 2 call site exactly.
  `EnemyXpDrop.xpOrbPrefab` field name matches the `SerializedObject.FindProperty`
  call in Task 3's script exactly, same for `XpBarComponent.fillImage`/`levelText`.
- **Trigger-safety check:** `ExperienceOrbComponent.OnTriggerEnter2D` filters by
  `PlayerLevelingComponent` presence specifically (not a broad catch-all), avoiding a
  repeat of Stage 3's self-damage bug class.
