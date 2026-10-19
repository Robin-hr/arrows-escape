using System;

namespace ArrowEscape.Core
{
    /// <summary>Pure math for fitting a centered board inside reserved screen margins.</summary>
    public static class ScreenFit
    {
        /// <summary>Fits a board with total padding per axis; margins are screen fractions from 0 to 0.45.</summary>
        public static ScreenFitResult Compute(int boardWidth, int boardHeight, float aspect,
            float marginLeft, float marginRight, float marginTop, float marginBottom,
            float paddingCells = 0.5f)
        {
            if (boardWidth < 1) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            if (boardHeight < 1) throw new ArgumentOutOfRangeException(nameof(boardHeight));
            if (!Finite(aspect) || aspect <= 0) throw new ArgumentOutOfRangeException(nameof(aspect));
            ValidateMargin(marginLeft, nameof(marginLeft));
            ValidateMargin(marginRight, nameof(marginRight));
            ValidateMargin(marginTop, nameof(marginTop));
            ValidateMargin(marginBottom, nameof(marginBottom));
            if (!Finite(paddingCells) || paddingCells < 0)
                throw new ArgumentOutOfRangeException(nameof(paddingCells));

            float usableWidth = 1 - marginLeft - marginRight;
            float usableHeight = 1 - marginTop - marginBottom;
            // Visible width = 2 * size * aspect; visible height = 2 * size.
            float size = Math.Max((boardWidth + paddingCells) / (2 * aspect * usableWidth),
                (boardHeight + paddingCells) / (2 * usableHeight));
            // Offset the camera so the usable rectangle's center lands on world (0, 0).
            return new ScreenFitResult(size, (marginRight - marginLeft) * size * aspect,
                (marginTop - marginBottom) * size);
        }

        private static void ValidateMargin(float margin, string name)
        {
            if (!Finite(margin) || margin < 0 || margin > 0.45f)
                throw new ArgumentOutOfRangeException(name, "Screen margins must be between 0 and 0.45.");
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
