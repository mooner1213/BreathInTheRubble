using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PreviewPhoneDiscoveryLight
{
    public static string Render()
    {
        var scene = SceneManager.GetActiveScene();
        var sequence = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
            .FirstOrDefault(item => item != null && item.GetType().Name == "PhoneWakeSequence")
            ?? throw new InvalidOperationException("PhoneWakeSequence was not found.");
        var phone = sequence.transform;
        var screen = phone.GetComponentsInChildren<Renderer>(true)
            .FirstOrDefault(item => item.name == "Screen")
            ?? throw new InvalidOperationException("Phone screen renderer was not found.");
        var spill = phone.GetComponentsInChildren<Light>(true)
            .FirstOrDefault(item => item.name == "Screen Spill")
            ?? throw new InvalidOperationException("Screen Spill light was not found.");
        var sourceCamera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(item => item.gameObject.activeInHierarchy)
            ?? throw new InvalidOperationException("Player camera was not found.");

        var lightStates = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Light>(true))
            .ToDictionary(light => light, light => light.enabled);
        var previousPipeline = QualitySettings.renderPipeline;
        RenderPipelineAsset previewPipeline = null;
        var previousMaterial = screen.sharedMaterial;
        var previousActive = RenderTexture.active;
        var previousSpillType = spill.type;
        var previousSpillColor = spill.color;
        var previousSpillIntensity = spill.intensity;
        var previousSpillRange = spill.range;
        var previousSpillShadows = spill.shadows;
        var previousSpillPosition = spill.transform.localPosition;
        Material previewMaterial = null;
        GameObject cameraObject = null;
        RenderTexture renderTexture = null;
        Texture2D image = null;

        try
        {
            if (previousPipeline != null)
            {
                previewPipeline = UnityEngine.Object.Instantiate(previousPipeline);
                var serializedPipeline = new SerializedObject(previewPipeline);
                serializedPipeline.FindProperty("m_AdditionalLightsRenderingMode").intValue = 1;
                serializedPipeline.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 2;
                serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
                QualitySettings.renderPipeline = previewPipeline;
            }
            foreach (var light in lightStates.Keys)
                light.enabled = false;

            spill.enabled = true;
            spill.type = LightType.Point;
            spill.color = new Color(0.62f, 0.78f, 1f);
            spill.intensity = 0.35f;
            spill.range = 0.95f;
            spill.shadows = LightShadows.None;
            spill.transform.localPosition = new Vector3(0f, 0f, -0.065f);

            previewMaterial = new Material(previousMaterial);
            previewMaterial.SetColor("_BaseColor", new Color(0.035f, 0.13f, 0.18f, 1f));
            previewMaterial.SetColor("_EmissionColor", new Color(0.24f, 0.82f, 1.4f, 1f));
            previewMaterial.EnableKeyword("_EMISSION");
            screen.sharedMaterial = previewMaterial;

            cameraObject = new GameObject("Temporary Phone Discovery Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(sourceCamera);
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.0015f, 0.002f, 0.003f, 1f);
            camera.fieldOfView = 74f;
            camera.nearClipPlane = 0.025f;
            camera.farClipPlane = 5f;
            camera.transform.position = sourceCamera.transform.position;
            var target = phone.position + new Vector3(0f, 0.12f, 0f);
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);

            renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();

            var output = Path.GetFullPath("Temp/PhoneDiscoveryCandidate.png");
            File.WriteAllBytes(output, image.EncodeToPNG());
            return output;
        }
        finally
        {
            RenderTexture.active = previousActive;
            QualitySettings.renderPipeline = previousPipeline;
            foreach (var pair in lightStates)
                pair.Key.enabled = pair.Value;
            spill.type = previousSpillType;
            spill.color = previousSpillColor;
            spill.intensity = previousSpillIntensity;
            spill.range = previousSpillRange;
            spill.shadows = previousSpillShadows;
            spill.transform.localPosition = previousSpillPosition;
            if (screen != null)
                screen.sharedMaterial = previousMaterial;
            if (previewMaterial != null)
                UnityEngine.Object.DestroyImmediate(previewMaterial);
            if (previewPipeline != null)
                UnityEngine.Object.DestroyImmediate(previewPipeline);
            if (cameraObject != null)
                UnityEngine.Object.DestroyImmediate(cameraObject);
            if (image != null)
                UnityEngine.Object.DestroyImmediate(image);
            if (renderTexture != null)
            {
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
