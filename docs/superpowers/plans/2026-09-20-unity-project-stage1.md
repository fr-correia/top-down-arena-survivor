# Unity Project Scaffold + Stage 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the Unity project (currently only CLAUDE.md/SESSION_PLAN.md exist at the
repo root) and implement Stage 1 from SESSION_PLAN.md: a controllable square that moves
around an empty arena with a camera that follows it smoothly.

**Architecture:** Gameplay math lives in plain, unit-tested C# classes
(`PlayerMovement`, `CameraFollow`); thin MonoBehaviours forward Unity lifecycle calls
and Input System reads into those plain classes. All GameObjects/prefabs/scene are
constructed by an Editor script run via `-executeMethod`, never hand-written YAML.

**Tech Stack:** Unity 6000.6.2f1, Universal Render Pipeline (2D Renderer), Input System
package (new backend only), Physics2D, Unity Test Framework (NUnit) EditMode tests.

## Global Constraints

- Unity version: **6000.6.2f1** exactly (already installed at
  `C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe`).
- Render pipeline: **URP with 2D Renderer** (not Built-in).
- Input: **Input System package**, active input handling set to new backend only (not
  "both"). No `.inputactions` asset — actions are built in code.
- Physics: **Physics2D** only (`Rigidbody2D`, `Collider2D`).
- Namespace root `ArenaSurvivor.*` matching folder (e.g. `ArenaSurvivor.Player`).
- PascalCase for classes/methods, camelCase for private fields, no `m_`/`_` prefixes.
- Every plain (non-MonoBehaviour) gameplay class with non-trivial logic gets a matching
  EditMode test in `Assets/Tests/EditMode`.
- No hand-edited scene/prefab YAML — build scenes/prefabs via Editor scripts only.
- Project root for the Unity project **is** the repo root: `D:\proj\top-down-arena-survivor`
  (POSIX form in this shell: `/d/proj/top-down-arena-survivor`). Do not create a nested
  subfolder for it.
- Unity CLI invocations in this plan use `-nographics` and always pass a `-logFile` so
  output can be inspected; when passing Windows paths from this (git-bash) shell to
  `Unity.exe`, convert them with `cygpath -w` first — Unity is a native Windows exe.
- Test runner exit codes (verified empirically): **0** = all tests passed, **2** = at
  least one test failed. `-testResults <path>` writes an NUnit XML file with a
  `result="Passed"` or `result="Failed"` attribute on the root `<test-run>` element.

---

## Task 1: Create the Unity project and folder structure

**Files:**
- Create: the Unity project itself, rooted at `D:\proj\top-down-arena-survivor`
  (`Assets/`, `Packages/`, `ProjectSettings/`, etc.)
- Create: `.gitignore`
- Modify: `CLAUDE.md` (fill in the `<FILL IN>` placeholders)
- Create: empty folders `Assets/Scripts/{Player,Enemies,Weapons,Systems,UI,Data}`,
  `Assets/ScriptableObjects/{Enemies,Weapons,Upgrades}`, `Assets/Prefabs`,
  `Assets/Scenes`, `Assets/Tests/EditMode`

**Interfaces:** None — this task produces the project shell that every later task
builds inside.

- [ ] **Step 1: Create the Unity project from the 2D URP template**

Run from the repo root (`/d/proj/top-down-arena-survivor`):

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
TEMPLATE="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/Resources/PackageManager/ProjectTemplates/com.unity.template.2d-cross-platform-2d-7.0.0.tgz"
"$UNITY" -batchmode -nographics -createProject "$(cygpath -w "$(pwd)")" -cloneFromTemplate "$(cygpath -w "$TEMPLATE")" -quit -logFile "$(cygpath -w "$(pwd)/Logs/create_project.log")"
echo "EXIT CODE: $?"
```

Expected: `EXIT CODE: 0`. This template ships with Unity 6000.6.2f1 itself, so its
bundled package versions (URP, Input System, 2D packages, Test Framework) are
guaranteed compatible — do not hand-pick package versions.

Verify `CLAUDE.md`, `SESSION_PLAN.md`, `.git/`, and `docs/` are untouched:

```bash
ls docs/superpowers/specs/ docs/superpowers/plans/
git status --short
```

Expected: both spec and plan files still present; `git status` shows the new Unity
folders as untracked, nothing pre-existing deleted.

- [ ] **Step 2: Strip template bloat (Welcome screen, sample scene, unused input actions asset)**

```bash
rm -rf Assets/Welcome Assets/Welcome.meta
rm -f Assets/Scenes/SampleScene.unity Assets/Scenes/SampleScene.unity.meta
rm -f Assets/Settings/InputSystem_Actions.inputactions Assets/Settings/InputSystem_Actions.inputactions.meta
```

- [ ] **Step 3: Prune the package manifest to what this project actually needs**

Replace `Packages/manifest.json` with:

```json
{
  "dependencies": {
    "com.unity.2d.sprite": "1.0.0",
    "com.unity.2d.tilemap": "1.0.0",
    "com.unity.ide.rider": "3.0.38",
    "com.unity.ide.visualstudio": "2.0.26",
    "com.unity.inputsystem": "1.20.0",
    "com.unity.render-pipelines.universal": "17.6.0",
    "com.unity.test-framework": "1.8.0",
    "com.unity.ugui": "2.6.0",
    "com.unity.modules.accessibility": "1.0.0",
    "com.unity.modules.adaptiveperformance": "1.0.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.physicscore2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tetgen": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.timelinefoundation": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vectorgraphics": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
```

(This drops `com.unity.2d.animation`, `com.unity.2d.aseprite`, `com.unity.2d.psdimporter`,
`com.unity.2d.spriteshape`, `com.unity.2d.tilemap.extras`, `com.unity.2d.tooling`,
`com.unity.collab-proxy`, `com.unity.learn.iet-framework`, `com.unity.visualscripting`,
`com.unity.timeline` — none of it is used by this project.)

- [ ] **Step 4: Add a Unity `.gitignore`**

Create `.gitignore`:

```gitignore
# Unity generated
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Mm]emoryCaptures/

# Asset meta backups
*.tmp

# Visual Studio / Rider
.vs/
.idea/
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# OS
.DS_Store
Thumbs.db

# Crashlogs
sysinfo.txt
crashlytics-build.properties
```

- [ ] **Step 5: Re-open the project headless to confirm a clean compile after pruning**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -quit -logFile "$(cygpath -w "$(pwd)/Logs/verify_step1.log")"
echo "EXIT CODE: $?"
```

Expected: `EXIT CODE: 0`. If not 0, print the log and fix before proceeding —
`grep -i error "Logs/verify_step1.log"` shows the compiler errors.

- [ ] **Step 6: Create the folder structure from CLAUDE.md**

```bash
mkdir -p Assets/Scripts/Player Assets/Scripts/Enemies Assets/Scripts/Weapons \
         Assets/Scripts/Systems Assets/Scripts/UI Assets/Scripts/Data \
         Assets/ScriptableObjects/Enemies Assets/ScriptableObjects/Weapons Assets/ScriptableObjects/Upgrades \
         Assets/Prefabs Assets/Tests/EditMode
```

(`Assets/Scenes` already exists from the template.)

- [ ] **Step 7: Fill in CLAUDE.md's placeholders**

In `CLAUDE.md`, replace:
- `- Unity version: <FILL IN — e.g. 6000.0.x LTS>` with
  `- Unity version: 6000.6.2f1`
- `- Render pipeline: <FILL IN — URP 2D recommended for a 2D project>` with
  `- Render pipeline: Universal Render Pipeline (2D Renderer)`
- `## Current status\n<FILL IN as you go — e.g. "Stage 1 (player movement + camera) in progress">`
  with
  `## Current status\nStage 1 (player movement + camera follow) in progress.`

- [ ] **Step 8: Commit**

```bash
git add -A -- Assets Packages ProjectSettings UserSettings.meta .gitignore CLAUDE.md \
  "*.sln" "*.slnx" 2>/dev/null
git add -A -- Assets Packages ProjectSettings .gitignore CLAUDE.md
git status --short
git commit -m "$(cat <<'EOF'
Scaffold Unity project (URP 2D, Input System) and CLAUDE.md folder structure

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

Check `git status --short` before committing — `Library/`, `Temp/`, `Logs/`, and
`UserSettings/` must NOT appear (the `.gitignore` from Step 4 excludes them). If any
of those show up, the `.gitignore` wasn't picked up before `git add`; fix and retry.

---

## Task 2: `PlayerMovement` plain class + tests

**Files:**
- Create: `Assets/Scripts/Player/PlayerMovement.cs`
- Create: `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef`
- Create: `Assets/Tests/EditMode/PlayerMovementTests.cs`

**Interfaces:**
- Produces: `ArenaSurvivor.Player.PlayerMovement` with
  `public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 moveInput, float speed, float deltaTime)`
  — normalizes `moveInput` when its magnitude exceeds 1, so diagonal input moves at the
  same speed as axis-aligned input.

- [ ] **Step 1: Create the EditMode test assembly definition**

Create `Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef`:

```json
{
    "name": "ArenaSurvivor.Tests.EditMode",
    "references": [],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing test**

Create `Assets/Tests/EditMode/PlayerMovementTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.Tests.EditMode
{
    public class PlayerMovementTests
    {
        [Test]
        public void ZeroInput_ProducesNoDisplacement()
        {
            var movement = new PlayerMovement();

            Vector2 result = movement.ComputeNextPosition(Vector2.zero, Vector2.zero, 5f, 1f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void StraightLineInput_MovesBySpeedTimesDeltaTime()
        {
            var movement = new PlayerMovement();

            Vector2 result = movement.ComputeNextPosition(Vector2.zero, Vector2.right, 5f, 0.5f);

            Assert.AreEqual(new Vector2(2.5f, 0f), result);
        }

        [Test]
        public void DiagonalInput_IsNormalized_SoSpeedMatchesAxisAligned()
        {
            var movement = new PlayerMovement();

            Vector2 diagonalResult = movement.ComputeNextPosition(Vector2.zero, new Vector2(1f, 1f), 5f, 1f);
            Vector2 straightResult = movement.ComputeNextPosition(Vector2.zero, Vector2.right, 5f, 1f);

            Assert.AreEqual(straightResult.magnitude, diagonalResult.magnitude, 0.0001f);
        }
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail (class doesn't exist yet)**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_red.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_red.log")"
echo "EXIT CODE: $?"
```

Expected: `EXIT CODE: 2` (or a nonzero compile-error exit) — `grep -i "error CS" Logs/task2_red.log` should show `PlayerMovement` does not exist in the current context.

- [ ] **Step 4: Implement `PlayerMovement`**

Create `Assets/Scripts/Player/PlayerMovement.cs`:

```csharp
using UnityEngine;

namespace ArenaSurvivor.Player
{
    public class PlayerMovement
    {
        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 moveInput, float speed, float deltaTime)
        {
            Vector2 direction = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            return currentPosition + direction * speed * deltaTime;
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task2_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task2_green.log")"
echo "EXIT CODE: $?"
grep -o 'result="[A-Za-z]*"' Logs/task2_green.xml | head -1
```

Expected: `EXIT CODE: 0` and `result="Passed"`.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Player/PlayerMovement.cs Assets/Tests/EditMode/PlayerMovementTests.cs Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef Assets/Tests/EditMode/ArenaSurvivor.Tests.EditMode.asmdef.meta
git commit -m "$(cat <<'EOF'
Add PlayerMovement plain class with EditMode tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

(Unity generates a `.meta` file for the new `.asmdef` and `.cs` files the moment it
imports them — the batchmode run in Step 5 already triggered that import, so the
`.meta` files exist on disk before this commit. Run `git status --short` first and add
any `.meta` siblings you see.)

---

## Task 3: `CameraFollow` plain class + tests

**Files:**
- Create: `Assets/Scripts/Player/CameraFollow.cs`
- Create: `Assets/Tests/EditMode/CameraFollowTests.cs`

**Interfaces:**
- Consumes: nothing from Task 2 (independent plain class).
- Produces: `ArenaSurvivor.Player.CameraFollow` with
  `public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 targetPosition, float followSpeed, float deltaTime)`
  and the bounds-clamped overload
  `public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 targetPosition, float followSpeed, float deltaTime, Bounds bounds)`.
  Both use exponential-decay interpolation (`t = 1 - e^(-followSpeed * deltaTime)`),
  which guarantees the result never overshoots `targetPosition` in a single call.

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/CameraFollowTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.Tests.EditMode
{
    public class CameraFollowTests
    {
        [Test]
        public void MovesTowardTarget()
        {
            var follow = new CameraFollow();
            Vector2 current = Vector2.zero;
            Vector2 target = new Vector2(10f, 0f);

            Vector2 result = follow.ComputeNextPosition(current, target, 5f, 0.1f);

            Assert.Greater(result.x, current.x);
            Assert.LessOrEqual(result.x, target.x);
        }

        [Test]
        public void NeverOvershootsTargetInOneStep()
        {
            var follow = new CameraFollow();
            Vector2 current = Vector2.zero;
            Vector2 target = new Vector2(10f, 0f);

            Vector2 result = follow.ComputeNextPosition(current, target, 50f, 1f);

            Assert.LessOrEqual(Vector2.Distance(result, target), Vector2.Distance(current, target));
        }

        [Test]
        public void ConvergesCloseToTargetAfterManySteps()
        {
            var follow = new CameraFollow();
            Vector2 current = Vector2.zero;
            Vector2 target = new Vector2(10f, 5f);

            for (int i = 0; i < 200; i++)
            {
                current = follow.ComputeNextPosition(current, target, 5f, 0.02f);
            }

            Assert.Less(Vector2.Distance(current, target), 0.01f);
        }

        [Test]
        public void BoundsOverload_ClampsResultInsideBounds()
        {
            var follow = new CameraFollow();
            var bounds = new Bounds(Vector2.zero, new Vector3(4f, 4f, 0f));

            Vector2 result = follow.ComputeNextPosition(Vector2.zero, new Vector2(100f, 100f), 50f, 1f, bounds);

            Assert.LessOrEqual(result.x, bounds.max.x);
            Assert.LessOrEqual(result.y, bounds.max.y);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task3_red.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task3_red.log")"
echo "EXIT CODE: $?"
```

Expected: nonzero exit, `grep -i "error CS" Logs/task3_red.log` shows `CameraFollow`
does not exist.

- [ ] **Step 3: Implement `CameraFollow`**

Create `Assets/Scripts/Player/CameraFollow.cs`:

```csharp
using UnityEngine;

namespace ArenaSurvivor.Player
{
    public class CameraFollow
    {
        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 targetPosition, float followSpeed, float deltaTime)
        {
            float t = 1f - Mathf.Exp(-followSpeed * deltaTime);
            return Vector2.Lerp(currentPosition, targetPosition, t);
        }

        public Vector2 ComputeNextPosition(Vector2 currentPosition, Vector2 targetPosition, float followSpeed, float deltaTime, Bounds bounds)
        {
            Vector2 next = ComputeNextPosition(currentPosition, targetPosition, followSpeed, deltaTime);
            next.x = Mathf.Clamp(next.x, bounds.min.x, bounds.max.x);
            next.y = Mathf.Clamp(next.y, bounds.min.y, bounds.max.y);
            return next;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task3_green.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task3_green.log")"
echo "EXIT CODE: $?"
grep -o 'result="[A-Za-z]*"' Logs/task3_green.xml | head -1
```

Expected: `EXIT CODE: 0`, `result="Passed"`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Player/CameraFollow.cs Assets/Tests/EditMode/CameraFollowTests.cs
git commit -m "$(cat <<'EOF'
Add CameraFollow plain class with EditMode tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: MonoBehaviour glue (`PlayerMovementComponent`, `CameraFollowComponent`)

**Files:**
- Create: `Assets/Scripts/Player/PlayerMovementComponent.cs`
- Create: `Assets/Scripts/Player/CameraFollowComponent.cs`

**Interfaces:**
- Consumes: `ArenaSurvivor.Player.PlayerMovement.ComputeNextPosition(Vector2, Vector2, float, float)`
  from Task 2; `ArenaSurvivor.Player.CameraFollow.ComputeNextPosition(Vector2, Vector2, float, float)`
  from Task 3.
- Produces: `ArenaSurvivor.Player.PlayerMovementComponent` (MonoBehaviour, requires
  `Rigidbody2D`) and `ArenaSurvivor.Player.CameraFollowComponent` (MonoBehaviour) with
  `public void SetTarget(Transform newTarget)` — used by Task 5's scene-builder script
  to wire the camera's target without touching the Inspector.

MonoBehaviours can't be exercised by EditMode NUnit tests without a running scene, so
this task's test is "the project still compiles" — verified in Step 2.

- [ ] **Step 1: Implement the MonoBehaviours**

Create `Assets/Scripts/Player/PlayerMovementComponent.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaSurvivor.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovementComponent : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private Rigidbody2D rb;
        private PlayerMovement playerMovement;
        private InputAction moveAction;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerMovement = new PlayerMovement();

            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
        }

        private void OnEnable()
        {
            moveAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
        }

        private void FixedUpdate()
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector2 nextPosition = playerMovement.ComputeNextPosition(rb.position, input, moveSpeed, Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);
        }
    }
}
```

Create `Assets/Scripts/Player/CameraFollowComponent.cs`:

```csharp
using UnityEngine;

namespace ArenaSurvivor.Player
{
    public class CameraFollowComponent : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float followSpeed = 5f;

        private CameraFollow cameraFollow;

        private void Awake()
        {
            cameraFollow = new CameraFollow();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector2 next = cameraFollow.ComputeNextPosition(transform.position, target.position, followSpeed, Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }
    }
}
```

- [ ] **Step 2: Verify the project still compiles cleanly**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -quit -logFile "$(cygpath -w "$(pwd)/Logs/task4_compile.log")"
echo "EXIT CODE: $?"
```

Expected: `EXIT CODE: 0`.

- [ ] **Step 3: Run the full EditMode suite as a regression check**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task4_tests.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task4_tests.log")"
echo "EXIT CODE: $?"
grep -o 'result="[A-Za-z]*"' Logs/task4_tests.xml | head -1
```

Expected: `EXIT CODE: 0`, `result="Passed"` (all Task 2/3 tests still pass — nothing here
changed their behavior).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Player/PlayerMovementComponent.cs Assets/Scripts/Player/CameraFollowComponent.cs
git commit -m "$(cat <<'EOF'
Add MonoBehaviour glue for player movement and camera follow

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Build the Stage 1 scene via an Editor script

**Files:**
- Create: `Assets/Editor/ArenaSceneBuilder.cs`
- Generate (by running the script, not by hand): `Assets/Sprites/PlaceholderSquare.png`,
  `Assets/Scenes/Arena.unity`

**Interfaces:**
- Consumes: `ArenaSurvivor.Player.PlayerMovementComponent` (Task 4),
  `ArenaSurvivor.Player.CameraFollowComponent.SetTarget(Transform)` (Task 4).
- Produces: `Assets/Scenes/Arena.unity`, containing a `Floor`, four boundary walls
  (`Wall_Top`/`Wall_Bottom`/`Wall_Left`/`Wall_Right`), a `Player` object, and a
  `Main Camera` object whose `CameraFollowComponent` target is the player.

- [ ] **Step 1: Write the scene-builder Editor script**

Create `Assets/Editor/ArenaSceneBuilder.cs`:

```csharp
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArenaSurvivor.Player;

namespace ArenaSurvivor.EditorTools
{
    public static class ArenaSceneBuilder
    {
        private const string SpriteFolder = "Assets/Sprites";
        private const string SpritePath = SpriteFolder + "/PlaceholderSquare.png";
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const float ArenaHalfSize = 10f;
        private const float WallThickness = 0.5f;

        [MenuItem("Arena Survivor/Build Stage 1 Scene")]
        public static void BuildStage1Scene()
        {
            Sprite squareSprite = GetOrCreateSquareSprite();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateSpriteObject("Floor", squareSprite, new Color(0.2f, 0.2f, 0.2f), Vector3.zero, new Vector3(ArenaHalfSize * 2f, ArenaHalfSize * 2f, 1f));

            CreateBoundaryWall(squareSprite, "Wall_Top", new Vector3(0f, ArenaHalfSize, 0f), new Vector2(ArenaHalfSize * 2f, WallThickness));
            CreateBoundaryWall(squareSprite, "Wall_Bottom", new Vector3(0f, -ArenaHalfSize, 0f), new Vector2(ArenaHalfSize * 2f, WallThickness));
            CreateBoundaryWall(squareSprite, "Wall_Left", new Vector3(-ArenaHalfSize, 0f, 0f), new Vector2(WallThickness, ArenaHalfSize * 2f));
            CreateBoundaryWall(squareSprite, "Wall_Right", new Vector3(ArenaHalfSize, 0f, 0f), new Vector2(WallThickness, ArenaHalfSize * 2f));

            GameObject player = CreateSpriteObject("Player", squareSprite, new Color(0.2f, 0.5f, 1f), Vector3.zero, Vector3.one);
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.AddComponent<BoxCollider2D>();
            player.AddComponent<PlayerMovementComponent>();

            var cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            CameraFollowComponent follow = cameraObject.AddComponent<CameraFollowComponent>();
            follow.SetTarget(player.transform);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log("Arena Survivor: Stage 1 scene built at " + ScenePath);
        }

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Color color, Vector3 position, Vector3 scale)
        {
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            go.transform.position = position;
            go.transform.localScale = scale;
            return go;
        }

        private static void CreateBoundaryWall(Sprite sprite, string name, Vector3 position, Vector2 size)
        {
            GameObject wall = CreateSpriteObject(name, sprite, new Color(0.1f, 0.1f, 0.1f), position, new Vector3(size.x, size.y, 1f));
            wall.AddComponent<BoxCollider2D>();
        }

        private static Sprite GetOrCreateSquareSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory(SpriteFolder);
            const int size = 100;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }
    }
}
```

Note: `Assets/Editor` folders are automatically excluded from player builds by Unity —
this is the correct home for editor-only tooling, and it's fine for it to reference
`ArenaSurvivor.Player` types directly since it never ships.

- [ ] **Step 2: Run the scene-builder script via batchmode**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -executeMethod ArenaSurvivor.EditorTools.ArenaSceneBuilder.BuildStage1Scene -quit -logFile "$(cygpath -w "$(pwd)/Logs/build_scene.log")"
echo "EXIT CODE: $?"
grep "Stage 1 scene built" Logs/build_scene.log
```

Expected: `EXIT CODE: 0` and the log line confirming the scene was built. Confirm the
generated files exist:

```bash
ls Assets/Scenes/Arena.unity Assets/Sprites/PlaceholderSquare.png
```

- [ ] **Step 3: Re-open the project headless once more to confirm no errors from the generated scene/asset**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -quit -logFile "$(cygpath -w "$(pwd)/Logs/task5_verify.log")"
echo "EXIT CODE: $?"
```

Expected: `EXIT CODE: 0`.

- [ ] **Step 4: Run the full EditMode suite one final time**

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" -runTests -testPlatform EditMode -testResults "$(cygpath -w "$(pwd)/Logs/task5_tests.xml")" -logFile "$(cygpath -w "$(pwd)/Logs/task5_tests.log")"
echo "EXIT CODE: $?"
grep -o 'result="[A-Za-z]*"' Logs/task5_tests.xml | head -1
```

Expected: `EXIT CODE: 0`, `result="Passed"`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Editor/ArenaSceneBuilder.cs Assets/Scenes/Arena.unity Assets/Sprites/PlaceholderSquare.png ProjectSettings/EditorBuildSettings.asset
git status --short
git add -A -- Assets/Scenes Assets/Sprites Assets/Editor
git commit -m "$(cat <<'EOF'
Build Stage 1 arena scene (player, camera follow, boundary walls) via Editor script

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 6: Hand off for a manual play-test**

This can't be verified headlessly. Tell the user:

> Stage 1 is built. Open the project in Unity Hub (project path
> `D:\proj\top-down-arena-survivor`, version 6000.6.2f1), open
> `Assets/Scenes/Arena.unity`, and press Play. Check: WASD/arrow keys/gamepad left
> stick move the player smoothly in 8 directions at equal speed, the camera keeps the
> player roughly centered without jitter or overshoot, and the player can't push
> through the arena's boundary walls. Report back any Console errors or anything that
> feels wrong.

---

## Self-Review Notes

- **Spec coverage:** project setup (Task 1), `PlayerMovement`/`CameraFollow` plain
  classes + tests (Tasks 2-3), MonoBehaviour glue (Task 4), player/arena/camera scene
  built via Editor script (Task 5), manual play-test handoff (Task 5 Step 6) — all spec
  sections are covered. Stages 2-7 are explicitly out of scope per the spec.
- **Placeholder scan:** no TBD/TODO; every step has literal, runnable code or commands.
- **Type consistency:** `PlayerMovement.ComputeNextPosition` and
  `CameraFollow.ComputeNextPosition` signatures are identical everywhere they're
  declared (Tasks 2-3) and consumed (Task 4). `CameraFollowComponent.SetTarget`
  declared in Task 4 matches its only call site in Task 5.
