# Stage 7 — Polish Pass (final stage)

> Self-approved by Claude Code per the user's standing authorization to work
> autonomously through the remaining stages.

## Goal
Per SESSION_PLAN.md Stage 7 (last stage): screen shake on hit/death, hit-flash
material feedback, a simple main menu + game-over screen, a run timer, and a simple
PlayerPrefs high-score save.

## Scope decision: shake on *player* hit/death, not every enemy death
With 100+ enemies dying per second at late game (Stage 6's own target), shaking the
camera on every enemy death would be unplayable noise. "Hit/death" is read as: the
camera shakes when the **player** takes damage (small shake) and when the **player**
dies (bigger shake) — the impactful, low-frequency events. Hit-flash stays generic
(fires for anyone with `HealthComponent`, enemies included) since visual damage
feedback per-hit at high frequency is fine — it's the camera-wide shake that doesn't
scale.

## `Health` gains an `OnDamaged` event
Needed by both hit-flash and player screen-shake, and flagged as a gap by earlier
stages' reviews (`HealthComponent` previously exposed only `OnDeath`). `Health.TakeDamage`
now fires `event Action<int> OnDamaged` (the amount) whenever it actually reduces
`CurrentHealth`, in addition to the existing `OnDeath` (a lethal hit fires both, in
that order). `HealthComponent` forwards it.

## `ScreenShake` (plain, `Systems`) + `ScreenShakeComponent` (`Player`, composes with `CameraFollowComponent`)
`ScreenShake.ComputeOffset(elapsedShakeTime, duration, magnitude, System.Random)` —
a decaying random offset (linearly shrinks to zero as `elapsedShakeTime` approaches
`duration`), engine-decoupled aside from `Vector2`, injectable `Random` for
deterministic tests. `ScreenShakeComponent` (lives on the Main Camera alongside
`CameraFollowComponent`) owns the timer and exposes `Shake(duration, magnitude)` +
`GetCurrentOffset()`.

**Composition, not a second position-writer:** `CameraFollowComponent` is already the
sole writer of `transform.position` in `LateUpdate`. Rather than have
`ScreenShakeComponent` *also* write `transform.position` (a race with no defined
order between two independent scripts' `LateUpdate`), `CameraFollowComponent` caches
an optional `ScreenShakeComponent` reference and adds its `GetCurrentOffset()` to the
computed follow position before the single `transform.position` write. This is a
small, targeted change to Stage 1's `CameraFollowComponent`, not a rewrite.

## `HitFlashComponent` (new, generic, `Systems`) — doubles as its own pooled-reuse reset
Subscribes to its own `HealthComponent.OnDamaged`, briefly sets its `SpriteRenderer`
to a flash color then reverts. The reset-on-pooled-reuse problem Stage 6's review
flagged ("an enemy that dies mid-flash comes back red") is solved for free: `OnEnable`
already fires every time a pooled object is reactivated (Stage 6 built the whole
registry on this guarantee), so `HitFlashComponent.OnEnable` resets the flash state
and re-subscribes — no separate reset-hook abstraction needed for this one case.

## `PauseState` (new, static, `Systems`) — centralizes `Time.timeScale` ownership
Stage 5's final review flagged this directly: multiple UI systems independently
setting `Time.timeScale = 0`/`1` will have "the first one to resume clobber the
others" the moment a second consumer exists — and this stage adds two more (main menu,
game-over) on top of the existing upgrade-choice pause. A reason-keyed set:
`Pause(string reason)` adds the reason and sets `timeScale = 0`;
`Resume(string reason)` removes it and only restores `timeScale = 1` once no reasons
remain. `UpgradeChoiceComponent` (Stage 5) is updated to call
`PauseState.Pause("upgrade")`/`Resume("upgrade")` instead of setting `Time.timeScale`
directly — closing the gap Stage 5 flagged, not just avoiding repeating it.

## `GameFlow` (plain, `Systems`) + `GameFlowComponent` (`UI`) — menu/playing/game-over
`GameFlow`: `enum GameFlowState { MainMenu, Playing, GameOver }`, `StartGame()`
(MainMenu→Playing, no-op otherwise), `EndGame()` (Playing→GameOver, no-op otherwise),
`ReturnToMenu()` (→MainMenu unconditionally, used after a scene reload), `event
Action<GameFlowState> OnStateChanged`. `GameFlowComponent` owns it, toggles the
MainMenu/GameOver UI panels, calls `PauseState.Pause("menu")`/`Pause("gameover")` (and
`Resume("menu")` on `StartGame`), subscribes to the player's `HealthComponent.OnDeath`
to call `EndGame()`.

**Restart = scene reload, not manual state reset.** Rebuilding every system's state
by hand (pool contents, spawner elapsed time, XP/level, all active enemies/orbs) for
an in-place restart is a large, error-prone undertaking for a "simple" game-over
screen. `SceneManager.LoadScene(SceneManager.GetActiveScene().name)` resets
everything correctly for free, and since the main menu is just a paused UI panel in
the same scene (not a separate scene), a reload naturally lands back at MainMenu,
paused, ready to press Start again. This is the standard, low-risk way this kind of
"simple" restart is done.

## `RunTimerComponent` (`UI`) + `HighScore` (plain, `Systems`) + `HighScoreComponent` (`Systems`)
`RunTimerComponent`: accumulates `Time.deltaTime` while running, formats `MM:SS`,
started/stopped by `GameFlowComponent` on `Playing`/`GameOver`. Kept as its own
concern rather than reusing `EnemySpawnerComponent.elapsedTime` — the spawner's clock
is a gameplay-balance internal, the run timer is a display/high-score concern;
conflating them would be a needless coupling for a superficial numeric match.

`HighScore`: constructor takes injected `Func<float> load` / `Action<float> save`
(so the comparison logic — is this new score actually better? — is unit-testable
without touching real `PlayerPrefs`), `float Best`, `bool TrySubmit(float newScore)`.
`HighScoreComponent` is the one-line adapter wiring those delegates to
`PlayerPrefs.GetFloat`/`SetFloat`/`Save`. On `GameOver`, `GameFlowComponent` submits
the run timer's elapsed seconds.

## `PlayerFeedbackComponent` (new, `Player`) — the player-specific shake trigger
Subscribes to the Player's own `HealthComponent.OnDamaged` (small shake) and
`OnDeath` (bigger shake), calls `ScreenShakeComponent.Shake(...)`. Kept separate from
the generic `HitFlashComponent` (which also fires on the player, since players should
flash too) so the player-only, camera-wide shake logic doesn't leak into the
otherwise-generic hit-flash component.

## Scene changes (Editor script, re-runnable safely, self-verifying)
A new `Stage7SceneBuilder`:
- Adds `HitFlashComponent` to `Enemy.prefab`.
- Adds `HitFlashComponent`, `PlayerFeedbackComponent` to the existing `Player`; adds
  `ScreenShakeComponent` to the existing `Main Camera`.
- Builds a MainMenu panel (title + Start button) and a GameOver panel (final time +
  best time + Play Again button), a small always-visible run-timer text, and
  `GameFlowComponent`/`RunTimerComponent`/`HighScoreComponent`, wiring everything.
  Reuses the `EventSystem` Stage 5 already added — does not create a second one.
- Self-verifies its own wiring before saving (the standing pattern since Stage 5).

## Testing
- **`Health` tests (extended)**: `OnDamaged` fires with the correct amount on a
  successful hit; does not fire for a non-positive/no-op `TakeDamage` call; a lethal
  hit fires both `OnDamaged` and `OnDeath`.
- **`ScreenShakeTests`**: offset is zero once `elapsedShakeTime >= duration`; offset
  magnitude never exceeds `magnitude`; offset magnitude shrinks as elapsed time
  approaches duration (with a fixed seed, compare an early-elapsed offset's magnitude
  against a late-elapsed one).
- **`GameFlowTests`**: `StartGame` from `MainMenu` transitions to `Playing` and fires
  the event exactly once; `StartGame` from any other state is a no-op; `EndGame`
  mirrors this for `Playing`→`GameOver`; `ReturnToMenu` always lands on `MainMenu`
  regardless of starting state.
- **`HighScoreTests`**: `TrySubmit` saves and returns `true` only when the new score
  exceeds the loaded best; `Best` reflects whatever the injected `load` returns,
  proving no hidden real-`PlayerPrefs` dependency in the tested logic.
- `ScreenShakeComponent`, `HitFlashComponent`, `PauseState`, `GameFlowComponent`,
  `RunTimerComponent`, `HighScoreComponent`, `PlayerFeedbackComponent`, and the
  `CameraFollowComponent` shake-composition change are MonoBehaviour/static wiring —
  no dedicated tests, consistent with this project's established convention.

## Out of scope
No settings/options menu. No pause menu during play (beyond the existing
upgrade-choice pause). No multiple named save slots. No animated UI transitions
(panels just show/hide). No scene-based main menu (stays a paused panel in the same
scene, per the restart-via-reload design above).
