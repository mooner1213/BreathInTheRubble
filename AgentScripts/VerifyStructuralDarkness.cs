using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using BreathInTheRubble.Prototype;
public static class VerifyStructuralDarkness
{
    public static object Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool enabled=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();
        var ending=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>());var sky=RenderSettings.skybox;
        var lights=UnityEngine.Object.FindObjectsByType<Light>(UnityEngine.FindObjectsInactive.Include,FindObjectsSortMode.None);var lightStates=lights.Select(l=>l.enabled).ToArray();
        var env=GameObject.Find("Prototype Environment").transform;var hidden=UnityEngine.Object.FindObjectsByType<Renderer>(UnityEngine.FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>!r.transform.IsChildOf(env)||r is ParticleSystemRenderer).ToArray();var hiddenStates=hidden.Select(r=>r.forceRenderingOff).ToArray();
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(UnityEngine.FindObjectsInactive.Include,FindObjectsSortMode.None);var canvasStates=canvases.Select(c=>c.enabled).ToArray();
        var go=new GameObject("Temporary structural darkness audit");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.fieldOfView=95;camera.nearClipPlane=.02f;camera.clearFlags=CameraClearFlags.Skybox;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var rt=new RenderTexture(512,512,24);var texture=new Texture2D(512,512,TextureFormat.RGB24,false);var active=RenderTexture.active;var samples=new List<object>();var leaks=new List<Vector3>();
        try{
            foreach(var l in lights)l.enabled=false;foreach(var r in hidden)r.forceRenderingOff=true;foreach(var c in canvases)c.enabled=false;RenderSettings.skybox=(Material)ending.FindProperty("daylightSky").objectReferenceValue;
            float[] times={0,7.8f,18.1f};Vector3[] directions={Vector3.forward,Vector3.back,Vector3.left,Vector3.right,Vector3.up,Vector3.down};sequence.BeginAftershockNow();
            foreach(var time in times){sequence.Advance(Mathf.Max(0,time-UnityEngine.Object.FindFirstObjectByType<AftershockDirector>().ElapsedSeconds));UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();foreach(var l in lights)l.enabled=false;foreach(var r in hidden)r.forceRenderingOff=true;
                foreach(var direction in directions){camera.transform.SetPositionAndRotation(Camera.main.transform.position,Quaternion.LookRotation(direction,Mathf.Abs(direction.y)>.9f?Vector3.forward:Vector3.up));camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,512,512),0,0);texture.Apply();var pixels=texture.GetPixels32();int maximum=pixels.Max(p=>Math.Max(p.r,Math.Max(p.g,p.b)));samples.Add(new{time,direction=direction.ToString(),maximum});if(maximum>0){File.WriteAllBytes("Temp/StructuralAftershockReview/DarknessFailure.png",texture.EncodeToPNG());for(int i=0;i<pixels.Length;i++)if(Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b))>0){var ray=camera.ViewportPointToRay(new Vector3((i%512+.5f)/512,(i/512+.5f)/512,0));if(leaks.All(d=>Vector3.Dot(d,ray.direction)<.999f))leaks.Add(ray.direction);}}}
            }
            File.WriteAllText("Temp/StructuralAftershockReview/Darkness.json",Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));File.WriteAllText("Temp/StructuralAftershockReview/GapDirections.json",Newtonsoft.Json.JsonConvert.SerializeObject(leaks.Select(d=>new[]{d.x,d.y,d.z}).ToArray()));return new{result=leaks.Count==0?"PASS":"GAPS",views=samples.Count,leakDirections=leaks.Count};
        }
        finally{sequence.RestoreAftershock();sequence.enabled=enabled;RenderSettings.skybox=sky;for(int i=0;i<lights.Length;i++)lights[i].enabled=lightStates[i];for(int i=0;i<hidden.Length;i++)hidden[i].forceRenderingOff=hiddenStates[i];for(int i=0;i<canvases.Length;i++)canvases[i].enabled=canvasStates[i];camera.targetTexture=null;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
