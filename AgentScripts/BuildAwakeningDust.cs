using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BuildAwakeningDust
{
    const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
    const string EffectsFolder = "Assets/_Project/Art/Effects";
    const string TexturePath = EffectsFolder + "/DustSoft.png";
    const string MaterialPath = EffectsFolder + "/DustParticle.mat";

    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Expected {ScenePath}, but active scene is {scene.path}.");
        if (scene.isDirty)
            throw new InvalidOperationException("Prototype scene has unsaved changes; refusing to overwrite them.");

        EnsureFolder("Assets/_Project/Art", "Effects");
        var texture = LoadOrCreateDustTexture();
        var material = LoadOrCreateDustMaterial(texture);

        var environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Prototype Environment")
            ?? throw new InvalidOperationException("Prototype Environment root was not found.");
        var existing = environment.transform.Find("Awakening Dust");
        if (existing != null)
            return "Awakening Dust already exists; no duplicate was created.";

        var root = new GameObject("Awakening Dust");
        Undo.RegisterCreatedObjectUndo(root, "Create awakening dust effect");
        root.transform.SetParent(environment.transform, false);

        var burst = CreateDustBurst(root.transform, material);
        var motes = CreateSuspendedMotes(root.transform, material);
        var controller = root.AddComponent<BreathInTheRubble.Prototype.AwakeningDustEffect>();
        var serializedController = new SerializedObject(controller);
        var systemsProperty = serializedController.FindProperty("dustSystems");
        systemsProperty.arraySize = 2;
        systemsProperty.GetArrayElementAtIndex(0).objectReferenceValue = burst;
        systemsProperty.GetArrayElementAtIndex(1).objectReferenceValue = motes;
        serializedController.FindProperty("startDelaySeconds").floatValue = 0.15f;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, scene.path))
            throw new InvalidOperationException($"Could not save {scene.path}.");
        AssetDatabase.SaveAssets();
        return $"burstMax={burst.main.maxParticles}|motesMax={motes.main.maxParticles}|material={material.name}";
    }

    static ParticleSystem CreateDustBurst(Transform parent, Material material)
    {
        var item = new GameObject("Initial Dust Cloud");
        Undo.RegisterCreatedObjectUndo(item, "Create initial dust cloud");
        item.transform.SetParent(parent, false);
        item.transform.localPosition = new Vector3(0f, 1.34f, 0.65f);
        var system = item.AddComponent<ParticleSystem>();

        var main = system.main;
        main.duration = 2.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.8f, 5.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.035f, 0.16f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.13f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.46f, 0.41f, 0.34f, 0.24f),
            new Color(0.66f, 0.61f, 0.52f, 0.40f));
        main.gravityModifier = 0.006f;
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)130) });

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.45f, 0.68f, 0.82f);

        var noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = 0.065f;
        noise.frequency = 0.28f;
        noise.scrollSpeed = 0.035f;
        noise.damping = true;
        noise.octaveCount = 1;

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateDustGradient(0.42f);

        ConfigureRenderer(system, material);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return system;
    }

    static ParticleSystem CreateSuspendedMotes(Transform parent, Material material)
    {
        var item = new GameObject("Suspended Dust Motes");
        Undo.RegisterCreatedObjectUndo(item, "Create suspended dust motes");
        item.transform.SetParent(parent, false);
        item.transform.localPosition = new Vector3(0f, 1.36f, 0.58f);
        var system = item.AddComponent<ParticleSystem>();

        var main = system.main;
        main.duration = 3f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.006f, 0.028f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.026f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.56f, 0.52f, 0.45f, 0.30f),
            new Color(0.76f, 0.70f, 0.60f, 0.48f));
        main.gravityModifier = 0.002f;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.05f, (short)38) });

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.38f, 0.72f, 0.88f);

        var noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Low;
        noise.strength = 0.024f;
        noise.frequency = 0.22f;
        noise.scrollSpeed = 0.018f;
        noise.damping = true;
        noise.octaveCount = 1;

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateDustGradient(0.50f);

        ConfigureRenderer(system, material);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return system;
    }

    static ParticleSystem.MinMaxGradient CreateDustGradient(float peakAlpha)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.88f, 0.82f, 0.72f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.12f),
                new GradientAlphaKey(peakAlpha * 0.65f, 0.70f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    static void ConfigureRenderer(ParticleSystem system, Material material)
    {
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.material = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.minParticleSize = 0.001f;
        renderer.maxParticleSize = 0.12f;
    }

    static Texture2D LoadOrCreateDustTexture()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (existing != null)
            return existing;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        texture.name = "DustSoft";
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                var distance = Vector2.Distance(uv, Vector2.one * 0.5f) * 2f;
                var edge = Mathf.Clamp01(1f - distance);
                var noise = Mathf.PerlinNoise(x * 0.19f + 2.7f, y * 0.19f + 6.1f);
                var alpha = Mathf.Pow(edge, 1.8f) * Mathf.Lerp(0.72f, 1f, noise);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
    }

    static Material LoadOrCreateDustMaterial(Texture2D texture)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
            return existing;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            throw new InvalidOperationException("A compatible particle shader was not found.");

        var material = new Material(shader) { name = "DustParticle" };
        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        material.SetColor("_BaseColor", new Color(0.68f, 0.62f, 0.52f, 0.90f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    static void EnsureFolder(string parent, string child)
    {
        var fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, child);
    }
}
