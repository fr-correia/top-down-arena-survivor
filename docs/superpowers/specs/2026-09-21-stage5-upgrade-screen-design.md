# Stage 5 — Upgrade Choice Screen

> Self-approved by Claude Code per the user's standing authorization to work
> autonomously through the remaining stages.

## Goal
Per SESSION_PLAN.md Stage 5: leveling up pauses the game (`Time.timeScale = 0`) and
offers 3 random upgrades to pick from, via `Upgrade` ScriptableObjects (name,
description, apply-effect) and a simple 3-button UI. Real upgrades: +damage, +move
speed, +attack cooldown reduction, +max HP.

## `Upgrade` as a ScriptableObject hierarchy (CLAUDE.md rule 2)
CLAUDE.md: "upgrade definitions... should be a ScriptableObject asset, not a number
buried in code." An abstract base + one concrete `[CreateAssetMenu]` subclass per
upgrade type is the idiomatic Unity pattern for "data + a small apply-effect" per
asset — each subclass's *magnitude* is a designer-tunable serialized field, and its
`Apply` method is a one-line forward into an existing component's new public mutator
(thin wiring, not gameplay logic — same bar as why MonoBehaviours don't get dedicated
tests). New folder `Assets/Scripts/Data/` (already named in CLAUDE.md's folder
structure) + new `ArenaSurvivor.Data.asmdef`.

- **`Upgrade`** (abstract, `Assets/Scripts/Data/Upgrade.cs`): `string DisplayName`,
  `string Description` (both serialized), `abstract void Apply(GameObject player)`.
- **`DamageUpgrade`**, **`MoveSpeedUpgrade`**, **`AttackCooldownUpgrade`**,
  **`MaxHealthUpgrade`** — four concrete subclasses, each with one serialized
  magnitude field and an `Apply` that does `player.GetComponent<X>().IncreaseY(amount)`
  (null-checked, matching the codebase's existing guard style).

## Extending existing components to be upgradeable (real, targeted refactors — flagged explicitly, not silent scope creep)
None of the four stats are currently mutable after spawn. Each of these was already
flagged as a known future need by an earlier stage's final review:
- **`ArenaSurvivor.Systems.Health`**: `MaxHealth` becomes `{ get; private set; }` (was
  get-only); new `void IncreaseMaxHealth(int amount)` — increases both `MaxHealth` and
  `CurrentHealth` by the same amount (an instant partial heal on pickup, the standard
  genre convention). `HealthComponent.IncreaseMaxHealth(int)` forwards to it.
- **`ArenaSurvivor.Systems.Cooldown`**: `interval` becomes a mutable `Interval { get;
  private set; }` property; new `void SetInterval(float newInterval)` clamps to a
  floor (0.1s) so an upgrade can never reduce fire rate to zero/negative. Existing
  behavior (`TryConsume`) is unchanged, just reads the property instead of the old
  private field.
- **`ArenaSurvivor.Player.PlayerMovementComponent`**: new `void IncreaseSpeed(float
  amount)` — one-line mutator on the existing private `moveSpeed` field.
- **`ArenaSurvivor.Weapons.AutoAttackComponent`**: damage moves from
  `ProjectileComponent`'s own fixed serialized default to being a property of the
  *attacker* — `[SerializeField] private int baseDamage = 10` + `private int
  damageBonus` (`IncreaseDamage(int)` adds to it), total passed to the projectile at
  launch time. New `void ReduceAttackInterval(float amount)` forwards to
  `cooldown.SetInterval(cooldown.Interval - amount)`.
- **`ArenaSurvivor.Weapons.ProjectileComponent`**: `Launch(Vector2 direction)` becomes
  `Launch(Vector2 direction, int damage)` — the projectile no longer carries its own
  fixed damage default; the shooter (`AutoAttackComponent`) decides damage per shot.
  This is the single call-site change needed for the damage upgrade to actually do
  anything.

## Random selection: `UpgradeSelector` (new plain class)
`Assets/Scripts/Systems/UpgradeSelector.cs`: engine-independent (`System.Random`,
generic `IReadOnlyList<T>`/`List<T>` only — no `UnityEngine` dependency), constructed
with an injected `System.Random` (real randomness at runtime, seeded determinism in
tests). `List<T> SelectRandomUnique<T>(IReadOnlyList<T> pool, int count)` — returns
`min(count, pool.Count)` distinct items (Fisher–Yates-style pick-and-remove), never a
duplicate. Reusable later for Stage 6's wave/spawn-pool selection too.

## UI + pause flow
**`UpgradeChoiceComponent`** (`Assets/Scripts/UI/`): subscribes to the existing
`PlayerLevelingComponent.OnLevelUp` event (Stage 4 already exposes this — no changes
needed there). On level-up: picks 3 upgrades via `UpgradeSelector`, populates 3
buttons' title/description text, shows the panel, sets `Time.timeScale = 0`. On a
button click: calls the chosen `Upgrade.Apply(player)`, hides the panel, resumes
(`Time.timeScale = 1`).

**Input System gotcha (must get right, not discover mid-task):** this project runs
the new Input System exclusively (Stage 1: `activeInputHandler: 1`, no legacy Input
Manager). uGUI's default `EventSystem` + `StandaloneInputModule` reads the *old*
`UnityEngine.Input` API and will not receive clicks. The scene-builder script must
add `UnityEngine.InputSystem.UI.InputSystemUIInputModule` (from the already-installed
Input System package) instead of the legacy module — this type's `Reset()` callback
auto-assigns default UI actions when the component is added via script or Inspector,
so no separate `.inputactions` asset is needed (consistent with Stage 1's
code-only-input approach).

## Scene/asset changes (Editor script, re-runnable safely per the established pattern)
A new `Stage5SceneBuilder`:
- Creates 4 `Upgrade` ScriptableObject assets under `Assets/ScriptableObjects/Upgrades/`
  (per CLAUDE.md's folder structure) via `ScriptableObject.CreateInstance` +
  `AssetDatabase.CreateAsset`.
- Builds a new Canvas with a semi-transparent modal panel (initially hidden), 3
  option buttons (each with a title + description `Text`), and an `EventSystem` +
  `InputSystemUIInputModule` (skipped if an `EventSystem` already exists in the
  scene — Unity warns about duplicates).
- Adds `UpgradeChoiceComponent` to the new canvas, wiring the 4 upgrade assets and
  the 3 buttons' text references.
- Same idempotency/null-check/save-prompt guard pattern as Stages 2-4's builders —
  and, learning from Stage 4's operational incident, any idempotency check here uses
  a plain on-disk/asset-existence check (`AssetDatabase.LoadAssetAtPath` returning
  non-null), never a `GetComponent`-based check on a type that carries
  `[RequireComponent]`, since that pattern was proven unreliable (Unity auto-injects
  required components in memory before they're serialized to disk).

## Testing
- **`UpgradeSelectorTests`**: pool smaller than requested count returns the whole
  pool; requesting fewer than pool size returns exactly that many, all unique, all
  drawn from the pool; running many trials with different seeds never produces a
  duplicate in one call's result.
- **`CooldownTests`** (extended): `SetInterval` changes future `TryConsume` timing
  correctly; a reduction below the floor clamps to the floor, never goes to
  zero/negative.
- **`HealthTests`** (extended): `IncreaseMaxHealth` raises both `MaxHealth` and
  `CurrentHealth` by the given amount; a non-positive amount is a no-op.
- MonoBehaviour mutators (`PlayerMovementComponent.IncreaseSpeed`,
  `AutoAttackComponent.IncreaseDamage`/`ReduceAttackInterval`,
  `HealthComponent.IncreaseMaxHealth`) and the `Upgrade` subclasses' `Apply` methods
  are thin one-line forwarders — no dedicated tests, consistent with the project's
  existing MonoBehaviour-testing convention.

## Out of scope
No spawner/waves (Stage 6). No more than the 4 named upgrades. No upgrade rarity/
weighting system. No "reroll" or "skip" option — SESSION_PLAN only asks for a
3-choice pick.
