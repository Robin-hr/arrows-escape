using System.IO;
using ArrowEscape.Core;
using ArrowEscape.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArrowEscape.Tests
{
    /// <summary>Checks real camera pixels with cell dots and arrows sharing the screen.</summary>
    public class SpriteRenderingTests
    {
        private GameObject host;
        private GameObject cameraObject;
        private RenderTexture target;
        private Texture2D image;

        /// <summary>Removes every temporary renderer, camera, texture, and cached sprite.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(image);
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            SpriteFactory.ClearCache();
        }

        /// <summary>Rendered head pixels point along the direction and remain wider than the tail.</summary>
        [TestCase(Direction.Up)]
        [TestCase(Direction.Left)]
        [TestCase(Direction.Down)]
        [TestCase(Direction.Right)]
        public void RenderedHeadPointsInCorrectWorldDirection(Direction direction)
        {
            host = new GameObject("Sprite render test");
            BoardView boardView = host.AddComponent<BoardView>();
            var board = new Board(1, 1);
            board.TryAddArrow(1, 0, 0, direction, out _);
            boardView.Show(board);
            foreach (Transform child in host.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
            Assert.That(boardView.TryGetArrowView(1, out ArrowView arrow), Is.True);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(arrow.transform.eulerAngles.z,
                ArrowRotation.GetRotationZ(direction))), Is.LessThan(0.001f));
            Assert.That(arrow.SpriteRenderer.sprite, Is.SameAs(SpriteFactory.GetArrowSprite()));
            Assert.That(arrow.SpriteRenderer.sortingOrder, Is.GreaterThan(ViewTheme.CellSortingOrder));

            Camera camera = CreateCamera();
            target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
            target.Create();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            ReadImage();
            SaveEvidence(direction);

            Vector3 forward = new Vector3(direction.GetDx(), direction.GetDy(), 0);
            Vector3 side = new Vector3(-forward.y, forward.x, 0);
            Vector3 head = (forward * 0.18f + side * 0.18f) * ViewTheme.ArrowFillRatio;
            Vector3 tail = (-forward * 0.18f + side * 0.18f) * ViewTheme.ArrowFillRatio;
            Assert.That(BrightnessAt(camera, head), Is.GreaterThan(0.6f), "Head shoulder must be filled.");
            Assert.That(BrightnessAt(camera, tail), Is.LessThan(0.1f), "Beside the tail must be empty, not a circle.");
        }

        private Camera CreateCamera()
        {
            cameraObject = new GameObject("Sprite test camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 0.6f;
            camera.aspect = 1;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 31;
            camera.transform.position = new Vector3(0, 0, -10);
            return camera;
        }

        private void ReadImage()
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
            }
            finally { RenderTexture.active = previous; }
        }

        private float BrightnessAt(Camera camera, Vector3 world)
        {
            Vector3 point = camera.WorldToViewportPoint(world);
            Color pixel = image.GetPixel(Mathf.FloorToInt(point.x * image.width),
                Mathf.FloorToInt(point.y * image.height));
            return (pixel.r + pixel.g + pixel.b) / 3;
        }

        private void SaveEvidence(Direction direction)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"arrow-render-{direction}.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
    }
}
