using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectPhoneLighting
{
    public static string RunAllLights()
    {
        var scene = SceneManager.GetActiveScene();
        var lights = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Light>(true))
            .Select(light =>
                $"{light.name}|{light.type}|enabled={light.enabled}|active={light.gameObject.activeInHierarchy}|" +
                $"intensity={light.intensity:F1}|range={light.range:F2}|shadows={light.shadows}|" +
                $"position={light.transform.position.ToString("F3")}");

        return $"ambient={RenderSettings.ambientMode}|color={RenderSettings.ambientLight}|" +
            $"intensity={RenderSettings.ambientIntensity:F2}|fog={RenderSettings.fog}|" +
            $"fogDensity={RenderSettings.fogDensity:F3}\n" + string.Join("\n", lights);
    }

    public static string ListScenes()
    {
        return string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount)
            .Select(index => SceneManager.GetSceneAt(index))
            .Select(scene => $"{scene.path}|loaded={scene.isLoaded}|dirty={scene.isDirty}|active={scene == SceneManager.GetActiveScene()}"));
    }

    public static string ReloadPrototypeFromDisk()
    {
        const string scenePath = "Assets/_Project/Scenes/Prototype.unity";
        if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != scenePath)
            throw new InvalidOperationException("Prototype is not the only active scene; refusing to reload it.");
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        return $"{scene.path}|dirty={scene.isDirty}";
    }

    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        var sequence = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
            .FirstOrDefault(item => item != null && item.GetType().Name == "PhoneWakeSequence");
        var phone = sequence != null ? sequence.transform : null;

        if (phone == null)
            return "{\"error\":\"SmartphonePrototype not found\"}";

        var phoneRenderer = phone.GetComponentsInChildren<Renderer>(true)
            .FirstOrDefault(item => item.name == "Screen");
        var camera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(item => item.gameObject.activeInHierarchy);
        var lights = phone.GetComponentsInChildren<Light>(true)
            .Select(light => new
            {
                light.name,
                type = light.type.ToString(),
                light.enabled,
                light.intensity,
                unit = light.lightUnit.ToString(),
                light.range,
                light.spotAngle,
                light.innerSpotAngle,
                localPosition = light.transform.localPosition.ToString("F3"),
                localForward = light.transform.localRotation * Vector3.forward
            })
            .ToArray();

        var sequenceData = "null";
        if (sequence != null)
        {
            var serialized = new SerializedObject(sequence);
            sequenceData = "{" +
                $"\"wakeDelay\":{serialized.FindProperty("wakeDelaySeconds").floatValue:F2}," +
                $"\"emission\":\"{serialized.FindProperty("screenEmissionColor").colorValue}\"" +
                "}";
        }

        return "{" +
            $"\"scene\":\"{scene.path}\"," +
            $"\"dirty\":{scene.isDirty.ToString().ToLowerInvariant()}," +
            $"\"playing\":{EditorApplication.isPlaying.ToString().ToLowerInvariant()}," +
            $"\"phonePosition\":\"{phone.position.ToString("F3")}\"," +
            $"\"phoneRotation\":\"{phone.eulerAngles.ToString("F1")}\"," +
            $"\"phonePrefab\":\"{PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(phone.gameObject)}\"," +
            $"\"screenBounds\":\"{(phoneRenderer != null ? phoneRenderer.bounds.ToString() : "missing")}\"," +
            $"\"camera\":\"{(camera != null ? camera.name + " pos=" + camera.transform.position.ToString("F3") + " rot=" + camera.transform.eulerAngles.ToString("F1") : "missing")}\"," +
            $"\"quality\":\"{QualitySettings.names[QualitySettings.GetQualityLevel()]}\"," +
            $"\"pixelLights\":{QualitySettings.pixelLightCount}," +
            $"\"sequence\":{sequenceData}," +
            "\"lights\":[" + string.Join(",", lights.Select(light =>
                "{" +
                $"\"name\":\"{light.name}\"," +
                $"\"type\":\"{light.type}\"," +
                $"\"enabled\":{light.enabled.ToString().ToLowerInvariant()}," +
                $"\"intensity\":{light.intensity:F1}," +
                $"\"unit\":\"{light.unit}\"," +
                $"\"range\":{light.range:F2}," +
                $"\"spotAngle\":{light.spotAngle:F1}," +
                $"\"innerSpotAngle\":{light.innerSpotAngle:F1}," +
                $"\"localPosition\":\"{light.localPosition}\"," +
                $"\"localForward\":\"{light.localForward.ToString("F3")}\"" +
                "}")) + "]" +
            "}";
    }
}
