using System.Collections.Generic;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Creates a tidy runtime hierarchy for a centered board.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        private readonly Dictionary<int, ArrowView> arrowViews = new Dictionary<int, ArrowView>();
        private Transform root;
        private Material arrowMaterial;
        private Material cellMaterial;
        private int width;
        private int height;

        /// <summary>Replaces the current display with the supplied board; null clears it.</summary>
        public void Show(Board board)
        {
            Clear();
            if (board == null) return;
            arrowMaterial = PrepareMaterial(arrowMaterial, SpriteFactory.GetArrowSprite());
            width = board.Width;
            height = board.Height;
            root = new GameObject("Board").transform;
            root.SetParent(transform, false);
            if (width <= ViewTheme.DotBoardLimit && height <= ViewTheme.DotBoardLimit)
                CreateDots();
            foreach (Arrow arrow in board.Arrows)
            {
                GameObject child = CreateChild($"Arrow {arrow.Id}", arrow.X, arrow.Y);
                ArrowView view = child.AddComponent<ArrowView>();
                view.Initialize(arrow, arrowMaterial);
                arrowViews.Add(arrow.Id, view);
            }
        }

        /// <summary>Maps a cell center directly to y-up world space centered on the origin.</summary>
        public Vector3 CellToWorld(int x, int y) =>
            new Vector3(x - (width - 1) * 0.5f, y - (height - 1) * 0.5f, 0);

        /// <summary>Finds the current arrow view with this identifier.</summary>
        public bool TryGetArrowView(int id, out ArrowView view) => arrowViews.TryGetValue(id, out view);

        /// <summary>Removes the generated display; runtime destruction finishes at frame end.</summary>
        public void Clear()
        {
            arrowViews.Clear();
            width = height = 0;
            if (root == null) return;
            root.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(root.gameObject);
            else DestroyImmediate(root.gameObject);
            root = null;
        }

        private void CreateDots()
        {
            cellMaterial = PrepareMaterial(cellMaterial, SpriteFactory.GetCellSprite());
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    SpriteRenderer dot = CreateChild($"Cell {x},{y}", x, y).AddComponent<SpriteRenderer>();
                    dot.sprite = SpriteFactory.GetCellSprite();
                    dot.color = ViewTheme.CellDot;
                    dot.sortingOrder = ViewTheme.CellSortingOrder;
                    dot.sharedMaterial = cellMaterial;
                    dot.transform.localScale = Vector3.one * ViewTheme.CellDotSize;
                }
        }

        private GameObject CreateChild(string name, int x, int y)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root, false);
            child.transform.position = CellToWorld(x, y);
            return child;
        }

        private void OnDestroy()
        {
            DestroyMaterial(arrowMaterial);
            DestroyMaterial(cellMaterial);
        }

        private static Material PrepareMaterial(Material material, Sprite sprite)
        {
            if (material == null)
                material = new Material(Shader.Find("Sprites/Default"))
                {
                    name = sprite.name + " default sprites", hideFlags = HideFlags.HideAndDontSave
                };
            // Bind each texture explicitly: URP must not reuse the dot texture for arrows.
            // All arrows still share one material, and all dots share another.
            material.mainTexture = sprite.texture;
            return material;
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
