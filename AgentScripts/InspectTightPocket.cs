using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectTightPocket
{
    public const string Dir="Temp/TightPocketReview/";
    public static object Before()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");Directory.CreateDirectory(Dir);EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Dir+"Before.unity",true);Views("Before");
        var names=new[]{"Front upper compressed masonry","Front compacted landing bank","Left shoulder packed backing","Right shoulder packed backing","Left leaning load slab","Right leaning load slab","Pipe landing bearing slab"};
        var result=new{head=Camera.main.transform.position.ToString("F3"),surfaces=names.Select(n=>{var go=GameObject.Find(n);var r=go.GetComponentsInChildren<MeshRenderer>(true).First(v=>v.enabled);return new{name=n,position=go.transform.position.ToString("F3"),min=r.bounds.min.ToString("F3"),max=r.bounds.max.ToString("F3")};}).ToArray(),rays=Distances()};
        File.WriteAllText(Dir+"Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object Distances()=>new[]{-45f,-25f,0,25f,45f}.Select(yaw=>{var d=Quaternion.Euler(0,yaw,0)*Vector3.forward;var hits=Physics.RaycastAll(Camera.main.transform.position,d,4).Where(h=>h.collider.GetComponent<MeshRenderer>() is MeshRenderer r&&r.enabled&&r.gameObject.activeInHierarchy).OrderBy(h=>h.distance).ToArray();return new{yaw,distance=hits.Length==0?-1:hits[0].distance,surface=hits.Length==0?"none":hits[0].collider.name};}).ToArray();
    public static object After(){Views("After");var distances=Distances();File.WriteAllText(Dir+"AfterDistances.json",Newtonsoft.Json.JsonConvert.SerializeObject(distances,Newtonsoft.Json.Formatting.Indented));return distances;}
    public static void Views(string prefix){Capture(prefix+"_Front",new Vector3(0,0,0));Capture(prefix+"_Right",new Vector3(15,50,0));Capture(prefix+"_Left",new Vector3(10,-50,0));}
    public static void Capture(string name,Vector3 angles,bool lamp=true)
    {
        var main=Camera.main;var go=new GameObject("Temporary tight pocket view"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;cam.fieldOfView=90;cam.nearClipPlane=.02f;cam.transform.SetPositionAndRotation(main.transform.position,Quaternion.Euler(angles));cam.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        if(lamp){var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.8f;light.range=4;light.shadows=LightShadows.Soft;}
        var rs=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=rs.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in rs)r.forceRenderingOff=true;var rt=new RenderTexture(1100,850,24);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes(Dir+name+".png",pixels.EncodeToPNG());}
        finally{for(int i=0;i<rs.Length;i++)rs[i].forceRenderingOff=states[i];cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
