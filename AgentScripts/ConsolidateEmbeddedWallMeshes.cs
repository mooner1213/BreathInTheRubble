using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class ConsolidateEmbeddedWallMeshes
{
    static object Combine(Transform parent,MeshFilter[] filters,string name,int materialCount,bool collision)
    {
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=Enumerable.Range(0,materialCount).Select(_=>new List<int>()).ToArray();
        foreach(var filter in filters)
        {
            var mesh=filter.sharedMesh;int offset=vertices.Count;var matrix=parent.worldToLocalMatrix*filter.transform.localToWorldMatrix;var normal=matrix.inverse.transpose;
            vertices.AddRange(mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)));normals.AddRange(mesh.normals.Select(n=>normal.MultiplyVector(n).normalized));uv.AddRange(mesh.uv);
            for(int sub=0;sub<mesh.subMeshCount;sub++)indices[sub%materialCount].AddRange(mesh.GetTriangles(sub).Select(i=>i+offset));
        }
        var combined=new Mesh{name=name};combined.SetVertices(vertices);combined.SetNormals(normals);combined.SetUVs(0,uv);combined.subMeshCount=materialCount;for(int sub=0;sub<materialCount;sub++)combined.SetTriangles(indices[sub],sub);combined.RecalculateBounds();
        var path="Assets/_Project/Art/Aftershock/EmbeddedWallFractures/"+name+".asset";if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("Refusing to overwrite "+path);AssetDatabase.CreateAsset(combined,path);
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=combined;go.AddComponent<MeshRenderer>().sharedMaterials=filters[0].GetComponent<Renderer>().sharedMaterials;
        if(collision)go.AddComponent<MeshCollider>().sharedMesh=combined;
        foreach(var filter in filters)filter.gameObject.SetActive(false);
        return new{name,sourceRenderers=filters.Length,vertices=vertices.Count,triangles=indices.Sum(t=>t.Count)/3};
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var sections=UnityEngine.Object.FindObjectsByType<AftershockFractureSection>(FindObjectsSortMode.None).OrderBy(s=>s.OpensAtSeconds).ToArray();var reports=new List<object>();
        for(int i=0;i<sections.Length;i++)
        {
            var section=sections[i];var cores=section.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Original Wall Core and Edge")).ToArray();
            if(cores.Length==0)throw new Exception("Already consolidated");reports.Add(Combine(section.transform,cores,"Wall"+(i+1)+"_CombinedCore",2,true));
            var rebar=(Transform)new SerializedObject(section).FindProperty("exposedRebar").objectReferenceValue;
            reports.Add(Combine(rebar,rebar.GetComponentsInChildren<MeshFilter>(),"Wall"+(i+1)+"_EmbeddedSteel",1,false));
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return reports;
    }
}
