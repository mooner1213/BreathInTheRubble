using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectEnvironment
{
    public static string Summary()
    {
        var scene = SceneManager.GetActiveScene();
        var filters = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true)).ToArray();
        var angular = filters.Count(filter => filter.sharedMesh != null &&
            AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith("Assets/_Project/Art/ProceduralRubble/"));
        var detailRoot = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == "Procedural Rubble Detail");
        var particles = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true)).Count();
        return $"scene={scene.path}|dirty={scene.isDirty}|angularMeshUsers={angular}|detailFragments={(detailRoot != null ? detailRoot.childCount : 0)}|particleSystems={particles}";
    }

    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        var output = new StringBuilder();
        output.AppendLine($"scene={scene.path}|dirty={scene.isDirty}|roots={scene.rootCount}");

        foreach (var root in scene.GetRootGameObjects())
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var particles = root.GetComponentsInChildren<ParticleSystem>(true);
            output.AppendLine($"ROOT|{root.name}|active={root.activeSelf}|renderers={renderers.Length}|particles={particles.Length}");
        }

        output.AppendLine("RENDERERS");
        foreach (var renderer in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                     .OrderBy(item => item.name))
        {
            var filter = renderer.GetComponent<MeshFilter>();
            var meshName = filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "none";
            var materials = string.Join(",", renderer.sharedMaterials.Where(material => material != null).Select(material => material.name));
            output.AppendLine($"{GetPath(renderer.transform)}|mesh={meshName}|mat={materials}|pos={renderer.transform.position:F3}|scale={renderer.transform.lossyScale:F3}|bounds={renderer.bounds.size:F3}");
        }

        output.AppendLine("PARTICLES");
        foreach (var particle in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true)))
        {
            var main = particle.main;
            var emission = particle.emission;
            var shape = particle.shape;
            output.AppendLine($"{GetPath(particle.transform)}|playing={particle.isPlaying}|duration={main.duration:F2}|loop={main.loop}|max={main.maxParticles}|rate={emission.rateOverTime.constant:F2}|shape={shape.shapeType}|scale={shape.scale:F2}");
        }

        var phoneSequence = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
            .FirstOrDefault(item => item != null && item.GetType().Name == "PhoneWakeSequence");
        if (phoneSequence != null)
        {
            output.AppendLine("PHONE");
            var phone = phoneSequence.transform;
            var screen = phone.GetComponentsInChildren<Renderer>(true).FirstOrDefault(item => item.name == "Screen");
            if (screen != null)
                output.AppendLine($"screenPos={screen.transform.position:F3}|screenForward={screen.transform.forward:F3}|screenUp={screen.transform.up:F3}");
            foreach (var light in phone.GetComponentsInChildren<Light>(true))
                output.AppendLine($"{light.name}|type={light.type}|enabled={light.enabled}|pos={light.transform.position:F3}|forward={light.transform.forward:F3}|intensity={light.intensity:F2}|range={light.range:F2}");
        }

        return output.ToString();
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
