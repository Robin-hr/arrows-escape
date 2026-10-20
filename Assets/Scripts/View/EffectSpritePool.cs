using System;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Bounded reusable sprite slots; full pools overwrite in round-robin order.</summary>
    public sealed class EffectSpritePool
    {
        private readonly SpriteRenderer[] sprites;
        private readonly Transform root;
        private readonly int sortingOrder;
        private int next;
        /// <summary>Maximum number of sprite objects.</summary>
        public int Capacity => sprites.Length;
        /// <summary>Creates a container; individual sprite objects are created lazily.</summary>
        public EffectSpritePool(Transform parent, string name, int capacity, int sortingOrder)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            sprites = new SpriteRenderer[capacity]; this.sortingOrder = sortingOrder;
            root = new GameObject(name).transform; root.SetParent(parent,false);
        }
        /// <summary>Activates the next slot and returns its stable array index.</summary>
        public SpriteRenderer Acquire(Sprite sprite, out int slot)
        {
            slot = next; next = (next + 1) % Capacity;
            if (root == null) return null;
            if (sprites[slot] == null)
            {
                var child = new GameObject("Effect " + slot); child.transform.SetParent(root,false);
                sprites[slot] = child.AddComponent<SpriteRenderer>();
            }
            SpriteRenderer renderer = sprites[slot]; renderer.sprite = sprite; renderer.sortingOrder = sortingOrder;
            renderer.transform.localRotation = Quaternion.identity; renderer.gameObject.SetActive(true);
            return renderer;
        }
        /// <summary>Returns a created slot, including an inactive one; unused/destroyed slots return null.</summary>
        public SpriteRenderer Get(int slot) => slot >= 0 && slot < Capacity && sprites[slot] != null ? sprites[slot] : null;
        /// <summary>Hides one slot without destroying it.</summary>
        public void Release(int slot) { SpriteRenderer renderer = Get(slot); if (renderer != null) renderer.gameObject.SetActive(false); }
        /// <summary>Hides all slots; their objects remain reusable.</summary>
        public void ReleaseAll() { for (int i = 0; i < Capacity; i++) Release(i); }
    }
}
