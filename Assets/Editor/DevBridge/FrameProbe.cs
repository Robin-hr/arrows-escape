using System;
using System.IO;
using UnityEngine;
using Unity.Profiling;

namespace ArrowEscape.DevBridge
{
    /// <summary>Development-only Play-mode probe (hooked to Application.onBeforeRender): frame times and main-thread allocations.</summary>
    internal sealed class FrameProbe
    {
        private float remaining, worst, sum;
        private int frames, slow, gcStart, lastFrame = -1;
        private long allocated, lastBytes;
        private string label, file;
        private ProfilerRecorder loop;
        private double loopSum, loopWorst;

        /// <summary>Starts measuring for a number of seconds and appends a line to the report file.</summary>
        public static void Run(string name, float seconds, string path)
        {
            var probe = new FrameProbe { label = name, remaining = seconds, file = path, gcStart = GC.CollectionCount(0), lastBytes = GC.GetAllocatedBytesForCurrentThread() };
            probe.loop = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop", 4);
            Application.onBeforeRender += probe.Sample;
        }

        private void Sample()
        {
            if (Time.frameCount == lastFrame) return; lastFrame = Time.frameCount;
            long now = GC.GetAllocatedBytesForCurrentThread();
            float dt = Time.unscaledDeltaTime;
            if (frames > 0) { allocated += now - lastBytes; sum += dt; worst = Mathf.Max(worst, dt); if (dt > 1f / 50f) slow++; }
            if (frames > 1 && loop.Valid && loop.Count > 0) { double ms = loop.LastValue / 1e6; loopSum += ms; loopWorst = Math.Max(loopWorst, ms); }
            frames++; remaining -= dt;
            if (remaining <= 0 || !Application.isPlaying)
            {
                Application.onBeforeRender -= Sample;
                int measured = Mathf.Max(1, frames - 1);
                File.AppendAllText(file, string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0}: frames {1} avg {2:0.0} ms worst {3:0.0} ms | game CPU (PlayerLoop) avg {10:0.00} ms worst {11:0.00} ms | alloc/frame {5:0} B gc {6} target {7} vsync {8}{9}",
                    label, measured, sum / measured * 1000f, worst * 1000f, slow, (double)allocated / measured, GC.CollectionCount(0) - gcStart,
                    Application.targetFrameRate, QualitySettings.vSyncCount, Environment.NewLine, loopSum / Math.Max(1, measured - 1), loopWorst));
                loop.Dispose();
            }
            lastBytes = GC.GetAllocatedBytesForCurrentThread();
        }
    }
}
