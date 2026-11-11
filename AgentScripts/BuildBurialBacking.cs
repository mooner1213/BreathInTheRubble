using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using BreathInTheRubble.Prototype;

public static class BuildBurialBacking
{
    const string Art="Assets/_Project/Art/BurialBacking";
    static int serial;
    static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static Mesh Extract(Mesh source,HashSet<int> keep,string name)
    {
        var map=new Dictionary<int,int>();var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var subs=new List<int[]>();var v=source.vertices;var n=source.normals;var u=source.uv;
        for(int sub=0;sub<source.subMeshCount;sub++)
        {
            var indices=new List<int>();var triangles=source.GetTriangles(sub);
            for(int i=0;i<triangles.Length;i+=3)
            {
                if(!keep.Contains(triangles[i])||!keep.Contains(triangles[i+1])||!keep.Contains(triangles[i+2]))continue;
                for(int k=0;k<3;k++){int a=triangles[i+k];if(!map.TryGetValue(a,out int b)){b=vertices.Count;map[a]=b;vertices.Add(v[a]);normals.Add(n.Length>a?n[a]:Vector3.up);uv.Add(u.Length>a?u[a]:Vector2.zero);}indices.Add(b);}
            }
            subs.Add(indices.ToArray());
        }
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=subs.Count;for(int i=0;i<subs.Count;i++)mesh.SetTriangles(subs[i],i);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
    public static List<int[]> Components(Mesh mesh)
    {
        var v=mesh.vertices;var parent=Enumerable.Range(0,v.Length).ToArray();var weld=new Dictionary<Vector3Int,int>();
        int Find(int a){while(parent[a]!=a){parent[a]=parent[parent[a]];a=parent[a];}return a;}
        void Join(int a,int b){a=Find(a);b=Find(b);if(a!=b)parent[a]=b;}
        for(int i=0;i<v.Length;i++){var key=Vector3Int.RoundToInt(v[i]*100000);if(weld.TryGetValue(key,out int j))Join(i,j);else weld[key]=i;}
        var t=mesh.triangles;for(int i=0;i<t.Length;i+=3){Join(t[i],t[i+1]);Join(t[i],t[i+2]);}
        return Enumerable.Range(0,v.Length).GroupBy(Find).Select(g=>g.ToArray()).Where(g=>g.Length>=3).ToList();
    }
    static Bounds WorldBounds(Transform t,Vector3[] vertices,IEnumerable<int> group)
    {
        bool first=true;var b=new Bounds();foreach(int i in group){var p=t.TransformPoint(vertices[i]);if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}return b;
    }
    static void Save(Mesh mesh,string suffix)=>AssetDatabase.CreateAsset(mesh,Art+"/"+(serial++).ToString("D3")+"_"+suffix+".asset");
    static void AppendRefs(SerializedObject s,string field,IEnumerable<UnityEngine.Object> values)
    {
        var a=s.FindProperty(field);var old=Enumerable.Range(0,a.arraySize).Select(i=>a.GetArrayElementAtIndex(i).objectReferenceValue);var all=old.Concat(values).Where(x=>x!=null).Distinct().ToArray();a.arraySize=all.Length;for(int i=0;i<all.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=all[i];
    }
    public static object Run()
    {
        if(Application.isPlaying||GameObject.Find("Dense permanent burial backing")!=null||AssetDatabase.IsValidFolder(Art))throw new Exception("Edit Mode and unapplied build required");
        AssetDatabase.CreateFolder("Assets/_Project/Art","BurialBacking");var env=GameObject.Find("Prototype Environment");var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var head=Camera.main.transform;
        var group=new GameObject("Dense permanent burial backing").transform;group.SetParent(env.transform,false);
        var looseRoot=new GameObject("Independent unsupported stone islands").transform;looseRoot.SetParent(env.transform,false);
        var persistent=GameObject.Find("Persistent structural rubble").transform;var additions=new List<RubbleStructuralCollapse.Element>();var replacements=new List<object>();var newStones=new List<Transform>();
        var hands=head.root.GetComponentsInChildren<Transform>(true);var left=hands.First(t=>t.name=="Left Controller");var right=hands.First(t=>t.name=="Right Controller");
        int released=0;
        foreach(var e in manager.Elements.Where(e=>e.loose==null).OrderBy(e=>Vector3.Distance(e.target.GetComponent<Renderer>()?.bounds.center??e.target.position,head.position)))
        {
            var r=e.target.GetComponent<MeshRenderer>();var filter=e.target.GetComponent<MeshFilter>();if(r==null||!r.enabled||filter?.sharedMesh==null||!filter.sharedMesh.isReadable||released>=96)continue;
            var original=filter.sharedMesh;var vertices=original.vertices;
            if(r.bounds.max.y<head.position.y+.15f||r.bounds.min.y>1.9f||Vector3.ProjectOnPlane(r.bounds.ClosestPoint(head.position)-head.position,Vector3.up).magnitude>1.15f)continue;
            var parts=Components(original);if(parts.Count<2)continue;var candidates=new List<int[]>();
            foreach(var part in parts)
            {
                var b=WorldBounds(e.target,vertices,part);float radius=Vector3.ProjectOnPlane(b.center-head.position,Vector3.up).magnitude;
                if(b.center.y>head.position.y+.12f&&b.center.y<1.87f&&radius>.40f&&radius<1.20f&&b.size.magnitude>.09f&&b.size.magnitude<.68f&&Mathf.Min(b.size.x,b.size.y,b.size.z)>.035f)candidates.Add(part);
            }
            if(candidates.Count==0)continue;
            candidates=candidates.OrderByDescending(p=>WorldBounds(e.target,vertices,p).size.magnitude).Take(Mathf.Min(8,96-released)).ToList();
            var removed=new HashSet<int>(candidates.SelectMany(p=>p));var remainder=Extract(original,new HashSet<int>(Enumerable.Range(0,vertices.Length).Where(i=>!removed.Contains(i))),original.name+" retained supported material");Save(remainder,"retained");filter.sharedMesh=remainder;EditorUtility.SetDirty(filter);
            foreach(var c in e.target.GetComponents<BoxCollider>())c.enabled=false;
            var originalShape=e.target.GetComponent<MeshCollider>();if(originalShape==null)originalShape=e.target.gameObject.AddComponent<MeshCollider>();originalShape.sharedMesh=remainder;originalShape.convex=false;originalShape.enabled=true;originalShape.contactOffset=.001f;EditorUtility.SetDirty(originalShape);
            foreach(var part in candidates)
            {
                var mesh=Extract(original,new HashSet<int>(part),"Separated unsupported stone "+released);var bounds=WorldBounds(e.target,vertices,part);var center=bounds.center;
                var transformMatrix=e.target.localToWorldMatrix;var normalsMatrix=transformMatrix.inverse.transpose;
                mesh.vertices=mesh.vertices.Select(v=>transformMatrix.MultiplyPoint3x4(v)-center).ToArray();mesh.normals=mesh.normals.Select(n=>normalsMatrix.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();Save(mesh,"loose");
                var go=new GameObject("Unsupported stone island "+released.ToString("D2"));go.transform.SetParent(looseRoot,false);go.transform.position=center;go.AddComponent<MeshFilter>().sharedMesh=mesh;var surface=go.AddComponent<MeshRenderer>();surface.sharedMaterials=r.sharedMaterials;surface.shadowCastingMode=ShadowCastingMode.TwoSided;
                var contact=UnityEngine.Object.Instantiate(mesh);contact.name=mesh.name+" contact core";contact.vertices=contact.vertices.Select(v=>v*.86f).ToArray();contact.RecalculateBounds();Save(contact,"contact");
                var shape=go.AddComponent<MeshCollider>();shape.sharedMesh=contact;shape.convex=true;shape.contactOffset=.001f;
                var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.mass=Mathf.Clamp(bounds.size.x*bounds.size.y*bounds.size.z*180,.08f,5);body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.maxDepenetrationVelocity=.18f;body.angularDamping=.6f;body.sleepThreshold=.005f;
                var chunk=go.AddComponent<AftershockFallingChunk>();Ref(chunk,"detachedRoot",persistent);Ref(chunk,"head",head);Ref(chunk,"leftHand",left);Ref(chunk,"rightHand",right);
                var data=new SerializedObject(chunk);data.FindProperty("headClearanceRadius").floatValue=.20f;data.FindProperty("handClearanceRadius").floatValue=0;data.ApplyModifiedPropertiesWithoutUndo();
                additions.Add(new RubbleStructuralCollapse.Element{target=go.transform,loose=chunk,phase=16.3f+released*2.713f,degrees=.65f,distance=.005f,failureResistance=Mathf.Lerp(.55f,1.35f,Mathf.Repeat(released*.6180339f,1)),releaseAt=-1,settlesAt=100});newStones.Add(go.transform);released++;
            }
            replacements.Add(new{source=e.target.name,mesh=original.name,islands=candidates.Count});
        }
        // Closed single-piece concrete blocks. Pile prefabs contain disconnected stones and cannot seal a cavity.
        var prefabPath=AssetDatabase.FindAssets("Wall_Grey_Big_01 t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(p=>System.IO.Path.GetFileNameWithoutExtension(p)=="Wall_Grey_Big_01");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);var source=prefab.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var sourceMesh=source.GetComponent<MeshFilter>().sharedMesh;var block=UnityEngine.Object.Instantiate(sourceMesh);var native=block.bounds;
        block.name="Closed giant concrete bearing";block.vertices=block.vertices.Select(v=>new Vector3((v.x-native.center.x)/native.size.x,(v.y-native.center.y)/native.size.y,(v.z-native.center.z)/native.size.z)).ToArray();block.RecalculateBounds();block.RecalculateNormals();block.RecalculateTangents();Save(block,"giant");
        var materials=source.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();if(materials.Any(m=>m==null))throw new Exception("Missing existing URP rubble material");
        var roof=new List<Transform>();var allBacking=new List<Renderer>();
        GameObject Bearing(string name,Vector3 position,Vector3 size,Quaternion rotation)
        {
            var go=new GameObject(name);go.transform.SetParent(group,false);go.transform.SetPositionAndRotation(position,rotation);go.transform.localScale=size;go.AddComponent<MeshFilter>().sharedMesh=block;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
            var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=block;collider.convex=false;collider.contactOffset=.001f;var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;
            additions.Add(new RubbleStructuralCollapse.Element{target=go.transform,phase=91.7f+allBacking.Count*3.37f,distance=.0015f,degrees=.10f,releaseAt=-1,settlesAt=100});allBacking.Add(renderer);return go;
        }
        for(int row=0;row<2;row++)for(int i=0;i<12;i++)
        {
            float angle=(i+(row==0?0:.5f))*30;var direction=Quaternion.Euler(0,angle,0)*Vector3.forward;
            float radius=row==0?1.27f:1.34f;
            Bearing("Interlocked buried bank "+row+" "+i,direction*radius+Vector3.up*(row==0?.63f:1.55f),new Vector3(1.02f,row==0?1.62f:1.35f,.72f),Quaternion.Euler((i%3-1)*2,angle,(i%2==0?1:-1)*3));
        }
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
        {
            var go=Bearing("Massive upper backing "+x+" "+z,new Vector3(x*1.18f,2.20f+(x+z)%2*.025f,.28f+z*1.18f),new Vector3(1.45f,.56f,1.45f),Quaternion.Euler(0,(x+z)%2*3,0));if(x==0)roof.Add(go.transform);
        }
        var ms=new SerializedObject(manager);var elements=ms.FindProperty("elements");int start=elements.arraySize;elements.arraySize+=additions.Count;
        for(int i=0;i<additions.Count;i++)
        {
            var p=elements.GetArrayElementAtIndex(start+i);var e=additions[i];p.FindPropertyRelative("target").objectReferenceValue=e.target;p.FindPropertyRelative("loose").objectReferenceValue=e.loose;p.FindPropertyRelative("phase").floatValue=e.phase;p.FindPropertyRelative("distance").floatValue=e.distance;p.FindPropertyRelative("degrees").floatValue=e.degrees;p.FindPropertyRelative("failureResistance").floatValue=e.failureResistance;p.FindPropertyRelative("releaseAt").floatValue=-1;p.FindPropertyRelative("settlesAt").floatValue=100;p.FindPropertyRelative("compression").vector3Value=Vector3.zero;p.FindPropertyRelative("openingOffset").vector3Value=Vector3.zero;p.FindPropertyRelative("powder").objectReferenceValue=null;p.FindPropertyRelative("cracks").arraySize=0;
        }
        ms.ApplyModifiedPropertiesWithoutUndo();
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var endingData=new SerializedObject(ending);var rubble=endingData.FindProperty("rubble");int previous=rubble.arraySize;rubble.arraySize+=roof.Count;
        var times=endingData.FindProperty("removalStarts");var offsets=endingData.FindProperty("removalOffsets");times.arraySize=offsets.arraySize=rubble.arraySize;
        for(int i=0;i<roof.Count;i++){rubble.GetArrayElementAtIndex(previous+i).objectReferenceValue=roof[i];times.GetArrayElementAtIndex(previous+i).floatValue=8+i*7;offsets.GetArrayElementAtIndex(previous+i).vector3Value=new Vector3(i%2==0?-3.5f:3.5f,1.8f,0);}
        AppendRefs(endingData,"lightBlockers",allBacking);AppendRefs(endingData,"apertureBlockers",roof.Select(t=>t.GetComponent<Renderer>()));endingData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);EditorUtility.SetDirty(ending);AssetDatabase.SaveAssets();Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(env.scene);EditorSceneManager.SaveScene(env.scene);
        var result=new{separatedStones=released,modifiedSources=replacements,permanentBacking=allBacking.Count,rescueRoofPieces=roof.Count,totalElements=manager.Elements.Length};
        File.WriteAllText("AgentScripts/ReviewImages/BurialGaps/Build.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{result.separatedStones,sources=replacements.Count,result.permanentBacking,result.rescueRoofPieces,result.totalElements};
    }
}
