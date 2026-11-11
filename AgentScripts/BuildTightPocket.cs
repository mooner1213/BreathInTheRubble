using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;

public static class BuildTightPocket
{
    const string Art="Assets/_Project/Art/TightPocket";
    static void Surface(GameObject go,PipeImpactSurface.SurfaceKind kind){var marker=go.AddComponent<PipeImpactSurface>();var data=new SerializedObject(marker);data.FindProperty("kind").enumValueIndex=(int)kind;data.ApplyModifiedProperties();}
    static GameObject Concrete(Transform parent,string source,string name,Vector3 center,Vector3 size,Vector3 angles,int id)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Props/"+source+".prefab");var original=prefab.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var raw=original.GetComponent<MeshFilter>().sharedMesh;var mesh=UnityEngine.Object.Instantiate(raw);var b=mesh.bounds;
        var matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(angles),new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z))*Matrix4x4.Translate(-b.center);mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();mesh.normals=mesh.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();var offset=mesh.bounds.center;mesh.vertices=mesh.vertices.Select(v=>v-offset).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name=name;AssetDatabase.CreateAsset(mesh,Art+"/PocketConcrete_"+id+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=center;go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=original.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;var shape=go.AddComponent<MeshCollider>();shape.sharedMesh=mesh;shape.contactOffset=.001f;Surface(go,PipeImpactSurface.SurfaceKind.Concrete);return go;
    }
    static GameObject Rebar(Transform parent,string name,Vector3[] path,float radius,int id)
    {
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var samples=new List<Vector3>();
        for(int segment=0;segment<path.Length-1;segment++)
        {
            var a=path[Mathf.Max(0,segment-1)];var b=path[segment];var c=path[segment+1];var d=path[Mathf.Min(path.Length-1,segment+2)];int count=Mathf.Max(2,Mathf.CeilToInt(Vector3.Distance(b,c)/.006f));
            for(int j=0;j<count;j++){float t=j/(float)count;var p=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);samples.Add(p);}
        }
        samples.Add(path[path.Length-1]);const int sides=10;float distance=0;
        for(int i=0;i<samples.Count;i++)
        {
            if(i>0)distance+=Vector3.Distance(samples[i-1],samples[i]);var tangent=(samples[Mathf.Min(samples.Count-1,i+1)]-samples[Mathf.Max(0,i-1)]).normalized;var side=Vector3.Cross(tangent,Vector3.up).normalized;if(side.sqrMagnitude<.1f)side=Vector3.right;var up=Vector3.Cross(side,tangent).normalized;
            float rib=.0014f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(distance/.021f*Mathf.PI*2)),6);
            for(int k=0;k<=sides;k++){float angle=k/(float)sides*Mathf.PI*2;var n=side*Mathf.Cos(angle)+up*Mathf.Sin(angle);vertices.Add(samples[i]+n*(radius+rib));normals.Add(n);uv.Add(new Vector2(k/(float)sides,distance*3));}
            if(i>0)for(int k=0;k<sides;k++){int a=(i-1)*(sides+1)+k,b=a+sides+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        }
        int start=vertices.Count;vertices.Add(samples[0]);normals.Add((samples[0]-samples[1]).normalized);uv.Add(Vector2.zero);int end=vertices.Count;vertices.Add(samples[samples.Count-1]);normals.Add((samples[samples.Count-1]-samples[samples.Count-2]).normalized);uv.Add(Vector2.one);
        for(int k=0;k<sides;k++){triangles.AddRange(new[]{start,k+1,k});int a=(samples.Count-1)*(sides+1)+k;triangles.AddRange(new[]{end,a,a+1});}
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Art+"/EmbeddedRebar_"+id+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MetalPipe/WornSteelPipe.mat");Surface(go,PipeImpactSurface.SurfaceKind.Metal);
        for(int i=0;i<path.Length-1;i++){var a=path[i];var b=path[i+1];var host=new GameObject("Metal contact segment "+i);host.transform.SetParent(go.transform,false);host.transform.localPosition=(a+b)*.5f;host.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);var shape=host.AddComponent<CapsuleCollider>();shape.direction=1;shape.radius=radius+.001f;shape.height=Vector3.Distance(a,b)+shape.radius*2;shape.contactOffset=.001f;}
        return go;
    }
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");if(GameObject.Find("Tight Initial Pocket")!=null)throw new Exception("Already applied");if(AssetDatabase.IsValidFolder(Art))throw new Exception("Asset folder exists");AssetDatabase.CreateFolder("Assets/_Project/Art","TightPocket");
        var parent=GameObject.Find("Interlocked Rubble Structure").transform;var root=new GameObject("Tight Initial Pocket");root.transform.SetParent(parent,false);
        // Keep the upper right view open to the existing deep fracture mouths.
        Concrete(root.transform,"Wall_Gray_Medium_02","Near left wedged slab",new Vector3(-.47f,.78f,.66f),new Vector3(.45f,.15f,.75f),new Vector3(0,10,72),0);
        Concrete(root.transform,"Wall_Gray_Medium_03","Near right wedged slab",new Vector3(.49f,.79f,.67f),new Vector3(.45f,.15f,.73f),new Vector3(0,-12,-72),1);
        Concrete(root.transform,"Bricks_Big_02","Slanted masonry across trapped legs",new Vector3(-.19f,.63f,.85f),new Vector3(.76f,.20f,.66f),new Vector3(-34,14,-14),2);
        GameObject.Find("Front upper compressed masonry").transform.position+=new Vector3(0,0,-.17f);
        // Both metal roots are embedded in the new concrete; only the steel carries a Metal marker.
        Rebar(root.transform,"Reachable ribbed rebar striker",new[]{new Vector3(.58f,.86f,.78f),new Vector3(.51f,.84f,.65f),new Vector3(.43f,.81f,.54f),new Vector3(.07f,.81f,.54f),new Vector3(.045f,.79f,.53f)},.0085f,0);
        Rebar(root.transform,"Bent left wall reinforcement",new[]{new Vector3(-.55f,.78f,.77f),new Vector3(-.47f,.88f,.59f),new Vector3(-.34f,.93f,.50f),new Vector3(-.31f,1.0f,.50f)},.0075f,1);
        Rebar(root.transform,"Lower broken wall tie",new[]{new Vector3(-.57f,.64f,.72f),new Vector3(-.49f,.69f,.61f),new Vector3(-.37f,.73f,.52f)},.0065f,2);
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var data=new SerializedObject(ending);var p=data.FindProperty("lightBlockers");var rs=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();p.arraySize=rs.Length;for(int i=0;i<rs.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=rs[i];data.ApplyModifiedProperties();
        Undo.RegisterCreatedObjectUndo(root,"Tighten trapped pocket and expose rebar");EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        return new{nearConcrete=3,embeddedRebar=3,frontMassCloser=.17f,metalRoute="existing PipeImpactAudio and PipeRescueSignalRelay",camera=Camera.main.transform.position.ToString("F3")};
    }
}
