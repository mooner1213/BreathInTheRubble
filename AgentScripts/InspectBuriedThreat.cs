using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectBuriedThreat
{
    public static object FinalAudit()
    {
        var root=GameObject.Find("Prototype Environment");var added=new[]{GameObject.Find("Close Packed Survival Pocket"),GameObject.Find("Foreground Load Failures")};
        var renderers=added.SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.enabled).ToArray();
        var result=new{scene=SceneManager.GetActiveScene().path,newMeshRenderers=renderers.Length,newParticleSystems=added.Sum(g=>g.GetComponentsInChildren<ParticleSystem>(true).Length),newTriangles=renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3),defaultOrBrokenMaterials=renderers.Where(r=>r.sharedMaterials.Any(m=>m==null||m.name=="Default-Material"||!m.shader.isSupported)).Select(r=>r.name).ToArray(),grabs=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).Select(g=>new{g.name,position=g.transform.position.ToString("F3")}).ToArray(),xrOrigins=UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length,inputManagers=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>(FindObjectsSortMode.None).Length};
        File.WriteAllText("Temp/BuriedThreatReview/FinalAudit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object Tracked()=>Camera.main.transform.root.GetComponentsInChildren<Transform>(true).Where(t=>t==Camera.main.transform||t.name=="Left Controller"||t.name=="Right Controller").Select(t=>new{t.name,world=t.position.ToString("F3"),local=t.localPosition.ToString("F3")}).ToArray();
    public static object Details()
    {
        return new {
          grabs=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).Select(g=>new{g.name,position=g.transform.position.ToString("F3")}).ToArray(),
          assets=new[]{"Props/Wall_Grey_Big_01","WithRebar/Rebar_Wall_01","WithRebar/Rebar_Wall_08","Props/Wall_Gray_Medium_03","Props/Bricks_Big_02","Piles/Rubble_Small_01","Piles/Rubble_Medium"}.Select(p=>{var g=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/"+p+".prefab");return new {p,renderers=g.GetComponent<LODGroup>().GetLODs()[0].renderers.Select(r=>new{r.name,size=r.bounds.size.ToString("F3")}).ToArray()};}).ToArray(),
          motionRefs=Refs(UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>(),"ceilingTargets"),
          rescueRefs=Refs(UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>(),"rubble"),
          ticks=EditorPrefs.GetBool("Unity.Pipeline.Editor.AutoTick",false),background=Application.runInBackground };
    }
    static string[] Refs(UnityEngine.Object o,string field){var p=new SerializedObject(o).FindProperty(field);return Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue?.name).ToArray();}
    public static object BeforeViews()=>Capture("Before");
    public static object AfterViews()=>Capture("After");
    public static object Capture(string prefix)
    {
        var main=Camera.main; var go=new GameObject("Temporary buried threat review"){hideFlags=HideFlags.HideAndDontSave};
        var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;cam.fieldOfView=90;cam.nearClipPlane=.02f;cam.transform.position=main.transform.position;
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.8f;light.range=4;light.shadows=LightShadows.Soft;
        var hidden=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=hidden.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in hidden)r.forceRenderingOff=true;
        var rt=new RenderTexture(1100,850,24);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        var angles=new[]{new Vector3(0,0,0),new Vector3(0,90,0),new Vector3(0,180,0),new Vector3(0,270,0),new Vector3(-70,0,0),new Vector3(65,0,0)};
        string[] names={"Front","Right","Rear","Left","Ceiling","Floor"};
        try{for(int i=0;i<angles.Length;i++){cam.transform.rotation=Quaternion.Euler(angles[i]);cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes("Temp/BuriedThreatReview/"+prefix+"_"+names[i]+".png",pixels.EncodeToPNG());}}
        finally{for(int i=0;i<hidden.Length;i++)hidden[i].forceRenderingOff=states[i];cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
        return "Temp/BuriedThreatReview/"+prefix;
    }
    public static object Run()
    {
        if(Application.isPlaying) throw new Exception("Edit mode required");
        Directory.CreateDirectory("Temp/BuriedThreatReview");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity") throw new Exception("Wrong scene");
        EditorSceneManager.SaveScene(scene,"Temp/BuriedThreatReview/Before.unity",true);
        var env=GameObject.Find("Prototype Environment");
        var renderers=env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        var result=new { scene=scene.path,head=Camera.main.transform.position.ToString("F3"),pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,
            roots=env.transform.Cast<Transform>().Select(t=>new{name=t.name,position=t.position.ToString("F3"),children=t.childCount}).ToArray(),
            surfaces=renderers.Select(r=>new{path=PathOf(r.transform),min=r.bounds.min.ToString("F3"),max=r.bounds.max.ToString("F3"),mesh=AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh)}).ToArray(),
            chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).Select(c=>new{path=PathOf(c.transform),bounds=c.GetComponent<Renderer>().bounds.ToString("F3")}).ToArray(),
            motion=EditorJsonUtility.ToJson(UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>()),
            director=EditorJsonUtility.ToJson(UnityEngine.Object.FindFirstObjectByType<AftershockDirector>()),
            rescue=EditorJsonUtility.ToJson(UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>()) };
        File.WriteAllText("Temp/BuriedThreatReview/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return new {scene=result.scene,result.head,result.pipeline,roots=result.roots,chunks=result.chunks,renderers=renderers.Length};
    }
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
}
