using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class InspectImportedRubble
{
    public static object Run()
    {
        var scene = SceneManager.GetActiveScene();
        var env = GameObject.Find("Prototype Environment");
        Directory.CreateDirectory("Temp/ImportedRubbleReview");
        File.WriteAllLines("Temp/ImportedRubbleReview/Environment.txt", env.GetComponentsInChildren<MeshRenderer>(true).Select(r => {
            var f=r.GetComponent<MeshFilter>();
            return $"{PathOf(r.transform)}|enabled={r.enabled}|active={r.gameObject.activeSelf}|pos={r.transform.position:F3}|size={r.bounds.size:F3}|center={r.bounds.center:F3}|mesh={AssetDatabase.GetAssetPath(f?.sharedMesh)}";
        }));
        return new {
            scene=scene.path,dirty=scene.isDirty,
            pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,
            camera=new{position=Camera.main.transform.position.ToString(),rotation=Camera.main.transform.eulerAngles.ToString()},
            environment=env.transform.Cast<Transform>().Select(t=>new{t.name,t.childCount}).ToArray(),
            prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/RubbleAndDebris"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>{
                var go=AssetDatabase.LoadAssetAtPath<GameObject>(p); var rs=go.GetComponentsInChildren<MeshRenderer>(true); var b=new Bounds();bool first=true;
                foreach(var r in rs.Where(r=>!r.name.Contains("LOD1")&&!r.name.Contains("LOD2")&&!r.name.Contains("LOD3"))) {if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}
                return new{path=p,size=b.size.ToString(),center=b.center.ToString(),lods=go.GetComponentsInChildren<LODGroup>(true).Length,renderers=rs.Length,triangles=go.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3),mats=rs.SelectMany(r=>r.sharedMaterials).Distinct().Where(m=>m!=null).Select(m=>new{m.name,shader=m.shader.name,path=AssetDatabase.GetAssetPath(m)}).ToArray()};
            }).ToArray(),
            motion=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(m=>m!=null && (m.GetType().Name=="AftershockEnvironmentMotion" ||m.GetType().Name=="RescueEndingDirector")).Select(m=>{var s=new SerializedObject(m); var it=s.GetIterator();var list=new System.Collections.Generic.List<string>();while(it.NextVisible(true)) if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue is Transform t)list.Add(it.propertyPath+"="+PathOf(t));return new{type=m.GetType().Name,targets=list};}).ToArray()
        };
    }
    static string PathOf(Transform t){return t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;}
}
