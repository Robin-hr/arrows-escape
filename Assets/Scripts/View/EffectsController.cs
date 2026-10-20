using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Owns bounded sprite effects outside the replaceable board root.</summary>
    [ExecuteAlways]
    public sealed class EffectsController : MonoBehaviour
    {
        private BoardView board;
        private ParticleSpec[] buffer;
        private Color[] confetti, exitColors;
        private Color[] iceColors;
        /// <summary>Independent slide-matching streak timelines.</summary>
        public StreakPlayer Streaks { get; private set; }
        /// <summary>Shared exit and confetti particle timelines.</summary>
        public ParticlePlayer Particles { get; private set; }
        /// <summary>Radial cell-dot wave.</summary>
        public DotWavePlayer DotWave { get; private set; }
        /// <summary>One gentle background pulse.</summary>
        public CameraPulsePlayer CameraPulse { get; private set; }
        /// <summary>Whether anything needs another tick or exact restoration.</summary>
        public bool HasActiveEffects => Streaks != null && (Streaks.ActiveCount > 0 || Particles.ActiveCount > 0 || DotWave.IsActive || CameraPulse.IsActive);
        /// <summary>Creates pools lazily and binds board replacement cleanup.</summary>
        public void Bind(BoardView boardView)
        {
            ClearAll(); if (board != null) board.BoardChanged -= ClearAll; board = boardView;
            if (board == null) return;
            board.BoardChanged += ClearAll;
            if (Streaks == null) {
                Streaks = new StreakPlayer(new EffectSpritePool(transform,"Streaks",ViewTheme.StreakCapacity,ViewTheme.EffectsSortingOrder),board);
                Particles = new ParticlePlayer(new EffectSpritePool(transform,"Particles",ViewTheme.ParticleCapacity,ViewTheme.EffectsSortingOrder),board);
                DotWave = new DotWavePlayer(); CameraPulse = new CameraPulsePlayer(); buffer = new ParticleSpec[ViewTheme.ParticleCapacity];
                var palette = ViewTheme.ConfettiColors; confetti = new Color[palette.Count]; for (int i = 0; i < confetti.Length; i++) confetti[i] = palette[i];
                exitColors = new[] { ViewTheme.ExitBurstColor };
                iceColors=new[]{ViewTheme.IceBurstColor};
            }
            else Streaks.Bind(board);
        }
        /// <summary>Starts a streak and a seeded cone of warm dots at the arrow tip.</summary>
        public void PlayExit(Arrow arrow, int boardWidth, int boardHeight, int moveCount)
        {
            if (board == null || Streaks == null || arrow == null) return;
            Streaks.Start(boardWidth,boardHeight,arrow);
            var settings = new BurstSettings { Count = ViewTheme.ExitBurstCount, DirectionDegrees = BurstPlanner.AngleFor(arrow.Direction),
                ConeHalfAngleDegrees = ViewTheme.ExitBurstCone, MinDistance = ViewTheme.ExitBurstMinDistance, MaxDistance = ViewTheme.ExitBurstMaxDistance,
                MinLifetime = ViewTheme.ExitBurstMinLifetime, MaxLifetime = ViewTheme.ExitBurstMaxLifetime, StartSize = ViewTheme.ExitBurstStartSize, EndSize = ViewTheme.ExitBurstEndSize, ColorCount = 1 };
            int count = BurstPlanner.Plan(unchecked(arrow.Id * 7919 + moveCount * 104729),settings,buffer);
            Vector2 origin = board.CellToWorld(arrow.X,arrow.Y); origin += new Vector2(arrow.Direction.GetDx(),arrow.Direction.GetDy()) * .35f;
            Particles.Spawn(origin,buffer,count,exitColors);
        }
        /// <summary>Starts the outward dot wave, center confetti, and one background pulse.</summary>
        public void PlayComplete(int width, int height, int originX, int originY)
        {
            if (board == null || Streaks == null) return;
            DotWave.Start(board,width,height,originX,originY); CameraPulse.Start();
            float radius = .5f * Mathf.Max(width,height) + 1;
            var settings = new BurstSettings { Count = ViewTheme.ConfettiCount, ConeHalfAngleDegrees = 180,
                MinDistance = ViewTheme.ConfettiMinDistance * radius, MaxDistance = ViewTheme.ConfettiMaxDistance * radius,
                MinLifetime = ViewTheme.ConfettiMinLifetime, MaxLifetime = ViewTheme.ConfettiMaxLifetime,
                StartSize = ViewTheme.ConfettiStartSize, EndSize = ViewTheme.ConfettiEndSize, ColorCount = confetti.Length };
            int count = BurstPlanner.Plan(unchecked(width * 31 + height),settings,buffer);
            Particles.Spawn(board.CellToWorld((width-1)/2f,(height-1)/2f),buffer,count,confetti);
        }
        /// <summary>Clears only streaks, as required after undo.</summary>
        public void ClearStreaks() => Streaks?.ClearAll();
        /// <summary>Spawns ice chips at the new logical front edge without moving any game object.</summary>
        public void PlaySlideStop(Arrow moved)
        {
            if(board==null||Particles==null||moved==null)return;
            var settings=new BurstSettings{Count=ViewTheme.IceBurstCount,DirectionDegrees=BurstPlanner.AngleFor(moved.Direction),ConeHalfAngleDegrees=ViewTheme.IceBurstCone,
                MinDistance=ViewTheme.IceBurstMinDistance,MaxDistance=ViewTheme.IceBurstMaxDistance,MinLifetime=ViewTheme.IceBurstMinLifetime,MaxLifetime=ViewTheme.IceBurstMaxLifetime,
                StartSize=ViewTheme.IceBurstStartSize,EndSize=ViewTheme.IceBurstEndSize,ColorCount=1};
            int count=BurstPlanner.Plan(unchecked(moved.Id*7919+moved.X*31+moved.Y*17),settings,buffer);
            Vector2 origin=board.CellToWorld(moved.X,moved.Y);origin+=new Vector2(moved.Direction.GetDx(),moved.Direction.GetDy())*.35f;
            Particles.Spawn(origin,buffer,count,iceColors);
        }
        /// <summary>Advances every effect without allocations.</summary>
        public void Tick(float deltaTime) { Streaks?.Tick(deltaTime); Particles?.Tick(deltaTime); DotWave?.Tick(deltaTime); CameraPulse?.Tick(deltaTime); }
        /// <summary>Hides pooled objects and restores every borrowed color and scale exactly.</summary>
        public void ClearAll() { Streaks?.ClearAll(); Particles?.ClearAll(); DotWave?.ClearAll(); CameraPulse?.ClearAll(); }
        private void OnDisable() => ClearAll();
        private void OnDestroy() { ClearAll(); if (board != null) board.BoardChanged -= ClearAll; }
    }
}
