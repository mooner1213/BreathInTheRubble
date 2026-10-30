using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TestRuntimeEnvironment
{
    public static string ReplayDust()
    {
        var scene = SceneManager.GetActiveScene();
        var effect = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<BreathInTheRubble.Prototype.AwakeningDustEffect>(true))
            .FirstOrDefault()
            ?? throw new InvalidOperationException("AwakeningDustEffect was not found.");

        effect.PlayNow();
        var systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var system in systems)
            system.Simulate(0.5f, true, false, false);
        EditorApplication.isPaused = true;

        return string.Join(";", systems.Select(system =>
            $"{system.name}:playing={system.isPlaying},alive={system.particleCount},max={system.main.maxParticles}"));
    }

    public static string Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        var particles = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true))
            .Select(system => $"{system.name}:playing={system.isPlaying},alive={system.particleCount}");
        var angularMeshes = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true))
            .Count(filter => filter.sharedMesh != null && filter.sharedMesh.name.StartsWith("AngularConcrete_", StringComparison.Ordinal));
        return $"time={Time.time:F2}|angularMeshes={angularMeshes}|particles=[{string.Join(";", particles)}]";
    }

    public static string InspectDustPlacement()
    {
        var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        var systems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var details = systems.Select(system =>
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            return $"{system.name}:pos={system.transform.position},bounds={renderer.bounds.center}/{renderer.bounds.size},enabled={renderer.enabled},mat={(renderer.sharedMaterial != null ? renderer.sharedMaterial.shader.name : "null")}";
        });
        return $"camera={camera.name}:pos={camera.transform.position},fwd={camera.transform.forward},near={camera.nearClipPlane:F3}|{string.Join(";", details)}";
    }

    public static string TestPhoneOcclusion()
    {
        var scene = SceneManager.GetActiveScene();
        var sequence = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
            .FirstOrDefault(item => item != null && item.GetType().Name == "PhoneWakeSequence")
            ?? throw new InvalidOperationException("PhoneWakeSequence was not found.");
        var phone = sequence.transform;
        var controller = phone.GetComponentInChildren<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(true)
            ?? throw new InvalidOperationException("PhoneScreenSpillLight was not found.");
        var spill = controller.GetComponent<Light>()
            ?? throw new InvalidOperationException("Screen Spill light was not found.");
        var body = phone.GetComponent<Rigidbody>();
        var originalPosition = phone.position;
        var originalRotation = phone.rotation;
        var originalKinematic = body != null && body.isKinematic;

        try
        {
            if (body != null)
                body.isKinematic = true;
            controller.SetEmissionLevel(1f);
            var faceUpIntensity = spill.intensity;

            phone.rotation = Quaternion.AngleAxis(180f, phone.right) * originalRotation;
            Physics.SyncTransforms();
            controller.SetEmissionLevel(1f);
            var faceDownIntensity = spill.intensity;
            return $"faceUp={faceUpIntensity:F3}|faceDown={faceDownIntensity:F3}|type={spill.type}|angle={spill.spotAngle:F1}";
        }
        finally
        {
            phone.SetPositionAndRotation(originalPosition, originalRotation);
            Physics.SyncTransforms();
            if (body != null)
                body.isKinematic = originalKinematic;
            controller.SetEmissionLevel(1f);
        }
    }
}
