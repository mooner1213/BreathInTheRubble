using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;

public static class BuildAwakeningAtmosphere
{
    static Material Create(string name,Shader shader)
    {
        var path="Assets/_Project/Art/Atmosphere/"+name+".mat";
        if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("Refusing to overwrite "+path);
        var material=new Material(shader){name=name,enableInstancing=true};AssetDatabase.CreateAsset(material,path);return material;
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        if(UnityEngine.Object.FindFirstObjectByType<AwakeningReveal>()!=null)throw new Exception("Awakening reveal already configured");
        var shader=Shader.Find("BreathInTheRubble/Light Scattering Dust");var eyelidShader=Shader.Find("BreathInTheRubble/Awakening Eyelids");
        if(shader==null||eyelidShader==null)throw new Exception("Shaders not imported");
        if(!AssetDatabase.IsValidFolder("Assets/_Project/Art/Atmosphere"))AssetDatabase.CreateFolder("Assets/_Project/Art","Atmosphere");
        var motes=Create("ScatteringDustMotes",shader);var haze=Create("ScatteringDustHaze",shader);
        var oldMotes=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Aftershock/AtmosphericDustUnlit.mat");var oldHaze=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Aftershock/AtmosphericVeilUnlit.mat");
        foreach(var pair in new[]{new[]{motes,oldMotes},new[]{haze,oldHaze}})
        {
            pair[0].SetTexture("_BaseMap",pair[1].GetTexture("_BaseMap"));pair[0].SetColor("_BaseColor",pair[1].GetColor("_BaseColor"));
            pair[0].SetFloat("_AmbientScatter",pair[0]==haze?.003f:.0015f);pair[0].SetFloat("_ScatteringGain",pair[0]==haze?.20f:.65f);
            pair[0].SetFloat("_SoftDistance",pair[0]==haze?.08f:.015f);pair[0].SetFloat("_NearFade",pair[0]==haze?.22f:.10f);
        }
        var particles=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach(var p in particles)
        {
            var renderer=p.GetComponent<ParticleSystemRenderer>();var old=renderer.sharedMaterial;
            if(old!=oldMotes&&old!=oldHaze)continue;
            Undo.RecordObject(renderer,"Light-reactive dust");renderer.sharedMaterial=old==oldHaze?haze:motes;renderer.receiveShadows=true;renderer.shadowCastingMode=ShadowCastingMode.Off;
            if(old==oldMotes)
            {
                var main=p.main;main.startSize=p.name.Contains("Suspended")?new ParticleSystem.MinMaxCurve(.0012f,.0035f):new ParticleSystem.MinMaxCurve(.0025f,.007f);
            }
        }
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var data=new SerializedObject(dust);data.FindProperty("dustColor").colorValue=new Color(.006f,.006f,.005f,1);data.ApplyModifiedPropertiesWithoutUndo();
        var camera=Camera.main;var cameraData=camera.GetUniversalAdditionalCameraData();cameraData.requiresDepthTexture=true;
        EditorUtility.SetDirty(cameraData);if(PrefabUtility.IsPartOfPrefabInstance(cameraData))PrefabUtility.RecordPrefabInstancePropertyModifications(cameraData);
        var node=new GameObject("Awakening Opening Eyes");node.transform.SetParent(camera.transform,false);
        var reveal=node.AddComponent<AwakeningReveal>();var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Soft Eyelid Overlay";quad.transform.SetParent(node.transform,false);quad.transform.localPosition=Vector3.forward*.025f;quad.transform.localScale=Vector3.one*.5f;
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());var eyelids=quad.GetComponent<MeshRenderer>();eyelids.sharedMaterial=Create("AwakeningEyelids",eyelidShader);eyelids.shadowCastingMode=ShadowCastingMode.Off;eyelids.receiveShadows=false;eyelids.enabled=false;
        var settings=new SerializedObject(reveal);settings.FindProperty("eyelidRenderer").objectReferenceValue=eyelids;settings.FindProperty("closedSeconds").floatValue=.6f;settings.FindProperty("openingSeconds").floatValue=2.8f;settings.ApplyModifiedPropertiesWithoutUndo();
        Undo.RegisterCreatedObjectUndo(node,"Opening-eye reveal");
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        return new{result="Opening eyes and light-reactive dust configured",openingSeconds=3.4f,systems=particles.Length,materials=new[]{AssetDatabase.GetAssetPath(motes),AssetDatabase.GetAssetPath(haze)},shaderErrors=ShaderUtil.GetShaderMessages(shader).Concat(ShaderUtil.GetShaderMessages(eyelidShader)).Select(m=>m.message).ToArray(),lightsUntouched=true};
    }
}
