# Arrow Escape

A Unity 6 (2D, C#) arrow-escape grid puzzle for Android, iOS, Windows, and macOS.
Each arrow occupies one cell. A tapped arrow slides off the board when every
cell ahead of it is empty. Edge arrows pointing outward can exit immediately.
The board is cleared when no arrows remain.

## Coordinates
- x is the column: zero at the left, increasing right.
- y is the row: zero at the bottom, increasing upward.
- Up = (0, +1), Down = (0, -1), Left = (-1, 0), Right = (+1, 0).

## Folder layout (under Assets)
- Scripts/Core: plain C# game logic, namespace ArrowEscape.Core.
- Scripts/View: visuals and input.
- Scripts/UI: interface code.
- Resources/Levels: level data.
- Tests/EditMode: Edit Mode tests.

## Rules
- Only do the task the user asks for; do not start later steps.
- One class per file; keep scripts short, simple, and well commented.
  DirectionExtensions may share Direction.cs with its enum as requested in Step 1.
- Core must have no UnityEngine dependencies or MonoBehaviour scripts.
- Use C# features supported by Unity 6 (C# 9).
- Use the new Input System.
- Use Canvas Scaler for UI.
- Ask before adding third-party packages.
- No lives system: use undo and a mistake counter.
- After each task, tell the user exactly what to do in the Unity Editor.

## Level data (Step 2)
- Scripts/Data (ArrowEscape.Data) references Core and uses the built-in
  JsonUtility and Resources APIs. Core never uses UnityEngine.
- JSON levels live directly in Assets/Resources/Levels, named level_001.json, etc.
  Resource paths omit the extension, for example Levels/level_001.
- JSON fields: name (string), width and height (integers from 1 to 64),
  arrows (nonempty array of objects with id, x, y, and direction).
  Direction is Up, Down, Left, or Right, ignoring case. IDs and cells are unique.
- Core validates data and builds boards; solvability is checked separately on a clone.

## View layer (Step 3)
- Assets/Scripts/View (ArrowEscape.View) references Core and Data. GameBootstrap
  creates the entire board display at runtime; no scene or prefab setup is needed.
- Runtime sprites are generated and cached; ViewTheme owns colors and sizes.
- ScreenFit in Core uses pure math to fit a board centered at world (0,0) inside
  screen margins. Padding is the total extra span per axis, split between edges.
  CameraFitter adds phone safe-area insets and refits when screen geometry changes.
- Core never uses UnityEngine. Scene files and prefabs must not be hand-edited.
- Step 3 displays levels only: no tapping, animation, UI, sound, or new game rules.

## Input (Step 4)
- Use only the new Input System. One mouse or touch pointer owns an interaction
  until release or cancellation; other fingers are ignored, even after an empty press.
- SelectRadius is 0.6 cells and CancelRadius is 0.8 cells, measured from cell centers.
- TapResolver finds the nearest arrow (lower id wins ties). TapTracker owns press,
  movement, cancellation, and release decisions. Core never uses UnityEngine.
- TapController highlights only tint and scale and emits ArrowTapped once per tap.
  ArrowTapped is the hook for Step 5; no gameplay listener is connected in Step 4.
- Step 4 never moves or removes arrows and adds no animation, UI, sound, or game rules.

## Gameplay (Step 5)
- Logic is instant and visuals are async: GameSession owns the live Board and removes
  free arrows immediately. BoardView and TapTracker use that same Board instance.
- ArrowAnimator uses explicit Tick(deltaTime), never coroutines. Released sliding views
  stay under the board root so switching or restarting levels cleans them up.
- ArrowView has one private Apply() writer combining base position, offset, highlight,
  flash, alpha, and sorting layers. Flash tint overrides highlight tint.
- Blocked taps bump, flash the nearest blocker, and count mistakes. Mistakes never fail a level.
- DebugHud is TEMPORARY and only exists in the Editor/development builds, until Step 8.
- Core never uses UnityEngine. No undo, menus, sound, haptics, hints, lives, or Step 6 work.

## Undo and UI (Step 6)
- LIFO history records only exits. MoveCount is progress (exits minus undos).
- Mistakes are never undone. UndoCount counts successful undos; clearing makes the attempt final.
- Restore using Board.TryAddArrow, creating a new instance at the end of insertion order.
- GameSession, BoardView, and TapTracker always share the same live Board.
- Runtime uGUI uses a scaled overlay Canvas, safe-area anchors, and InputSystemUIInputModule.
- UI buttons and the completion overlay block whole pointer interactions from reaching the board.
- DebugHud is removed. The HUD and completion panel replace it.
- Core never uses UnityEngine. No scene/prefab edits or Step 7 work.

## Progress and saving (Step 7)
- Stable level ids are resource file names without folders or extensions, never indices.
- Index zero is unlocked; each later index unlocks when its immediate predecessor is completed.
- Best means one complete attempt: fewer mistakes, then fewer undos. Store that pair together.
- Version 1 JSON is progress.json under Application.persistentDataPath. Save only at level start,
  level clear, and an explicit reset; no mid-attempt state, cloud sync, or Step 8 features.
- Core holds pure rules without engine dependencies or file access. Data handles JsonUtility and disk.
- Corrupt and newer saves are copied to progress.corrupt.json / progress.newer.json before replacement.
  Failed backups preserve the original; any storage failure leaves play available in memory.
- Tests inject IProgressStore through GameBootstrap.Initialize and use unique temp folders for disk tests.
  Edit-mode bootstrap callers default to MemoryProgressStore; StartInitialLevel exercises startup explicitly.
- Runtime Next/Previous do not wrap; Inspector debug jumps wrap and ignore locks.
- Editor menus: ArrowEscape/Reveal Save Folder and ArrowEscape/Delete Save File (confirmation required).

## Screens and settings (Step 8)
- One scene: runtime uGUI only. No scene loads, scene/prefab edits, or Step 9 work.
- ScreenStateMachine in Core is the navigation owner. Initial state: MainMenu.
- Transition table (all others reject without events):
  - OpenLevelSelect: MainMenu or LevelComplete -> LevelSelect.
  - OpenSettings: MainMenu or Paused -> Settings; remember that return state.
  - StartPlaying: MainMenu, LevelSelect, Paused, LevelComplete -> Playing;
    Playing -> Playing succeeds without an event.
  - Pause: Playing -> Paused. Resume: Paused -> Playing.
  - LevelCompleted: Playing -> LevelComplete.
  - ToMainMenu: LevelSelect, Paused, LevelComplete -> MainMenu.
  - Back: MainMenu no-op; LevelSelect -> MainMenu; Settings -> remembered origin;
    Playing -> Paused; Paused -> Playing; LevelComplete -> LevelSelect.
- Pause is an explicit GameController/ArrowAnimator flag. Explicit Tick remains testable.
  Leaving a paused attempt for MainMenu discards it without recording completion.
- Menu level starts use TryStartLevel, which enforces progress unlocks. ShowLevel and
  DebugJump remain explicit Inspector/test debug tools, never menu entry points.
- EnsureInitialized loads both stores once. autoStartOnStart defaults true for manual
  components, but the runtime-created bootstrap sets it false and opens MainMenu.
- settings.json lives beside progress.json under Application.persistentDataPath:
  version 1, soundEnabled=true, hapticsEnabled=true by default. Preferences only;
  sound/music/haptic playback is not implemented. Corrupt/newer files are backed up.
- Tests inject both stores; disk tests use unique temporary folders, never player saves.
- PointerInputAdapter supports independent added blockers plus the existing HUD predicate.
- Exactly one EventSystem uses InputSystemUIInputModule. Menus use ArrowEscapeMenus,
  overlay order 100 above the HUD at order 90, safe-area layout, and the same 1080x1920 scaler.
- Core never uses UnityEngine or file access. No global clock changes or coroutines.

## Hints and guides (Step 9)
- HintFinder walks each arrow's path once, without cloning or repeated solving. Among free
  arrows prefer highest NewlyFreed, then highest Dependents, then lowest id.
- Removing a free arrow never makes this puzzle harder. Revisit hint safety and usefulness
  when special arrow types are introduced.
- Hints never modify board state, mistakes, moves, undos, undo history, saved progress, or
  Flawless. HintCount is an in-memory request count only, reset when an attempt starts.
- One hint pulse and one reusable amber line at a time. Blocked taps replace the line and
  keep the hint. Exits clear both BEFORE sliding; successful undo and board replacement clear both.
  Ignored/paused taps clear neither. New Hint requests replace the pulse without stacking.
- ArrowView.Apply remains the only appearance writer: flash > highlight > hint > normal tint;
  independent scale multipliers combine without drift. Guides freeze on pause, while explicit
  Tick remains testable. Guide objects live outside the replaceable BoardView root.
- HUD Undo, Hint, Restart are separate, nonoverlapping pointer-blocked buttons. Pause is also blocked.
- Core never uses UnityEngine or file access. No hint costs, limits, saved hint data, or Step 10 work.

## Audio (Step 10)
- All sounds are generated from sample arrays at runtime; no imported audio files or mixer assets.
- C major pentatonic starts at C4 (261.6256 Hz), with 11 notes across two octaves plus C6.
  Notes ping-pong up and down. The melody depends only on post-move MoveCount:
  exit uses PingPong(MoveCount - 1); undo uses PingPong(MoveCount), softly replaying that exit.
  GameController updates the session before ArrowExited and ArrowRestored; keep that event ordering.
- Cue gains: Exit 0.6, Blocked 0.55, Undo 0.4, Hint 0.4, LevelComplete 0.7, Preview 0.5.
  Blocked is a low thud; Hint is G5 and Preview is E5. The jingle is C5 E5 G5 A5 C6,
  with 0.15 seconds of baked-in lead silence and a total length of 1.49 seconds.
- Core contains pitch math, note planning, deterministic synthesis, and voice allocation only.
  Core never uses UnityEngine or System.IO. View creates clips and plays them through ISoundPlayer.
- UnitySoundPlayer prepares clips before gameplay and reuses six child AudioSources; idle voices
  win, otherwise the next source is stolen in round-robin order. No per-tap synthesis or new sources.
- AudioDirector reads SoundEnabled for every event. Switching off immediately stops all voices;
  switching on plays one preview note. Haptics preference changes do not produce audio.
- Level start and application pause stop audio. EnsureAudioListener reuses an existing listener,
  or adds one to the main camera/director; extra runtime listeners are disabled.
- Tests inject a fake ISoundPlayer and memory settings; they need no audio output device.
  Replace sound playback later by implementing ISoundPlayer, or change the generated clips in
  ProceduralAudio. Runtime bootstrap adds AudioDirector; test-created bootstraps do not.
- No music, volume sliders, button sounds, new settings, scene/prefab edits, or Step 11 work.

## Game feel (Step 11)
- Effects are pure visuals: they never change the board, session, counters, history, progress,
  hints, guides or sound. Removing every effect object must leave gameplay identical.
- Effects use bounded reusable SpriteRenderer pools (8 streaks, 96 particles), rather than
  particle/trail components, so capacity, timing, determinism and cleanup are explicitly testable.
  No new shaders, materials or asset files. The two pre-existing shader lookups remain unchanged.
- Streaks independently mirror the slide's duration, distance and EaseInQuad. They never read
  an arrow transform. BurstPlanner uses a local seeded xorshift32 generator and stratified angles.
- Completion uses center confetti, an outward cell-dot wave from the last exited cell, and one
  0.6-second background pulse of at most 22 percent toward the lighter theme color.
- Captured dot colors/scales and the theme background restore exactly on finish, clear or restart.
  Pool children belong to EffectsController, outside BoardView's replaceable Board root.
  LevelStarted clears all; undo clears streaks only. Hidden boards clear all before pause checking.
  Pause freezes the director's automatic frames; explicit effect Tick remains testable.
- Approved sorting: dots 0, normal/highlighted/flashed/hinted arrows 1, effects 2, guides 3,
  sliding arrows 4. HUD and menus retain their existing canvas orders 90 and 100.
- Haptic cues (duration/amplitude/minimum interval): Exit 12ms/70/0.04s; Blocked 40ms/200/0.08s;
  Undo 8ms/45/0.04s; Preview 15ms/90/0.2s; Complete 20ms/90, gap60ms, 20ms/130, gap60ms,
  45ms/220 with a 0.5s minimum interval. Throttles are independent and rejected calls do not extend them.
- AndroidHapticsPlayer is deliberately NOT inside an #if: JNI C# compiles in Editor. The factory
  constructs it only on Android. API26+ caches VibrationEffects; older versions use cached legacy
  waveforms. First JNI failure disables haptics for the session and warns once. Test hardware is fake.
- HapticsEnabled is read for every cue; off cancels, on previews through the throttle. Hint has no
  vibration. Level start/app pause cancel. iOS remains unsupported until a future native plugin.
- Android builds need the VIBRATE manifest permission; never edit Android plugins or project settings
  automatically for this step. Core never uses UnityEngine or file access. No Step 12 work.

## Ice arrows (Step 12)
- ArrowKind defaults to Normal. Missing/null/empty JSON type means Normal; "normal" and
  "ice" are case-insensitive. Test levels live in Resources/TestLevels, never the real Levels list.
- Forward line means every cell strictly ahead to the edge. Both kinds exit if that whole line
  is empty. Otherwise Ice slides across the consecutive empty gap before its nearest blocker;
  an adjacent blocker counts a mistake. Normal blocked arrows still count mistakes.
- Logic changes instantly, replacing an immutable Arrow at the same insertion index; stale
  instances are ignored. Views rebase without a jump and animate their remaining offset to zero.
- LIFO action history records exits and slides. The original exit-history stack still defines
  MoveCount (exits minus exit undos), per the explicit Step 12 clarification. Slides and their
  undos never change MoveCount. Undo restores kind; slide undo finds the current arrow by id.
- CYCLE (4x3): Ice 1 (0,0) Right; 2 (3,0) Up; 3 (3,2) Left; 4 (0,2) Down.
  Slide 1 to (2,0), then exit 4,3,2,1. MoveCount ends at 4.
- TRAP (6x3): Ice 1 (0,0) Right; 2 (3,0) Up; 3 (4,2) Down; 4 (5,2) Left;
  5 (5,0) Up. Exit 2 then slide 1 to (4,0) is a dead end. Undo recovers.
- Greedy removal is invalid with Ice because removing an arrow can change a future slide stop.
  PuzzleSolver uses compact arrays, an explicit DFS stack, deterministic id order and memoized
  dead states, with only safe-exit reduction. A free arrow is safe iff it lies on NO other
  live Ice arrow's entire forward line. Removing it cannot change an Ice stop and can only
  free paths: free arrows never move and an advancing Ice arrow's forward line only shrinks.
- Analyze never mutates the board; Solved has a replayable path, Unsolvable/Unknown have none.
  Default search budget is 200000 nodes. Stuck checks use 20000 nodes only after board changes
  with live Ice; Unknown means NOT stuck. They never run in Update/Tick.
- Ice hints use the solver's first action (which may slide); scores are zero. Normal hints and
  greedy solvability keep their old paths. A stuck banner disables Hint but keeps Undo/Restart.
- Apply remains the sole ArrowView appearance writer. Ice has a notched sprite and blue base tint;
  every temporary layer restores exactly. Guides clear before slides and slide undos.
- Slide sound is a generated 0.16s descending cue, volume 0.5 (undo 0.35); exit melody is unchanged.
  Slide haptic is 10ms/amplitude55/minimum interval0.05s. HapticThrottle and AndroidHapticsPlayer
  extend to six cues additively; the original five patterns and behavior remain unchanged.
- Sandbox context menus on GameBootstrap (Play mode): Debug: Sandbox Ice 1, 2, 3.
  Sandbox never remembers a level, records completion or saves. Replay/Restart reload its resource.
  NextLevel is a no-op retaining sandbox; CanGoNext=false and IsLastLevel=true. TryStartLevel,
  PreviousLevel, debug jumps and ResetProgress clear sandbox first. Completion is explicitly unsaved.
- Every FUTURE arrow type must update the solver, hint finder, undo, generator and sandbox tests.
  Core never uses UnityEngine or file IO. No scene/prefab edits and no Step 13 work.

## Level generation (Step 13)
- Editor-only authoring: Tools/Arrow Escape/Level Generator. Nothing generates automatically
  or per frame. No runtime random-level button, new screen, save change or Step 14 work.
- PLACE reverses an exit: add on an empty cell only when its complete forward line is empty.
  UNSLIDE reverses an Ice slide: its current front-adjacent cell must be occupied; move backward
  one or more in-bounds empty cells. Every inverse is legal on the exact intermediate forward
  board, so reversing the construction operations is a guaranteed clearing solution.
- SeededRandom is independent integer-only SplitMix64 with rejection-sampled bounds.
  Attempt seeds use a documented fixed 32-bit avalanche. Never use clocks, platform hashes,
  System.Random or engine randomness. Step 14 puzzles must match across every device.
  Existing DeterministicRandom remains unchanged.
- Cells enumerate top row first then left to right, directions in enum order, and arrows by id.
  Final ids are renumbered 1..n in reading order; intended actions are remapped so ids do not
  reveal the construction order. Preserve the golden RNG values and board snapshot unless a
  deliberate compatibility change is requested.
- Analyzer owns its compact state; PuzzleSolver is untouched and remains the authority.
  Metrics include counts/kinds, authority path/slides, initial exits/slides and blocker chains,
  bounded exhaustive DAG states/dead states/clearing sequences/trap edges and deterministic
  playout trap rate/average choices. Sequence counts saturate at 10^15.
  Default exhaustive budget is 50000 states, with 128 playouts at seed7177. Incomplete exhaustive
  counts are zero and never enter the score; playouts always run. Generator candidates use a
  5000-state exhaustive cap so tier retries remain bounded without changing score inputs.
- Solvable Normal-only boards have zero dead states, trap moves and failed playouts. Unsolvable
  Normal boards report their actual dead states and trap rates (explicit Step 13 clarification).
- Difficulty uses integer permille components: size arrows/60, scarcity 1000-average choice,
  chain length/12, playout traps, and authority slides/6, each clamped to 0..1000.
  Weights remain 25/25/20/20/10; thresholds are 250/310/380, all in DifficultyWeights.
- Quality filters require proven solvability, enough initially free exits, at least one initially
  blocked arrow for counts above two, and no direction above 60 percent for counts of eight or
  more. Required-slide Ice requests must have an unsolvable all-Normal copy and at least one
  authority-solver slide. Retries may fail; never silently relax these filters.
- IntendedSolution is reversed construction history. Its slide count may differ from the
  authority solver's SlidesInSolution; validate each path against its own actions.
- Export writes explicit "type":"normal" or "type":"ice", with string direction names.
  Do not post-process JSON. Existing Levels and TestLevels files stay untouched.
- Default export folder: Assets/Resources/GeneratedLevels. Refuse folders outside Assets and
  both Resources/Levels and Resources/TestLevels (including descendants). Reject traversal and
  linked export folders. Unique filenames never overwrite. Tests write only to owned temp folders.
- Existing ArrowEscape.Editor stays Editor-only, with Core/Data/View references; EditMode tests
  may reference it. Play in Sandbox loads the last exported Resources file through the existing
  sandbox API and never records player progress.
- Every FUTURE arrow type must add its reverse move to generation, its rules to analysis, and
  its cases to the independent cross-check tests. Core has no engine or file-access dependencies.


## Path arrows (Step 13B)
- Cells are an immutable tail-to-head chain of distinct orthogonal neighbours. X/Y are the head;
  the final segment defines Direction. Legacy constructors create length-one paths; Ice stays length one.
- Board occupancy indexes every cell. A ray starts strictly ahead of the head; even the arrow's
  own body blocks it. GetBlockingCell returns the first occupied ray cell. Remove frees the full path.
- JSON cells is a flat x0,y0,x1,y1 array. Missing/null/empty preserves old x/y levels. Validation
  collects odd arrays, bounds, adjacency, repeats, head mismatch, direction mismatch, overlaps,
  multi-cell Ice and self-blocking errors. Export includes path coordinates for multi-cell arrows.
- Exit undo appends ALL restored arrows, including paths, just like legacy exits. Compare undo
  round trips sorted by id, never by insertion order. Moves still count exits minus exit undos.
- Tap selection uses the nearest occupied cell within 0.6 cells; equal distances prefer lower id.
  PATH1's empty (1,2) misses; (0.5,1) selects id3. Hints count distinct owners once per ray.
- Compact solver and analyzer include body occupancy and Ice stops at body cells. Preserve exact
  single-cell solver paths and NodesVisited. No shape generation is part of this step.
- Paper background and dark ink are shared theme values. Length-one arrows retain their sprites.
  Paths use a ribbon mesh with rounded joins/tail and triangular head, sharing the existing sprite
  material with a white per-renderer texture. No new shader lookup or material asset.
- Path exits advance a window along body then straight head extension; undo reverses that motion.
  Blocked paths nudge 0.15 cells for 0.14 seconds; guides target the blocking cell. Layer priority
  stays flash > highlight > hint > base; all cleared layers restore exact base values.
- Single-cell-only attempts retain legacy dot sprites, completion wave and large-board omission.
  Attempts loaded with paths keep a single mesh DotGrid through clear/restart. Its completion wave
  reuses EffectMath timing and colors, preallocated buffers, pause/hidden-board rules and exact reset.
  DotWavePlayer is unchanged. Idle dots do not update their mesh.
- Path boards enable pinch/two-finger pan and cursor-wheel/right-drag navigation when fitted cells
  are below 0.30 inches (DPI fallback 160). Zoom caps at 0.45 inches per cell; camera stays over
  the board and resets on restart/start. A path-board single-pointer drag over 12px cancels its tap.
- Sandbox Path 1/2/3 live only in Resources/TestLevels; real Levels and player save formats stay
  unchanged. Core remains free of engine dependencies and file IO.

## Shape levels (Step 13C)
- ShapeKind contains ellipse, oval, semicircle, four triangles, square, rectangle,
  rhombus, parallelogram, trapezium, kite and regular pentagon through decagon.
  Requested dimensions have minimum 8 for triangles/quadrilaterals, 10 for curves,
  12 for pentagon/hexagon, and 18 for heptagon through decagon. Default aspects
  are supplied by ShapeMask.DefaultSize; manual requested boxes are supported.
- Masks classify cell centers with integer/long arithmetic and trim empty outside
  rows/columns only. Regular polygon vertices are fixed million-unit constants.
- ShapeLevelGenerator uses SeededRandom, explicit reading/enum orders, no floating
  arithmetic, clear-head-ray PLACE, backward corner-filling growth and tail extension.
  An extension cannot cross its own ray or any later-placed arrow's ray: reversing
  placement therefore still leaves every next exit clear. No Ice is generated.
- Where both mask dimensions exceed twice the path-length cap, target length is the minimum
  of three bounded draws; otherwise it is one bounded draw. This reduces sealed empty pockets
  on large masks without changing length limits, placement rules or the smaller-board snapshot.
- Quality gates preserve >=900/1000 coverage by default, 1..300/1000 initially free
  arrows, <=45% of any head direction, length limits and proven solvability. IDs
  follow head reading order. Tier misses return the closest passing candidate;
  failure is reported rather than weakening a gate. Existing rating constants stay.
- Normal-only analyzer states share immutable nearest-first blocker lists; only alive flags change.
  Ice states retain the original coordinate search. This preserves all analysis metrics and paths.
- Analysis uses a bounded 128-state exhaustive search and 32 deterministic playouts
  for authoring. All analysis fields are exposed; incomplete exhaustive counts are
  not treated as completed evidence. The golden Hexagon18x16 seed12345 test uses
  an explicit 500-attempt budget. Change its snapshot only deliberately.
- Shapes tab: generate/preview, seed stepping, generic safe export and sandbox play.
  ShapePackPlan requests all 19 shapes in three inside-mask-area bands: 70, 180 and 330
  cells, within 12 percent where minimum sizes and phone limits permit. Choose the closest
  default-aspect raster area, never below minimum, width <=24 and height <=36. Report
  minimum-size and phone-cap exceptions. Reviewed seeds are plan data, not generator changes.
  Pack generation requests no tier; labels come from the unchanged rating thresholds.
  Prepare all 57, stable-sort by score, then use nearest valid forward swaps to keep four
  intervening levels between identical shapes. Allow at most a 15-point dip and 45-point
  jump; the opening score must be <=230. Reject a pack that cannot meet these checks.
  Assign numbers 004..060 only after ordering. CreateRequests supplies unnumbered-intent
  slots; Create and OrderedResourcePaths derive the final plan from actual builder output.
  The approved 57-file pack is installed as levels 004..060 after the original three.
  Future replacements must be staged and reviewed before installation. Prepare a summary first.
  The separate pack writer requires confirmation in the Editor, accepts only new
  level_NNN_shape_<known-name>.json with NNN>=004, and never overwrites files.
  Generic export still refuses Resources/Levels; default is Resources/GeneratedLevels.
- Minimum-size exceptions: Small Rectangle96, Semicircle158, Ellipse80, Oval90,
  Pentagon100, Hexagon108, Heptagon240, Octagon264, Nonagon246, Decagon252 cells.
  Heptagon/Octagon/Nonagon/Decagon also exceed Medium at those same minimum areas.
  Large width-cap exceptions: EquilateralTriangle252, Semicircle224, ScaleneTriangle288,
  Kite288 cells. Width 24 takes priority; never stretch the default aspect to force area.
- Rebuild via Tools > Arrow Escape > Level Generator > Shapes > Build Shape Pack:
  prepare summary. ShapePackPlan.CreateRequests retains the reviewed sizes/seeds;
  ShapePackBuilder prepares without tier targets and ShapePackOrdering numbers final output.
  Review the summary before Confirm and write reviewed pack. The writer refuses existing
  files: an installed pack is not overwritten by rebuilding. For verification, use the
  temp-folder tests or stage a rebuild outside the live catalog, then compare the JSON.
  Final approved score range is 227..534, largest adjacent jump27, no dips; four levels
  separate repeats. Tier labels are derived, never generation targets for the pack.
- Catalog ordering remains ordinal filenames; progress remains keyed by filename ID.
  The original three bytes/order are protected. Legacy flow fixtures inject a fixed
  original-three catalog before initialization; the runtime default uses real Resources.
- Level select shows aspect-preserving 128-square paper thumbnails above numbers on
  rounded cards. Only visible tiles request rendering, maximum two new textures per
  frame. Cache keys are level IDs; destroyed textures regenerate, closing clears them.
  Locked thumbnails use 35% opacity with unchanged unlock rules. No save format changes.
- Core remains free of engine and file-access dependencies.

## Stars
- Step 14A: TapCount counts Exited, Slid and Blocked results. Ignored taps,
  hints and undo never change it. MoveCount remains exit progress and all older
  counters retain their meanings. StartLevel/Restart reset TapCount.
- Par is fixed when an attempt starts: arrow count without ice (no solver),
  solver path length with ice, or arrow count plus ice count if not Solved.
  Empty diagnostic boards use a minimum budget of one so HUD math stays valid.
- Two-star limit is par + ceil(par/3), using integer math. Completion earns
  three stars at/below par, two at/below that limit, otherwise one. HUD shows
  accepted taps / par with normal, amber and red text and three potential stars;
  losing a star shakes that slot. Hint use does not affect stars or Flawless.
- Completion shows three procedural star slots and moves / par. Earned stars
  pop sequentially at 0.25-second intervals with quieter rising synthesized
  chimes and light haptics. Tick drives presentation; pause freezes it. Settings
  gate every cue. Missing slots remain grey outlines. New best refers to a
  strict improvement in saved stars; first clears retain First clear instead.
- progress.json version 2 adds bestStars per record. Version 1 migration retains
  completions, lastLevelId and the paired bestMistakes/bestUndos, assigning zero
  stars. Loading does not write; the next normal atomic save writes version 2.
  Best stars never decrease. TotalStars sums saved best stars. No best-moves
  field existed previously; the paired best result remains unchanged.
- Save once at completion as before. Sandbox results say Practice - not saved
  and never persist stars. Level select displays stars only on completed tiles,
  between thumbnail and number. Runtime sprites are cached; no art/audio assets.
- Core remains independent of engine and file APIs. Tests use fake stores or
  isolated temporary folders for actual versioned-file migration, never player saves.

## Economy and shop
- EconomyConfig: first clear 10 coins plus 10 per star; genuine replays pay 5
  plus 10 per newly improved star. CoinReward uses inventory RewardedStars,
  independently of progress. Coins are integer-only, clamped to 9,999,999.
- inventory.json is a separate version-1 payload under persistentDataPath. Never
  add inventory fields to progress.json or settings.json. Known defaults are
  Classic Ink (classic) and Paper (paper), owned and equipped for free.
- Receipts are two integers only: NextAttemptId and LastPaidAttemptId. Missing
  fields load as zero. Zero means no paid attempt; BeginAttempt issues at least
  one, then increments in memory. Every StartLevel/Restart issues a fresh id.
  Award only when attemptId > LastPaidAttemptId, persist that id and ensure
  NextAttemptId > attemptId. Duplicate clear delivery pays zero; replay uses a
  fresh id and pays normally. Never wrap exhausted integer ids into paid ids.
- Save inventory BEFORE progress on every non-sandbox clear. A crash between
  the two loses at most that clear's progress; its saved receipt prevents paying
  it twice. Inventory failure keeps progress in memory until inventory can be
  committed. The next normal start retries pending inventory before progress.
- Save once per award, successful purchase or equip. Sandbox never awards coins.
  There is no retroactive grant for old progress: the first inventory-recorded
  clear uses the first-clear rule, regardless of old progress records.
- Atomic writes use temp plus replace/move, like progress. Corrupt inventories
  are RENAMED to unique inventory.corrupt-N.json files. Never delete/overwrite
  backups. Unreadable/newer files and failed renames forbid destructive writes.
  Unknown owned ids remain in the payload but have no active shop semantics;
  invalid/unowned/wrong-slot equipment falls back to classic/paper.
- ShopCatalog ids are permanent once released. There are 20 priced arrows,
  two free defaults, and six unavailable themes. Themes say Coming soon until
  Step 14E. Equipment currently changes only Normal arrow base tint, with a
  3:1 paper-contrast guard; Classic is exact and Ice is unchanged. Temporary
  appearance layers still restore the exact equipped base tint.
- FileInventoryStore accepts injected IInventoryFiles. Tests use fake stores
  and a fake filesystem, never player files. Development grants/resets live
  inside UNITY_EDITOR || DEVELOPMENT_BUILD; reset first makes a unique backup.

## UI style
- Runtime uGUI retains the existing CanvasScaler and safe-area approach. UiStyle
  owns paper #E8F2F2, white surface, ink #1C2228, muted #5B6770, gold #F5B700,
  danger #E5484D, shared rounded surfaces, shadows and typography. SpriteFactory
  caches every procedural glyph and nine-slice surface. No images/audio assets.
- One OFL Fredoka font is bundled under Assets/Fonts/Resources, statically
  instantiated at weight 500 from the official Google Fonts variable font;
  Assets/Fonts/OFL.txt preserves the license. LegacyRuntime remains a fallback.
- Main menu: best-star capacity and coin/shop header, bold title and animated
  ribbon underline, then Play/Continue, Levels, Shop and Settings. Buttons are
  capped at 520 reference units; a prepared staggered entrance and four drifting
  ribbon graphics share a static dot mesh. IMenuBackground is the Step 14E seam.
- Shop is a child flow of MainMenu, preserving ScreenStateMachine's prior
  navigation contract. Back/Escape consumes confirmation, then detail, then shop.
  Existing level select/settings/pause/completion retain their layouts/actions.
- Shop sections follow catalog order, responsive 2/3/4-column grids, category
  frames and a subtle legendary glint. Detail uses one buy/equip action and an
  explicit purchase confirmation. The preview states that full effects come later.
- ArrowPreviewRenderer reuses PathRibbon geometry and draws only visible requests,
  at most two per frame; textures/buffers are prepared at shop open and destroyed
  at close. Contrast-adjusted previews match the temporary readable game tint.
  Steps 14C/14D can replace preview drawing and equipped visuals at these seams.
- Completion coins appear after the earned stars, count up for 0.6 seconds, and
  emit one setting-gated coin cue. EconomyFeedback routes presentation cues into
  the existing six-source player and haptic director; old gameplay subscriptions
  remain independent. Button, purchase and coin sounds are generated by code.
- Tick/Update paths reuse arrays, positions and cached counter strings. No
  coroutines, global time-scale changes or legacy input. Core stays free of
  engine and filesystem dependencies. Step 14C has not started.
