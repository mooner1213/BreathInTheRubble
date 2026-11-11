using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class InspectAftershockRoom
{
    public static object ReadStructure()
    {
        return GameObject.Find("Prototype Environment").GetComponentsInChildren<Transform>(true)
            .Where(t=>t.name.Contains("Shell") || t.name.Contains("Face") || t.name.Contains("Seal") || t.name.Contains("Wall") || t.name.Contains("Rear") || t.name.Contains("Surround") || t.name.Contains("Crevice"))
            .Select(t=>new{t.name,parent=t.parent?.name,children=t.childCount,renderers=t.GetComponentsInChildren<MeshRenderer>(true).Length}).ToArray();
    }
    public static object Run()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        string path="Assets/_Project/Scenes/Backups/BeforeSettlingDust_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,path,true))throw new Exception("Backup failed.");
        var environment=GameObject.Find("Prototype Environment");
        return new{backup=path,dirty=scene.isDirty,children=environment.transform.Cast<Transform>().Select(t=>new{t.name,children=t.childCount,position=t.position.ToString(),renderers=t.GetComponentsInChildren<MeshRenderer>(true).Length,colliders=t.GetComponentsInChildren<Collider>(true).Length}).ToArray()};
    }
}
