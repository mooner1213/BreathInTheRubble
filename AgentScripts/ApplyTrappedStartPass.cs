using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ApplyTrappedStartPass
{
    const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
    const string PhonePrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";
    const string EffectsFolder = "Assets/_Project/Art/Effects";
    const string VeilTexturePath = EffectsFolder + "/DustVeilNoise.png";
    const string VeilMaterialPath = EffectsFolder + "/DustVeil.mat";
    const float SeatedCameraHeight = 1.02f;

    static readonly Vector3[] SealPositions =
    {
        new(-0.52f, 0.26f, 0.91f), new(0.02f, 0.27f, 0.96f), new(0.54f, 0.25f, 0.92f),
        new(-0.56f, 0.61f, 0.99f), new(-0.12f, 0.62f, 0.91f), new(0.32f, 0.62f, 0.98f), new(0.62f, 0.61f, 0.91f),
        new(-0.50f, 0.98f, 0.94f), new(-0.06f, 0.99f, 1.01f), new(0.39f, 0.98f, 0.93f),
        new(-0.34f, 1.27f, 0.97f), new(0.24f, 1.26f, 0.96f)
    };

    static readonly Vector3[] SealScales =
    {
        new(0.62f, 0.38f, 0.38f), new(0.58f, 0.40f, 0.42f), new(0.62f, 0.37f, 0.38f),
        new(0.48f, 0.42f, 0.40f), new(0.50f, 0.43f, 0.44f), new(0.50f, 0.42f, 0.41f), new(0.38f, 0.40f, 0.39f),
        new(0.53f, 0.43f, 0.42f), new(0.50f, 0.44f, 0.45f), new(0.55f, 0.43f, 0.41f),
        new(0.67f, 0.34f, 0.43f), new(0.69f, 0.35f, 0.42f)
    };

    public static string Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop play mode before applying the trapped-start pass.");

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Expected {ScenePath}, but active scene is {scene.path}.");
        if (scene.isDirty)
            throw new InvalidOperationException("Prototype scene has unsaved changes; refusing to overwrite them.");

        var cameraHeightResult = LowerSeatedCamera(scene);
        var sealCount = BuildFrontRubbleSeal(scene);
        var dustResult = UpgradeDust(scene);
        var phoneResult = TunePhoneSpill(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, scene.path))
            throw new InvalidOperationException($"Could not save {scene.path}.");
        AssetDatabase.SaveAssets();
        return $"{cameraHeightResult}|frontSeal={sealCount}|{dustResult}|{phoneResult}";
    }

    static string LowerSeatedCamera(Scene scene)
    {
        var origin = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Component>(true))
            .FirstOrDefault(item => item != null && item.GetType().Name == "XROrigin")
            ?? throw new InvalidOperationException("XROrigin was not found.");
        var serialized = new SerializedObject(origin);
        var height = serialized.FindProperty("m_CameraYOffset")
            ?? throw new InvalidOperationException("XROrigin camera Y offset property was not found.");
        var offsetObject = serialized.FindProperty("m_CameraFloorOffsetObject")?.objectReferenceValue as GameObject
            ?? throw new InvalidOperationException("XROrigin camera floor offset object was not found.");

        Undo.RecordObject(origin, "Lower seated camera height");
        Undo.RecordObject(offsetObject.transform, "Lower seated camera height");
        height.floatValue = SeatedCameraHeight;
        serialized.ApplyModifiedProperties();
        var local = offsetObject.transform.localPosition;
        offsetObject.transform.localPosition = new Vector3(local.x, SeatedCameraHeight, local.z);
        EditorUtility.SetDirty(origin);
        EditorUtility.SetDirty(offsetObject.transform);
        return $"cameraHeight={SeatedCameraHeight:F2}";
    }

    static int BuildFrontRubbleSeal(Scene scene)
    {
        var environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Prototype Environment")
            ?? throw new InvalidOperationException("Prototype Environment was not found.");
        var realism = environment.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == "Earthquake Realism Pass")
            ?? environment.transform;
        var existing = realism.Find("Front Rubble Seal");
        if (existing != null)
            return existing.childCount;

        var meshes = Enumerable.Range(1, 6)
            .Select(index => AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{index:00}.asset"))
            .Where(mesh => mesh != null)
            .ToArray();
        if (meshes.Length == 0)
            throw new InvalidOperationException("Procedural rubble meshes were not found.");
        var materials = new[]
        {
            "Assets/_Project/Materials/ConcreteFractured.mat",
            "Assets/_Project/Materials/ConcreteExposedCore.mat",
            "Assets/_Project/Materials/ConcreteFreshEdge.mat",
            "Assets/_Project/Materials/ConcreteDust.mat"
        }.Select(AssetDatabase.LoadAssetAtPath<Material>).Where(material => material != null).ToArray();
        if (materials.Length == 0)
            throw new InvalidOperationException("Concrete rubble materials were not found.");

        var root = new GameObject("Front Rubble Seal");
        Undo.RegisterCreatedObjectUndo(root, "Create front rubble seal");
        root.transform.SetParent(realism, false);

        for (var index = 0; index < SealPositions.Length; index++)
        {
            var chunk = new GameObject($"Front Seal Chunk {index + 1:00}");
            Undo.RegisterCreatedObjectUndo(chunk, "Create front seal chunk");
            chunk.transform.SetParent(root.transform, false);
            chunk.transform.localPosition = SealPositions[index];
            chunk.transform.localRotation = Quaternion.Euler(
                9f + index * 23f % 47f,
                -28f + index * 37f % 61f,
                -16f + index * 29f % 39f);
            chunk.transform.localScale = SealScales[index];
            chunk.isStatic = true;

            var mesh = meshes[index % meshes.Length];
            var filter = chunk.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = chunk.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials[index % materials.Length];
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            var collider = chunk.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = mesh.bounds.size;
        }
        return SealPositions.Length;
    }

    static string UpgradeDust(Scene scene)
    {
        var effect = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<BreathInTheRubble.Prototype.AwakeningDustEffect>(true))
            .FirstOrDefault()
            ?? throw new InvalidOperationException("AwakeningDustEffect was not found.");
        var burst = effect.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(system => system.name == "Initial Dust Cloud")
            ?? throw new InvalidOperationException("Initial Dust Cloud was not found.");
        var motes = effect.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(system => system.name == "Suspended Dust Motes")
            ?? throw new InvalidOperationException("Suspended Dust Motes was not found.");

        ConfigureBurst(burst);
        ConfigureMotes(motes);
        var veil = effect.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(system => system.name == "Dense Dust Veil");
        if (veil == null)
            veil = CreateVeil(effect.transform, LoadOrCreateVeilMaterial());
        else
            ConfigureVeil(veil, LoadOrCreateVeilMaterial());

        var serialized = new SerializedObject(effect);
        var systems = serialized.FindProperty("dustSystems");
        systems.arraySize = 3;
        systems.GetArrayElementAtIndex(0).objectReferenceValue = veil;
        systems.GetArrayElementAtIndex(1).objectReferenceValue = burst;
        systems.GetArrayElementAtIndex(2).objectReferenceValue = motes;
        serialized.FindProperty("startDelaySeconds").floatValue = 0.05f;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(effect);
        return "dust=veil24+cloud160+motes72/fade5s";
    }

    static void ConfigureBurst(ParticleSystem system)
    {
        Undo.RecordObject(system, "Upgrade awakening dust cloud");
        system.transform.localPosition = new Vector3(0f, 1.00f, 0.62f);
        var main = system.main;
        main.duration = 2.2f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4.0f, 6.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.025f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.022f, 0.085f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.43f, 0.39f, 0.33f, 0.18f),
            new Color(0.65f, 0.58f, 0.48f, 0.34f));
        main.maxParticles = 190;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)160) });
        var shape = system.shape;
        shape.scale = new Vector3(1.45f, 0.64f, 0.82f);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        EditorUtility.SetDirty(system);
    }

    static void ConfigureMotes(ParticleSystem system)
    {
        Undo.RecordObject(system, "Upgrade suspended dust motes");
        system.transform.localPosition = new Vector3(0f, 1.03f, 0.58f);
        var main = system.main;
        main.duration = 3f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 13f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.008f, 0.035f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.018f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.58f, 0.53f, 0.44f, 0.25f),
            new Color(0.78f, 0.71f, 0.58f, 0.46f));
        main.maxParticles = 90;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.04f, (short)72) });
        var shape = system.shape;
        shape.scale = new Vector3(1.38f, 0.70f, 0.88f);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        EditorUtility.SetDirty(system);
    }

    static ParticleSystem CreateVeil(Transform parent, Material material)
    {
        var item = new GameObject("Dense Dust Veil");
        Undo.RegisterCreatedObjectUndo(item, "Create dense awakening dust veil");
        item.transform.SetParent(parent, false);
        var system = item.AddComponent<ParticleSystem>();
        ConfigureVeil(system, material);
        return system;
    }

    static void ConfigureVeil(ParticleSystem system, Material material)
    {
        system.transform.localPosition = new Vector3(0f, 1.02f, 0.46f);
        var main = system.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4.2f, 5.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.006f, 0.045f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.36f, 0.82f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.48f, 0.43f, 0.35f, 0.38f),
            new Color(0.68f, 0.61f, 0.49f, 0.62f));
        main.gravityModifier = 0.002f;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.28f, 0.72f, 0.52f);
        var noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = 0.055f;
        noise.frequency = 0.32f;
        noise.scrollSpeed = 0.025f;
        noise.damping = true;
        noise.octaveCount = 2;
        var lifetime = system.colorOverLifetime;
        lifetime.enabled = true;
        lifetime.color = CreateVeilGradient();

        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.minParticleSize = 0.025f;
        renderer.maxParticleSize = 0.62f;
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        EditorUtility.SetDirty(system);
        EditorUtility.SetDirty(renderer);
    }

    static ParticleSystem.MinMaxGradient CreateVeilGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.72f, 0.66f, 0.56f), 0f),
                new GradientColorKey(new Color(0.55f, 0.50f, 0.42f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.78f, 0f),
                new GradientAlphaKey(0.62f, 0.18f),
                new GradientAlphaKey(0.28f, 0.52f),
                new GradientAlphaKey(0.08f, 0.82f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    static Material LoadOrCreateVeilMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(VeilMaterialPath);
        if (existing != null)
            return existing;

        var texture = LoadOrCreateVeilTexture();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            throw new InvalidOperationException("A compatible particle shader was not found.");
        var material = new Material(shader) { name = "DustVeil" };
        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        material.SetColor("_BaseColor", new Color(0.64f, 0.58f, 0.48f, 0.88f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent + 5;
        AssetDatabase.CreateAsset(material, VeilMaterialPath);
        return material;
    }

    static Texture2D LoadOrCreateVeilTexture()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(VeilTexturePath);
        if (existing != null)
            return existing;

        const int size = 192;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true) { name = "DustVeilNoise" };
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                var centered = (uv - Vector2.one * 0.5f) * 2f;
                var warpedX = centered.x + (Mathf.PerlinNoise(uv.y * 4.1f + 8.3f, 1.7f) - 0.5f) * 0.24f;
                var warpedY = centered.y + (Mathf.PerlinNoise(2.4f, uv.x * 4.5f + 3.1f) - 0.5f) * 0.24f;
                var edge = Mathf.SmoothStep(1f, 0.1f, Mathf.Sqrt(warpedX * warpedX + warpedY * warpedY));
                var large = Mathf.PerlinNoise(uv.x * 3.2f + 2.7f, uv.y * 3.2f + 5.9f);
                var medium = Mathf.PerlinNoise(uv.x * 8.7f + 11.3f, uv.y * 8.7f + 4.4f);
                var fine = Mathf.PerlinNoise(uv.x * 21f + 1.8f, uv.y * 21f + 9.2f);
                var density = Mathf.Clamp01(large * 0.58f + medium * 0.29f + fine * 0.13f);
                var alpha = edge * Mathf.SmoothStep(0.20f, 0.86f, density);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        File.WriteAllBytes(Path.GetFullPath(VeilTexturePath), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(VeilTexturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(VeilTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 1;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(VeilTexturePath);
    }

    static string TunePhoneSpill(Scene scene)
    {
        ConfigurePhoneController(scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(true))
            .FirstOrDefault() ?? throw new InvalidOperationException("Scene phone spill controller was not found."));

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
        return "phoneOcclusion=0.004-0.035m/exponent0.35/incidence0.975/down0.995";
    }

    static void ConfigurePhoneController(BreathInTheRubble.Prototype.PhoneScreenSpillLight controller)
    {
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("probeRadius").floatValue = 0.004f;
        serialized.FindProperty("blockedDistance").floatValue = 0.004f;
        serialized.FindProperty("clearDistance").floatValue = 0.035f;
        serialized.FindProperty("fadeResponseExponent").floatValue = 0.35f;
        serialized.FindProperty("minimumOcclusionIncidence").floatValue = 0.975f;
        serialized.FindProperty("minimumDownwardFacingForOverlap").floatValue = 0.995f;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
    }
}
