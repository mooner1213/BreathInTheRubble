using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEngine;

namespace BreathInTheRubble.EditorTools
{
    /// <summary>Opt-in recording of the real startup. Does not hold the phone, replay effects or add lights.</summary>
    [InitializeOnLoad]
    public static class AwakeningStartupCapture
    {
        const string ArmedKey = "BreathInTheRubble.StartupCaptureArmed";
        static readonly float[] Times = { .10f, .90f, 1.50f, 2.20f, 3.50f, 4.20f, 5.50f };
        static readonly List<object> frames = new List<object>();
        static int next;
        static AwakeningStartupCapture() { EditorApplication.playModeStateChanged += OnPlayState; }
        public static void Arm()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Arm before starting Play Mode.");
            SessionState.SetBool(ArmedKey, true);
        }
        static void OnPlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ArmedKey, false))
            {
                SessionState.SetBool(ArmedKey, false); next = 0; frames.Clear();
                EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.ExitingPlayMode) EditorApplication.update -= Tick;
        }
        static void Tick()
        {
            if (!Application.isPlaying || Camera.main == null || next >= Times.Length) return;
            if (Time.timeSinceLevelLoad < Times[next]) return;
            try
            {
                var camera = Camera.main; var reveal = UnityEngine.Object.FindFirstObjectByType<AwakeningReveal>();
                var phone = UnityEngine.Object.FindFirstObjectByType<PhoneWakeSequence>();
                var driver = UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();
                var dust = UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
                Directory.CreateDirectory("Temp/PhoneLitAwakeningReview");
                string path = Path.GetFullPath("Temp/PhoneLitAwakeningReview/Startup_" + Times[next].ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "s.png");
                var previous = RenderTexture.active; var old = camera.targetTexture;
                var rt = new RenderTexture(1200, 900, 24); var image = new Texture2D(1200, 900, TextureFormat.RGB24, false);
                double mean = 0; double maximum = 0; double centerMean = 0;
                try
                {
                    camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0); image.Apply();
                    var pixels = image.GetPixels32(); var center = new List<double>();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        var p = pixels[i]; double l = (.2126 * p.r + .7152 * p.g + .0722 * p.b) / 255.0;
                        mean += l; maximum = Math.Max(maximum, l);
                        int x = i % 1200, y = i / 1200;
                        if (x > 240 && x < 960 && y > 360 && y < 540) center.Add(l);
                    }
                    mean /= pixels.Length; centerMean = center.Average(); File.WriteAllBytes(path, image.EncodeToPNG());
                }
                finally { camera.targetTexture = old; RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                frames.Add(new { targetTime = Times[next], gameTime = Time.timeSinceLevelLoad, frame = Time.frameCount, opening = reveal.Opening, phoneAwake = phone.IsAwake,
                    held = driver != null && driver.IsHolding, flashlight = phone.GetComponent<PhoneInterfaceController>().FlashlightEnabled,
                    phonePosition = phone.transform.position.ToString("F3"), particles = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Sum(p => p.particleCount),
                    haze = dust.HazeAmount, mean, maximum, centerMean, path });
                next++;
                if (next >= Times.Length)
                {
                    File.WriteAllText("Temp/PhoneLitAwakeningReview/Startup.json", Newtonsoft.Json.JsonConvert.SerializeObject(frames, Newtonsoft.Json.Formatting.Indented));
                    EditorApplication.update -= Tick;
                    Debug.Log("[Awakening startup capture] Completed real startup recording; phone stayed at rest, no extra inspection lights.");
                }
            }
            catch (Exception error) { EditorApplication.update -= Tick; Debug.LogException(error); }
        }
    }
}
