using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;

public static class BuildInterlockedRubble
{
    const string Art="Assets/_Project/Art/InterlockedRubble";
    static int clipped;
    static List<GameObject> pieces=new List<GameObject>();
    static GameObject Add(Transform parent,string asset,string name,Vector3 center,Vector3 angles,float scale)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/"+asset+".prefab");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);go.name=name;go.transform.rotation=Quaternion.Euler(angles);go.transform.localScale=Vector3.one*scale;
        foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
        var lod=go.GetComponent<LODGroup>();var ls=lod.GetLODs();lod.enabled=false;
        for(int i=0;i<ls.Length;i++)foreach(var r in ls[i].renderers){r.enabled=i==0;r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;}
        var bounds=ls[0].renderers[0].bounds;foreach(var r in ls[0].renderers.Skip(1))bounds.Encapsulate(r.bounds);go.transform.position+=center-bounds.center;
        foreach(var r in ls[0].renderers){var filter=r.GetComponent<MeshFilter>();var collider=r.GetComponent<MeshCollider>();if(collider==null)collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;collider.contactOffset=.001f;collider.enabled=true;
            var marker=r.GetComponent<PipeImpactSurface>();if(marker==null)marker=r.gameObject.AddComponent<PipeImpactSurface>();var so=new SerializedObject(marker);so.FindProperty("kind").enumValueIndex=asset.Contains("Rebar")?(int)PipeImpactSurface.SurfaceKind.Metal:(int)PipeImpactSurface.SurfaceKind.Concrete;so.ApplyModifiedProperties();}
        Undo.RegisterCreatedObjectUndo(go,"Arrange interlocked rubble");pieces.Add(go);return go;
    }
    static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Seat(GameObject go)
    {
        var own=go.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled).ToArray();foreach(var c in own)c.enabled=false;
        var verts=go.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>().enabled).SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).OrderBy(v=>v.y).ToArray();
        float correction=float.NegativeInfinity;
        foreach(var p in verts.Take(Mathf.Max(4,verts.Length/8))){var hits=Physics.RaycastAll(new Vector3(p.x,p.y+.25f,p.z),Vector3.down,1.2f).Where(h=>h.collider.enabled&&h.point.y<=p.y+.025f).OrderByDescending(h=>h.point.y).ToArray();if(hits.Length>0)correction=Mathf.Max(correction,hits[0].point.y-p.y);}
        if(!float.IsNegativeInfinity(correction)&&Mathf.Abs(correction)<.18f)go.transform.position+=Vector3.up*(correction+.001f);
        foreach(var c in own)c.enabled=true;Physics.SyncTransforms();
    }
    struct Vertex{public Vector3 p,n;public Vector2 uv;public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
    static float Side(Vector3 p,Vector2 a,Vector2 b)=> (b.x-a.x)*(p.z-a.y)-(b.y-a.y)*(p.x-a.x);
    static List<Vertex> Clip(List<Vertex> input,Vector2 a,Vector2 b,bool inside)
    {
        var output=new List<Vertex>();if(input.Count==0)return output;var previous=input[input.Count-1];float dp=Side(previous.p,a,b);bool pin=inside?dp>=0:dp<=0;
        foreach(var current in input){float dc=Side(current.p,a,b);bool cin=inside?dc>=0:dc<=0;if(cin!=pin)output.Add(Vertex.Lerp(previous,current,dp/(dp-dc)));if(cin)output.Add(current);previous=current;dp=dc;pin=cin;}return output;
    }
    // Only the permanent outer mass is trimmed. Native individual pieces close the rough rescue shaft.
    static void OpenShaft(MeshFilter filter)
    {
        var renderer=filter.GetComponent<MeshRenderer>();var b=renderer.bounds;
        if(b.max.y<1.13f||b.min.x>.39f||b.max.x<-.39f||b.min.z>.72f||b.max.z<-.17f)return;
        Vector2[] rim=Enumerable.Range(0,8).Select(i=>{float a=i*Mathf.PI/4;return new Vector2(Mathf.Cos(a)*.37f, .27f+Mathf.Sin(a)*.43f);}).ToArray();
        var src=filter.sharedMesh;var v=src.vertices;var n=src.normals;var uv=src.uv;var positions=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var subs=new List<int[]>();
        for(int sub=0;sub<src.subMeshCount;sub++){var indices=new List<int>();var triangles=src.GetTriangles(sub);for(int i=0;i<triangles.Length;i+=3){var poly=new List<Vertex>();for(int j=0;j<3;j++){int id=triangles[i+j];poly.Add(new Vertex{p=filter.transform.TransformPoint(v[id]),n=n[id],uv=uv.Length>id?uv[id]:Vector2.zero});}
                for(int edge=0;edge<rim.Length&&poly.Count>=3;edge++){var outside=Clip(poly,rim[edge],rim[(edge+1)%rim.Length],false);if(outside.Count>=3){int start=positions.Count;foreach(var p in outside){positions.Add(filter.transform.InverseTransformPoint(p.p));normals.Add(p.n);uvs.Add(p.uv);}for(int j=1;j<outside.Count-1;j++)indices.AddRange(new[]{start,start+j,start+j+1});}poly=Clip(poly,rim[edge],rim[(edge+1)%rim.Length],true);}}
            subs.Add(indices.ToArray());}
        var mesh=new Mesh{name="Rough Shaft Rim "+(++clipped),indexFormat=IndexFormat.UInt32};mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.subMeshCount=subs.Count;for(int i=0;i<subs.Count;i++)mesh.SetTriangles(subs[i],i);mesh.RecalculateBounds();mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh,Art+"/RoughShaftRim_"+clipped.ToString("000")+".asset");filter.sharedMesh=mesh;
    }
    static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedProperties();}
    static void ArrayRefs(UnityEngine.Object target,string field,UnityEngine.Object[] values){var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedProperties();}
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");if(AssetDatabase.IsValidFolder(Art))throw new Exception("Interlocked pass already exists");AssetDatabase.CreateFolder("Assets/_Project/Art","InterlockedRubble");
        var scene=SceneManager.GetActiveScene();var env=GameObject.Find("Prototype Environment");var sealedMass=GameObject.Find("Sealed Rubble Burial").transform;
        int retired=0;
        foreach(var r in env.GetComponentsInChildren<MeshRenderer>(true))if(!r.transform.IsChildOf(sealedMass)&&r.name!="Phone Ledge"&&r.name!="Bottle Resting Slab"){if(r.enabled)retired++;r.enabled=false;}
        foreach(var c in env.GetComponentsInChildren<Collider>(true))if(!c.transform.IsChildOf(sealedMass)&&c.name!="Floor Slab"&&c.name!="Phone Ledge"&&c.name!="Bottle Resting Slab")c.enabled=false;
        var sections=UnityEngine.Object.FindObjectsByType<AftershockFractureSection>(FindObjectsInactive.Include,FindObjectsSortMode.None);foreach(var s in sections)s.gameObject.SetActive(false);
        ArrayRefs(UnityEngine.Object.FindFirstObjectByType<AftershockDirector>(),"fractureSections",Array.Empty<UnityEngine.Object>());
        foreach(var f in sealedMass.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>().enabled))OpenShaft(f);
        var root=new GameObject("Interlocked Rubble Structure");root.transform.SetParent(env.transform,false);
        Add(root.transform,"Piles/Rubble_Medium","Compacted rubble beneath legs",new Vector3(-.03f,.21f,.52f),new Vector3(0,17,0),.34f);
        Add(root.transform,"Piles/Rubble_Small_01","Rear load bearing rubble",new Vector3(.01f,.13f,-.63f),new Vector3(0,101,0),.36f);
        for(int side=-1;side<=1;side+=2){
            var foot=Add(root.transform,"Props/Wall_Grey_Big_01",side<0?"Left foundation slab":"Right foundation slab",new Vector3(side*.61f,.26f,.17f),new Vector3(6,side*13,side*4),.70f);Seat(foot);
            var lower=Add(root.transform,"Props/Wall_Gray_Medium_02",side<0?"Left wedged upright":"Right wedged upright",new Vector3(side*.61f,.56f,.25f),new Vector3(0,side*8,side*67),1.05f);Seat(lower);
            var upper=Add(root.transform,"Props/Wall_Grey_Big_01",side<0?"Left leaning load slab":"Right leaning load slab",new Vector3(side*.60f,.91f,.14f),new Vector3(0,side*10,side*72),.75f);Seat(upper);
            Add(root.transform,"WithRebar/Rebar_Wall_08",side<0?"Left beam locking shard":"Right beam locking shard",new Vector3(side*.57f,1.21f,.30f),new Vector3(0,side*12,side*4),.47f);
        }
        Add(root.transform,"Props/Wall_Grey_Big_01","Rear leaning cross slab",new Vector3(.13f,.73f,-.73f),new Vector3(77,11,-8),.86f);
        Add(root.transform,"Props/Wall_Gray_Medium_03","Rear bottle pocket brace",new Vector3(-.48f,.47f,-.66f),new Vector3(69,-30,12),.80f);
        var seat=Add(root.transform,"Props/Wall_Grey_Big_01","Pipe landing bearing slab",new Vector3(-.03f,.40f,.27f),new Vector3(0,12,0),.67f);
        // A small, framed extraction opening. The surrounding canopy remains in place at completion.
        Add(root.transform,"Props/Wall_Side_01","Shaft left bearing edge",new Vector3(-.48f,1.34f,.27f),new Vector3(0,0,-7),.62f);
        Add(root.transform,"Props/Wall_Side_01","Shaft right bearing edge",new Vector3(.48f,1.36f,.27f),new Vector3(0,0,8),.62f);
        Add(root.transform,"Props/Wall_Grey_Big_01","Shaft front lintel",new Vector3(.01f,1.36f,.82f),new Vector3(0,0,-3),.78f);
        Add(root.transform,"Props/Wall_Grey_Big_01","Shaft rear lintel",new Vector3(-.02f,1.39f,-.29f),new Vector3(0,5,4),.78f);
        var plugs=new Transform[4];
        float[] heights={2.68f,2.24f,1.84f,1.36f};
        for(int i=0;i<4;i++)plugs[i]=Add(root.transform,"Props/Wall_Grey_Big_01","Individual rescue slab "+(i+1)+" - "+new[]{"outermost","upper","lower","closest"}[i],new Vector3(i%2==0?-.015f:.015f,heights[i],.27f),new Vector3(i%2==0?2:-2,90+i*3,i%2==0?-2:2),1.12f).transform;
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();ArrayRefs(ending,"rubble",plugs.Cast<UnityEngine.Object>().ToArray());var ed=new SerializedObject(ending);var starts=ed.FindProperty("removalStarts");var offsets=ed.FindProperty("removalOffsets");starts.arraySize=offsets.arraySize=4;
        for(int i=0;i<4;i++){starts.GetArrayElementAtIndex(i).floatValue=3+i*14;offsets.GetArrayElementAtIndex(i).vector3Value=new Vector3(i%2==0?-1.45f:1.45f,.24f,.65f);}
        ed.FindProperty("removalSeconds").floatValue=10;ed.FindProperty("durationSeconds").floatValue=60;ed.ApplyModifiedProperties();
        var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();ArrayRefs(motion,"ceilingTargets",Array.Empty<UnityEngine.Object>());ArrayRefs(motion,"shakeTargets",Array.Empty<UnityEngine.Object>());ArrayRefs(motion,"roomTargets",new UnityEngine.Object[]{root.transform});
        var drop=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var pd=new SerializedObject(drop);
        string[] fields={"stowedPose","lipPose","releasePose"};Vector3[] poses={new Vector3(-.19f,1.72f,.58f),new Vector3(-.19f,1.30f,.46f),new Vector3(-.18f,1.08f,.40f)};
        for(int i=0;i<3;i++){var t=(Transform)pd.FindProperty(fields[i]).objectReferenceValue;t.SetParent(root.transform,true);t.SetPositionAndRotation(poses[i],Quaternion.Euler(75-i*22,5,-18));}
        Physics.SyncTransforms();var landing=(Transform)pd.FindProperty("landingPose").objectReferenceValue;
        if(Physics.Raycast(new Vector3(-.025f,.75f,.25f),Vector3.down,out var hit,.6f))landing.position=new Vector3(-.025f,hit.point.y+.012f,.25f);
        pd.FindProperty("slideSeconds").floatValue=1.65f;pd.FindProperty("tipSeconds").floatValue=.40f;pd.FindProperty("fallSeconds").floatValue=.38f;pd.FindProperty("settleSeconds").floatValue=.3f;pd.ApplyModifiedProperties();drop.transform.SetPositionAndRotation(poses[0],((Transform)pd.FindProperty("stowedPose").objectReferenceValue).rotation);
        drop.GetComponent<Renderer>().enabled=true;
        // Keep a small reusable set. Their actual rigid bodies now settle on the matching visible meshes.
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();var dd=new SerializedObject(debris);var old=dd.FindProperty("fragments");var pool=Enumerable.Range(0,old.arraySize).Select(i=>(Transform)old.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();foreach(var f in pool)f.gameObject.SetActive(false);
        var chosen=pool.Take(28).ToArray();ArrayRefs(debris,"fragments",chosen.Cast<UnityEngine.Object>().ToArray());dd=new SerializedObject(debris);var releases=dd.FindProperty("releaseTimes");releases.arraySize=chosen.Length;
        var src=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Props/Brick_Small_03.prefab").GetComponentsInChildren<MeshFilter>(true).First(f=>f.name.EndsWith("LOD0"));
        for(int i=0;i<chosen.Length;i++){var f=chosen[i];f.SetParent(root.transform,true);int side=i%2==0?-1:1;f.position=new Vector3(side*(.49f+(i%4)*.015f),1.26f+(i%3)*.018f,.10f+(i%6)*.12f);f.localScale=Vector3.one*(.20f+(i%4)*.025f);f.rotation=Quaternion.Euler(i*37,i*71,i*23);f.GetComponent<MeshFilter>().sharedMesh=src.sharedMesh;f.GetComponent<MeshRenderer>().sharedMaterials=src.GetComponent<MeshRenderer>().sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();releases.GetArrayElementAtIndex(i).floatValue=new[]{3.2f,4.8f,6.6f,10.2f,13.4f,16.2f}[i%6]+(i%4)*.07f;}
        dd.ApplyModifiedProperties();ArrayRefs(debris,"protectedColliders",Camera.main.transform.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).Cast<UnityEngine.Object>().ToArray());
        ArrayRefs(ending,"lightBlockers",env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Cast<UnityEngine.Object>().ToArray());
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return new{retiredLegacyRenderers=retired,nativeSupportedPieces=pieces.Count,shaftRimMeshes=clipped,physicalFragments=chosen.Length,rescueSlabs=4,outerToInner=true,landing=landing.position.ToString()};
    }
}
