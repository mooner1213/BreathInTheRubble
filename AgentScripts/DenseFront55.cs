using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class DenseFront55
{
    const string Art="Assets/_Project/Art/DenseFront55";
    static int id;
    public static object Tune()
    {
        if(Application.isPlaying||AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/DenseRubble_10.asset")!=null)throw new Exception("Edit mode and unapplied tune required");id=10;
        foreach(var name in new[]{"Left buried seam backing","Center buried seam backing","Right buried seam backing"})GameObject.Find(name).transform.position+=Vector3.forward*.23f;
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var pd=new SerializedObject(pipe);((Transform)pd.FindProperty("stowedPose").objectReferenceValue).SetPositionAndRotation(new Vector3(0,2.45f,.88f),Quaternion.Euler(0,12,90));((Transform)pd.FindProperty("lipPose").objectReferenceValue).SetPositionAndRotation(new Vector3(0,1.055f,.83f),Quaternion.Euler(0,12,90));((Transform)pd.FindProperty("releasePose").objectReferenceValue).SetPositionAndRotation(new Vector3(-.025f,1.045f,.34f),Quaternion.Euler(0,12,90));pipe.transform.SetPositionAndRotation(new Vector3(0,2.45f,.88f),Quaternion.Euler(0,12,90));
        var chunks=GameObject.Find("Concealed close seam chips").GetComponentsInChildren<AftershockFallingChunk>();float[] xs={-.30f,-.17f,-.04f,.09f,.23f,.34f},ys={1.045f,1.06f,1.055f,1.06f,1.045f,1.085f},zs={.59f,.62f,.59f,.63f,.60f,.65f};Vector3[] sizes={new Vector3(.21f,.07f,.13f),new Vector3(.16f,.055f,.18f),new Vector3(.21f,.07f,.12f),new Vector3(.14f,.07f,.20f),new Vector3(.18f,.055f,.14f),new Vector3(.21f,.06f,.16f)},angles={new Vector3(73,-15,23),new Vector3(65,18,-26),new Vector3(89,7,5),new Vector3(72,-28,31),new Vector3(97,12,-13),new Vector3(53,-35,19)};
        foreach(var c in chunks)
        {
            int index=Array.IndexOf(chunks,c),wave=index/9;var so=new SerializedObject(c);var ignores=so.FindProperty("ignoreColliders");var kept=Enumerable.Range(0,ignores.arraySize).Select(i=>ignores.GetArrayElementAtIndex(i).objectReferenceValue).Where(v=>v!=null&&(!(v is Component cc)||cc.gameObject!=pipe.gameObject)).ToArray();ignores.arraySize=kept.Length;for(int k=0;k<kept.Length;k++)ignores.GetArrayElementAtIndex(k).objectReferenceValue=kept[k];so.ApplyModifiedProperties();
            if(index%9!=0){c.transform.position+=new Vector3(xs[wave]-new[]{-.30f,-.18f,-.06f,.06f,.18f,.30f}[wave],ys[wave]-1.055f,zs[wave]-.59f);continue;}
            c.transform.position=new Vector3(xs[wave],ys[wave],zs[wave]);var mesh=CopyMesh(wave==3?"Bricks_Medium_01":wave%2==0?"Wall_Gray_Medium_02":"Wall_DarkGrey_Medium_01","Irregular wedged plate "+wave,sizes[wave],angles[wave],out var mats);c.GetComponent<MeshFilter>().sharedMesh=mesh;c.GetComponent<MeshRenderer>().sharedMaterials=mats;c.GetComponent<MeshCollider>().sharedMesh=mesh;var box=c.GetComponent<BoxCollider>();box.center=mesh.bounds.center;box.size=mesh.bounds.size;
        }
        Save();return "Irregular breakaway faces; original horizontal pipe route restored behind packed backing";
    }
    static void Save(){EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
    static Mesh CopyMesh(string source,string name,Vector3 size,Vector3 angles,out Material[] materials)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Props/"+source+".prefab");var r=prefab.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var raw=r.GetComponent<MeshFilter>().sharedMesh;var mesh=UnityEngine.Object.Instantiate(raw);var b=mesh.bounds;var m=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(angles),new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z))*Matrix4x4.Translate(-b.center);mesh.vertices=mesh.vertices.Select(v=>m.MultiplyPoint3x4(v)).ToArray();mesh.normals=mesh.normals.Select(n=>m.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();var shift=mesh.bounds.center;mesh.vertices=mesh.vertices.Select(v=>v-shift).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Art+"/DenseRubble_"+(id++)+".asset");materials=r.sharedMaterials.Select(mat=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+mat.name+"_URP.mat")).ToArray();return mesh;
    }
    static GameObject Solid(Transform parent,string source,string name,Vector3 position,Vector3 size,Vector3 angles)
    {
        var mesh=CopyMesh(source,name,size,angles,out var materials);var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=materials;r.shadowCastingMode=ShadowCastingMode.TwoSided;var c=go.AddComponent<MeshCollider>();c.sharedMesh=mesh;c.contactOffset=.001f;go.AddComponent<PipeImpactSurface>();return go;
    }
    public static object Build()
    {
        if(Application.isPlaying||AssetDatabase.IsValidFolder(Art))throw new Exception("Edit mode and unapplied scene required");Directory.CreateDirectory("Temp/DenseFront55Review");EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Temp/DenseFront55Review/Before.unity",true);AssetDatabase.CreateFolder("Assets/_Project/Art","DenseFront55");
        var center=GameObject.Find("Center crushed bearing stone").transform;Physics.SyncTransforms();var ray=new Ray(Camera.main.transform.position,Vector3.forward);var shape=center.GetComponent<MeshCollider>();if(!shape.Raycast(ray,out var hit,2))throw new Exception("Center ray missing");center.position+=Vector3.forward*(.55f-hit.distance);
        GameObject.Find("Front face left pressed rubble").transform.position+=Vector3.forward*.08f;GameObject.Find("Front face right interlocked masonry").transform.position+=Vector3.forward*.08f;
        var root=new GameObject("Dense backed front seams");root.transform.SetParent(GameObject.Find("Close buried cell").transform,false);
        Solid(root.transform,"Wall_DarkGrey_Medium_01","Left buried seam backing",new Vector3(-.38f,1.045f,.86f),new Vector3(.42f,.16f,.51f),new Vector3(88,0,-4));
        Solid(root.transform,"Wall_Gray_Medium_03","Center buried seam backing",new Vector3(-.035f,1.09f,.90f),new Vector3(.32f,.15f,.48f),new Vector3(88,0,2));
        Solid(root.transform,"Bricks_Big_02","Right buried seam backing",new Vector3(.475f,1.065f,.88f),new Vector3(.46f,.17f,.53f),new Vector3(87,0,4));
        Solid(root.transform,"Wall_Grey_Small_02","Deep utility slit packed end",new Vector3(.19f,1.055f,1.09f),new Vector3(.24f,.15f,.64f),new Vector3(88,0,-3));
        var chunks=GameObject.Find("Concealed close seam chips").GetComponentsInChildren<AftershockFallingChunk>();var pocket=GameObject.Find("Concealed close seam chips").GetComponent<RubbleFailurePocket>();var data=new SerializedObject(pocket);var times=data.FindProperty("chipReleaseTimes");var random=new System.Random(55421);Func<float,float,float> R=(a,b)=>a+(float)random.NextDouble()*(b-a);float[] xs={-.30f,-.18f,-.06f,.06f,.18f,.30f},waves={2.84f,4.13f,6.72f,9.11f,12.30f,15.02f};
        for(int i=0;i<chunks.Length;i++)
        {
            int wave=i/9;bool plate=i%9==0;var c=chunks[i];c.transform.position=new Vector3(xs[wave]+(plate?0:R(-.035f,.035f)),plate?1.055f:R(1.035f,1.075f),plate?.59f:R(.630f,.65f));
            if(plate){var mesh=CopyMesh(wave%2==0?"Wall_Grey_Small_02":"Bricks_Medium_01","Wedged front breakaway plate "+wave,new Vector3(.15f,.055f,.14f),new Vector3(79,R(-5,5),R(-4,4)),out var materials);c.GetComponent<MeshFilter>().sharedMesh=mesh;c.GetComponent<MeshRenderer>().sharedMaterials=materials;c.GetComponent<MeshCollider>().sharedMesh=mesh;var box=c.GetComponent<BoxCollider>();box.center=mesh.bounds.center;box.size=mesh.bounds.size;}
            var so=new SerializedObject(c);so.FindProperty("releaseVelocity").vector3Value=new Vector3(-xs[wave]*R(.1f,.3f),R(-.03f,.02f),plate?R(-.95f,-.72f):R(-1.25f,-.80f));so.FindProperty("headClearanceRadius").floatValue=0;so.FindProperty("handClearanceRadius").floatValue=0;so.FindProperty("colliderWhileAttached").boolValue=plate;so.ApplyModifiedProperties();c.GetComponent<BoxCollider>().enabled=plate;times.GetArrayElementAtIndex(i).floatValue=waves[wave]+(plate?0:R(.18f,.88f));
        }
        data.ApplyModifiedProperties();
        foreach(var c in GameObject.Find("Deep Layered Collapse").GetComponentsInChildren<AftershockFallingChunk>())
        {
            var inward=Vector3.ProjectOnPlane(Camera.main.transform.position-c.transform.position,Vector3.up).normalized;var so=new SerializedObject(c);so.FindProperty("releaseVelocity").vector3Value=inward*R(.55f,.95f)+Vector3.down*.10f;so.FindProperty("headClearanceRadius").floatValue=.08f;so.FindProperty("handClearanceRadius").floatValue=.05f;so.ApplyModifiedProperties();
        }
        // Thread the physical pipe vertically through the narrow, backed utility seam.
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();data=new SerializedObject(pipe);var stow=(Transform)data.FindProperty("stowedPose").objectReferenceValue;var lip=(Transform)data.FindProperty("lipPose").objectReferenceValue;var release=(Transform)data.FindProperty("releasePose").objectReferenceValue;stow.SetPositionAndRotation(new Vector3(.185f,2.45f,.875f),Quaternion.Euler(0,12,0));lip.SetPositionAndRotation(new Vector3(.185f,.765f,.875f),Quaternion.Euler(0,12,0));release.SetPositionAndRotation(new Vector3(-.025f,.765f,.34f),Quaternion.Euler(0,12,0));pipe.transform.SetPositionAndRotation(stow.position,stow.rotation);
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();data=new SerializedObject(ending);var p=data.FindProperty("lightBlockers");var rs=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();p.arraySize=rs.Length;for(int i=0;i<rs.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=rs[i];data.ApplyModifiedProperties();Physics.SyncTransforms();Save();return new{frontDistance=.55f,backingMasses=4,forwardFalls=chunks.Length,deepFallsAimedInward=225};
    }
}
