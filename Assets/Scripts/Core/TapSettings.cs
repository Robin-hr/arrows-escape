using System;

namespace ArrowEscape.Core
{
    /// <summary>Immutable tap distances, measured from cell centers in cells.</summary>
    public sealed class TapSettings
    {
        /// <summary>Maximum distance at which a press can select an arrow.</summary>
        public float SelectRadius { get; }
        /// <summary>Maximum release distance from the originally pressed arrow.</summary>
        public float CancelRadius { get; }

        /// <summary>Creates finite, positive selection and cancellation distances.</summary>
        public TapSettings(float selectRadius = 0.6f, float cancelRadius = 0.8f)
        {
            if (float.IsNaN(selectRadius) || float.IsInfinity(selectRadius) || selectRadius <= 0)
                throw new ArgumentOutOfRangeException(nameof(selectRadius));
            if (float.IsNaN(cancelRadius) || float.IsInfinity(cancelRadius) || cancelRadius < selectRadius)
                throw new ArgumentOutOfRangeException(nameof(cancelRadius));
            SelectRadius = selectRadius;
            CancelRadius = cancelRadius;
        }
    }
}
