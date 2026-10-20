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
