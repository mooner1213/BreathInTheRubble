using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TestBlackPhoneAttraction
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public static string PreviewBlackStart()
    {
        PrepareScene(0.75f, false, false);
        return "blackStart|" + MeasureLuminanceInternal();
    }

    public static string PreviewPhoneForward()
    {
        PrepareScene(6f, true, false);
        return "phoneForward|" + MeasureLuminanceInternal();
    }

    public static string PreviewPhoneLookAt()
    {
        PrepareScene(6f, true, true);
        return "phoneLookAt|" + MeasureLuminanceInternal();
    }

    public static string PreviewPhoneBalanced()
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(FindObjectsInactive.Include)
            ?? throw new InvalidOperationException("Phone screen spill controller was not found.");
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("maximumIntensity").floatValue = 0.55f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var light = controller.GetComponent<Light>();
        light.range = 1.05f;
        light.spotAngle = 120f;
        light.innerSpotAngle = 82f;
        PrepareScene(6f, true, false);
        return "phoneBalanced|" + MeasureLuminanceInternal();
    }

    public static string MeasureLuminance() => MeasureLuminanceInternal();

    public static string MeasureDustTimeline()
    {
        var checkpoints = new[] { 0.75f, 6f, 12f, 20f };
        return string.Join("\n", checkpoints.Select(seconds =>
        {
            var systems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var system in systems)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play(true);
                system.Simulate(seconds, true, true, false);
            }
            return $"t={seconds:F2}|{string.Join(";", systems.Select(DescribeDust))}";
        }));
    }

    static void PrepareScene(float dustSeconds, bool phoneAwake, bool lookAtPhone)
    {
        var camera = Camera.main ?? throw new InvalidOperationException("Main Camera was not found.");
        var phoneSequence = UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneWakeSequence>(FindObjectsInactive.Include)
            ?? throw new InvalidOperationException("PhoneWakeSequence was not found.");
        var phone = phoneSequence.transform;
        var screen = phone.GetComponentsInChildren<Renderer>(true).FirstOrDefault(item => item.name == "Screen")
            ?? throw new InvalidOperationException("Phone screen renderer was not found.");
        var spillController = phone.GetComponentInChildren<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(true)
            ?? throw new InvalidOperationException("Phone screen spill controller was not found.");
        var flashlight = phone.GetComponentsInChildren<Light>(true).FirstOrDefault(item => item.name == "Screen Flashlight");

        HideControllerObjects();
        var screenMaterial = screen.material;
        if (phoneAwake)
        {
            screenMaterial.SetColor(BaseColorId, new Color(0.025f, 0.12f, 0.18f, 1f));
            screenMaterial.SetColor(EmissionColorId, new Color(0.18f, 0.95f, 1.35f, 1f));
            screenMaterial.EnableKeyword("_EMISSION");
            spillController.SetEmissionLevel(1f);
        }
        else
        {
            screenMaterial.SetColor(BaseColorId, Color.black);
            screenMaterial.SetColor(EmissionColorId, Color.black);
            screenMaterial.DisableKeyword("_EMISSION");
            spillController.SetEmissionLevel(0f);
        }
        if (flashlight != null)
            flashlight.enabled = false;

        foreach (var system in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(true);
            system.Simulate(dustSeconds, true, true, false);
        }

        camera.transform.localRotation = Quaternion.identity;
        if (lookAtPhone)
        {
            var target = screen.bounds.center;
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);
        }
        EditorApplication.isPaused = true;
    }

    static void HideControllerObjects()
    {
        foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name == "Left Controller" || transform.name == "Right Controller")
                transform.gameObject.SetActive(false);
        }
    }

    static string MeasureLuminanceInternal()
    {
        var camera = Camera.main ?? throw new InvalidOperationException("Main Camera was not found.");
        const int width = 256;
        const int height = 144;
        var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false, true);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            var pixels = texture.GetPixels32();
            UnityEngine.Object.DestroyImmediate(texture);

            var sum = 0d;
            var max = 0f;
            var visible = 0;
            var bright = 0;
            foreach (var pixel in pixels)
            {
                var luminance = (0.2126f * pixel.r + 0.7152f * pixel.g + 0.0722f * pixel.b) / 255f;
                sum += luminance;
                max = Mathf.Max(max, luminance);
                if (luminance > 0.005f)
                    visible++;
                if (luminance > 0.10f)
                    bright++;
            }
            return $"mean={sum / pixels.Length:F5}|max={max:F3}|visible={(visible * 100f / pixels.Length):F2}%|bright={(bright * 100f / pixels.Length):F2}%";
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
        }
    }

    static string DescribeDust(ParticleSystem system)
    {
        var particles = new ParticleSystem.Particle[Mathf.Max(1, system.particleCount)];
        var count = system.GetParticles(particles);
        var alpha = count == 0 ? 0f : particles.Take(count).Average(item => item.GetCurrentColor(system).a / 255f);
        return $"{system.name}:{count}@{alpha:F3}";
    }
}
