using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TuneAwakeningDust
{
    const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
    const string MaterialPath = "Assets/_Project/Art/Effects/DustParticle.mat";

    public static string Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop play mode before tuning the dust effect.");

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || scene.isDirty)
            throw new InvalidOperationException("Prototype scene must be active and clean.");

        var systems = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true))
            .ToArray();
        var burst = systems.FirstOrDefault(system => system.name == "Initial Dust Cloud")
            ?? throw new InvalidOperationException("Initial Dust Cloud was not found.");
        var motes = systems.FirstOrDefault(system => system.name == "Suspended Dust Motes")
            ?? throw new InvalidOperationException("Suspended Dust Motes was not found.");

        ConfigureColor(burst,
            new Color(0.46f, 0.41f, 0.34f, 0.24f),
            new Color(0.66f, 0.61f, 0.52f, 0.40f), 0.42f);
        ConfigureColor(motes,
            new Color(0.56f, 0.52f, 0.45f, 0.30f),
            new Color(0.76f, 0.70f, 0.60f, 0.48f), 0.50f);
        ConfigurePlacement(burst, new Vector3(0f, 1.34f, 0.65f), new Vector3(1.45f, 0.68f, 0.82f));
        ConfigurePlacement(motes, new Vector3(0f, 1.36f, 0.58f), new Vector3(1.38f, 0.72f, 0.88f));

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath)
            ?? throw new InvalidOperationException("Dust material was not found.");
        material.SetColor("_BaseColor", new Color(0.68f, 0.62f, 0.52f, 0.90f));
        EditorUtility.SetDirty(material);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scene.path);
        AssetDatabase.SaveAssets();
        return "dustCenteredInFront=0.58-0.65m|burstAlpha=0.24-0.40/0.42|motesAlpha=0.30-0.48/0.50|materialAlpha=0.90";
    }

    static void ConfigureColor(ParticleSystem system, Color minimum, Color maximum, float peakAlpha)
    {
        var main = system.main;
        main.startColor = new ParticleSystem.MinMaxGradient(minimum, maximum);

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
        var lifetime = system.colorOverLifetime;
        lifetime.enabled = true;
        lifetime.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    static void ConfigurePlacement(ParticleSystem system, Vector3 localPosition, Vector3 boxScale)
    {
        system.transform.localPosition = localPosition;
        var shape = system.shape;
        shape.scale = boxScale;
    }
}
