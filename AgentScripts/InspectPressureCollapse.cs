using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectPressureCollapse
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var scene=SceneManager.GetActiveScene();var backup="Assets/_Project/Scenes/Backups/BeforePressureCollapse_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
        var env=GameObject.Find("Prototype Environment");
        var chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Select(c=>new{path=Path(c.transform),body=c.GetComponent<Rigidbody>().isKinematic,scale=c.transform.lossyScale.ToString("F3"),bounds=c.GetComponent<Renderer>().bounds.ToString("F3"),allLod0=c.GetComponentInParent<LODGroup>().GetLODs()[0].renderers.Select(r=>new{r.name,bounds=r.bounds.ToString("F3"),hasBody=r.GetComponent<Rigidbody>()!=null}).ToArray()}).ToArray();
        var top=env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.max.y>1.25f&&Mathf.Abs(r.bounds.center.x)<1.4f&&Mathf.Abs(r.bounds.center.z)<1.7f).Select(r=>new{path=Path(r.transform),min=r.bounds.min.ToString("F3"),max=r.bounds.max.ToString("F3"),mesh=AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh),collider=r.GetComponent<Collider>()?.enabled}).ToArray();
        var motion=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>());
        var result=new{backup,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,autoTick="inspect editor state",chunks,ceilingTargets=motion.FindProperty("ceilingTargets").arraySize,top};Directory.CreateDirectory("Temp/PressureCollapseReview");File.WriteAllText("Temp/PressureCollapseReview/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{backup,chunkCount=chunks.Length,chunks,topRenderers=top.Length};
    }
    static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
}
