using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BreathInTheRubble.Prototype;
public static class SealBurialBacking
{
    static void Append(SerializedObject s,string name,IEnumerable<UnityEngine.Object> values){var p=s.FindProperty(name);int i=p.arraySize;var list=values.ToArray();p.arraySize+=list.Length;foreach(var value in list)p.GetArrayElementAtIndex(i++).objectReferenceValue=value;}
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");var group=GameObject.Find("Dense permanent burial backing");if(group.transform.Find("Outer closed bearing 0")!=null)throw new Exception("Already sealed");
        var mesh=new Mesh{name="Closed irregular deep concrete mass"};var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        var corners=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
        corners[0]+=new Vector3(.013f,0,.009f);corners[2]+=new Vector3(-.009f,-.012f,0);corners[5]+=new Vector3(-.012f,.006f,-.009f);corners[7]+=new Vector3(.01f,-.012f,-.012f);
        int[][] faces={new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,4,7,3},new[]{1,2,6,5},new[]{3,7,6,2},new[]{0,1,5,4}};
        foreach(var face in faces){int start=vertices.Count;var normal=Vector3.Cross(corners[face[1]]-corners[face[0]],corners[face[2]]-corners[face[0]]).normalized;foreach(int c in face){vertices.Add(corners[c]);normals.Add(normal);var p=corners[c];uv.Add(Mathf.Abs(normal.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(normal.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y));}triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});}
        mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,"Assets/_Project/Art/BurialBacking/ClosedDeepMass.asset");
        var material=group.GetComponentInChildren<MeshRenderer>().sharedMaterial;var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var added=new List<Transform>();var roof=new List<Transform>();
        GameObject Create(string name,Vector3 p,Vector3 size,Quaternion r)
        {
            var go=new GameObject(name);go.transform.SetParent(group.transform,false);go.transform.SetPositionAndRotation(p,r);go.transform.localScale=size;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;var c=go.AddComponent<MeshCollider>();c.sharedMesh=mesh;c.contactOffset=.001f;var b=go.AddComponent<Rigidbody>();b.isKinematic=true;b.useGravity=false;b.interpolation=RigidbodyInterpolation.Interpolate;added.Add(go.transform);return go;
        }
        for(int i=0;i<12;i++){float angle=15+i*30;var direction=Quaternion.Euler(0,angle,0)*Vector3.forward;Create("Outer closed bearing "+i,direction*1.62f+Vector3.up*1.0f,new Vector3(1.12f,2.65f,.75f),Quaternion.Euler(0,angle,0));}
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++){var go=Create("Deep roof mass "+x+" "+z,new Vector3(x*1.18f,2.48f,.28f+z*1.18f),new Vector3(1.32f,.36f,1.32f),Quaternion.identity);if(x==0)roof.Add(go.transform);}
        var s=new SerializedObject(manager);var array=s.FindProperty("elements");int entryStart=array.arraySize;array.arraySize+=added.Count;
        for(int i=0;i<added.Count;i++){var e=array.GetArrayElementAtIndex(entryStart+i);e.FindPropertyRelative("target").objectReferenceValue=added[i];e.FindPropertyRelative("loose").objectReferenceValue=null;e.FindPropertyRelative("phase").floatValue=7.3f+i*5.17f;e.FindPropertyRelative("distance").floatValue=.001f;e.FindPropertyRelative("degrees").floatValue=.08f;e.FindPropertyRelative("failureResistance").floatValue=0;e.FindPropertyRelative("releaseAt").floatValue=-1;e.FindPropertyRelative("compression").vector3Value=Vector3.zero;e.FindPropertyRelative("openingOffset").vector3Value=Vector3.zero;e.FindPropertyRelative("cracks").arraySize=0;e.FindPropertyRelative("powder").objectReferenceValue=null;}s.ApplyModifiedPropertiesWithoutUndo();
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();s=new SerializedObject(ending);var rubble=s.FindProperty("rubble");int n=rubble.arraySize;Append(s,"rubble",roof);var offsets=s.FindProperty("removalOffsets");var times=s.FindProperty("removalStarts");offsets.arraySize=times.arraySize=rubble.arraySize;
        for(int i=0;i<roof.Count;i++){offsets.GetArrayElementAtIndex(n+i).vector3Value=new Vector3(i%2==0?-3.5f:3.5f,1.8f,0);times.GetArrayElementAtIndex(n+i).floatValue=8+i*7;}
        Append(s,"lightBlockers",added.Select(t=>t.GetComponent<Renderer>()));Append(s,"apertureBlockers",roof.Select(t=>t.GetComponent<Renderer>()));s.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(group.scene);EditorSceneManager.SaveScene(group.scene);return new{additionalClosedMasses=added.Count,rescueCaps=roof.Count};
    }
}
