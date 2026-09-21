# Stage 4 — XP and Leveling

> Self-approved by Claude Code per the user's standing authorization to work
> autonomously through the remaining stages.

## Goal
Per SESSION_PLAN.md Stage 4: killing enemies drops XP orbs; collecting enough
triggers a level-up; a basic on-screen XP bar shows progress.

## Folder placement (per CLAUDE.md, not my own choice)
CLAUDE.md's own folder structure comment says `Systems/ (spawner, XP/leveling, game
state)` — so `PlayerLeveling`/`PlayerLevelingComponent` and `ExperienceOrb`/
`ExperienceOrbComponent` all go in `Assets/Scripts/Systems/`, joining the existing
`Health`/`HealthComponent`/`Cooldown`. This means no new asmdef is needed for any of
them — they land in the existing `ArenaSurvivor.Systems.asmdef` (references: []),
already referenced by the test asmdef.

## `PlayerLeveling` (plain) + `PlayerLevelingComponent`
- `PlayerLeveling`: `int Level { get; }` (starts 1), `int CurrentXp { get; }`,
  `int XpToNextLevel { get; }`, `event Action<int> OnLevelUp` (passes the new level).
  `void AddExperience(int amount)`: ignores non-positive amounts; adds to `CurrentXp`;
  while `CurrentXp >= XpToNextLevel`, subtracts the threshold, increments `Level`,
  recomputes `XpToNextLevel`, fires `OnLevelUp` — so one large XP gain can trigger
  multiple level-ups in a single call, each firing the event once.
- Curve: `XpToNextLevel = ceil(10 * level^1.2)`, matching SESSION_PLAN's example
  formula exactly (`base=10, exponent=1.2`).
- `PlayerLevelingComponent`: thin owner, `public void AddExperience(int)`, re-exposes
  `OnLevelUp`, plus read-only passthrough properties (`Level`, `CurrentXp`,
  `XpToNextLevel`) — the XP bar UI needs these to render, and this is the first stage
  that actually needs read access to a Systems component's live state (Stage 3's
  review flagged `HealthComponent` lacking this same kind of accessor; not touching
  `HealthComponent` here — out of scope, no UI needs it yet).

## `ExperienceOrb` (plain) + `ExperienceOrbComponent`
- `ExperienceOrb`: constructor `(int xpValue, float magnetRadius)`, `int XpValue { get;
  }`, `float MagnetRadius { get; }`, `Vector2 ComputeNextPosition(Vector2
  currentPosition, Vector2 playerPosition, float magnetSpeed, float deltaTime)` — if
  the distance to the player exceeds `MagnetRadius`, returns `currentPosition`
  unchanged (orb sits still); otherwise moves toward the player via
  `Vector2.MoveTowards` (never overshoots, same pattern as `EnemyChase`).
- `ExperienceOrbComponent`: no `Rigidbody2D` needed — the orb only needs a trigger
  `Collider2D` to be picked up, and Unity 2D generates trigger events for a
  static-collider-vs-dynamic-rigidbody pair (exactly how the arena's walls already
  block the player without their own `Rigidbody2D`). Caches the player's
  `PlayerLevelingComponent` once in `Awake` (there's only ever one player). Moves via
  direct `transform.position` assignment in `Update` (no physics needed). On
  `OnTriggerEnter2D`, **filters by type first** — checks for a `PlayerLevelingComponent`
  on the other collider before doing anything — applying the lesson from Stage 3's
  critical bug (a blind "any `HealthComponent`/component I touch" check is how the
  player-shoots-itself bug happened; this stage checks for the specific type it
  expects to interact with, same as the fix in `ProjectileComponent`).

## Enemy XP drop
- **`EnemyXpDrop`** (new, `Assets/Scripts/Enemies/`, mirrors `EnemyDeathReaction`'s
  shape as a separate, single-purpose component rather than merging into
  `EnemyDeathReaction`): `[RequireComponent(typeof(HealthComponent))]`, subscribes to
  `OnDeath`, instantiates the XP orb prefab at the enemy's position. Kept separate from
  `EnemyDeathReaction` (destroy) for the same single-responsibility reason
  `PlayerDeathReaction` was kept separate from `HealthComponent` in Stage 2 — both
  components can subscribe to the same `OnDeath` event independently; reading
  `transform.position` in the same frame `Destroy()` was called is safe (Unity defers
  actual teardown to end-of-frame).

## Basic XP bar (uGUI, not UI Toolkit)
`com.unity.ugui` is already installed (kept during Stage 1's package pruning
specifically for this). A plain `Canvas` (Screen Space - Overlay) with a background
bar `Image` and a foreground `Image` (`Image.Type.Filled`, `FillMethod.Horizontal`)
plus a small "Lv. N" `Text` (legacy uGUI `Text`, using Unity's built-in
`LegacyRuntime.ttf` font resource — no TextMeshPro asset-import step needed). No
`EventSystem`/`GraphicRaycaster` interaction is needed yet (a fill bar isn't
interactive) — Stage 5's upgrade-choice buttons will add that when they need it.
- **`XpBarComponent`** (new, `Assets/Scripts/UI/`, new `ArenaSurvivor.UI.asmdef`
  referencing `ArenaSurvivor.Systems` + `UnityEngine.UI`): caches
  `PlayerLevelingComponent` in `Awake`, and in `Update` sets
  `fillImage.fillAmount = CurrentXp / (float)XpToNextLevel` and
  `levelText.text = "Lv. " + Level`. Simple per-frame poll for a smooth-looking bar —
  this is a UI consumer's implementation choice, not a violation of CLAUDE.md's
  "plain classes should be event-driven" rule (that rule is about `PlayerLeveling`'s
  own public API, which already offers both `OnLevelUp` and readable state).

## Scene/prefab changes (Editor script, re-runnable safely per the established pattern)
A new `Stage4SceneBuilder`:
- Creates `Assets/Prefabs/XpOrb.prefab` (small square, cyan-tinted, no `Rigidbody2D`,
  trigger `BoxCollider2D`, `ExperienceOrbComponent`).
- Adds `EnemyXpDrop` to the existing `Assets/Prefabs/Enemy.prefab`, wired to the XP
  orb prefab.
- Adds `PlayerLevelingComponent` to the existing `Player` in `Assets/Scenes/Arena.unity`.
- Builds the Canvas/bar/text UI hierarchy in the scene, wiring `XpBarComponent`'s
  references.
- Same idempotency/null-check/save-prompt guards as Stage 2/3's builders.

## Testing
- **`PlayerLevelingTests`**: XP below threshold doesn't level up; XP crossing the
  threshold levels up exactly once with the correct remainder carried over; one large
  XP gain triggers multiple level-ups, `OnLevelUp` firing once per level gained;
  non-positive amounts are ignored; the curve strictly increases from level to level.
- **`ExperienceOrbTests`**: outside magnet radius, position doesn't change; inside
  radius, moves toward the player without overshooting; exactly at the radius
  boundary is treated as "in range" (inclusive).

## Out of scope
No upgrade-choice screen yet (Stage 5). No spawner/waves (Stage 6). No health bar (not
requested by any stage yet). No orb pooling (Stage 6, same as projectiles).
