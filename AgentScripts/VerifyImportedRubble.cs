using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class VerifyImportedRubble
{
    static void Check(bool value,string text){if(!value)throw new Exception(text);}
    public static object Prepare()
    {
        var previews=Resources.FindObjectsOfTypeAll<GameObject>().Select(g=>g.scene).Where(s=>s.IsValid()&&EditorSceneManager.IsPreviewScene(s)&&s.path.Contains("BeforeImportedRubble")).Distinct().ToArray();
        foreach(var s in previews)EditorSceneManager.ClosePreviewScene(s);
        return new{closedPreviews=previews.Length,runInBackground=Application.runInBackground};
    }
    public static object Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"PC simulation only");
        var scene=SceneManager.GetActiveScene();var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();
        var head=Camera.main.transform;var headPosition=head.position;var headRotation=head.rotation;
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();if(driver!=null)driver.SetDesktopMode(true);
        bool active=sequence.enabled;sequence.enabled=false;
        var surfaces=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.sharedMesh!=null&&f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().sharedMaterials.Any(m=>m!=null&&m.name.Contains("_URP"))).ToArray();
        var shaders=surfaces.SelectMany(f=>f.GetComponent<Renderer>().sharedMaterials).Distinct().ToArray();
        var targets=new SerializedObject(ending).FindProperty("rubble");var roofs=Enumerable.Range(0,targets.arraySize).Select(i=>(Transform)targets.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var positions=roofs.Select(t=>t.position).ToArray();
        try{
            Check(surfaces.Length>=329,"Missing imported replacements: "+surfaces.Length);
            Check(shaders.All(m=>m!=null&&m.shader.name=="Universal Render Pipeline/Lit"&&m.GetTexture("_BaseMap")!=null),"Missing imported maps or URP shader");
            Check(UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length==1,"Duplicate XR Origin");
            sequence.RestoreAftershock();sequence.BeginAftershockNow();sequence.Advance(7f);
            Check(motion.AppliedMaximumDrop>0 && motion.AppliedMaximumDrop<=motion.MaximumConfiguredDrop+.001f,"Ceiling motion missing/out of bounds");
            sequence.Advance(11.2f);Check(sequence.Stage==GameStage.Isolation,"Aftershock does not finish");
            var chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None);Check(chunks.All(c=>c.Released),"Wall fracture release missing");
            sequence.RestoreAftershock();Check(chunks.All(c=>!c.Released&&c.GetComponent<Rigidbody>().isKinematic),"Fracture reset failed");
            sequence.PreviewNextStage();Check(ending.IsRunning,"Rescue preview missing");ending.Advance(12f);
            Check(ending.OpeningAmount>0 && roofs.Where((t,i)=>t.position!=positions[i]).Any(),"Imported overhead surfaces did not move in rescue");
            ending.Advance(ending.DurationSeconds);Check(ending.IsComplete&&ending.WhiteoutAmount==0&&ending.SunlightAdmission>0,"Natural daylight rescue completion failed");
            sequence.RestoreAftershock();Check(roofs.Where((t,i)=>Vector3.Distance(t.position,positions[i])>.001f).Count()==0,"Roof reset changed");
            Check(head.position==headPosition&&head.rotation==headRotation,"Verification moved HMD");
            Debug.Log("[Imported rubble verification] PASS: 329 imported surfaces, URP textures, aftershock/fracture/reset, rescue removal/completion/reset, single XR Origin.");
            var result=new{result="PASS",surfaces=surfaces.Length,materials=shaders.Length,movingRescueRoofs=roofs.Length,fractureChunks=chunks.Length,frame=Time.frameCount};
            File.WriteAllText("Temp/ImportedRubbleReview/Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }finally{sequence.RestoreAftershock();sequence.enabled=active;}
    }
}
