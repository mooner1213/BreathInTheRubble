using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class RefineBurialBacking
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");var group=GameObject.Find("Dense permanent burial backing");
        var prefabPath=AssetDatabase.FindAssets("Wall_Grey_Big_01 t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(p=>Path.GetFileNameWithoutExtension(p)=="Wall_Grey_Big_01");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<LODGroup>().GetLODs()[0].renderers[0].GetComponent<MeshFilter>().sharedMesh;
        var native=source.bounds;var block=group.GetComponentInChildren<MeshFilter>().sharedMesh;
        bool thinY=native.size.y<native.size.z&&native.size.y<native.size.x;
        var vertices=source.vertices.Select(v=>{var q=new Vector3((v.x-native.center.x)/native.size.x,(v.y-native.center.y)/native.size.y,(v.z-native.center.z)/native.size.z);return thinY?new Vector3(q.x,q.z,-q.y):q;}).ToArray();
        block.vertices=vertices;block.triangles=source.triangles;block.uv=source.uv;block.RecalculateBounds();block.RecalculateNormals();block.RecalculateTangents();EditorUtility.SetDirty(block);
        foreach(var c in group.GetComponentsInChildren<MeshCollider>()){c.sharedMesh=null;c.sharedMesh=block;EditorUtility.SetDirty(c);}
        var stones=GameObject.Find("Independent unsupported stone islands").GetComponentsInChildren<MeshRenderer>();int details=0;
        foreach(Transform bank in group.transform)
        {
            if(!bank.name.StartsWith("Interlocked buried bank")||bank.Find("Embedded bank detail 0")!=null)continue;
            int index=details;
            for(int i=0;i<3;i++)
            {
                var reference=stones[(index*7+i*13)%stones.Length];var mesh=reference.GetComponent<MeshFilter>().sharedMesh;var host=new GameObject("Embedded bank detail "+i);
                host.transform.SetPositionAndRotation(bank.TransformPoint(new Vector3((i-1)*.26f,(i%2==0?-.18f:.2f),-.49f)),bank.rotation*Quaternion.Euler(i*21,10-i*19,i*34));host.transform.SetParent(bank,true);
                host.AddComponent<MeshFilter>().sharedMesh=mesh;var r=host.AddComponent<MeshRenderer>();r.sharedMaterials=reference.sharedMaterials;r.shadowCastingMode=ShadowCastingMode.TwoSided;details++;
            }
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(group.scene);EditorSceneManager.SaveScene(group.scene);return new{nativeBounds=native.size.ToString("F3"),thinY,embeddedDetails=details};
    }
}
