using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TestTrappedStartPass
{
    public static string PreviewDustEarly() => PreviewDust(0.55f);
    public static string PreviewDustLate() => PreviewDust(4.65f);

    public static string InspectRuntime()
    {
        var scene = SceneManager.GetActiveScene();
        var camera = Camera.main ?? throw new InvalidOperationException("Main Camera was not found.");
        var seal = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == "Front Rubble Seal")
            ?? throw new InvalidOperationException("Front Rubble Seal was not found.");
        var colliders = seal.GetComponentsInChildren<Collider>(true);
        var renderers = seal.GetComponentsInChildren<Renderer>(true);
        var systems = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true)).ToArray();
        return $"time={Time.time:F2}|camera={camera.transform.position}|sealChunks={seal.childCount}|sealRenderers={renderers.Count(item => item.enabled)}|sealColliders={colliders.Count(item => item.enabled)}|particles={string.Join(";", systems.Select(system => system.name + ":" + system.particleCount))}";
    }

    public static string TestPhoneAngles()
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(FindObjectsInactive.Include)
            ?? throw new InvalidOperationException("PhoneScreenSpillLight was not found.");
        var spill = controller.GetComponent<Light>()
            ?? throw new InvalidOperationException("Screen Spill Light was not found.");
        var phone = controller.transform.root;
        var body = phone.GetComponent<Rigidbody>();
        var originalPosition = phone.position;
        var originalRotation = phone.rotation;
        var originalKinematic = body != null && body.isKinematic;
        var angles = new[] { 0f, 60f, 90f, 120f, 150f, 165f, 175f, 180f };

        try
        {
            if (body != null)
                body.isKinematic = true;
            var results = angles.Select(angle =>
            {
                phone.SetPositionAndRotation(originalPosition, Quaternion.AngleAxis(angle, originalRotation * Vector3.right) * originalRotation);
                Physics.SyncTransforms();
                controller.SetEmissionLevel(1f);
                return $"{angle:F0}deg={spill.intensity:F3}";
            });
            return string.Join("|", results);
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

    public static string DiagnosePhoneAngles()
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneScreenSpillLight>(FindObjectsInactive.Include)
            ?? throw new InvalidOperationException("PhoneScreenSpillLight was not found.");
        var phone = controller.transform.root;
        var originalPosition = phone.position;
        var originalRotation = phone.rotation;
        var body = phone.GetComponent<Rigidbody>();
        var originalKinematic = body != null && body.isKinematic;
        var hits = new RaycastHit[16];
        var angles = new[] { 0f, 90f, 120f, 150f, 165f, 175f, 180f };
        try
        {
            if (body != null)
                body.isKinematic = true;
            return string.Join("\n", angles.Select(angle =>
            {
                phone.SetPositionAndRotation(originalPosition, Quaternion.AngleAxis(angle, originalRotation * Vector3.right) * originalRotation);
                Physics.SyncTransforms();
                var direction = controller.transform.forward;
                var count = Physics.SphereCastNonAlloc(controller.transform.position, 0.004f, direction, hits, 0.035f, ~0, QueryTriggerInteraction.Ignore);
                var details = hits.Take(count)
                    .Where(hit => hit.collider != null && !hit.collider.transform.IsChildOf(phone))
                    .Select(hit => $"{hit.collider.name}@{hit.distance:F4}/inc={Vector3.Dot(-direction, hit.normal):F3}/nY={hit.normal.y:F3}");
                return $"{angle:F0}deg dir={direction} y={direction.y:F3} hits=[{string.Join(",", details)}]";
            }));
        }
        finally
        {
            phone.SetPositionAndRotation(originalPosition, originalRotation);
            Physics.SyncTransforms();
            if (body != null)
                body.isKinematic = originalKinematic;
        }
    }

    static string PreviewDust(float seconds)
    {
        foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(item => IsControllerVisual(item.transform)))
            renderer.enabled = false;

        var systems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var system in systems)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(true);
            system.Simulate(seconds, true, true, false);
        }
        EditorApplication.isPaused = true;
        return $"preview={seconds:F2}s|{string.Join(";", systems.Select(DescribeSystem))}";
    }

    static string DescribeSystem(ParticleSystem system)
    {
        var particles = new ParticleSystem.Particle[Mathf.Max(1, system.particleCount)];
        var count = system.GetParticles(particles);
        var averageAlpha = count == 0 ? 0f : particles.Take(count).Average(particle => particle.GetCurrentColor(system).a / 255f);
        return $"{system.name}:alive={count},alpha={averageAlpha:F3}";
    }

    static bool IsControllerVisual(Transform transform)
    {
        while (transform != null)
        {
            if (transform.name == "Left Controller" || transform.name == "Right Controller")
                return true;
            transform = transform.parent;
        }
        return false;
    }
}
