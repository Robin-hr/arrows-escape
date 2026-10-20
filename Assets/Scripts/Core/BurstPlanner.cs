using System;

namespace ArrowEscape.Core
{
    /// <summary>Writes repeatable, evenly spread particles into a caller-owned buffer.</summary>
    public static class BurstPlanner
    {
        /// <summary>Places one jittered angle in each slice of the requested cone.</summary>
        public static int Plan(int seed, BurstSettings settings, ParticleSpec[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (settings.Count <= 0) return 0;
            Validate(settings);
            var random = new DeterministicRandom(seed);
            int count = Math.Min(settings.Count, buffer.Length);
            float slice = 2 * settings.ConeHalfAngleDegrees / settings.Count;
            for (int i = 0; i < count; i++)
            {
                double angle = (settings.DirectionDegrees - settings.ConeHalfAngleDegrees + (i + random.NextFloat()) * slice) * Math.PI / 180;
                buffer[i] = new ParticleSpec {
                    DirX = (float)Math.Cos(angle), DirY = (float)Math.Sin(angle),
                    Distance = random.Range(settings.MinDistance,settings.MaxDistance),
                    Lifetime = random.Range(settings.MinLifetime,settings.MaxLifetime),
                    StartSize = settings.StartSize, EndSize = settings.EndSize, ColorIndex = i % settings.ColorCount
                };
            }
            return count;
        }
        private static void Validate(BurstSettings s)
        {
            if (!Finite(s.DirectionDegrees) || !Finite(s.ConeHalfAngleDegrees) || s.ConeHalfAngleDegrees < 0 || s.ConeHalfAngleDegrees > 180 ||
                !Finite(s.MinDistance) || !Finite(s.MaxDistance) || s.MinDistance > s.MaxDistance ||
                !Finite(s.MinLifetime) || !Finite(s.MaxLifetime) || s.MinLifetime > s.MaxLifetime ||
                !(s.StartSize > 0) || !Finite(s.StartSize) || !(s.EndSize > 0) || !Finite(s.EndSize) || s.ColorCount < 1)
                throw new ArgumentOutOfRangeException(nameof(s));
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        /// <summary>Converts a grid direction to standard mathematical degrees.</summary>
        public static float AngleFor(Direction direction)
        {
            switch (direction) {
                case Direction.Right: return 0; case Direction.Up: return 90;
                case Direction.Left: return 180; case Direction.Down: return 270;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
