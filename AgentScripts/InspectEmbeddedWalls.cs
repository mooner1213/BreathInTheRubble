using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectEmbeddedWalls
{
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var scene=SceneManager.GetActiveScene();
        var backup="Assets/_Project/Scenes/Backups/BeforeEmbeddedWallFracture_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        EditorSceneManager.SaveScene(scene,backup,true);
        return new{backup, walls=new[]{"Rear Left Fractured Panel","Rear Diagonal Reinforced Slab","Rear Inner Fracture Right"}.Select(n=>{
            var go=GameObject.Find(n);var m=go.GetComponent<MeshFilter>().sharedMesh;
            return new{name=n,position=go.transform.position.ToString("F4"),rotation=go.transform.eulerAngles.ToString("F2"),scale=go.transform.lossyScale.ToString("F4"),bounds=m.bounds.ToString(),vertices=m.vertexCount,triangles=m.triangles.Length/3,mesh=AssetDatabase.GetAssetPath(m),materials=go.GetComponent<Renderer>().sharedMaterials.Select(x=>x.name).ToArray(),colliders=go.GetComponents<Collider>().Select(c=>new{type=c.GetType().Name,enabled=c.enabled,trigger=c.isTrigger}).ToArray()};}).ToArray(),
            terrain=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.bounds.center.z<-.25f&&c.bounds.center.y<.8f).Select(c=>new{name=c.name,type=c.GetType().Name,bounds=c.bounds.ToString(),body=c.attachedRigidbody!=null}).ToArray(),
            motion=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>()).FindProperty("roomTargets").arraySize};
    }
}
