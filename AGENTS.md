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
