namespace ArrowEscape.Core
{
    /// <summary>Orthographic camera settings for a board centered at the world origin.</summary>
    public readonly struct ScreenFitResult
    {
        /// <summary>Half the visible world height.</summary>
        public float OrthographicSize { get; }
        /// <summary>The camera's horizontal world position.</summary>
        public float CameraX { get; }
        /// <summary>The camera's vertical world position.</summary>
        public float CameraY { get; }

        /// <summary>Creates immutable camera settings.</summary>
        public ScreenFitResult(float orthographicSize, float cameraX, float cameraY)
        {
            OrthographicSize = orthographicSize;
            CameraX = cameraX;
            CameraY = cameraY;
        }
    }
}
