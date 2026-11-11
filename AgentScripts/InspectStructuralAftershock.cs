using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectStructuralAftershock
{
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=SceneManager.GetActiveScene();
        var path="Assets/_Project/Scenes/Backups/BeforeStructuralAftershock_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";if(!EditorSceneManager.SaveScene(scene,path,true))throw new Exception("Backup failed");
        var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();var so=new SerializedObject(motion);var root=GameObject.Find("Interlocked Rubble Structure");
        var objects=root.transform.Cast<Transform>().Where(t=>t.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.enabled)).Select(t=>{var rs=t.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return new{name=t.name,position=t.position.ToString("F3"),min=b.min.ToString("F3"),max=b.max.ToString("F3"),size=b.size.ToString("F3")};}).ToArray();
        Directory.CreateDirectory("Temp/StructuralAftershockReview");File.WriteAllText("Temp/StructuralAftershockReview/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(objects,Newtonsoft.Json.Formatting.Indented));
        return new{backup=path,head=Camera.main.transform.position.ToString(),ceilingTargets=so.FindProperty("ceilingTargets").arraySize,ceilingDrop=so.FindProperty("ceilingDrop").floatValue,roomTargets=so.FindProperty("roomTargets").arraySize,objects};
    }
}
