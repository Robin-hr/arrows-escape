using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Fits the main camera and refits only when screen geometry changes.</summary>
    public sealed class CameraFitter : MonoBehaviour
    {
        private int boardWidth;
        private int boardHeight;
        private int screenWidth;
        private int screenHeight;
        private Rect safeArea;
        private bool missingCameraLogged;
        private Color SkinBackground
        {
            get
            {
                var board=GetComponent<BoardView>();if(board!=null&&board.HasDarkTheme)return ThemeDesign.Background(board.ThemeId);if(board!=null&&ArrowSkinRegistry.Find(board.ArrowSkinId)?.PrefersDarkBoard==true)return board.IsLightBoard?ViewTheme.Background:ArrowSkinRegistry.Find(board.ArrowSkinId).DarkBoardColor;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var skins=GetComponent<SkinDirector>();if(skins!=null&&skins.IsDarkBoard)return new Color32(14,17,22,255);
#endif
                return ViewTheme.Background;
            }
        }

        /// <summary>Fits the centered board, reserving default margins plus safe-area insets.</summary>
        public void Fit(int width, int height)
        {
            boardWidth = width;
            boardHeight = height;
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            safeArea = Screen.safeArea;
            Camera camera = Camera.main;
            if (camera == null)
            {
                if (!missingCameraLogged)
                    Debug.LogError("ArrowEscape: no Camera.main found. Tag the scene camera MainCamera.", this);
                missingCameraLogged = true;
                return;
            }
            if (screenWidth < 1 || screenHeight < 1) return;
            ScreenFitResult fit = ScreenFit.Compute(width, height, (float)screenWidth / screenHeight,
                Margin(ViewTheme.SideMargin, safeArea.xMin / screenWidth),
                Margin(ViewTheme.SideMargin, (screenWidth - safeArea.xMax) / screenWidth),
                Margin(ViewTheme.TopMargin, (screenHeight - safeArea.yMax) / screenHeight),
                Margin(ViewTheme.BottomMargin, safeArea.yMin / screenHeight), ViewTheme.PaddingCells);
            camera.orthographic = true;
            camera.orthographicSize = fit.OrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkinBackground;
            camera.transform.SetPositionAndRotation(new Vector3(fit.CameraX, fit.CameraY, ViewTheme.CameraZ),
                Quaternion.identity);
            var zoom=GetComponent<BoardZoom>();if(zoom!=null)zoom.ResetFit();
        }

        /// <summary>Applies the dark solid background immediately, silently skipping a missing camera.</summary>
        public void ApplyBackground()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkinBackground;
        }

        private void Update()
        {
            if (boardWidth > 0 && boardHeight > 0 && (Screen.width != screenWidth ||
                Screen.height != screenHeight || Screen.safeArea != safeArea))
                Fit(boardWidth, boardHeight);
        }

        // Keep a usable rectangle even if a device reports unusually large insets.
        private static float Margin(float reserved, float inset) =>
            Mathf.Clamp(reserved + inset, 0, ViewTheme.MaxMargin);
    }
}
