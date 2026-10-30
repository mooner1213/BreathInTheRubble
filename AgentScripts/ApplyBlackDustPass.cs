using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ApplyBlackDustPass
{
    const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
    const string DustMaterialPath = "Assets/_Project/Art/Effects/DustParticle.mat";
    const string VeilMaterialPath = "Assets/_Project/Art/Effects/DustVeil.mat";
    const string PhonePrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";

    public static string Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop play mode before applying the black dust pass.");

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Expected {ScenePath}, but active scene is {scene.path}.");
        if (scene.isDirty)
            throw new InvalidOperationException("Prototype scene has unsaved changes; refusing to overwrite them.");

        ConfigurePitchBlackEnvironment(scene);
        var dustMaterial = ConfigureLitDustMaterial(DustMaterialPath, new Color(0.42f, 0.38f, 0.32f, 0.72f));
        var veilMaterial = ConfigureLitDustMaterial(VeilMaterialPath, new Color(0.34f, 0.31f, 0.27f, 0.76f));
        var result = ConfigureDustSystems(scene, dustMaterial, veilMaterial);
        ConfigurePhoneDiscoveryLight(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, scene.path))
            throw new InvalidOperationException($"Could not save {scene.path}.");
        AssetDatabase.SaveAssets();
        return $"ambient=black|reflection=0|fog=black|{result}|shader=URP Particles Lit|phoneSpill=0.55/1.05m";
    }

    static void ConfigurePitchBlackEnvironment(Scene scene)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.055f;

        foreach (var camera in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)))
        {
            Undo.RecordObject(camera, "Set pitch-black camera background");
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            EditorUtility.SetDirty(camera);
        }

        foreach (var light in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Light>(true)))
        {
            if (light.transform.root.name.StartsWith("Smartphone", StringComparison.Ordinal))
                continue;
            Undo.RecordObject(light, "Disable environment light for pitch black start");
            light.enabled = false;
            EditorUtility.SetDirty(light);
        }
    }

    static Material ConfigureLitDustMaterial(string path, Color baseColor)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path)
            ?? throw new InvalidOperationException($"Dust material was not found: {path}");
        var shader = Shader.Find("Universal Render Pipeline/Particles/Lit")
            ?? throw new InvalidOperationException("URP Particles/Lit shader was not found.");
        var baseMap = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;

        material.shader = shader;
        if (baseMap != null)
        {
            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_MainTex", baseMap);
        }
        material.SetColor("_BaseColor", baseColor);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.02f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_EMISSION");
        material.renderQueue = (int)RenderQueue.Transparent + (path == VeilMaterialPath ? 5 : 0);
        EditorUtility.SetDirty(material);
        return material;
    }

    static string ConfigureDustSystems(Scene scene, Material dustMaterial, Material veilMaterial)
    {
        var effect = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<BreathInTheRubble.Prototype.AwakeningDustEffect>(true))
            .FirstOrDefault() ?? throw new InvalidOperationException("AwakeningDustEffect was not found.");
        var systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        var cloud = systems.FirstOrDefault(system => system.name == "Initial Dust Cloud")
            ?? throw new InvalidOperationException("Initial Dust Cloud was not found.");
        var motes = systems.FirstOrDefault(system => system.name == "Suspended Dust Motes")
            ?? throw new InvalidOperationException("Suspended Dust Motes was not found.");
        var veil = systems.FirstOrDefault(system => system.name == "Dense Dust Veil")
            ?? throw new InvalidOperationException("Dense Dust Veil was not found.");

        ConfigureCloud(cloud, dustMaterial);
        ConfigureMotes(motes, dustMaterial);
        ConfigureVeil(veil, veilMaterial);
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("startDelaySeconds").floatValue = 0.05f;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(effect);
        return "dustBursts=64+280+140|dustMax=64+340+180|clear=10-30s";
    }

    static void ConfigureCloud(ParticleSystem system, Material material)
    {
        Undo.RecordObject(system, "Increase slow dust cloud");
        var main = system.main;
        main.duration = 4f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(12f, 18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.012f, 0.075f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.075f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.50f, 0.46f, 0.39f, 0.30f),
            new Color(0.78f, 0.70f, 0.58f, 0.58f));
        main.gravityModifier = 0.002f;
        main.maxParticles = 340;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)280) });
        var noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = 0.045f;
        noise.frequency = 0.24f;
        noise.scrollSpeed = 0.015f;
        noise.damping = true;
        noise.octaveCount = 2;
        var lifetime = system.colorOverLifetime;
        lifetime.enabled = true;
        lifetime.color = CreateSlowFadeGradient(0.54f, 0.28f);
        ConfigureRenderer(system, material, 0.16f);
    }

    static void ConfigureMotes(ParticleSystem system, Material material)
    {
        Undo.RecordObject(system, "Increase suspended dust motes");
        var main = system.main;
        main.duration = 5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(20f, 30f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.004f, 0.022f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.016f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.58f, 0.54f, 0.47f, 0.34f),
            new Color(0.86f, 0.78f, 0.65f, 0.68f));
        main.gravityModifier = 0.0008f;
        main.maxParticles = 180;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.05f, (short)140) });
        var lifetime = system.colorOverLifetime;
        lifetime.enabled = true;
        lifetime.color = CreateSlowFadeGradient(0.62f, 0.42f);
        ConfigureRenderer(system, material, 0.10f);
    }

    static void ConfigureVeil(ParticleSystem system, Material material)
    {
        Undo.RecordObject(system, "Slow dense dust veil");
        var main = system.main;
        main.duration = 2f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 13f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.003f, 0.025f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.34f, 0.78f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.44f, 0.41f, 0.36f, 0.42f),
            new Color(0.70f, 0.64f, 0.54f, 0.72f));
        main.gravityModifier = 0.0005f;
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)64) });
        var noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = 0.038f;
        noise.frequency = 0.20f;
        noise.scrollSpeed = 0.012f;
        noise.damping = true;
        noise.octaveCount = 2;
        var lifetime = system.colorOverLifetime;
        lifetime.enabled = true;
        lifetime.color = CreateSlowFadeGradient(0.78f, 0.48f);
        ConfigureRenderer(system, material, 0.65f);
    }

    static ParticleSystem.MinMaxGradient CreateSlowFadeGradient(float startAlpha, float lateAlpha)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.78f, 0.72f, 0.62f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(startAlpha, 0f),
                new GradientAlphaKey(startAlpha, 0.30f),
                new GradientAlphaKey(lateAlpha, 0.68f),
                new GradientAlphaKey(lateAlpha * 0.35f, 0.90f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    static void ConfigureRenderer(ParticleSystem system, Material material, float maxParticleSize)
    {
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.enableGPUInstancing = true;
        renderer.maxParticleSize = maxParticleSize;
        EditorUtility.SetDirty(system);
        EditorUtility.SetDirty(renderer);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static void ConfigurePhoneDiscoveryLight(Scene scene)
    {
        var sceneController = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(true))
            .FirstOrDefault() ?? throw new InvalidOperationException("Scene phone spill controller was not found.");
        ConfigurePhoneController(sceneController);

        var prefabRoot = PrefabUtility.LoadPrefabContents(PhonePrefabPath);
        try
        {
            var prefabController = prefabRoot.GetComponentInChildren<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(true)
                ?? throw new InvalidOperationException("Phone prefab spill controller was not found.");
            ConfigurePhoneController(prefabController);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PhonePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    static void ConfigurePhoneController(BreathInTheRubble.Prototype.PhoneScreenSpillLight controller)
    {
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("maximumIntensity").floatValue = 0.55f;
        serialized.ApplyModifiedProperties();
        var light = controller.GetComponent<Light>();
        if (light != null)
        {
            light.range = 1.05f;
            light.spotAngle = 120f;
            light.innerSpotAngle = 82f;
            EditorUtility.SetDirty(light);
        }
        EditorUtility.SetDirty(controller);
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
    }
}
