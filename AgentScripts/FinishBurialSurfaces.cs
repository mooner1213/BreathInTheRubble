using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class FinishBurialSurfaces
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");var group=GameObject.Find("Dense permanent burial backing");
        var stones=GameObject.Find("Independent unsupported stone islands").GetComponentsInChildren<MeshRenderer>().Where(r=>{var b=r.GetComponent<MeshFilter>().sharedMesh.bounds;return Mathf.Min(b.size.x,b.size.y,b.size.z)>.35f*Mathf.Max(b.size.x,b.size.y,b.size.z)&&b.size.magnitude>.14f;}).ToArray();
        int index=0,roofs=0;foreach(Transform t in group.transform)
        {
            if(t.name.StartsWith("Massive upper backing")){t.localScale=new Vector3(1.45f,1.45f,.56f);t.rotation=Quaternion.Euler(90,t.eulerAngles.y,0);roofs++;}
            foreach(var r in t.GetComponentsInChildren<MeshRenderer>().Where(r=>r.transform!=t))
            {
                var source=stones[index++%stones.Length];r.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;r.sharedMaterials=source.sharedMaterials;EditorUtility.SetDirty(r);
            }
        }
        EditorSceneManager.MarkSceneDirty(group.scene);EditorSceneManager.SaveScene(group.scene);return new{compactStoneVariants=stones.Length,details=index,roofs};
    }
}
