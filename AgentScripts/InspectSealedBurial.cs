using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BreathInTheRubble.Prototype;
public static class InspectSealedBurial
{
    public static object Run()
    {
        var scene=SceneManager.GetActiveScene();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var data=new SerializedObject(ending);
        var target=data.FindProperty("rubble");var starts=data.FindProperty("removalStarts");var offsets=data.FindProperty("removalOffsets");
        var result=new{scene=scene.path,dirty=scene.isDirty,pipeline=UniversalRenderPipeline.asset.name,hdr=UniversalRenderPipeline.asset.supportsHDR,shadows=UniversalRenderPipeline.asset.supportsMainLightShadows,
            sky=RenderSettings.skybox?.name,ambient=RenderSettings.ambientLight.ToString(),mode=RenderSettings.ambientMode,reflection=RenderSettings.reflectionIntensity,
            camera=new{Camera.main.clearFlags,Camera.main.backgroundColor,Camera.main.transform.position},
            lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(l=>new{l.name,l.type,l.enabled,l.intensity,l.range,pos=l.transform.position.ToString(),rotation=l.transform.eulerAngles.ToString(),l.shadows}).ToArray(),
            removal=Enumerable.Range(0,target.arraySize).Select(i=>new{name=target.GetArrayElementAtIndex(i).objectReferenceValue.name,start=starts.GetArrayElementAtIndex(i).floatValue,offset=offsets.GetArrayElementAtIndex(i).vector3Value.ToString()}).ToArray(),
            overrides=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(v=>new{v.name,v.weight,profile=v.sharedProfile?.name,enabled=v.enabled,layer=v.gameObject.layer}).ToArray(),
            props=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>new{p.name,pos=p.transform.position.ToString()}).ToArray()
        };return Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore});
    }
}
