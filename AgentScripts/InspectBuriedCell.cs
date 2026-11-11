using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectBuriedCell
{
    public const string Dir="Temp/BuriedCellReview/";
    public static object Before()
    {
        Directory.CreateDirectory(Dir);if(Application.isPlaying)throw new Exception("Edit mode required");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Dir+"Before.unity",true);Views("Before");
        var env=GameObject.Find("Prototype Environment");
        var data=new{head=Camera.main.transform.position.ToString("F3"),fov=Camera.main.fieldOfView,meshes=env.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.bounds.max.y>.65f&&r.bounds.min.y<1.65f).Select(r=>new{name=r.name,mesh=r.GetComponent<MeshFilter>()?.sharedMesh?.name,path=AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh),position=r.transform.position.ToString("F3"),min=r.bounds.min.ToString("F3"),max=r.bounds.max.ToString("F3")}).ToArray()};
        File.WriteAllText(Dir+"Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return new{data.head,data.fov,count=data.meshes.Length};
    }
    public static void Views(string prefix){Capture(prefix+"_Front",Vector3.zero);Capture(prefix+"_Up",new Vector3(-48,0,0));Capture(prefix+"_Left",new Vector3(-15,-48,0));Capture(prefix+"_Right",new Vector3(-15,48,0));Capture(prefix+"_WideUp",new Vector3(-48,0,0),true,Vector3.zero,100);Capture(prefix+"_LeanUp",new Vector3(-48,0,0),true,new Vector3(.12f,.10f,.10f),100);}
    public static void Capture(string name,Vector3 angles,bool lamp=true,Vector3 offset=default(Vector3),float fov=0)
    {
        Directory.CreateDirectory(Dir);var main=Camera.main;var go=new GameObject("Temporary buried cell view"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;if(fov>0)cam.fieldOfView=fov;cam.nearClipPlane=.02f;cam.transform.SetPositionAndRotation(main.transform.position+offset,Quaternion.Euler(angles));cam.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        if(lamp){var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.6f;light.range=4;light.shadows=LightShadows.Soft;}
        var rs=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=rs.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in rs)r.forceRenderingOff=true;var rt=new RenderTexture(1200,850,24);var pixels=new Texture2D(1200,850,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1200,850),0,0);pixels.Apply();File.WriteAllBytes(Dir+name+".png",pixels.EncodeToPNG());}
        finally{for(int i=0;i<rs.Length;i++)rs[i].forceRenderingOff=states[i];cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
    public static object After(){Views("After");return "Captured";}
}
