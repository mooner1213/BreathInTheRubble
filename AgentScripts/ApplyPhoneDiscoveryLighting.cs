using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ApplyPhoneDiscoveryLighting
{
    const string PrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";
    const string PerformancePipelinePath = "Assets/Settings/Project Configuration/Performance URP Config.asset";

    static readonly string[] EnvironmentLightsToDisable =
    {
        "Dusty Ambient Fill",
        "Narrow Crack Light",
        "Weak Concrete Bounce",
        "TEST - Player Area Fill"
    };

    public static string Run()
    {
        ConfigurePhonePrefab();
        ConfigurePerformancePipeline();
        var disabledLightCount = DisableEnvironmentLights();
        AssetDatabase.SaveAssets();

        return "{" +
            $"\"prefab\":\"{PrefabPath}\"," +
            "\"spillIntensity\":0.35," +
            "\"spillRange\":0.95," +
            "\"screenWakeFadeSeconds\":0.70," +
            "\"additionalLightsPerObject\":2," +
            $"\"disabledEnvironmentLights\":{disabledLightCount}" +
            "}";
    }

    public static string RevertPhoneSpillOverrides()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Project/Scenes/Prototype.unity")
            throw new InvalidOperationException($"Expected Prototype scene, but active scene is {scene.path}.");

        var sequence = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
            .FirstOrDefault(item => item != null && item.GetType().Name == "PhoneWakeSequence")
            ?? throw new InvalidOperationException("PhoneWakeSequence was not found in the scene.");
        var spill = sequence.GetComponentsInChildren<Light>(true)
            .FirstOrDefault(light => light.name == "Screen Spill")
            ?? throw new InvalidOperationException("Screen Spill light was not found in the scene.");

        PrefabUtility.RevertObjectOverride(spill, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, scene.path))
            throw new InvalidOperationException($"Could not save {scene.path}.");

        return $"intensity={spill.intensity:F2}|range={spill.range:F2}|sceneDirty={scene.isDirty}";
    }

    public static string ApplyDirectionalSpillFix()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var spill = root.GetComponentsInChildren<Light>(true)
                .FirstOrDefault(light => light.name == "Screen Spill")
                ?? throw new InvalidOperationException("Screen Spill light was not found in the phone prefab.");
            var flashlight = root.GetComponentsInChildren<Light>(true)
                .FirstOrDefault(light => light.name == "Screen Flashlight")
                ?? throw new InvalidOperationException("Screen Flashlight was not found in the phone prefab.");

            spill.type = LightType.Spot;
            spill.color = new Color(0.62f, 0.78f, 1f);
            spill.intensity = 0.35f;
            spill.range = 0.95f;
            spill.spotAngle = 125f;
            spill.innerSpotAngle = 82f;
            spill.shadows = LightShadows.None;
            spill.lightUnit = LightUnit.Candela;
            spill.transform.localPosition = new Vector3(0f, 0f, -0.012f);
            spill.transform.localRotation = flashlight.transform.localRotation;
            spill.enabled = false;

            var controller = spill.GetComponent<BreathInTheRubble.Prototype.PhoneScreenSpillLight>();
            if (controller == null)
                controller = spill.gameObject.AddComponent<BreathInTheRubble.Prototype.PhoneScreenSpillLight>();
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("spillLight").objectReferenceValue = spill;
            serializedController.FindProperty("maximumIntensity").floatValue = 0.35f;
            serializedController.FindProperty("probeRadius").floatValue = 0.012f;
            serializedController.FindProperty("blockedDistance").floatValue = 0.045f;
            serializedController.FindProperty("clearDistance").floatValue = 0.11f;
            serializedController.FindProperty("occlusionMask").intValue = -1;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            var sequence = root.GetComponents<MonoBehaviour>()
                .FirstOrDefault(item => item != null && item.GetType().Name == "PhoneWakeSequence")
                ?? throw new InvalidOperationException("PhoneWakeSequence was not found in the phone prefab.");
            var serializedSequence = new SerializedObject(sequence);
            serializedSequence.FindProperty("screenSpillLight").objectReferenceValue = spill;
            serializedSequence.FindProperty("screenSpillController").objectReferenceValue = controller;
            serializedSequence.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            return "type=Spot|angle=125|inner=82|range=0.95|occlusion=0.045-0.11";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void ConfigurePhonePrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var spill = root.GetComponentsInChildren<Light>(true)
                .FirstOrDefault(light => light.name == "Screen Spill")
                ?? throw new InvalidOperationException("Screen Spill light was not found in the phone prefab.");
            spill.type = LightType.Point;
            spill.color = new Color(0.62f, 0.78f, 1f);
            spill.intensity = 0.35f;
            spill.range = 0.95f;
            spill.shadows = LightShadows.None;
            spill.lightUnit = LightUnit.Candela;
            spill.transform.localPosition = new Vector3(0f, 0f, -0.065f);
            spill.enabled = false;

            var flashlight = root.GetComponentsInChildren<Light>(true)
                .FirstOrDefault(light => light.name == "Screen Flashlight")
                ?? throw new InvalidOperationException("Screen Flashlight was not found in the phone prefab.");
            flashlight.enabled = false;

            var sequence = root.GetComponents<MonoBehaviour>()
                .FirstOrDefault(item => item != null && item.GetType().Name == "PhoneWakeSequence")
                ?? throw new InvalidOperationException("PhoneWakeSequence was not found in the phone prefab.");
            var serializedSequence = new SerializedObject(sequence);
            serializedSequence.FindProperty("screenWakeFadeSeconds").floatValue = 0.7f;
            serializedSequence.FindProperty("screenOnColor").colorValue = new Color(0.035f, 0.13f, 0.18f, 1f);
            serializedSequence.FindProperty("screenEmissionColor").colorValue = new Color(0.24f, 0.82f, 1.4f, 1f);
            serializedSequence.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void ConfigurePerformancePipeline()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PerformancePipelinePath)
            ?? throw new InvalidOperationException($"Could not load {PerformancePipelinePath}.");
        var serializedPipeline = new SerializedObject(pipeline);
        serializedPipeline.FindProperty("m_AdditionalLightsRenderingMode").intValue = 1;
        serializedPipeline.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 2;
        serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
    }

    static int DisableEnvironmentLights()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Project/Scenes/Prototype.unity")
            throw new InvalidOperationException($"Expected Prototype scene, but active scene is {scene.path}.");
        if (scene.isDirty)
            throw new InvalidOperationException("Prototype scene has unsaved changes; refusing to save over them.");

        var lights = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Light>(true))
            .Where(light => EnvironmentLightsToDisable.Contains(light.name))
            .ToArray();
        foreach (var light in lights)
        {
            Undo.RecordObject(light, "Disable non-phone discovery light");
            light.enabled = false;
            EditorUtility.SetDirty(light);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, scene.path))
            throw new InvalidOperationException($"Could not save {scene.path}.");
        return lights.Length;
    }
}
