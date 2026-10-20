namespace ArrowEscape.UI
{
    /// <summary>Step 14E can replace menu background presentation without changing navigation.</summary>
    public interface IMenuBackground
    {
        /// <summary>Resets the presentation on entering the menu.</summary>
        void Restart();
        /// <summary>Advances an already prepared background.</summary>
        void Tick(float deltaTime);
    }
}
