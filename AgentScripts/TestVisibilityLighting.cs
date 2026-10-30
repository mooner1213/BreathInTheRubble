using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class TestVisibilityLighting
{
    const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
    const string TestLightName = "TEST - Player Area Fill";

    public static string Apply()
    {
        var originalScene = SceneManager.GetActiveScene();
        var wasLoaded = SceneManager.GetSceneByPath(ScenePath).isLoaded;
        var scene = wasLoaded
            ? SceneManager.GetSceneByPath(ScenePath)
            : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException($"Could not load {ScenePath}.");
        if (wasLoaded && scene.isDirty)
            throw new InvalidOperationException("Prototype scene already has unsaved changes; refusing to overwrite them.");

        try
        {
            SceneManager.SetActiveScene(scene);
            var lightingRoot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Prototype Lighting")
                ?? throw new InvalidOperationException("Prototype Lighting root was not found.");

            var testLightTransform = lightingRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == TestLightName);
            if (testLightTransform == null)
            {
                var testLightObject = new GameObject(TestLightName);
                testLightObject.transform.SetParent(lightingRoot.transform, false);
                testLightTransform = testLightObject.transform;
            }

            testLightTransform.localPosition = new Vector3(0f, 1.28f, -0.08f);
            var testLight = testLightTransform.GetComponent<Light>();
            if (testLight == null)
                testLight = testLightTransform.gameObject.AddComponent<Light>();
            testLight.type = LightType.Point;
            testLight.color = new Color(0.82f, 0.86f, 0.92f);
            testLight.intensity = 18000f;
            testLight.range = 4.0f;
            testLight.shadows = LightShadows.None;
            testLight.enabled = true;

            var ambientFill = lightingRoot.GetComponentsInChildren<Light>(true)
                .FirstOrDefault(light => light.name == "Dusty Ambient Fill");
            if (ambientFill != null)
            {
                ambientFill.intensity = 4000f;
                ambientFill.range = 3.0f;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.40f, 0.46f);
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.fog = true;
            RenderSettings.fogDensity = 0.010f;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"Could not save {ScenePath}.");

            return "{" +
                $"\"scene\":\"{scene.path}\"," +
                $"\"testLightEnabled\":{testLight.enabled.ToString().ToLowerInvariant()}," +
                $"\"testLightIntensity\":{testLight.intensity:F1}," +
                $"\"testLightRange\":{testLight.range:F1}," +
                $"\"ambientIntensity\":{RenderSettings.ambientIntensity:F2}," +
                $"\"fogDensity\":{RenderSettings.fogDensity:F3}" +
                "}";
        }
        finally
        {
            if (originalScene.IsValid() && originalScene.isLoaded)
                SceneManager.SetActiveScene(originalScene);
            if (!wasLoaded && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static string RenderPreview()
    {
        var originalScene = SceneManager.GetActiveScene();
        var wasLoaded = SceneManager.GetSceneByPath(ScenePath).isLoaded;
        var scene = wasLoaded
            ? SceneManager.GetSceneByPath(ScenePath)
            : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var hiddenRoots = new Dictionary<GameObject, bool>();
        GameObject cameraObject = null;
        RenderTexture renderTexture = null;
        Texture2D image = null;
        var previousActive = RenderTexture.active;

        try
        {
            SceneManager.SetActiveScene(scene);
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var loadedScene = SceneManager.GetSceneAt(index);
                if (loadedScene == scene)
                    continue;
                foreach (var root in loadedScene.GetRootGameObjects())
                {
                    hiddenRoots[root] = root.activeSelf;
                    root.SetActive(false);
                }
            }

            cameraObject = new GameObject("Temporary Test Visibility Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.002f, 0.003f, 0.004f, 1f);
            camera.nearClipPlane = 0.025f;
            camera.farClipPlane = 5f;
            camera.fieldOfView = 78f;
            camera.transform.position = new Vector3(0f, 1.11f, -0.08f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0.08f, 0.72f, 0.72f) - camera.transform.position);

            renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            var output = Path.GetFullPath("Temp/PrototypeTestVisibility.png");
            File.WriteAllBytes(output, image.EncodeToPNG());
            return output;
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (cameraObject != null)
                UnityEngine.Object.DestroyImmediate(cameraObject);
            if (image != null)
                UnityEngine.Object.DestroyImmediate(image);
            if (renderTexture != null)
            {
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
            foreach (var pair in hiddenRoots)
                pair.Key.SetActive(pair.Value);
            if (originalScene.IsValid() && originalScene.isLoaded)
                SceneManager.SetActiveScene(originalScene);
            if (!wasLoaded && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
