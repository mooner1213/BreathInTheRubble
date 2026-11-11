using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using BreathInTheRubble.Prototype;
public static class RefineSeismicDust
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var so=new SerializedObject(dust);var veil=so.FindProperty("dustVeil").objectReferenceValue as ParticleSystem;
        so.FindProperty("collapseDensity").floatValue=2;so.ApplyModifiedPropertiesWithoutUndo();
        var main=veil.main;main.maxParticles=100;main.startSize=new ParticleSystem.MinMaxCurve(.32f,.52f);main.startColor=new ParticleSystem.MinMaxGradient(new Color(.56f,.53f,.47f,.72f),new Color(.72f,.68f,.60f,.88f));
        var mat=veil.GetComponent<ParticleSystemRenderer>().sharedMaterial;var color=mat.GetColor("_BaseColor");color.a=.88f;mat.SetColor("_BaseColor",color);EditorUtility.SetDirty(mat);EditorUtility.SetDirty(veil);
        var camera=Camera.main;var data=camera.GetUniversalAdditionalCameraData();var prior=data.requiresDepthOption;data.requiresDepthOption=CameraOverrideOption.On;EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);EditorSceneManager.SaveScene(camera.gameObject.scene);
        return new{veil.name,main.maxParticles,previousDepthOption=prior.ToString(),depth=data.requiresDepthTexture,peakDensity=2};
    }
}
