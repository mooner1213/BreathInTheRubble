using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class TuneAwakeningAtmosphere
{
    public static object InspectStreams()
    {
        var awakening=UnityEngine.Object.FindFirstObjectByType<AwakeningDustEffect>();awakening.PlayNow();var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();dust.ReplayAwakening();
        return awakening.GetComponentsInChildren<ParticleSystem>().Select(p=>
        {
            p.Simulate(1f,false,false,false);p.Pause(false);var renderer=p.GetComponent<ParticleSystemRenderer>();var streams=new System.Collections.Generic.List<ParticleSystemVertexStream>();renderer.GetActiveVertexStreams(streams);
            var mesh=new Mesh();renderer.BakeMesh(mesh,Camera.main,ParticleSystemBakeMeshOptions.BakeRotationAndScale);var uv=mesh.uv.Take(8).Select(x=>x.ToString("F3")).ToArray();var uv2=mesh.uv2.Take(8).Select(x=>x.ToString("F3")).ToArray();UnityEngine.Object.DestroyImmediate(mesh);
            return new{name=p.name,mode=renderer.renderMode.ToString(),streams=streams.Select(s=>s.ToString()).ToArray(),uv,uv2};
        }).ToArray();
    }
    public static object Run()
    {
        AssetDatabase.ImportAsset("Assets/_Project/Art/LightScatteringDust.shader",ImportAssetOptions.ForceUpdate);
        var cameraData=Camera.main.GetUniversalAdditionalCameraData();cameraData.requiresDepthTexture=true;
        EditorUtility.SetDirty(cameraData);if(PrefabUtility.IsPartOfPrefabInstance(cameraData))PrefabUtility.RecordPrefabInstancePropertyModifications(cameraData);
        var haze=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Atmosphere/ScatteringDustHaze.mat");
        haze.SetFloat("_ScatteringGain",.60f);haze.SetFloat("_AmbientScatter",.004f);EditorUtility.SetDirty(haze);
        var reveal=UnityEngine.Object.FindFirstObjectByType<AwakeningReveal>();var eyelids=(MeshRenderer)new SerializedObject(reveal).FindProperty("eyelidRenderer").objectReferenceValue;
        eyelids.sortingOrder=32767;eyelids.sharedMaterial.renderQueue=5000;EditorUtility.SetDirty(eyelids.sharedMaterial);
        foreach(var p in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            if(p.GetComponent<Renderer>().sharedMaterial!=haze)continue;
            var main=p.main;var color=main.startColor.colorMax;color.a=p.name.Contains("Veil")?.32f:.36f;main.startColor=color;
            if(p.name=="Dense Dust Veil"){var emission=p.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0f,36)});}
        }
        var atmosphere=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>());atmosphere.FindProperty("startingDensity").floatValue=.9f;atmosphere.ApplyModifiedPropertiesWithoutUndo();
        var go=GameObject.Find("Faint Dust Light Through Rubble");
        if(go==null)
        {
            go=new GameObject("Faint Dust Light Through Rubble");go.transform.SetParent(GameObject.Find("Prototype Lighting").transform,false);
            go.transform.position=new Vector3(-.36f,1.16f,.40f);go.transform.rotation=Quaternion.LookRotation(new Vector3(.18f,-.18f,-1f));
            var light=go.AddComponent<Light>();light.type=LightType.Spot;light.range=1.65f;light.innerSpotAngle=12f;light.color=new Color(.48f,.52f,.58f);light.shadows=LightShadows.None;
        }
        go.GetComponent<Light>().intensity=.008f;go.GetComponent<Light>().spotAngle=52f;
        if(!Application.isPlaying){AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());}
        return new{hazeGain=haze.GetFloat("_ScatteringGain"),textureScale=haze.GetTextureScale("_BaseMap").ToString(),textureST=haze.GetVector("_BaseMap_ST").ToString(),eyelidOrder=eyelids.sortingOrder,queue=eyelids.sharedMaterial.renderQueue,faintLight=go.GetComponent<Light>().intensity,canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Select(c=>new{name=c.name,mode=c.renderMode.ToString(),order=c.sortingOrder,overrideSorting=c.overrideSorting}).ToArray()};
    }
}
