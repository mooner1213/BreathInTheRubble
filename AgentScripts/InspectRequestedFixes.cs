using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectRequestedFixes
{
    public static string Summary()
    {
        var scene = SceneManager.GetActiveScene();
        var camera = Camera.main ?? throw new InvalidOperationException("Main Camera was not found.");
        var seal = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == "Front Rubble Seal")
            ?? throw new InvalidOperationException("Front Rubble Seal was not found.");
        var particles = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true)).ToArray();
        var controller = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(true))
            .FirstOrDefault() ?? throw new InvalidOperationException("Phone spill controller was not found.");
        var serialized = new SerializedObject(controller);
        return $"scene={scene.path}|dirty={scene.isDirty}|cameraY={camera.transform.position.y:F2}|frontSeal={seal.childCount}|colliders={seal.GetComponentsInChildren<Collider>(true).Length}|dustSystems={particles.Length}|phoneClear={serialized.FindProperty("clearDistance").floatValue:F3}|phoneDown={serialized.FindProperty("minimumDownwardFacingForOverlap").floatValue:F3}";
    }

    public static string InspectDarkness()
    {
        var scene = SceneManager.GetActiveScene();
        var output = new StringBuilder();
        output.AppendLine($"ambientMode={RenderSettings.ambientMode}|ambientIntensity={RenderSettings.ambientIntensity:F3}|ambientLight={RenderSettings.ambientLight}|skybox={(RenderSettings.skybox != null ? RenderSettings.skybox.name : "null")}|reflectionIntensity={RenderSettings.reflectionIntensity:F3}|fog={RenderSettings.fog}");
        foreach (var camera in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)))
            output.AppendLine($"CAMERA|{GetPath(camera.transform)}|clear={camera.clearFlags}|background={camera.backgroundColor}|active={camera.gameObject.activeInHierarchy}|enabled={camera.enabled}");
        foreach (var light in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Light>(true)))
            output.AppendLine($"LIGHT|{GetPath(light.transform)}|active={light.gameObject.activeInHierarchy}|enabled={light.enabled}|type={light.type}|intensity={light.intensity:F3}|range={light.range:F2}|color={light.color}");
        foreach (var system in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true)))
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            var material = renderer.sharedMaterial;
            var baseMap = material != null && material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
            output.AppendLine($"DUST|{system.name}|max={system.main.maxParticles}|life={system.main.startLifetime.constantMin:F1}-{system.main.startLifetime.constantMax:F1}|material={material?.name}|shader={material?.shader?.name}|baseMap={AssetDatabase.GetAssetPath(baseMap)}|keywords={(material != null ? string.Join(",", material.shaderKeywords) : "")}");
        }
        return output.ToString();
    }

    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        var output = new StringBuilder();
        output.AppendLine($"scene={scene.path}|dirty={scene.isDirty}|playing={EditorApplication.isPlaying}");

        var cameras = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .OrderBy(camera => GetPath(camera.transform));
        output.AppendLine("CAMERAS");
        foreach (var camera in cameras)
            output.AppendLine($"{GetPath(camera.transform)}|active={camera.gameObject.activeInHierarchy}|enabled={camera.enabled}|local={camera.transform.localPosition}|world={camera.transform.position}|fwd={camera.transform.forward}|parent={camera.transform.parent?.name}");

        output.AppendLine("XR_AND_ORIGINS");
        foreach (var transform in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                     .Where(item => item.name.Contains("XR", StringComparison.OrdinalIgnoreCase) ||
                                    item.name.Contains("Origin", StringComparison.OrdinalIgnoreCase) ||
                                    item.name.Contains("Offset", StringComparison.OrdinalIgnoreCase)))
            output.AppendLine($"{GetPath(transform)}|local={transform.localPosition}|world={transform.position}|rot={transform.eulerAngles}");

        var mainCamera = Camera.main ?? cameras.FirstOrDefault(camera => camera.enabled);
        if (mainCamera != null)
        {
            output.AppendLine("FRONT_RENDERERS");
            foreach (var renderer in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                         .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                         .Select(renderer => new
                         {
                             Renderer = renderer,
                             Forward = Vector3.Dot(renderer.bounds.center - mainCamera.transform.position, mainCamera.transform.forward),
                             Lateral = Mathf.Abs(Vector3.Dot(renderer.bounds.center - mainCamera.transform.position, mainCamera.transform.right)),
                             Vertical = Mathf.Abs(Vector3.Dot(renderer.bounds.center - mainCamera.transform.position, mainCamera.transform.up))
                         })
                         .Where(item => item.Forward > 0f && item.Forward < 2.5f && item.Lateral < 1.3f && item.Vertical < 1.3f)
                         .OrderBy(item => item.Forward))
            {
                var filter = renderer.Renderer.GetComponent<MeshFilter>();
                var meshName = filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "none";
                output.AppendLine($"{GetPath(renderer.Renderer.transform)}|mesh={meshName}|center={renderer.Renderer.bounds.center}|size={renderer.Renderer.bounds.size}|forward={renderer.Forward:F2}|mat={string.Join(",", renderer.Renderer.sharedMaterials.Where(mat => mat != null).Select(mat => mat.name))}");
            }
        }

        output.AppendLine("DUST");
        foreach (var system in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true)))
        {
            var main = system.main;
            var shape = system.shape;
            output.AppendLine($"{GetPath(system.transform)}|pos={system.transform.position}|life={main.startLifetime.constantMin:F2}-{main.startLifetime.constantMax:F2}|size={main.startSize.constantMin:F3}-{main.startSize.constantMax:F3}|max={main.maxParticles}|shape={shape.shapeType}/{shape.scale}|mat={AssetDatabase.GetAssetPath(system.GetComponent<ParticleSystemRenderer>()?.sharedMaterial)}");
        }

        var spillController = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(true))
            .FirstOrDefault();
        if (spillController != null)
        {
            var serialized = new SerializedObject(spillController);
            var light = spillController.GetComponent<Light>();
            output.AppendLine("PHONE_SPILL");
            output.AppendLine($"path={GetPath(spillController.transform)}|localPos={spillController.transform.localPosition}|fwd={spillController.transform.forward}|type={light?.type}|intensity={light?.intensity:F3}|probe={serialized.FindProperty("probeRadius").floatValue:F3}|blocked={serialized.FindProperty("blockedDistance").floatValue:F3}|clear={serialized.FindProperty("clearDistance").floatValue:F3}");
        }

        return output.ToString();
    }

    public static string InspectXROrigin()
    {
        var scene = SceneManager.GetActiveScene();
        var component = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Component>(true))
            .FirstOrDefault(item => item != null && item.GetType().Name == "XROrigin")
            ?? throw new InvalidOperationException("XROrigin component was not found.");
        var serialized = new SerializedObject(component);
        var property = serialized.GetIterator();
        var output = new StringBuilder($"type={component.GetType().FullName}|path={GetPath(component.transform)}\n");
        if (property.NextVisible(true))
        {
            do
            {
                if (property.name.Contains("camera", StringComparison.OrdinalIgnoreCase) ||
                    property.name.Contains("height", StringComparison.OrdinalIgnoreCase) ||
                    property.name.Contains("origin", StringComparison.OrdinalIgnoreCase) ||
                    property.name.Contains("offset", StringComparison.OrdinalIgnoreCase))
                    output.AppendLine($"{property.propertyPath}|type={property.propertyType}|value={GetValue(property)}");
            }
            while (property.NextVisible(false));
        }
        return output.ToString();
    }

    static string GetValue(SerializedProperty property)
    {
        return property.propertyType switch
        {
            SerializedPropertyType.Float => property.floatValue.ToString("F3"),
            SerializedPropertyType.Integer => property.intValue.ToString(),
            SerializedPropertyType.Boolean => property.boolValue.ToString(),
            SerializedPropertyType.Enum => property.enumDisplayNames[property.enumValueIndex],
            SerializedPropertyType.ObjectReference => property.objectReferenceValue != null ? property.objectReferenceValue.name : "null",
            _ => property.propertyType.ToString()
        };
    }

    static string GetPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
