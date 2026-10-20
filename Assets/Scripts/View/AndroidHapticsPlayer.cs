using System;
using ArrowEscape.Core;
using UnityEngine;

namespace ArrowEscape.View
{
    /// <summary>Cached Android vibration effects. Deliberately compiled in Editor to catch C# errors.</summary>
    public sealed class AndroidHapticsPlayer : IHapticsPlayer, IDisposable
    {
        private AndroidJavaObject vibrator;
        private readonly AndroidJavaObject[] effects = new AndroidJavaObject[10];
        private readonly object[][] arguments = new object[10][];
        private bool warned;
        /// <summary>False when the phone lacks a motor or the first Android call failed.</summary>
        public bool IsSupported { get; private set; }
        /// <summary>Discovers the motor and prepares every waveform once; failures never escape.</summary>
        public AndroidHapticsPlayer()
        {
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null) return;
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService","vibrator");
                }
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;
                int sdk; using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                if (sdk >= 26)
                {
                    using (var factory = new AndroidJavaClass("android.os.VibrationEffect"))
                        for (int i = 0; i < effects.Length; i++) PrepareModern(factory,(HapticCue)i);
                }
                else for (int i = 0; i < arguments.Length; i++) PrepareLegacy((HapticCue)i);
                IsSupported = true;
            }
            catch (Exception error) { Fail(error); }
        }
        private void PrepareModern(AndroidJavaClass factory, HapticCue cue)
        {
            var pattern = HapticDesign.GetPattern(cue); int i = (int)cue;
            if (pattern.Count == 1) effects[i] = factory.CallStatic<AndroidJavaObject>("createOneShot",(long)pattern[0].DurationMs,pattern[0].Amplitude);
            else
            {
                int length = HapticWaveform.RequiredLength(pattern.Count); var timings = new long[length]; var amplitudes = new int[length];
                HapticWaveform.Build(pattern,timings,amplitudes);
                effects[i] = factory.CallStatic<AndroidJavaObject>("createWaveform",timings,amplitudes,-1);
            }
            if (effects[i] == null) throw new InvalidOperationException("Android returned no vibration effect.");
            arguments[i] = new object[] { effects[i] };
        }
        private void PrepareLegacy(HapticCue cue)
        {
            var pattern = HapticDesign.GetPattern(cue);
            if (pattern.Count == 1) arguments[(int)cue] = new object[] { HapticWaveform.TotalMilliseconds(pattern) };
            else
            {
                int length = HapticWaveform.RequiredLength(pattern.Count); var timings = new long[length]; var amplitudes = new int[length];
                HapticWaveform.Build(pattern,timings,amplitudes); arguments[(int)cue] = new object[] { timings,-1 };
            }
        }
        /// <summary>Uses a cached effect or legacy pattern; the first failure disables the session.</summary>
        public void Play(HapticCue cue)
        {
            if (!IsSupported) return;
            try { if ((int)cue < 0 || (int)cue >= arguments.Length) return; vibrator.Call("vibrate",arguments[(int)cue]); }
            catch (Exception error) { Fail(error); }
        }
        /// <summary>Cancels vibration safely.</summary>
        public void Cancel()
        { if (!IsSupported) return; try { vibrator.Call("cancel",Array.Empty<object>()); } catch (Exception error) { Fail(error); } }
        private void Fail(Exception error)
        {
            IsSupported = false;
            if (warned) return; warned = true;
            Debug.LogWarning("ArrowEscape haptics unavailable for this session: " + error.Message);
        }
        /// <summary>Releases cached Java references without allowing cleanup failures to escape.</summary>
        public void Dispose()
        {
            Cancel(); IsSupported = false;
            for (int i = 0; i < effects.Length; i++) {
                try { effects[i]?.Dispose(); } catch (Exception error) { Fail(error); }
                effects[i] = null; arguments[i] = null;
            }
            try { vibrator?.Dispose(); } catch (Exception error) { Fail(error); } vibrator = null;
        }
    }
}
