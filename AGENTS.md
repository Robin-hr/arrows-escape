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
