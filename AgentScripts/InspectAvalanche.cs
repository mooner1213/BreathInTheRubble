using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectAvalanche
{
    public static object RuntimeChunks()=>UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).Select((c,i)=>new{i,c.name,parent=c.transform.parent.name,center=c.GetComponent<Renderer>().bounds.center.ToString("F3")}).ToArray();
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        Directory.CreateDirectory("Temp/AvalancheReview");var scene=SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene,"Temp/AvalancheReview/Before_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var data=new SerializedObject(pipe);
        var result=new{scene=scene.path,pipe=pipe.transform.position.ToString("F3"),scale=pipe.transform.lossyScale.ToString("F3"),bounds=pipe.GetComponent<Renderer>().bounds.ToString("F3"),colliders=pipe.GetComponents<Collider>().Select(c=>new{type=c.GetType().Name,json=EditorJsonUtility.ToJson(c)}).ToArray(),markers=new[]{"stowedPose","lipPose","releasePose","landingPose"}.Select(n=>{var t=(Transform)data.FindProperty(n).objectReferenceValue;return new{field=n,t.name,position=t.position.ToString("F3"),angles=t.eulerAngles.ToString("F3")};}).ToArray(),pipeData=EditorJsonUtility.ToJson(pipe),roof=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.max.y>1.05f&&r.bounds.min.y<1.8f&&r.bounds.min.x<.4f&&r.bounds.max.x>-.6f&&r.bounds.min.z<.8f&&r.bounds.max.z>-.1f).Select(r=>new{path=PathOf(r.transform),min=r.bounds.min.ToString("F3"),max=r.bounds.max.ToString("F3")}).ToArray(),rain=UnityEngine.Object.FindObjectsByType<AftershockRubbleRain>(FindObjectsSortMode.None).Select(r=>EditorJsonUtility.ToJson(r)).ToArray(),particles=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(p=>p.name.Contains("Gravity")).Select(p=>new{p.name,position=p.transform.position.ToString("F3"),max=p.main.maxParticles,size=p.main.startSize.constantMax,lifetime=p.main.startLifetime.constantMax}).ToArray()};
        File.WriteAllText("Temp/AvalancheReview/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{result.scene,result.pipe,result.bounds,result.colliders,result.markers,result.rain,result.particles,roof=result.roof.Length};
    }
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
}

