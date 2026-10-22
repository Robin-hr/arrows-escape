using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.UI
{
    /// <summary>A player's avatar portrait inside their equipped frame (art in Resources/Brand/Avatars and /Frames).</summary>
    public sealed class AvatarBadge : MonoBehaviour
    {
        /// <summary>Avatar diameter as a fraction of the frame art (inner ring radius 166 of 512, plus overlap).</summary>
        public const float AvatarScale = 0.664f;
        /// <summary>Portrait image.</summary>
        public UnityEngine.UI.RawImage AvatarImage { get; private set; }
        /// <summary>Frame image drawn over the portrait.</summary>
        public UnityEngine.UI.RawImage FrameImage { get; private set; }
        /// <summary>Shown avatar id.</summary>
        public string AvatarId { get; private set; }
        /// <summary>Shown frame id, or null for none.</summary>
        public string FrameId { get; private set; }

        /// <summary>Creates a badge of the given square size.</summary>
        public static AvatarBadge Create(string name, Transform parent, float size, Vector2 position)
        {
            var rect = UiFactory.Rect(name, parent, new Vector2(size, size), position);
            var badge = rect.gameObject.AddComponent<AvatarBadge>();
            badge.AvatarImage = Layer("Avatar", rect, AvatarScale);
            badge.FrameImage = Layer("Frame", rect, 1f);
            badge.Set(Inventory.DefaultAvatarId, Inventory.DefaultFrameId);
            return badge;
        }

        private static UnityEngine.UI.RawImage Layer(string name, RectTransform parent, float scale)
        {
            var image = UiFactory.Rect(name, parent, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            float inset = (1f - scale) * 0.5f;
            image.rectTransform.anchorMin = new Vector2(inset, inset); image.rectTransform.anchorMax = new Vector2(1 - inset, 1 - inset);
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Texture for an avatar id, or null.</summary>
        public static Texture2D AvatarTexture(string id) => string.IsNullOrEmpty(id) ? null : BrandArt.Texture("Avatars/" + id);
        /// <summary>Texture for a frame id, or null.</summary>
        public static Texture2D FrameTexture(string id) => string.IsNullOrEmpty(id) ? null : BrandArt.Texture("Frames/" + id);

        /// <summary>Shows an avatar and frame; a null frame hides the frame layer.</summary>
        public void Set(string avatarId, string frameId)
        {
            AvatarId = avatarId; FrameId = frameId;
            Texture2D avatar = AvatarTexture(avatarId), frame = FrameTexture(frameId);
            AvatarImage.texture = avatar; AvatarImage.color = avatar != null ? Color.white : new Color(0.2f, 0.16f, 0.55f, 1f);
            FrameImage.texture = frame; FrameImage.enabled = frame != null;
        }
    }
}
