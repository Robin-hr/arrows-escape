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
- ShopCatalog ids remain reserved forever, including retired ids. There are 19 priced arrows,
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


## Arrow skins (Step 14C)
- Part 1 implements frostbite, inferno_streak, plasma_bolt, void_whisper,
  obsidian_shard, chromium_spike, celestial_quill,
  emerald_viper and crimson_relic only. Other paid arrows retain the 14B tint.
  Classic keeps its original renderer, meshes, sprites, colors and sorting exactly;
  Ice ignores skin equipment. Inventory/progress/settings formats do not change.
- Art sources: Assets/ArtSource/arrows_sheet_a.png and arrows_sheet_b.png.
  Tools > Arrow Escape > Import Arrow Art reads source bytes without changing them.
  It detects bounds within label-free regions, estimates the local border matte,
  un-premultiplies soft glows, pads six transparent pixels and normalizes tips right.
  Assets/ArtSource/Arrows/<id>.png overrides a sheet without flipping the override.
  Exports full/head/tail/seamless shaft, plus sheet-B icons, under Assets/Art/Arrows.
  All twenty catalog references plus jade_serpent are imported; this does not
  implement Part 2 recipes. Contact sheet: Logs/ArtImport/contact.png.
- ArrowSkinRegistry owns optional body/pattern/core/rim, imported head/tail, halo,
  idle/flight/impact/return/trail and quiet procedural sound recipes. SkinVisual
  attaches separate appearance children and preserves ArrowView's classic state
  adapter. ArrowView.Apply remains the only owner-transform/layer writer.
- PathRibbon UV u measures distance in cells and v crosses the width, so patterns
  tile rather than stretch. Geometry and 0.26-cell body width are unchanged.
  Skin-only tags suppress generic heads under reference art and duplicate glow
  caps; Classic ignores the added UV channels. Head/tail art follows the moving
  path window, including reversed undo and blocked nudge.
- Two mobile unlit shaders: ArrowSkinRibbon.shader and ArrowSkinGlow.shader.
  ArrowSkinResources in Resources explicitly references both shaders and imported
  art; no new shader-name lookup. Runtime materials are shared by recipe/mode.
  Noise, scrolling, shimmer, glints, lightning and flight waves run on the GPU.
  One board clock freezes on pause; live preview clocks are independent.
- Paper uses saturated normal-alpha halos plus dark art/body edges. Dark boards
  use additive halos. Editor/development Debug: Dark board switches only the
  runtime comparison background to #0E1116, never the saved theme.
- Flash > highlight > hint remain independent bright outlines/overlays over the
  skin. Clearing them restores exactly; no state changes the art/base recipe.
  Every new skin must pass 100-cycle state restoration and visual comparison.
- Dense cells below 0.30 inch (160-dpi fallback) disable ambient particles,
  simplify patterns and omit optional halo/tail layers. Exit effects remain.
  One deterministic ambient start per 0.4 s across the board. Twelve reusable
  fifty-particle systems, with a shared 600-live-particle budget including preview
  pools; twelve reusable laser/prism/mist ribbons and thirty-two frost marks.
  No effect objects are created per exit and no allocations occur in warmed Tick.
  Hidden boards clear effects; pause freezes automatic ticks; explicit ticks test.
- SkinSoundSynth creates deterministic chime/fire/laser/whisper/crackle/glass/
  metal/bell/hiss/gem textures at setup. They play quietly below the unchanged
  MoveCount pentatonic melody and blocked thud through the existing six voices.
  Sound off silences/stops the shared player. Preview cues do not advance melody.
- Shop cards use extracted full art on dark display cases for implemented skins.
  Details use an isolated small render-texture board, Watch it fly (also every
  three seconds) and a local Light/Dark switch. No equipment/progress mutation
  occurs in previews. Level select remains unchanged.
- Tools > Arrow Escape > Skin Gallery (Play mode) shows all ten bent samples,
  fly buttons, dark/light and LOD controls. Captures go to Logs/SkinShots with
  <id>_rest.png, <id>_flight.png and compare.png (reference beside game).
  The optional Editor-only 100-arrow profiler uses memory stores and explicit
  activation, never player saves. Every new skin must pass visual comparison,
  particle/ambient limits, deterministic choices and allocation budget tests.
  Built skins replace the generic exit burst; Classic and Ice keep it. Native
  particles reserve the legacy 96-sprite pool whenever present, so completion
  confetti plus skin particles also stays within the total six-hundred budget.
- This step allows pooled ParticleSystem components and runtime shader materials
  only for skins. It does not replace Classic's earlier sprite effect system.
  No scene/prefab/manifest/ProjectSettings edit, global time-scale change,
  coroutine, legacy input, new level, Part 2 recipe or theme background visual.


## Skin quality standard (Inferno Streak)
- Step 14C-R1 changes Inferno only. References under ArtSource/Skins are visual
  targets, never runtime textures, sprites, masks or traced data. Only the Editor
  comparison command opens reference.png, to place the unchanged target above a
  screenshot. No runtime/Resources dependency is allowed. The old Inferno crop
  directory is retired; Import Arrow Art skips it and cannot recreate it.
- InfernoDesign authors three curved swept-back tongues per side. A symmetric
  floating-point signed-distance field is generated from those curves, not pixels.
  The head shader samples that generated field with scrolling seamless fractal
  noise for 10 Hz flutter; the bright center grades through pale yellow, gold,
  orange and deep red. A thin red-brown paper rim is available for debugging.
- InfernoTextures creates cached runtime-only 256x256 R8 tileable noise, an authored
  256x256 RFloat head SDF, and 64x64 halo, round spark and elongated spark textures.
  Destroyed caches regenerate. Nothing is exported as an image asset.
- InfernoVisual uses a separate fire-only ribbon mesh that omits redundant straight
  caps, retaining rounded bends. Its shader uses distance-in-cells UVs, backwards
  2.5-cell/s noise, 35%-to-100% width taper and a fading rear quarter. Its separate
  head, halo and fading flight streak never alter the classic mesh or game rules.
  InfernoProcedural.shader is the third shader, referenced by ArrowSkinResources;
  the other two shaders and the other nine recipes remain unchanged.
- Temperature follows quadratic-flight speed: 0 red-orange at launch, rising
  monotonically to 1 white-hot. Undo reverses the existing track animation and
  the temperature. A 0.12 s head flare supplements the existing bump/guide/flash.
  Highlight, hint and blocked outlines use independent state data and restore exactly.
- ArrowSkin.PrefersDarkBoard selects #0D0F12 for Inferno, with faint board dots.
  It is presentation only, never saved as a theme. Unequipping restores Paper.
  SkinDirector's development Dark board toggle exposes the light alpha/rim fallback.
  HUD labels and budget colors gain contrast only on Inferno; menus and layouts
  are unchanged. Paper and Classic retain their original behavior.
- Idle emits two sparks per Inferno every 0.4 s (5/s) as one board-wide batch,
  retaining the global ambient-start throttle. Dense LOD disables idle emission
  and simplifies noise below 0.30 inch/cell. Flight always emits. Native pools
  preserve the combined 600-particle cap and reserve the existing 96-sprite pool.
  Sources and buffers are prepared outside Update; warmed Tick paths allocate zero.
- InfernoSound generates a seeded filtered fire whoosh/crackle below the existing
  pentatonic melody; blocked uses a quieter short crackle. Existing sound settings,
  six voices, pause behavior and move-to-note mapping are unchanged.
- Shop Inferno card uses a straight live procedural preview, idle only; the detail
  keeps the existing bent live preview, dark initially, with Watch it fly and sound.
  Other cards retain their original art and layout. No economy or save-schema change.
- Tools > Arrow Escape > Skin Gallery > Capture procedural Inferno saves the hero,
  flight, comparison, eight-cell bent arrow, blocked and dense 24x24 shots in
  Logs/SkinShots. InfernoRound1..4 retain the visual iterations. The target is shown
  above flight in inferno_compare.png and above idle in inferno_compare_idle.png.
- Tools > Arrow Escape > Skin Gallery - Profile Inferno uses memory-only stores,
  100 arrows at 1080x1920, classic baselines and light/dark idle/flight phases.
  Record CPU, draw calls and GPU timing availability; never claim zero GPU time
  when counters are unavailable. Target extra CPU is under 3 ms; validate phones.
- Future skins must follow this fully procedural layer model, reference-only rule,
  temperature/state restoration checks, at least three inspected visual iterations,
  live previews and allocation/particle/performance budgets before acceptance.


## Skin quality standard: Elemental signatures (Step 14C-R2)
- Frostbite, Plasma Bolt and Void Whisper are fully procedural. Their reference.png
  files are Editor comparison targets only: never crops, textures, masks, traced
  inputs or runtime dependencies. The importer skips these ids and the retired
  Thunderbolt slot. The five other implemented recipes keep their numeric indices.
- One additional shared shader, ElementalProcedural.shader, renders crystal,
  contained energy, smoke, rifts and exact-track residues. ArrowSkinResources holds
  its explicit reference. It reuses generated deterministic seamless noise and
  the existing preallocated rounded ribbon buffers; no new image/material assets.
  Inferno's rest renderer, texture generation and shader remain unchanged.
- Every future skin must make its name felt. Include anticipation, action and
  follow-through, eased changes, secondary motion, core/body/halo/atmosphere depths,
  rim light and fine inner detail. Visual anticipation never blocks the next tap.
  Signature action lasts at most 0.9 seconds; lingering frost may melt over 1.2.
  Never use screen copies or GrabPass. Add every future skin to this table:

  | Skin | Name meaning | Signature moment |
  | --- | --- | --- |
  | Inferno Streak | Raging fire, a line burned by speed | Exact travelled-path scorch cools yellow to dark red over 0.8 seconds. Rest pixels unchanged. |
  | Frostbite | Creeping freeze and a predator's bite | Ice fangs grow and snap; branching frost creeps from the travelled path and melts. Smaller blocked bite. |
  | Plasma Bolt | Barely contained energy released instantly | Contracting head rings, half-duration visual exit, ion line and seeded wisps; blocked arc reaches the blocking cell. |
  | Void Whisper | Swallowed light and a secret departure | Nearby dots dim; the dart is clipped into a closing dark rift with inward smoke. A blocked dark pulse lasts 0.2 seconds. |

- ArrowSkin.DarkBoardColor: Inferno #0D0F12, Frostbite #0B1620,
  Plasma #071418, Void #0E0C14. These unsaved board preferences never change
  Paper/Classic or saved themes. Light fallback uses alpha blending and dark rims;
  smoke uses alpha absorption on dark boards too. HUD contrast follows the board.
- State outlines remain independent and restore exactly after 100 cycles. Dense
  LOD disables ambient particles/optional atmosphere; exits still play. Void uses a brighter violet centerline only in LOD, tested at >=3:1 contrast against its board. All pools
  share the 600-particle cap including previews and reserved completion confetti.
  Warmed Update/Tick paths use preallocated buffers and no managed allocations.
- DotAbsorption captures exact dot colors and uses separate target buffers. Exits
  ease the remaining field back; clearing, restart, new level and unequip restore
  exact original values. Existing completion-wave math and DotWavePlayer stay intact.
  Single-cell tracks use the real AnimationMath exit distance; translation drives
  the same rift clipping as moving path windows. Single-cell undo keeps its existing
  immediate placement with cosmetic return effects; path undo reverses travel.
  Exact-track residue pools are outside the replaceable board root and are cleared
  on completion/restart/new level and undo. Pause freezes automatic advancement.
- All four Elemental shop cards use isolated straight live previews inside their
  OWN rounded masked top area. The name and state rows are always below that area.
  Details have a bent live mini-board, an italic signature line and Watch it fly.
- Catalog ids are permanent reservations: retired ids are NEVER reused.
  thunderbolt_dart is retired. Inventory version 1 adds bounded
  RefundedRetiredItemIds receipts and PendingRetirementCoins. An owned retired dart
  refunds 150 exactly once, removes every duplicate ownership entry and falls back
  to Classic when equipped. Refund and receipt save atomically in inventory.json;
  failed saves retain the original. Overflow waits until spending creates space.
  Other owned/unknown ids, attempts, rewards, progress and settings are untouched.
- Skin Gallery - Capture Elemental saves three reviewed iterations, heroes,
  comparisons, eight-frame signature strips, bent/dense/blocked views and the
  elemental_sheet.png contact sheet under Logs/SkinShots. Profile Elemental uses
  memory-only stores and 100-arrow classic baselines at 1080x1920. Validate GPU
  timing availability, extra CPU under 3 ms, draws, and phone hardware separately.


## Catalog long paths and live themes (Step 14D)
- CatalogMinArrowLength = 4 and CatalogMaxArrowLength = 10. Shape generation
  enforces these limits after tail extension. LevelBuilder, ice, legacy fixtures
  and sandbox continue accepting their existing legal lengths.
- Starter boxes are Square 6x6 (4-5 arrows), Rectangle 6x8 (5-7), and
  RightTriangle 8x8 (6-8): at least two free arrows and at most one bend per path.
  Starter authoring explicitly permits these smaller masks. File names remain
  unchanged, so progress, best stars and rewards retain their existing keys.
- Original starter bytes live ONLY in TestLevels/legacy_level_001..003. Gameplay
  tests inject these fixtures with the original logical IDs; fixtures never enter
  the real catalog, level select or player progress.
- The reviewed shape pack keeps its 57 existing shape identities and positions,
  area bands, width <=24 and height <=36. Reviewed seeds, turn biases and sizes
  reproduce the installed pack. Scores use the unchanged rater; adjacent dips
  are at most 15 and jumps at most 45. The dedicated writer still refuses to
  overwrite existing catalog files. Export to an empty staging folder to rebuild.
- Six themes are available at their original catalog prices. ThemeDesign holds
  palettes; ThemeBackdrop draws three procedural GPU parallax layers and a soft
  35%-black rounded board panel. HUD bars receive matching readability surfaces.
  ThemeMenuBackground implements IMenuBackground; Paper retains its old renderer.
- Non-Paper themes take precedence over a skin's DarkBoardColor. Classic uses
  #E9EEF2 on dark themes and its exact original ink on Paper. Feedback colors
  pass a 3:1 contrast guard. Themes never change save formats or puzzle logic.
- Theme recipes: basalt/lava/embers; faceted ice/snow/light shaft; neon
  skyline/windows/holograms/data; trees/mushrooms/roots/fireflies; sandstone
  glyphs/torches; rotating nebula/stars/asteroids. Generated noise is cached.
  Three GPU layers plus the board panel use four draws, with no screen copy.
  Visible theme instances share at most 150 analytical motes, separate from
  the 600 arrow-particle budget. Gameplay pause freezes their clock.
- Live theme cards fill their own clipped preview area. The detail sheet adds
  a sample board with the equipped arrow. Names and state rows remain outside
  preview masks. Level select/settings/shop retain their plain screen style.

## Rare skin quality standard (Step 14D)
- Obsidian, Chromium, Celestial, Emerald and Crimson are procedural recipes in
  the shared ribbon/head/halo framework. RareThemeProcedural is the one added
  shader and is explicitly referenced by ArrowSkinResources; it also contains
  the theme passes. No Shader.Find or reference-image textures are introduced.
  Reference targets and Emerald's alternate remain Editor-only comparison input.
- Body silhouette and UV distance stay consistent with path geometry. Narrow
  highlights and reduced LOD heads keep dense paths distinct. Idle animation is
  shader-driven; state overlays restore exact rest values. Elemental recipes and
  Inferno's rest look stay unchanged. Classic and ice keep their existing paths.
- Signature moments are visual only and use preallocated exact-track residue
  slots, ring fields and constellation markers. Logic accepts the next tap
  immediately. Ambient timing, pooled trails and all previews share the 600 cap.
  Rare particles ease in/out and tumble; reduced LOD disables idle particles.
- Additional name/signature rows:

  | Skin | Name meaning | Signature |
  | --- | --- | --- |
  | Obsidian Shard | Volcanic glass, a razor cutting fragment | Molten hairline cracks cool to glass, a bright thin slice, tumbling glass fragments. |
  | Chromium Spike | Mirror chrome that pierces | Reflections gather at the tip, a cone ring and rainbow prism trail split into spectral sparks. |
  | Celestial Quill | A feather pen writing among stars | Golden ink follows the exact path; corner/end stars form a brief constellation. |
  | Emerald Viper | Jade serpent coiling to strike | The coil tightens, eyes flash, a lunge carries a side wave and shedding green scales. |
  | Crimson Relic | An ancient artifact with a living heart | A geometric sigil awakens under the arrow, bands light toward the ruby, a star flare and relic dust follow. |

- Dark boards: Obsidian #101218, Chromium #0F1116, Celestial #0E0D18,
  Emerald #07130D, Crimson #14070A. Equipped themes override these preferences.
- Flavor synthesis is quiet beneath the unchanged MoveCount pentatonic melody;
  settings silence both layers. Skin Gallery captures three refinement rounds,
  heroes, flight comparisons, bent/dense/blocked boards and eight-frame signatures.
  rare_sheet.png is the set review. Every future skin requires these visual and
  allocation/budget checks before release. Desktop Editor profiler measurements
  are evidence for that environment, not claims about physical phone performance.
- Reviewed starter recipes (all coverage floor 900, turn bias 0, max one bend,
  min two free, one attempt): Square 6x6 seed 10569 (4-5 arrows), Rectangle
  6x8 seed 21161 (5-7), RightTriangle 8x8 seed 30966 (6-8). ShapePackPlan uses
  CatalogShapePlan for the exact 57 reviewed long-path seeds and turn biases;
  do not sort or renumber this released identity list during a rebuild.
- A rejected body-growth attempt changes no occupancy. ShapeLevelGenerator
  therefore caches its ordered PLACE candidates and weights until a placement
  succeeds. It consumes exactly the previous RNG sequence and preserves golden
  snapshots; this avoids repeating mask scans during bounded failed attempts.


## Rare skin rebuild quality gate (Step 14D-R1)
- Rebuild and inspect one recipe at a time: Emerald Viper, Celestial Quill,
  Crimson Relic, Chromium Spike, Obsidian Shard. Save at least three comparison
  rounds and score silhouette, material shading, color, detail, signature,
  dense-board readability and both review themes before advancing. Four is a
  readable premium procedural interpretation, not a claim of photographic identity.
- RareR1.cginc authors the five analytical materials inside the existing explicit
  RareThemeProcedural shader. The theme function and Elemental shader recipes are
  unchanged. Reference images remain Editor comparison targets, never runtime inputs.
- Opaque rare surfaces have at least 0.30-cell visible width and heads measure
  1.0-1.2 cells. RareRibbonSurface preserves distance UVs and prepares reusable
  widened strip buffers. RareCapsSurface batches head and actual feather fletching
  (curved quill, angled barbs and ragged edges) into one draw. Full detail uses three
  base draws; dense idle uses two, with fletching retained. Halos sit behind solids.
- Viper composites an under-coil, wood cylinder and over-coil with jade scales,
  belly, gloss, eye, jaw and tongue. Quill uses asymmetric split vanes, winding
  stardust, a visible golden ink stroke and corner/end constellation. Relic uses
  raised gold collars, a beveled ruby setting with connected side gems, a tall
  central starburst and awakening sigil. Chromium uses cylindrical reflection bands,
  four broad spectral ribbons, gathering tip light and a cone shockwave. Obsidian
  uses six flowing glass strands, molten cracks, a cooling edge flash, a razor cut
  and twelve analytical tumbling edge fragments. Its small dark feather tail keeps
  the shared fletching rule even though the target primarily emphasizes strands.
- Rare anticipation is visual only (about 0.24 seconds within the existing flight
  duration). Puzzle moves remain instant. State outlines clear exactly; feedback,
  reverse travel, pause, hidden-board cleanup and restart behavior stay intact.
- Analytical glass fragments reserve twelve slots per active burst from the shared
  600-particle allowance. SkinParticlePool reclaims old native motes if necessary;
  clearing or completing a signature releases its reservation. Existing completion
  confetti retains its reserved capacity. Dense LOD suppresses optional atmosphere.
- RareR1Capture saves individual rounds and final hero/flight/bent/dense/blocked/
  theme/signature renders in Logs/SkinShots/RareR1. Signature captures advance at
  60 Hz between the eight panels. The final review retains reference-left/render-
  right rows in Logs/Review/rare_v2_reference_vs_built.png. Review every image.
- Quality tests measure rendered width and head length, draw count, visible
  anticipation/action changes, exact state restoration and prepared-buffer reuse.
  Native particle saturation is additionally observed in the Play Mode benchmark;
  Edit Mode verifies the shared capacity reservation without assuming native
  particle simulation is available. Keep existing assertions unchanged.

- Rare R1 fragment reservations also prune destroyed Unity renderers, so a plain
  C# signature pool cannot hold particle capacity after its host disappears even
  if a caller omitted Dispose. The fixed reservation table allocates nothing in
  Tick and shares the same six-hundred limit.

## Neon home, profile and snake-arrow loading screens (Step 14E)
- Brand art lives in Assets/Resources/Brand (loaded by UI/BrandArt at runtime; sprites are created with explicit
  9-slice borders, so import settings cannot break them). Orbitron Black is in Assets/Fonts/Resources.
- Loading element: Core/SnakeLoaderModel (pure, deterministic 7x7 self-solving snake-arrow puzzle, 11 snakes,
  one bump + white blocker flash) drawn by UI/SnakeLoaderGraphic (one mesh, neon tile, glow/core/highlight passes).
- UI/LoadingOverlay is a separate canvas (sorting 300) that blocks input. Fixed durations and sizes:
  Startup 3 s (large loader + logo + loading_background), Level 1 s (medium, "LEVEL N"), Profile 2 s (small).
  The game state underneath is already final; the overlay is cosmetic only.
- AppFlowController shows Startup after Initialize, Level on every new level start (not on restart from Pause),
  Profile when the header avatar opens ProfileView. LoadingScreensEnabled defaults to Application.isPlaying,
  so Edit Mode tests keep the instant flow; set it to true in a test to exercise the overlay.
- Home page (MainMenuView): always the neon backdrop (equipped themes affect the play area only). Header:
  stars pill (opens Level Select), profile avatar, coins pill (opens Shop). PLAY shows "LEVEL N" (ContinueIndex).
  CompletedText, ContinueText and QuitButton remain as hidden, still-updated API.
- ProfileView + Core/ProfileStats: read-only levels, stars, 3-star levels, coins, owned arrows/themes, equipped items.
- Editor/ReleaseSetup applies product name "Arrows Escape", company, id com.hebrusrobin.arrowsescape, portrait
  orientation, dark splash and the app icons (incl. Android adaptive) once per editor session.
- Editor/DevBridge (Editor-only) lets the AI assistant run refresh/tests/menu items/Play-mode timelines through
  Logs/DevBridge/cmd.txt while the Editor is open. Never shipped in builds.

## Neon UI everywhere, avatars and frames, developer coins (Step 14F)
- Every screen uses the neon kit (`UI/NeonKit.cs`): `neon_panel_fill/rim` (9-slice 60) for cards and panels, `neon_btn_fill(_cta/_danger)` + `neon_btn_rim` (9-slice 40) for buttons, glowing line icons `icon_*.png`, Orbitron labels. `UiFactory.Button` builds neon buttons (`NeonButtonStyle` Normal/Primary/Danger); `UiFactory.Panel(..., ViewTheme.PanelCard)` builds a neon panel. Label text stays the first `Text` child so button-text tests keep working.
- Levels, Settings, Pause, Confirm, Level Complete, HUD, Shop and Profile are neon. The play area itself keeps the equipped shop theme; the HUD info bar is dark glass so its light text (`HudView.TextColor/AmberColor/RedColor`) reads on every theme. Settings shows the neon backdrop from the home page and only a dim veil when opened over a paused board.
- Home page: PLAY/LEVEL text and tile icons/labels are centred on the lit face of the button art (the art has a drop edge at the bottom). Shop icon is a trolley.
- Shop slots: `ShopSlot { Arrow, Theme, Avatar, Frame }`. 12 avatars (`avatar_*`, free `avatar_player`) and 10 frames (`frame_*`, free `frame_bronze`), art in `Resources/Brand/Avatars` (384 px circles) and `Resources/Brand/Frames` (512 px, transparent centre, inner ring radius 166 px; avatar drawn at 0.664 of the frame — `AvatarBadge`). Art is generated by scripts (`avatars.py`, `frames.py`, `kit.py`), not hand-edited.
- Inventory gains `EquippedAvatarId`/`EquippedFrameId` (sanitized to the free defaults). The home profile button and profile page show `AvatarBadge`; tapping the profile avatar opens the shop on Avatars and Back returns to the profile.
- Developer coins: five quick taps (≤0.8 s apart, `TapStreak`) on the coin pill grant `EconomyConfig.DeveloperGrant` (50,000) and save. The first tap still opens the shop; the shop's coin pill sits in the same place and continues the streak. Turn off with `EconomyConfig.DeveloperGrantEnabled = false` before a store release.

## Curved painted arrows, shape framing, level intro, 60 fps, premium avatars (Step 14G)
- Arrow paths are smoothed by `CurvedTrack` (quadratic fillets, radius 0.36 cells, 6 points per bend) before the `PathRibbon` is built; every skin inherits the rounded bends. The head cell is never filleted; body length = curved distance to the head cell, `ExitDistance` is unchanged.
- Classic arrows use painted 2D art from `Resources/Arrows`: `arrow_body.png` (greyscale tube profile across the ribbon's V) and `arrow_head.png` (rounded bevelled head, tip at the right edge, pivot (1, 0.5)). Both are tinted by the arrow colour; the ribbon's flat head triangle is hidden (vertex alpha 0) while the head sprite is shown. Art is generated by `arrowart.py`.
- Rare arrows draw no fletching tail (`RareCapsSurface.Build(..., showTail: false)`).
- `DotGrid` draws dots only on cells the arrows occupy at the start. `BoardView.ContentBounds` is the shape's bounds; `CameraFitter` frames and centres it, and the theme readability panel follows it.
- `BoardIntro` lands the arrows (lift + fade, centre-outward wave, ~0.8 s) when a level starts in Play mode; AppFlow plays it as the 1 s loader fades (no delay on restart) and blocks board input while it runs.
- `FrameRateSetup` sets `targetFrameRate = 60`, `vSyncCount = 0` and keeps the screen awake (phones default to 30 fps otherwise).
- The loader puzzle sits on the neon panel (`SnakeLoaderGraphic.DrawTile = false`) and its snakes have rounded bends.
- Premium avatars (10, category "Premium", 800–2000 coins) are cut from the user's reference sheet by `premium.py`.

## Warp backdrop, 300 more levels, trophies, leagues and world ranking (Step 14H)
- Neon backdrop = static `home_base.png` (sky, glow, stars, horizon) + `WarpBackground` (MaskableGraphic, own nested canvas) + `home_arrows.png`. Warp streaks fly out of the vanishing point and the floor grid rows scroll toward the viewer; both use one camera model and `WarpBackground.Speed`, driven by one static unscaled clock so every screen stays in step. Art by `warpbase.py`.
- Catalog: 360 levels. 1–60 are the reviewed pack. 61–360 repeat Easy, Medium, Hard (61 Easy, 62 Medium, 63 Hard …), generated offline by the `levelgen` console tool (compiles Core) and listed in `Editor/CatalogExpansionPlan` (number, tier, shape, size, seed). Easy/Medium stay inside their score bands with gently growing boards; Hard boards grow through the catalog and may rate Expert. All boards obey the catalog rules (arrow length 4–10, ≥90 % coverage, ≤45 % per direction, solvable, at least one blocked arrow).
- Long lists clip with `RectMask2D` (culls off-screen tiles) instead of stencil masks.
- Trophies (`Core/TrophyRules`): every valid clear receipt awards 3★ = 30, 2★ = 25–30, 1★ = 15–25 (deterministic per level + attempt). `Inventory.Trophies`; `ShopService.LastTrophyReward`; `GameBootstrap.LastTrophyReward/LastLeaguePromoted`. Level Complete shows coins and trophies side by side and a promotion banner.
- Leagues: Bronze 0, Silver 300, Gold 900, Platinum 1800, Diamond 3200, Crown 5000 trophies. Emblems in `Resources/Brand/Leagues` (`leagues.py`). Profile shows a league card (tap → ranking).
- World ranking: home tile RANKING (four 214 px tiles) and `RankingView` (your row + top 50). Score = trophies × 10000 + stars (`TrophyRules.RankingScore`). Backend is `ILeaderboardService` via `LeaderboardRegistry` (Offline by default, used by Edit Mode tests). `Online/UgsLeaderboard` (assembly ArrowEscape.Online, package com.unity.services.leaderboards) registers itself at startup, signs in anonymously and uses leaderboard id `world_ranking`; it never throws and shows an offline notice when the project is not linked or there is no network. Scores are submitted after every trophy award and when the ranking opens.

## Daily puzzle, daily rewards, limited hints/undos, uniform arrow heads, developer level unlock (Step 14I)
- Every skin wears the classic painted head (`SkinVisual.UniformHead`: same sprite, size and tip placement as `PathArrowView.HeadArt`, tinted `SkinVisual.HeadColor(skin)`); skin-specific heads (SDF heads, rare caps, art heads) and tails (ice fletching, art tails) are never drawn. Only the body keeps the skin.
- Hints and undos are limited: `Inventory.Hints/Undos` start at 3 (`EconomyConfig.StartingHints/StartingUndos`). The HUD shows the count on each button ("+" at zero). With none left the `BoostOfferView` offers "watch ad +1" or "buy 1 for 300 coins" (`EconomyConfig.BoostPrice`). A hint is spent only when one is shown; an undo only when something was undone.
- Rewarded ads go through `RewardedAds.Service` (`IRewardedAds`). `AdPlaceholderView` (3-second stand-in, instant outside Play mode) is installed until a real ad network (e.g. Unity LevelPlay) is registered.
- Daily rewards (`DailyRewardRules`): one claim per local calendar day, days 1-7 = 300 coins, 1 undo, 1 hint, 500 coins, 1 undo, 1 hint, mystery. Missing a day does not reset the cycle. Day 7 picks from undo/hint/100-300 coins/an unowned arrow skin, frame or avatar (cheapest third per slot) and `DailyRewardsView` spins a reel before revealing it. The page opens by itself after the startup loader when a reward is waiting.
- Daily puzzle: `DailyPuzzleBuilder.Build(yyyymmdd)` generates one 24 x 36 board (outline from the date) — the same board for every player on a date; built on a worker thread in Play mode. `GameBootstrap.StartDailyLevel` plays it without touching level progress; `BoardZoom.Forced` enables pinch/scroll zoom, one-finger drag pan and the HUD zoom buttons. Three hearts: each mistake costs one; at zero `DailyFailView` offers restart or an ad to continue with one heart. On a clear the streak is saved (`DailyStreakRules`), `DailyResultView` shows the fire streak and any new badge, and Share copies a short text (date, moves, mistakes, hearts, streak) to the clipboard.
- Streak = consecutive days solved (alive until the end of the next day); badges at 100, 200 … 1,000 days on the Daily page. Art in `Resources/Brand/Daily` (`daily.py`).
- Developer option (with `EconomyConfig.DeveloperGrantEnabled`): three quick taps on a locked level tile open that level for testing (`LockedTileTap`, `GameBootstrap.TryStartLevelIgnoringLock`).
