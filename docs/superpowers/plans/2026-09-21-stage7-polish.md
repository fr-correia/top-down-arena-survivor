# Stage 7 — Polish Pass Implementation Plan (final stage)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Screen shake on player hit/death, hit-flash on any damaged entity, a simple
main menu + game-over screen (same scene, paused panels, restart via scene reload), a
run timer, and a PlayerPrefs high score.

**Architecture:** `ScreenShake`/`GameFlow`/`HighScore` (plain, tested) live in
`Systems`. `PauseState` (static, `Systems`) centralizes `Time.timeScale` ownership
across the upgrade panel (Stage 5, updated here) and the new menu/game-over panels.
`CameraFollowComponent` (Stage 1) composes an optional `ScreenShakeComponent`'s offset
rather than fighting over `transform.position`. `HitFlashComponent`'s `OnEnable`
doubles as its own pooled-reuse reset (no new abstraction needed).

**Tech Stack:** Unity 6000.6.2f1, uGUI, `UnityEngine.SceneManagement`, `PlayerPrefs`.
No new packages. **Zero new asmdef references needed this stage** — every new file was
placed in a folder whose existing asmdef already covers its dependencies (verified
below in Global Constraints).

## Global Constraints

- Namespace root `ArenaSurvivor.*` matching folder; PascalCase/camelCase, no `m_`/`_`.
- Plain classes hold logic, MonoBehaviours/static wiring are thin — no dedicated
  tests for the latter, consistent with this project's established convention.
- **Camera-shake scope:** shake triggers on the PLAYER taking damage and the PLAYER
  dying — never on enemy deaths (100+ enemies dying per second would make constant
  shake unplayable). Hit-flash stays generic (fires for any `HealthComponent`).
- **`PauseState` is the only thing allowed to set `Time.timeScale` going forward.**
  `UpgradeChoiceComponent` (Stage 5) is updated in this plan to route through it
  instead of setting `Time.timeScale` directly — this closes a gap Stage 5's own
  final review flagged, not just avoids repeating it.
- **Restart reloads the scene** (`SceneManager.LoadScene(SceneManager.GetActiveScene().name)`)
  rather than manually resetting every system's state — simpler and far less
  error-prone for a "simple" game-over screen. Because `PauseState`'s reasons are
  static and survive a same-session scene reload (unlike scene objects, which are
  correctly torn down), `PauseState.ClearAll()` MUST be called before reloading, or
  a stale "gameover"/"menu" reason permanently locks `Time.timeScale` at 0 after
  restart — this is a real bug this plan's own `PauseState` design was built to
  avoid, not a hypothetical one.
- **Idempotency-check safety (Stage 4's lesson, still standing):** never use
  `GetComponent<T>()` as an existence guard when `T` has `[RequireComponent]` unless
  you're checking for that exact type `T` itself (checking for `HitFlashComponent`
  presence to gate adding `HitFlashComponent` is fine; checking for some OTHER type
  as a proxy would not be).
- **Self-verify wiring before saving (Stage 5's lesson, standing pattern):** walk
  every serialized object reference with `SerializedObject`/`SerializedProperty`
  before `SaveScene`, refuse to save if anything is null.
- **Asset-vs-scene-load ordering (Stage 5 AND Stage 6's lesson — this has now bitten
  twice):** any ScriptableObject/asset creation must happen either entirely before
  `EditorSceneManager.OpenScene` with no live reference held across it, or entirely
  after — never create an asset, then open a scene, then try to use the
  already-created asset reference (the scene load's unused-asset-unload pass can
  invalidate it). This stage creates no new assets (`GameFlowComponent` etc. are
  scene-only components, not ScriptableObjects), so this specific hazard doesn't
  apply here, but the ordering discipline still matters for the `EventSystem`/prefab
  work — do all prefab edits and lookups by path (`AssetDatabase.LoadAssetAtPath`)
  either before `OpenScene` or freshly after it, never hold a reference across.
- Unity CLI: wrap invocations in a `.sh` script (`run_unity.sh`), always `-quit` on
  any call that isn't `-runTests` (a past stage hung 90+ minutes from a missing
  `-quit`). `-nographics` + `-logFile` always; `cygpath -w` for Windows paths. Confirm
  logs actually reached compilation/test execution, not an early licensing-handshake
  abort.
- Existing state before this plan: `Assets/Scripts/Systems/{Health,HealthComponent,
  Cooldown,PlayerLeveling,PlayerLevelingComponent,ExperienceOrb,
  ExperienceOrbComponent,UpgradeSelector,PooledObject}.cs`;
  `Assets/Scripts/Player/{PlayerMovement,PlayerMovementComponent,CameraFollow,
  CameraFollowComponent,PlayerDeathReaction}.cs` (`ArenaSurvivor.Player.asmdef`
  references `["Unity.InputSystem", "ArenaSurvivor.Systems"]`); `Assets/Scripts/UI/
  {XpBarComponent,UpgradeChoiceComponent}.cs` (`ArenaSurvivor.UI.asmdef` references
  `["ArenaSurvivor.Systems", "ArenaSurvivor.Data", "UnityEngine.UI"]`). Current
  EditMode test count: 54. **Read the actual current content of any file this plan
  modifies before editing it** (`Health.cs`, `HealthComponent.cs`,
  `CameraFollowComponent.cs`, `UpgradeChoiceComponent.cs`) — adapt to the real file if
  it differs from this plan's assumed shape, but produce the exact interfaces/behavior
  each step specifies.

---

## Task 1: Plain classes — `Health.OnDamaged`, `ScreenShake`, `GameFlow`, `HighScore`

**Files:**
- Modify: `Assets/Scripts/Systems/Health.cs`
- Modify: `Assets/Tests/EditMode/HealthTests.cs` (append, don't remove existing)
- Create: `Assets/Scripts/Systems/ScreenShake.cs`
- Create: `Assets/Tests/EditMode/ScreenShakeTests.cs`
- Create: `Assets/Scripts/Systems/GameFlow.cs`
- Create: `Assets/Tests/EditMode/GameFlowTests.cs`
- Create: `Assets/Scripts/Systems/HighScore.cs`
- Create: `Assets/Tests/EditMode/HighScoreTests.cs`

**Interfaces:**
- Produces: `Health.OnDamaged` (`event Action<int>`, fires with the amount whenever
  `TakeDamage` actually reduces health; a lethal hit fires both `OnDamaged` then
  `OnDeath`). Consumed by Task 2's `HitFlashComponent`/`HealthComponent` forwarder and
  Task 3's `PlayerFeedbackComponent`.
- Produces: `ArenaSurvivor.Systems.ScreenShake.ComputeOffset(float elapsedShakeTime,
  float duration, float magnitude, System.Random random)` → `Vector2`. Consumed by
  Task 2's `ScreenShakeComponent`.
- Produces: `ArenaSurvivor.Systems.GameFlowState` (enum: `MainMenu`, `Playing`,
  `GameOver`) and `ArenaSurvivor.Systems.GameFlow` — `GameFlowState State { get; }`,
  `event Action<GameFlowState> OnStateChanged`, `void StartGame()`, `void EndGame()`,
  `void ReturnToMenu()`. Consumed by Task 3's `GameFlowComponent`.
- Produces: `ArenaSurvivor.Systems.HighScore` — constructor `(Func<float> load,
  Action<float> save)`, `float Best { get; }`, `bool TrySubmit(float newScore)`.
  Consumed by Task 2's `HighScoreComponent`.

- [ ] **Step 1: Read `Health.cs` first**

```bash
cat Assets/Scripts/Systems/Health.cs
```

- [ ] **Step 2: Add `OnDamaged` to `Health`**

Add `public event Action<int> OnDamaged;` alongside the existing `OnDeath` event
declaration. In `TakeDamage`, right after `CurrentHealth = Math.Max(0, CurrentHealth
- amount);` and BEFORE the death check, add `OnDamaged?.Invoke(amount);`. The
resulting method should read like:
```csharp
public void TakeDamage(int amount)
{
    if (amount <= 0 || IsDead)
    {
        return;
    }

    CurrentHealth = Math.Max(0, CurrentHealth - amount);
    OnDamaged?.Invoke(amount);

    if (CurrentHealth == 0)
    {
        IsDead = true;
        OnDeath?.Invoke();
    }
}
```
(Adapt to the real method body if it differs from this — the key requirement is
`OnDamaged` fires with the actual damage amount on every successful hit, before the
death check.)

- [ ] **Step 3: Append tests to `HealthTests.cs`**

```csharp
[Test]
public void OnDamaged_FiresWithCorrectAmount_OnSuccessfulHit()
{
    var health = new Health(100);
    int receivedAmount = 0;
    health.OnDamaged += amount => receivedAmount = amount;

    health.TakeDamage(15);

    Assert.AreEqual(15, receivedAmount);
}

[Test]
public void OnDamaged_DoesNotFire_ForNoOpTakeDamage()
{
    var health = new Health(100);
    int fireCount = 0;
    health.OnDamaged += _ => fireCount++;

    health.TakeDamage(0);
    health.TakeDamage(-5);

    Assert.AreEqual(0, fireCount);
}

[Test]
public void LethalHit_FiresBothOnDamagedAndOnDeath()
{
    var health = new Health(10);
    bool damagedFired = false;
    bool deathFired = false;
    health.OnDamaged += _ => damagedFired = true;
    health.OnDeath += () => deathFired = true;

    health.TakeDamage(10);

    Assert.IsTrue(damagedFired);
    Assert.IsTrue(deathFired);
}
```

- [ ] **Step 4: Write the failing tests for `ScreenShake`**

Create `Assets/Tests/EditMode/ScreenShakeTests.cs`:
```csharp
using System;
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class ScreenShakeTests
    {
        [Test]
        public void ElapsedAtOrPastDuration_ReturnsZero()
        {
            var shake = new ScreenShake();
            var random = new Random(1);

            Vector2 result = shake.ComputeOffset(1f, 1f, 0.5f, random);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void OffsetMagnitude_NeverExceedsMagnitude()
        {
            var shake = new ScreenShake();
            var random = new Random(2);

            for (int i = 0; i < 50; i++)
            {
                float elapsed = i * 0.01f;
                Vector2 offset = shake.ComputeOffset(elapsed, 0.5f, 0.3f, random);

                Assert.LessOrEqual(Mathf.Abs(offset.x), 0.3f);
                Assert.LessOrEqual(Mathf.Abs(offset.y), 0.3f);
            }
        }

        [Test]
        public void OffsetMagnitude_ShrinksAsElapsedApproachesDuration()
        {
            var shake = new ScreenShake();
            var earlyRandom = new Random(42);
            var lateRandom = new Random(42);

            Vector2 earlyOffset = shake.ComputeOffset(0f, 1f, 1f, earlyRandom);
            Vector2 lateOffset = shake.ComputeOffset(0.95f, 1f, 1f, lateRandom);

            Assert.Greater(earlyOffset.magnitude, lateOffset.magnitude);
        }
    }
}
```

- [ ] **Step 5: Run to verify it fails**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_red.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_red.log")"
```
Expected: nonzero exit; `ScreenShake` does not exist (the `Health`/`GameFlow`/
`HighScore` tests you'll add in later steps will also fail to compile at this point —
that's expected, this is one combined RED state for the whole task).

- [ ] **Step 6: Implement `ScreenShake`**

Create `Assets/Scripts/Systems/ScreenShake.cs`:
```csharp
using System;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class ScreenShake
    {
        public Vector2 ComputeOffset(float elapsedShakeTime, float duration, float magnitude, Random random)
        {
            if (elapsedShakeTime >= duration || duration <= 0f)
            {
                return Vector2.zero;
            }

            float remainingFraction = 1f - (elapsedShakeTime / duration);
            float x = ((float)random.NextDouble() * 2f - 1f) * magnitude * remainingFraction;
            float y = ((float)random.NextDouble() * 2f - 1f) * magnitude * remainingFraction;
            return new Vector2(x, y);
        }
    }
}
```

- [ ] **Step 7: Implement `GameFlow`**

Create `Assets/Scripts/Systems/GameFlow.cs`:
```csharp
using System;

namespace ArenaSurvivor.Systems
{
    public enum GameFlowState
    {
        MainMenu,
        Playing,
        GameOver
    }

    public class GameFlow
    {
        public GameFlowState State { get; private set; } = GameFlowState.MainMenu;

        public event Action<GameFlowState> OnStateChanged;

        public void StartGame()
        {
            if (State != GameFlowState.MainMenu)
            {
                return;
            }

            SetState(GameFlowState.Playing);
        }

        public void EndGame()
        {
            if (State != GameFlowState.Playing)
            {
                return;
            }

            SetState(GameFlowState.GameOver);
        }

        public void ReturnToMenu()
        {
            SetState(GameFlowState.MainMenu);
        }

        private void SetState(GameFlowState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
```

Create `Assets/Tests/EditMode/GameFlowTests.cs`:
```csharp
using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class GameFlowTests
    {
        [Test]
        public void StartGame_FromMainMenu_TransitionsToPlaying_FiresEventOnce()
        {
            var flow = new GameFlow();
            int eventCount = 0;
            GameFlowState lastState = GameFlowState.MainMenu;
            flow.OnStateChanged += state => { eventCount++; lastState = state; };

            flow.StartGame();

            Assert.AreEqual(GameFlowState.Playing, flow.State);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(GameFlowState.Playing, lastState);
        }

        [Test]
        public void StartGame_FromNonMainMenuState_IsNoOp()
        {
            var flow = new GameFlow();
            flow.StartGame();
            int eventCount = 0;
            flow.OnStateChanged += _ => eventCount++;

            flow.StartGame();

            Assert.AreEqual(GameFlowState.Playing, flow.State);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void EndGame_FromPlaying_TransitionsToGameOver()
        {
            var flow = new GameFlow();
            flow.StartGame();

            flow.EndGame();

            Assert.AreEqual(GameFlowState.GameOver, flow.State);
        }

        [Test]
        public void EndGame_FromNonPlayingState_IsNoOp()
        {
            var flow = new GameFlow();

            flow.EndGame();

            Assert.AreEqual(GameFlowState.MainMenu, flow.State);
        }

        [Test]
        public void ReturnToMenu_AlwaysLandsOnMainMenu()
        {
            var flow = new GameFlow();
            flow.StartGame();
            flow.EndGame();

            flow.ReturnToMenu();

            Assert.AreEqual(GameFlowState.MainMenu, flow.State);
        }
    }
}
```

- [ ] **Step 8: Implement `HighScore`**

Create `Assets/Scripts/Systems/HighScore.cs`:
```csharp
using System;

namespace ArenaSurvivor.Systems
{
    public class HighScore
    {
        private readonly Func<float> load;
        private readonly Action<float> save;

        public HighScore(Func<float> load, Action<float> save)
        {
            this.load = load;
            this.save = save;
        }

        public float Best => load();

        public bool TrySubmit(float newScore)
        {
            if (newScore <= Best)
            {
                return false;
            }

            save(newScore);
            return true;
        }
    }
}
```

Create `Assets/Tests/EditMode/HighScoreTests.cs`:
```csharp
using NUnit.Framework;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Tests.EditMode
{
    public class HighScoreTests
    {
        [Test]
        public void TrySubmit_SavesAndReturnsTrue_WhenNewScoreExceedsBest()
        {
            float stored = 10f;
            var highScore = new HighScore(() => stored, v => stored = v);

            bool result = highScore.TrySubmit(20f);

            Assert.IsTrue(result);
            Assert.AreEqual(20f, stored);
        }

        [Test]
        public void TrySubmit_DoesNotSave_WhenNewScoreDoesNotExceedBest()
        {
            float stored = 10f;
            var highScore = new HighScore(() => stored, v => stored = v);

            bool result = highScore.TrySubmit(5f);

            Assert.IsFalse(result);
            Assert.AreEqual(10f, stored);
        }

        [Test]
        public void Best_ReflectsInjectedLoadFunction()
        {
            var highScore = new HighScore(() => 42f, v => { });

            Assert.AreEqual(42f, highScore.Best);
        }
    }
}
```

- [ ] **Step 9: Run to verify everything passes**

```bash
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task1_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task1_green.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task1_green.xml | head -1
```
Expected: exit 0, `total="68" passed="68" failed="0"` (54 existing + 3 `Health` + 3
`ScreenShake` + 5 `GameFlow` + 3 `HighScore`).

- [ ] **Step 10: Commit**

```bash
git add Assets/Scripts/Systems/Health.cs Assets/Tests/EditMode/HealthTests.cs Assets/Scripts/Systems/ScreenShake.cs Assets/Tests/EditMode/ScreenShakeTests.cs Assets/Scripts/Systems/GameFlow.cs Assets/Tests/EditMode/GameFlowTests.cs Assets/Scripts/Systems/HighScore.cs Assets/Tests/EditMode/HighScoreTests.cs
git status --short
git commit -m "$(cat <<'EOF'
Add Health.OnDamaged, ScreenShake, GameFlow, and HighScore plain classes

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: `PauseState`, `ScreenShakeComponent`, `HitFlashComponent`, `RunTimerComponent`, `HighScoreComponent`

**Files:**
- Create: `Assets/Scripts/Systems/PauseState.cs`
- Create: `Assets/Scripts/Player/ScreenShakeComponent.cs`
- Modify: `Assets/Scripts/Player/CameraFollowComponent.cs`
- Create: `Assets/Scripts/Systems/HitFlashComponent.cs`
- Create: `Assets/Scripts/UI/RunTimerComponent.cs`
- Create: `Assets/Scripts/Systems/HighScoreComponent.cs`
- Modify: `Assets/Scripts/UI/UpgradeChoiceComponent.cs`

**Interfaces:**
- Produces: `ArenaSurvivor.Systems.PauseState` (static) — `bool IsPaused`, `void
  Pause(string reason)`, `void Resume(string reason)`, `void ClearAll()`. Consumed by
  the `UpgradeChoiceComponent` update in this task and Task 3's `GameFlowComponent`.
- Produces: `ArenaSurvivor.Player.ScreenShakeComponent` — `void Shake(float duration,
  float magnitude)`, `Vector3 GetCurrentOffset()`. Consumed by
  `CameraFollowComponent` (this task) and Task 3's `PlayerFeedbackComponent`.
- Produces: `ArenaSurvivor.UI.RunTimerComponent` — `float ElapsedSeconds`, `void
  StartTimer()`, `void StopTimer()`. Consumed by Task 3's `GameFlowComponent`.
- Produces: `ArenaSurvivor.Systems.HighScoreComponent` — `float Best`, `bool
  TrySubmit(float score)`. Consumed by Task 3's `GameFlowComponent`.

- [ ] **Step 1: Implement `PauseState`**

Create `Assets/Scripts/Systems/PauseState.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public static class PauseState
    {
        private static readonly HashSet<string> activeReasons = new HashSet<string>();

        public static bool IsPaused => activeReasons.Count > 0;

        public static void Pause(string reason)
        {
            activeReasons.Add(reason);
            Time.timeScale = 0f;
        }

        public static void Resume(string reason)
        {
            activeReasons.Remove(reason);
            if (activeReasons.Count == 0)
            {
                Time.timeScale = 1f;
            }
        }

        public static void ClearAll()
        {
            activeReasons.Clear();
            Time.timeScale = 1f;
        }
    }
}
```

- [ ] **Step 2: Implement `ScreenShakeComponent`**

Create `Assets/Scripts/Player/ScreenShakeComponent.cs`:
```csharp
using System;
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Player
{
    public class ScreenShakeComponent : MonoBehaviour
    {
        private ScreenShake shake;
        private Random random;
        private float shakeDuration;
        private float shakeMagnitude;
        private float shakeElapsed;
        private Vector3 currentOffset;

        private void Awake()
        {
            shake = new ScreenShake();
            random = new Random();
        }

        public void Shake(float duration, float magnitude)
        {
            shakeDuration = duration;
            shakeMagnitude = magnitude;
            shakeElapsed = 0f;
        }

        public Vector3 GetCurrentOffset()
        {
            return currentOffset;
        }

        private void Update()
        {
            if (shakeElapsed < shakeDuration)
            {
                shakeElapsed += Time.deltaTime;
                Vector2 offset = shake.ComputeOffset(shakeElapsed, shakeDuration, shakeMagnitude, random);
                currentOffset = new Vector3(offset.x, offset.y, 0f);
            }
            else
            {
                currentOffset = Vector3.zero;
            }
        }
    }
}
```

- [ ] **Step 3: Read `CameraFollowComponent.cs`, then compose the shake offset**

```bash
cat Assets/Scripts/Player/CameraFollowComponent.cs
```
Add a private field `private ScreenShakeComponent screenShake;`, cache it in `Awake`
via `screenShake = GetComponent<ScreenShakeComponent>();` (it may be null if the
Main Camera doesn't have one yet — that's fine, handled below). In `LateUpdate`,
change the final position write from directly assigning `transform.position` to
composing the shake offset first:
```csharp
private void LateUpdate()
{
    if (target == null)
    {
        return;
    }

    Vector2 next = cameraFollow.ComputeNextPosition(transform.position, target.position, followSpeed, Time.deltaTime);
    Vector3 finalPosition = new Vector3(next.x, next.y, transform.position.z);

    if (screenShake != null)
    {
        finalPosition += screenShake.GetCurrentOffset();
    }

    transform.position = finalPosition;
}
```
(Adapt variable names to the real file if they differ — the key requirement is
`CameraFollowComponent` remains the sole writer of `transform.position`, adding the
shake offset on top of the computed follow position.)

- [ ] **Step 4: Implement `HitFlashComponent`**

Create `Assets/Scripts/Systems/HitFlashComponent.cs`:
```csharp
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(HealthComponent))]
    public class HitFlashComponent : MonoBehaviour
    {
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.1f;

        private SpriteRenderer spriteRenderer;
        private HealthComponent healthComponent;
        private Color originalColor;
        private float flashTimeRemaining;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            healthComponent = GetComponent<HealthComponent>();
            originalColor = spriteRenderer.color;
        }

        private void OnEnable()
        {
            flashTimeRemaining = 0f;
            spriteRenderer.color = originalColor;
            healthComponent.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            healthComponent.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(int amount)
        {
            flashTimeRemaining = flashDuration;
            spriteRenderer.color = flashColor;
        }

        private void Update()
        {
            if (flashTimeRemaining <= 0f)
            {
                return;
            }

            flashTimeRemaining -= Time.deltaTime;
            if (flashTimeRemaining <= 0f)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }
}
```

- [ ] **Step 5: Implement `RunTimerComponent`**

Create `Assets/Scripts/UI/RunTimerComponent.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvivor.UI
{
    public class RunTimerComponent : MonoBehaviour
    {
        [SerializeField] private Text timerText;

        private float elapsed;
        private bool running;

        public float ElapsedSeconds => elapsed;

        public void StartTimer()
        {
            elapsed = 0f;
            running = true;
        }

        public void StopTimer()
        {
            running = false;
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            elapsed += Time.deltaTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
```

- [ ] **Step 6: Implement `HighScoreComponent`**

Create `Assets/Scripts/Systems/HighScoreComponent.cs`:
```csharp
using UnityEngine;

namespace ArenaSurvivor.Systems
{
    public class HighScoreComponent : MonoBehaviour
    {
        private const string PlayerPrefsKey = "HighScoreSeconds";

        private HighScore highScore;

        public float Best => highScore.Best;

        private void Awake()
        {
            highScore = new HighScore(
                () => PlayerPrefs.GetFloat(PlayerPrefsKey, 0f),
                value =>
                {
                    PlayerPrefs.SetFloat(PlayerPrefsKey, value);
                    PlayerPrefs.Save();
                });
        }

        public bool TrySubmit(float score)
        {
            return highScore.TrySubmit(score);
        }
    }
}
```

- [ ] **Step 7: Read `UpgradeChoiceComponent.cs`, then route its pause through `PauseState`**

```bash
cat Assets/Scripts/UI/UpgradeChoiceComponent.cs
```
Find the line that sets `Time.timeScale = 0f;` (in the method that shows the panel
on level-up) and replace it with `PauseState.Pause("upgrade");`. Find the line that
sets `Time.timeScale = 1f;` (in the method that applies the chosen upgrade and hides
the panel) and replace it with `PauseState.Resume("upgrade");`. `ArenaSurvivor.Systems`
should already be imported in this file (it already uses `PlayerLevelingComponent`
from that namespace) — if not, add `using ArenaSurvivor.Systems;`.

- [ ] **Step 8: Verify compile + full regression**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task2_compile.log")"
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_regression.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_regression.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task2_regression.xml | head -1
```
Expected: both exit 0, `total="68" passed="68" failed="0"`.

- [ ] **Step 9: Commit**

```bash
git add Assets/Scripts/Systems/PauseState.cs Assets/Scripts/Player/ScreenShakeComponent.cs Assets/Scripts/Player/CameraFollowComponent.cs Assets/Scripts/Systems/HitFlashComponent.cs Assets/Scripts/UI/RunTimerComponent.cs Assets/Scripts/Systems/HighScoreComponent.cs Assets/Scripts/UI/UpgradeChoiceComponent.cs
git status --short
git commit -m "$(cat <<'EOF'
Add PauseState, screen shake composition, hit-flash, run timer, high score

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: `GameFlowComponent` + `PlayerFeedbackComponent`

**Files:**
- Create: `Assets/Scripts/UI/GameFlowComponent.cs`
- Create: `Assets/Scripts/Player/PlayerFeedbackComponent.cs`

**Interfaces:**
- Consumes: `ArenaSurvivor.Systems.{GameFlow,GameFlowState,PauseState,
  HighScoreComponent}`, `ArenaSurvivor.UI.RunTimerComponent`,
  `ArenaSurvivor.Systems.HealthComponent` (Tasks 1-2).
- Produces: `ArenaSurvivor.UI.GameFlowComponent` — `[SerializeField] GameObject
  mainMenuPanel`, `gameOverPanel`; `[SerializeField] Text finalTimeText`,
  `bestTimeText`; `[SerializeField] RunTimerComponent runTimer`; `[SerializeField]
  HighScoreComponent highScoreComponent`; `[SerializeField] HealthComponent
  playerHealth`; `public void StartGame()`, `public void RestartGame()`. Consumed by
  Task 4's scene-wiring script (which sets all the serialized references and wires
  `StartGame`/`RestartGame` to button `onClick`).
- Produces: `ArenaSurvivor.Player.PlayerFeedbackComponent` — no public API beyond
  Unity lifecycle; consumed only by Task 4 adding it to the Player.

- [ ] **Step 1: Implement `GameFlowComponent`**

Create `Assets/Scripts/UI/GameFlowComponent.cs`:
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.UI
{
    public class GameFlowComponent : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text finalTimeText;
        [SerializeField] private Text bestTimeText;
        [SerializeField] private RunTimerComponent runTimer;
        [SerializeField] private HighScoreComponent highScoreComponent;
        [SerializeField] private HealthComponent playerHealth;

        private GameFlow flow;

        private void Awake()
        {
            flow = new GameFlow();
            flow.OnStateChanged += HandleStateChanged;
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath += HandlePlayerDeath;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath -= HandlePlayerDeath;
            }
        }

        private void Start()
        {
            HandleStateChanged(flow.State);
        }

        public void StartGame()
        {
            PauseState.Resume("menu");
            flow.StartGame();
        }

        public void RestartGame()
        {
            PauseState.ClearAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void HandlePlayerDeath()
        {
            flow.EndGame();
        }

        private void HandleStateChanged(GameFlowState state)
        {
            mainMenuPanel.SetActive(state == GameFlowState.MainMenu);
            gameOverPanel.SetActive(state == GameFlowState.GameOver);

            switch (state)
            {
                case GameFlowState.MainMenu:
                    PauseState.Pause("menu");
                    break;
                case GameFlowState.Playing:
                    runTimer.StartTimer();
                    break;
                case GameFlowState.GameOver:
                    runTimer.StopTimer();
                    bool isNewBest = highScoreComponent.TrySubmit(runTimer.ElapsedSeconds);
                    finalTimeText.text = FormatTime(runTimer.ElapsedSeconds);
                    bestTimeText.text = "Best: " + FormatTime(highScoreComponent.Best) + (isNewBest ? " (New!)" : "");
                    PauseState.Pause("gameover");
                    break;
            }
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return string.Format("{0:00}:{1:00}", minutes, secs);
        }
    }
}
```

- [ ] **Step 2: Implement `PlayerFeedbackComponent`**

Create `Assets/Scripts/Player/PlayerFeedbackComponent.cs`:
```csharp
using UnityEngine;
using ArenaSurvivor.Systems;

namespace ArenaSurvivor.Player
{
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerFeedbackComponent : MonoBehaviour
    {
        [SerializeField] private float hitShakeDuration = 0.15f;
        [SerializeField] private float hitShakeMagnitude = 0.15f;
        [SerializeField] private float deathShakeDuration = 0.4f;
        [SerializeField] private float deathShakeMagnitude = 0.35f;

        private HealthComponent healthComponent;
        private ScreenShakeComponent screenShake;

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
            screenShake = Object.FindFirstObjectByType<ScreenShakeComponent>();
        }

        private void OnEnable()
        {
            healthComponent.OnDamaged += HandleDamaged;
            healthComponent.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            healthComponent.OnDamaged -= HandleDamaged;
            healthComponent.OnDeath -= HandleDeath;
        }

        private void HandleDamaged(int amount)
        {
            if (screenShake != null)
            {
                screenShake.Shake(hitShakeDuration, hitShakeMagnitude);
            }
        }

        private void HandleDeath()
        {
            if (screenShake != null)
            {
                screenShake.Shake(deathShakeDuration, deathShakeMagnitude);
            }
        }
    }
}
```

- [ ] **Step 3: Verify compile + full regression**

```bash
bash run_unity.sh -quit -logFile "$(cygpath -w "$(pwd)/Logs/task3_compile.log")"
bash run_unity.sh -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task3_regression.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task3_regression.log")"
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/task3_regression.xml | head -1
```
Expected: both exit 0, `total="68" passed="68" failed="0"`.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/GameFlowComponent.cs Assets/Scripts/Player/PlayerFeedbackComponent.cs
git status --short
git commit -m "$(cat <<'EOF'
Add GameFlowComponent and PlayerFeedbackComponent

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Wire polish into scene and prefabs via an Editor script

**Files:**
- Create: `Assets/Editor/Stage7SceneBuilder.cs`
- Modify (via script): `Assets/Prefabs/Enemy.prefab`
- Modify (via script): `Assets/Scenes/Arena.unity` (Player gains
  `HitFlashComponent`+`PlayerFeedbackComponent`; Main Camera gains
  `ScreenShakeComponent`; new `FlowCanvas` with main-menu panel, game-over panel, run
  timer text, `GameFlowComponent`/`RunTimerComponent`/`HighScoreComponent`)

**Interfaces:**
- Consumes: all of Tasks 1-3's new MonoBehaviours. Relies on the `Player` GameObject,
  `Camera.main`, and the `EventSystem` (from Stage 5) already existing in
  `Assets/Scenes/Arena.unity`, and `Assets/Prefabs/Enemy.prefab` existing.

- [ ] **Step 1: Write the Editor script**

Create `Assets/Editor/Stage7SceneBuilder.cs`:
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArenaSurvivor.Systems;
using ArenaSurvivor.Player;
using ArenaSurvivor.UI;

namespace ArenaSurvivor.EditorTools
{
    public static class Stage7SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";

        [MenuItem("Arena Survivor/Build Stage 7 Polish")]
        public static void AddPolish()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            AddHitFlashToEnemyPrefab();

            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            if (Object.FindFirstObjectByType<GameFlowComponent>() != null)
            {
                Debug.LogWarning("Arena Survivor: GameFlowComponent already present — Stage 7 wiring already applied, skipping.");
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Arena Survivor: Stage7SceneBuilder could not find a GameObject named 'Player' in " + ScenePath);
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Arena Survivor: Stage7SceneBuilder could not find the Main Camera in " + ScenePath);
                return;
            }

            HealthComponent playerHealth = player.GetComponent<HealthComponent>();

            AddPlayerAndCameraComponents(player, mainCamera.gameObject);
            EnsureEventSystem();

            GameFlowComponent gameFlow = BuildFlowUI(playerHealth);

            if (!VerifyWiring(gameFlow))
            {
                Debug.LogError("Arena Survivor: Stage7SceneBuilder wiring verification failed — scene NOT saved.");
                return;
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 7 polish wired into " + ScenePath);
        }

        private static void AddHitFlashToEnemyPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

            if (prefabRoot.GetComponent<HitFlashComponent>() == null)
            {
                prefabRoot.AddComponent<HitFlashComponent>();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, EnemyPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static void AddPlayerAndCameraComponents(GameObject player, GameObject cameraObject)
        {
            if (player.GetComponent<HitFlashComponent>() == null)
            {
                player.AddComponent<HitFlashComponent>();
            }

            if (player.GetComponent<PlayerFeedbackComponent>() == null)
            {
                player.AddComponent<PlayerFeedbackComponent>();
            }

            if (cameraObject.GetComponent<ScreenShakeComponent>() == null)
            {
                cameraObject.AddComponent<ScreenShakeComponent>();
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static GameFlowComponent BuildFlowUI(HealthComponent playerHealth)
        {
            var canvasObject = new GameObject("FlowCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject mainMenuPanel = BuildMainMenuPanel(canvasObject.transform, out Button startButton);
            GameObject gameOverPanel = BuildGameOverPanel(canvasObject.transform, out Button restartButton, out Text finalTimeText, out Text bestTimeText);
            Text runTimerText = CreateText(canvasObject.transform, "RunTimerText", "00:00", 24, new Vector2(0.9f, 0.95f), new Vector2(200f, 50f));

            RunTimerComponent runTimer = canvasObject.AddComponent<RunTimerComponent>();
            var runTimerSerialized = new SerializedObject(runTimer);
            runTimerSerialized.FindProperty("timerText").objectReferenceValue = runTimerText;
            runTimerSerialized.ApplyModifiedProperties();

            HighScoreComponent highScoreComponent = canvasObject.AddComponent<HighScoreComponent>();

            GameFlowComponent gameFlow = canvasObject.AddComponent<GameFlowComponent>();
            var flowSerialized = new SerializedObject(gameFlow);
            flowSerialized.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            flowSerialized.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            flowSerialized.FindProperty("finalTimeText").objectReferenceValue = finalTimeText;
            flowSerialized.FindProperty("bestTimeText").objectReferenceValue = bestTimeText;
            flowSerialized.FindProperty("runTimer").objectReferenceValue = runTimer;
            flowSerialized.FindProperty("highScoreComponent").objectReferenceValue = highScoreComponent;
            flowSerialized.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            flowSerialized.ApplyModifiedProperties();

            startButton.onClick.AddListener(gameFlow.StartGame);
            restartButton.onClick.AddListener(gameFlow.RestartGame);

            return gameFlow;
        }

        private static GameObject BuildMainMenuPanel(Transform parent, out Button startButton)
        {
            GameObject panel = CreateFullScreenPanel(parent, "MainMenuPanel");

            CreateText(panel.transform, "Title", "Arena Survivor", 48, new Vector2(0.5f, 0.65f), new Vector2(600f, 80f));
            startButton = CreateButton(panel.transform, "StartButton", "Start", new Vector2(0.5f, 0.4f));

            return panel;
        }

        private static GameObject BuildGameOverPanel(Transform parent, out Button restartButton, out Text finalTimeText, out Text bestTimeText)
        {
            GameObject panel = CreateFullScreenPanel(parent, "GameOverPanel");

            CreateText(panel.transform, "Title", "Game Over", 48, new Vector2(0.5f, 0.7f), new Vector2(600f, 80f));
            finalTimeText = CreateText(panel.transform, "FinalTimeText", "00:00", 28, new Vector2(0.5f, 0.55f), new Vector2(400f, 50f));
            bestTimeText = CreateText(panel.transform, "BestTimeText", "Best: 00:00", 22, new Vector2(0.5f, 0.48f), new Vector2(400f, 50f));
            restartButton = CreateButton(panel.transform, "RestartButton", "Play Again", new Vector2(0.5f, 0.3f));

            panel.SetActive(false);

            return panel;
        }

        private static GameObject CreateFullScreenPanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.85f);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchorPosition, Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorPosition;
            rect.anchorMax = anchorPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorPosition)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.3f);
            Button button = buttonObject.AddComponent<Button>();
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorPosition;
            rect.anchorMax = anchorPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(240f, 60f);

            CreateText(buttonObject.transform, "Label", label, 24, new Vector2(0.5f, 0.5f), new Vector2(220f, 50f));

            return button;
        }

        private static bool VerifyWiring(GameFlowComponent gameFlow)
        {
            var serialized = new SerializedObject(gameFlow);
            bool ok = true;

            ok &= VerifyField(serialized, "mainMenuPanel");
            ok &= VerifyField(serialized, "gameOverPanel");
            ok &= VerifyField(serialized, "finalTimeText");
            ok &= VerifyField(serialized, "bestTimeText");
            ok &= VerifyField(serialized, "runTimer");
            ok &= VerifyField(serialized, "highScoreComponent");
            ok &= VerifyField(serialized, "playerHealth");

            return ok;
        }

        private static bool VerifyField(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == null)
            {
                Debug.LogError("Arena Survivor: Stage7SceneBuilder verification failed — '" + propertyName + "' is null.");
                return false;
            }

            return true;
        }
    }
}
```

- [ ] **Step 2: Run the script via batchmode**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage7SceneBuilder.AddPolish -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage7.log")"
grep "Stage 7 polish wired" Logs/build_stage7.log
```
Expected: exit 0, the success line genuinely present (not a skip or a verification
failure — if "wiring verification failed" appears instead, investigate before
assuming anything succeeded).

- [ ] **Step 3: Verify the changes actually landed**

```bash
grep -c "HitFlashComponent" Assets/Prefabs/Enemy.prefab
grep -c "HitFlashComponent\|PlayerFeedbackComponent\|ScreenShakeComponent\|GameFlowComponent\|RunTimerComponent\|HighScoreComponent" Assets/Scenes/Arena.unity
```
Expected: both greater than 0 (the second should be several matches, one per
component type added to the scene).

- [ ] **Step 4: Confirm idempotency (safe to click twice)**

```bash
bash run_unity.sh -executeMethod ArenaSurvivor.EditorTools.Stage7SceneBuilder.AddPolish -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_stage7_rerun.log")"
grep "already present" Logs/build_stage7_rerun.log
grep -c "GameFlowComponent" Assets/Scenes/Arena.unity
```
Expected: the warning line present; the `GameFlowComponent` count unchanged from
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
Expected: exit 0, `total="68" passed="68" failed="0"`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Editor/Stage7SceneBuilder.cs Assets/Prefabs/Enemy.prefab Assets/Scenes/Arena.unity
git status --short
git commit -m "$(cat <<'EOF'
Wire screen shake, hit-flash, main menu, game-over, timer, and high score into scene

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 8: Note for later manual play-test — this is the last stage, be thorough**

Can't verify headlessly: does the main menu actually show on first load with the game
paused, does clicking Start actually unpause and begin the run, does the camera
visibly shake on taking damage and more strongly on death, do enemies/player flash
white on hit (and NOT get stuck flashing red after a pooled enemy dies mid-flash and
respawns), does dying show the game-over panel with the correct final time, does the
high score correctly persist across a restart (and across fully closing/reopening the
Editor, since it's `PlayerPrefs`-backed), does "Play Again" actually reset everything
cleanly via the scene reload (no leftover paused state, no lingering old enemies),
does the run timer count up correctly and stop exactly at death. Leave a thorough
note for the user — this is the last stage, so this is effectively the final
end-to-end play-test of the whole game.

---

## Self-Review Notes

- **Spec coverage:** `Health.OnDamaged`/`ScreenShake`/`GameFlow`/`HighScore` (Task 1),
  `PauseState`/shake composition/hit-flash/run timer/high-score component (Task 2),
  `GameFlowComponent`/`PlayerFeedbackComponent` (Task 3), scene/prefab wiring
  (Task 4) — every spec bullet (screen shake, hit-flash, main menu, game-over, run
  timer, high score) is covered.
- **Placeholder scan:** none — every step has literal runnable code/commands, aside
  from the deliberate "read the real file first" verify-before-edit instructions for
  files this plan didn't create.
- **Type consistency:** `ScreenShakeComponent.Shake(float, float)`/`GetCurrentOffset()`
  used identically by `CameraFollowComponent` (Task 2) and `PlayerFeedbackComponent`
  (Task 3). `GameFlow`'s public API matches between its definition (Task 1) and
  `GameFlowComponent`'s usage (Task 3). `PauseState.Pause`/`Resume`/`ClearAll` used
  consistently across `UpgradeChoiceComponent` (Task 2) and `GameFlowComponent`
  (Task 3) — no code outside these two ever sets `Time.timeScale` directly. Every
  `SerializedObject.FindProperty` name in `Stage7SceneBuilder` (Task 4) matches the
  actual private field name declared in `GameFlowComponent`/`RunTimerComponent`
  (Tasks 2-3) exactly.
- **Known-bug avoidance, explicitly checked:** `RestartGame` calls
  `PauseState.ClearAll()` before the scene reload — without this, a stale "gameover"
  pause reason (static state, survives a same-session scene reload) would
  permanently lock `Time.timeScale` at 0 after the first restart. This was caught
  and designed around during planning, not left for a later review to find.
