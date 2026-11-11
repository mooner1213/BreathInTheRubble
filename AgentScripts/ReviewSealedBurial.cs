using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BreathInTheRubble.Prototype;

public static class ReviewSealedBurial
{
    static List<Vector3> pixelLeaks=new List<Vector3>();
    sealed class Surface
    {
        public MeshFilter filter;public Vector3[] vertices;public int[] triangles;public Bounds bounds;public Matrix4x4 toLocal;
        public bool Hit(Ray world){var ray=new Ray(toLocal.MultiplyPoint3x4(world.origin),toLocal.MultiplyVector(world.direction).normalized);if(!bounds.IntersectRay(ray))return false;
            for(int i=0;i<triangles.Length;i+=3){var a=vertices[triangles[i]];var e1=vertices[triangles[i+1]]-a;var e2=vertices[triangles[i+2]]-a;var h=Vector3.Cross(ray.direction,e2);float d=Vector3.Dot(e1,h);if(Mathf.Abs(d)<.000001f)continue;float inverse=1/d;var s=ray.origin-a;float u=inverse*Vector3.Dot(s,h);if(u<0||u>1)continue;var q=Vector3.Cross(s,e1);float v=inverse*Vector3.Dot(ray.direction,q);if(v<0||u+v>1)continue;if(inverse*Vector3.Dot(e2,q)>.001f)return true;}return false;
        }
    }
    static Surface[] Surfaces(){var env=GameObject.Find("Prototype Environment");return env.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.GetComponent<Renderer>() is Renderer r&&r.enabled&&r.gameObject.activeInHierarchy).Where(f=>{var lod=f.GetComponentInParent<LODGroup>();return lod==null||lod.GetLODs()[0].renderers.Contains(f.GetComponent<Renderer>());}).Select(f=>new Surface{filter=f,vertices=f.sharedMesh.vertices,triangles=f.sharedMesh.triangles,bounds=f.sharedMesh.bounds,toLocal=f.transform.worldToLocalMatrix}).ToArray();}
    static List<Ray> Leaks(Surface[] surfaces,int n=2048){
        var origins=new[]{new Vector3(0,.9f,0),new Vector3(.16f,.9f,0),new Vector3(-.16f,.9f,0),new Vector3(0,1.03f,0),new Vector3(0,.9f,.16f),new Vector3(0,.9f,-.16f)};
        var leaks=new List<Ray>();foreach(var origin in origins)for(int i=0;i<n;i++){float y=1-2*(i+.5f)/n;float radius=Mathf.Sqrt(1-y*y);float theta=i*2.39996323f;var ray=new Ray(origin,new Vector3(Mathf.Cos(theta)*radius,y,Mathf.Sin(theta)*radius));if(!surfaces.Any(s=>s.Hit(ray)))leaks.Add(ray);}return leaks;
    }
    public static object SealAndAudit()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");int added=0;int initial=-1;
        foreach(var lod in GameObject.Find("Sealed Rubble Burial").GetComponentsInChildren<LODGroup>(true)){lod.enabled=false;var ls=lod.GetLODs();for(int i=0;i<ls.Length;i++)foreach(var r in ls[i].renderers)r.enabled=i==0;}
        var pixelFile="Temp/SealedBurialReview/PixelLeaks.json";var extra=new Queue<Ray>();
        if(File.Exists(pixelFile))foreach(var entry in Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(pixelFile)))extra.Enqueue(new Ray(new Vector3(0,.9f,0),new Vector3((float)entry[0],(float)entry[1],(float)entry[2])));
        for(int pass=0;pass<25;pass++){
            var leaks=Leaks(Surfaces());if(initial<0)initial=leaks.Count;if(extra.Count>0)leaks.Insert(0,extra.Dequeue());if(leaks.Count==0)break;
            var ray=leaks[0];var parent=GameObject.Find(ray.direction.y>.18f?"Burial Roof Outer Layer":"Overlapping Side Collapse").transform;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Piles/Rubble_Big.prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name="Staggered gap closure "+(added+1);go.transform.rotation=Quaternion.FromToRotation(Vector3.up,-ray.direction);go.transform.localScale=Vector3.one*.64f;
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;}
            foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
            var lod=go.GetComponent<LODGroup>();var ls=lod.GetLODs();ls[ls.Length-1].screenRelativeTransitionHeight=0;lod.SetLODs(ls);lod.enabled=false;for(int i=0;i<ls.Length;i++)foreach(var r in ls[i].renderers)r.enabled=i==0;var rs=ls[0].renderers;var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);go.transform.position+=ray.origin+ray.direction*2.2f-b.center;added++;
        }
        var final=Leaks(Surfaces());var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var so=new SerializedObject(ending);var p=so.FindProperty("lightBlockers");var blockers=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();p.arraySize=blockers.Length;for(int i=0;i<blockers.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=blockers[i];so.ApplyModifiedProperties();
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Temp/SealedBurialReview");File.WriteAllText(pixelFile,"[]");var result=new{rays=12288,headSamplePositions=6,initialLeaks=initial,additionalPaidRubble=added,remainingLeaks=final.Count,checks="Actual LOD0 triangles plus rendered pixel holes; no external wall/void planes used"};File.WriteAllText("Temp/SealedBurialReview/Coverage.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object InspectViews()=>CaptureViews("Inspection",true);
    public static object CaptureViews(string prefix,bool lamp)
    {
        Directory.CreateDirectory("Temp/SealedBurialReview");var main=Camera.main;var go=new GameObject("Temporary burial review"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.fieldOfView=90;camera.nearClipPlane=.02f;camera.transform.position=main.transform.position;
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;data.requiresDepthTexture=true;
        if(lamp){var l=go.AddComponent<Light>();l.type=LightType.Point;l.intensity=.65f;l.range=4;l.shadows=LightShadows.Soft;}
        var xr=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=xr.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in xr)r.forceRenderingOff=true;
        string[] names={"Front","Right","Rear","Left","Ceiling","Floor"};Vector3[] angles={new Vector3(0,0,0),new Vector3(0,90,0),new Vector3(0,180,0),new Vector3(0,270,0),new Vector3(-80,0,0),new Vector3(70,0,0)};
        var values=new List<object>();
        try{for(int i=0;i<names.Length;i++){camera.transform.rotation=Quaternion.Euler(angles[i]);values.Add(Save(camera,prefix+"_"+names[i]));}}finally{for(int i=0;i<xr.Length;i++)xr[i].forceRenderingOff=states[i];UnityEngine.Object.DestroyImmediate(go);}return values;
    }
    static object Save(Camera camera,string name){var rt=new RenderTexture(1100,850,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var active=RenderTexture.active;var previous=camera.targetTexture;
        try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes("Temp/SealedBurialReview/"+name+".png",pixels.EncodeToPNG());double total=0;int clipped=0;var colors=pixels.GetPixels32();for(int i=0;i<colors.Length;i++){var c=colors[i];float value=(c.r*.2126f+c.g*.7152f+c.b*.0722f)/255f;total+=value;if(value>.98f)clipped++;if(name.StartsWith("Closed_NoPhoneLight")&&Mathf.Max(c.r,c.g,c.b)>3){var d=camera.ViewportPointToRay(new Vector3((i%1100+.5f)/1100,(i/1100+.5f)/850,0)).direction;if(pixelLeaks.All(p=>Vector3.Dot(p,d)<.985f))pixelLeaks.Add(d);}}return new{name,mean=total/(1100*850),clipped=clipped/(1100f*850f)};}finally{camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
    public static object RuntimeReview()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required");var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();bool enabled=sequence.enabled;sequence.enabled=false;var frames=new List<object>();var head=Camera.main.transform;var start=head.position;var rotation=head.rotation;
        try{
            sequence.RestoreAftershock();sequence.BeginAftershockNow();sequence.Advance(18.1f);UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>().Clear();
            sequence.PreviewNextStage();foreach(float time in new[]{0f,10f,22f,34f,46f,60f}){ending.Advance(time-ending.ElapsedSeconds);frames.Add(new{time,ending.OpeningAmount,ending.SunlightAdmission,ending.WhiteoutAmount,images=CaptureViews("Rescue_"+time.ToString("00"),false)});}
            if(!ending.IsComplete||ending.WhiteoutAmount!=0)throw new Exception("Completion must preserve the natural view");if(head.position!=start||head.rotation!=rotation)throw new Exception("Head moved");
            var result=new{result="PASS",frames};File.WriteAllText("Temp/SealedBurialReview/Rescue.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return "PASS: natural rescue capture at 0/10/22/34/46/60 seconds; no camera movement or whiteout";
        }finally{sequence.RestoreAftershock();sequence.enabled=enabled;}
    }
    public static object Darkness()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool enabled=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();
        var env=GameObject.Find("Prototype Environment").transform;var data=new SerializedObject(ending);var sun=(Light)data.FindProperty("openingLight").objectReferenceValue;
        var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None);var lightStates=lights.Select(l=>l.enabled).ToArray();
        var hidden=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>!r.transform.IsChildOf(env)||r is ParticleSystemRenderer).ToArray();var rendererStates=hidden.Select(r=>r.forceRenderingOff).ToArray();
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None);var canvasStates=canvases.Select(c=>c.enabled).ToArray();var sky=RenderSettings.skybox;var clear=Camera.main.clearFlags;
        try{
            foreach(var l in lights)l.enabled=false;foreach(var r in hidden)r.forceRenderingOff=true;foreach(var c in canvases)c.enabled=false;
            RenderSettings.skybox=(Material)data.FindProperty("daylightSky").objectReferenceValue;Camera.main.clearFlags=CameraClearFlags.Skybox;
            pixelLeaks.Clear();var result=CaptureViews("Closed_NoPhoneLight",false);File.WriteAllText("Temp/SealedBurialReview/PixelLeaks.json",Newtonsoft.Json.JsonConvert.SerializeObject(pixelLeaks.Select(p=>new[]{p.x,p.y,p.z}).ToArray()));File.WriteAllText("Temp/SealedBurialReview/Darkness.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }finally{for(int i=0;i<lights.Length;i++)lights[i].enabled=lightStates[i];for(int i=0;i<hidden.Length;i++)hidden[i].forceRenderingOff=rendererStates[i];for(int i=0;i<canvases.Length;i++)canvases[i].enabled=canvasStates[i];RenderSettings.skybox=sky;Camera.main.clearFlags=clear;sequence.enabled=enabled;}
    }
}
