using System;
using System.IO;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class AtmospherePreview
{
    public static object Inspect()
    {
        var camera = Camera.main;
        return new { position = camera.transform.position.ToString(), forward = camera.transform.forward.ToString(), mask = camera.cullingMask,
            dust = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(p => {
                var r = p.GetComponent<ParticleSystemRenderer>(); var m = r.sharedMaterial;
                return new { p.name, enabled = r.enabled, layer = p.gameObject.layer, position = p.transform.position.ToString(), shader = m.shader.name,
                    color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "none", texture = m.GetTexture("_BaseMap")?.name,
                    size = p.main.startSize.constantMax, alpha = p.main.startColor.colorMax.a, blend = m.GetFloat("_DstBlend") };
            }).ToArray() };
    }
    public static object SaveRestorePoint()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit mode required.");
        var scene = SceneManager.GetActiveScene();
        string path = "Assets/_Project/Scenes/Backups/BeforeStrongerAftershock_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, path, true)) throw new Exception("Backup failed.");
        return new { backup = path, pipeline = GraphicsSettings.currentRenderPipeline.name, camera = Camera.main.transform.position.ToString(),
            dust = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(p => new { p.name, max = p.main.maxParticles, loop = p.main.loop, size = p.main.startSize.constantMax, rate = p.emission.rateOverTime.constant, material = p.GetComponent<ParticleSystemRenderer>().sharedMaterial.name }).ToArray() };
    }

    public static object CaptureBefore() { return Suite("Before"); }
    public static object CaptureAfter() { return Suite("After"); }

    static object Suite(string label)
    {
        if (!Application.isPlaying || DesktopPhoneTestDriver.HasPhysicalHmd) throw new Exception("Desktop Play Mode required.");
        var sequence = UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var awakening = UnityEngine.Object.FindFirstObjectByType<AwakeningDustEffect>();
        var systems = awakening.GetComponentsInChildren<ParticleSystem>(true);
        var atmosphere = UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        sequence.enabled = false;
        try
        {
            sequence.RestoreAftershock();
            if(atmosphere!=null){atmosphere.ReplayAwakening();atmosphere.Advance(0.65f);}
            awakening.PlayNow();
            foreach (var system in systems) system.Simulate(0.65f, false, true, false);
            string start = Capture(label + "_Start");
            foreach (var system in systems) system.Simulate(25f, false, true, false);
            if(atmosphere!=null)atmosphere.Advance(25f);
            string ambient = Capture(label + "_Ambient");
            sequence.BeginAftershockNow(); sequence.Advance(4.95f);
            var shockSystems = sequence.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var system in shockSystems) system.Simulate(0.8f, false, false, false);
            if(atmosphere!=null)atmosphere.Advance(0.8f);
            string shock = Capture(label + "_Aftershock");
            UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();
            string dustOnly = Capture(label + "_DustOnly");
            return new { start, ambient, shock, dustOnly, particles = systems.Concat(shockSystems).Select(p => new { p.name, alive = p.particleCount, limit = p.main.maxParticles }).ToArray() };
        }
        finally { sequence.RestoreAftershock(); sequence.enabled = true; }
    }

    public static string Capture(string label)
    {
        Directory.CreateDirectory("Temp/AtmosphereReview");
        var camera = Camera.main;
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = new RenderTexture(1000, 800, 24);
        var output = new Texture2D(1000, 800, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render(); camera.Render();
            RenderTexture.active = target;
            output.ReadPixels(new Rect(0, 0, 1000, 800), 0, 0); output.Apply();
            string path = Path.GetFullPath("Temp/AtmosphereReview/" + label + ".png");
            File.WriteAllBytes(path, output.EncodeToPNG());
            return path;
        }
        finally
        {
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(output); target.Release(); UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
