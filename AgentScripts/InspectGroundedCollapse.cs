using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectGroundedCollapse
{
    static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=SceneManager.GetActiveScene();
        var backup="Assets/_Project/Scenes/Backups/BeforeGroundedCollapse_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
        Directory.CreateDirectory("Temp/GroundedCollapseReview");
        var env=GameObject.Find("Prototype Environment");
        File.WriteAllLines("Temp/GroundedCollapseReview/BeforeObjects.txt",env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Select(r=>$"{Path(r.transform)}|center={r.bounds.center:F3}|size={r.bounds.size:F3}|collider={(r.GetComponent<Collider>()!=null?r.GetComponent<Collider>().enabled:false)}"));
        var drop=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var data=new SerializedObject(drop);var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var ed=new SerializedObject(ending);var rubble=ed.FindProperty("rubble");var starts=ed.FindProperty("removalStarts");
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();var dd=new SerializedObject(debris);var fragments=dd.FindProperty("fragments");var ends=dd.FindProperty("landingPositions");
        var result=new{backup,scene=scene.path,pipe=new{pos=drop.transform.position.ToString(),poses=new[]{"stowedPose","lipPose","releasePose","landingPose"}.Select(n=>{var t=(Transform)data.FindProperty(n).objectReferenceValue;return new{n,path=Path(t),pos=t.position.ToString(),rot=t.eulerAngles.ToString()};}).ToArray()},
            debris=Enumerable.Range(0,fragments.arraySize).Select(i=>new{name=fragments.GetArrayElementAtIndex(i).objectReferenceValue.name,start=((Transform)fragments.GetArrayElementAtIndex(i).objectReferenceValue).position.ToString(),end=ends.GetArrayElementAtIndex(i).vector3Value.ToString()}).ToArray(),
            rescue=Enumerable.Range(0,rubble.arraySize).Select(i=>new{name=rubble.GetArrayElementAtIndex(i).objectReferenceValue.name,start=starts.GetArrayElementAtIndex(i).floatValue}).ToArray(),
            physicsChunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).Select(c=>new{c.name,path=Path(c.transform),pos=c.transform.position.ToString(),bounds=c.GetComponent<Renderer>().bounds.size.ToString(),colliderMesh=c.GetComponent<MeshCollider>().sharedMesh.name,visualMesh=c.GetComponent<MeshFilter>().sharedMesh.name}).ToArray()};
        File.WriteAllText("Temp/GroundedCollapseReview/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{backup,fragments=fragments.arraySize,rescueTargets=rubble.arraySize,activeRenderers=env.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled&&r.gameObject.activeInHierarchy)};
    }
}
