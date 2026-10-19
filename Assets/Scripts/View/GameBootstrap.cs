using ArrowEscape.Core;
using ArrowEscape.Data;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Loads and draws a level automatically, without input or gameplay.</summary>
    [RequireComponent(typeof(BoardView), typeof(CameraFitter))]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int startLevelIndex = 0;
        private int currentLevelIndex;

        private void Start() => ShowLevel(startLevelIndex);

        /// <summary>Loads and shows a zero-based level index, or logs errors and clears the view.</summary>
        public void ShowLevel(int index)
        {
            BoardView view = GetComponent<BoardView>();
            view.Clear();
            var paths = LevelRepository.GetLevelResourcePaths();
            if (index < 0 || index >= paths.Count)
            {
                Debug.LogError($"ArrowEscape: level index {index} is unavailable ({paths.Count} levels found).", this);
                return;
            }
            LevelLoadResult result = LevelRepository.LoadFromResources(paths[index]);
            if (!result.Success)
            {
                foreach (string error in result.Errors) Debug.LogError(error, this);
                return;
            }
            currentLevelIndex = index;
            view.Show(result.Board);
            GetComponent<CameraFitter>().Fit(result.Board.Width, result.Board.Height);
        }

        /// <summary>Shows the next level, wrapping from the last back to the first.</summary>
        [ContextMenu("Next Level")]
        public void NextLevel() => ChangeLevel(1);

        /// <summary>Shows the previous level, wrapping from the first back to the last.</summary>
        [ContextMenu("Previous Level")]
        public void PreviousLevel() => ChangeLevel(-1);

        private void ChangeLevel(int offset)
        {
            int count = LevelRepository.GetLevelResourcePaths().Count;
            ShowLevel(count == 0 ? 0 : (currentLevelIndex + offset + count) % count);
        }

        private void OnDestroy() => SpriteFactory.ClearCache();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeRoot()
        {
            if (Object.FindFirstObjectByType<GameBootstrap>(FindObjectsInactive.Include) != null) return;
            // Required components are added automatically, all on this one root.
            new GameObject("ArrowEscape").AddComponent<GameBootstrap>();
        }
    }
}
