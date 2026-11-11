using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectCollapse360
{
    public const string Dir="Temp/Collapse360Review/";
    public static object Before()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");Directory.CreateDirectory(Dir);EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Dir+"Before.unity",true);Views("Before");
        var data=new{head=Camera.main.transform.position.ToString("F3"),props=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).Select(g=>new{g.name,position=g.transform.position.ToString("F3")}).ToArray(),surfaces=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.bounds.max.y>.4f&&r.bounds.min.y<1.6f&&r.bounds.center.magnitude<2).Select(r=>new{r.name,position=r.transform.position.ToString("F3"),min=r.bounds.min.ToString("F3"),max=r.bounds.max.ToString("F3")}).ToArray()};File.WriteAllText(Dir+"Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return new{data.head,data.props,count=data.surfaces.Length};
    }
    public static void Views(string prefix){Capture(prefix+"_Front",Vector3.zero,true);Capture(prefix+"_Right",new Vector3(0,90,0),true);Capture(prefix+"_Rear",new Vector3(0,180,0),true);Capture(prefix+"_Left",new Vector3(0,270,0),true);Capture(prefix+"_Above",new Vector3(-78,0,0),true);Capture(prefix+"_Below",new Vector3(60,0,0),true);}
    public static object After(){Views("After");return "Six directions captured";}
    public static void Capture(string name,Vector3 angles,bool lamp)
    {
        Directory.CreateDirectory(Dir);var main=Camera.main;var go=new GameObject("Temporary 360 rubble inspection"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;cam.fieldOfView=90;cam.nearClipPlane=.02f;cam.transform.SetPositionAndRotation(main.transform.position,Quaternion.Euler(angles));cam.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        if(lamp){var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.35f;light.range=4;light.shadows=LightShadows.Soft;}
        var rs=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=rs.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in rs)r.forceRenderingOff=true;var rt=new RenderTexture(1100,850,24);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes(Dir+name+".png",pixels.EncodeToPNG());}finally{for(int i=0;i<rs.Length;i++)rs[i].forceRenderingOff=states[i];cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
