using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
public static class NaturalizeShaftEdges
{
    static float Radius(Vector3 p,float height){float x=p.x/.39f,z=(p.z-.27f)/.46f;float a=Mathf.Atan2(z,x);return (x*x+z*z)/(1+.10f*Mathf.Sin(a*3+height*2.1f));}
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");int count=0;
        foreach(var f in GameObject.Find("Sealed Rubble Burial").GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&AssetDatabase.GetAssetPath(f.sharedMesh).Contains("/InterlockedRubble/RoughShaftRim_"))){
            var source=PrefabUtility.GetCorrespondingObjectFromSource(f);if(source==null)throw new Exception("Original paid fragment missing");var mesh=UnityEngine.Object.Instantiate(source.sharedMesh);mesh.name="Native fractured aperture "+(++count);var vertices=mesh.vertices;float height=f.GetComponent<Renderer>().bounds.center.y;
            for(int sub=0;sub<mesh.subMeshCount;sub++){var triangles=source.sharedMesh.GetTriangles(sub);var kept=new System.Collections.Generic.List<int>();for(int i=0;i<triangles.Length;i+=3){var a=f.transform.TransformPoint(vertices[triangles[i]]);var b=f.transform.TransformPoint(vertices[triangles[i+1]]);var c=f.transform.TransformPoint(vertices[triangles[i+2]]);
                    bool remove=Radius((a+b+c)/3,height)<1 || new[]{a,b,c}.Count(p=>Radius(p,height)<1)>=2;
                    if(!remove)kept.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}mesh.SetTriangles(kept,sub);}
            mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,"Assets/_Project/Art/InterlockedRubble/NativeAperture_"+count.ToString("000")+".asset");f.sharedMesh=mesh;
        }
        if(count==0)throw new Exception("No aperture rim meshes selected");var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return new{nativeBrokenEdges=count,syntheticCutVertices=0};
    }
}
