using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;

// One-time scene migration. Original models, meshes and line objects are retained.
public static class RefineAftershockContacts
{
    const string Art="Assets/_Project/Art/ContactFractures";
    public static object PipeContactFilters()
    {
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var cap=pipe.GetComponent<Collider>();var p=new SerializedObject(pipe).FindProperty("ignoredColliders");
        return new{serialized=Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue?.name).ToArray(),ignored=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c!=cap&&Physics.GetIgnoreCollision(cap,c)).Select(c=>new{c.name,type=c.GetType().Name,p=c.bounds.center.ToString("F3")}).ToArray(),cap.isTrigger,body=pipe.GetComponent<Rigidbody>().detectCollisions};
    }
    public static object ProbePipeColliders()
    {
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var cap=pipe.GetComponent<CapsuleCollider>();var rotation=Quaternion.Euler(0,12,90);var axis=rotation*Vector3.up*(cap.height*.5f-cap.radius);var hits=new List<object>();
        bool wasEnabled=cap.enabled;cap.enabled=true;Physics.SyncTransforms();
        for(float y=2.4f;y>.9f;y-=.1f)
        {
            var center=new Vector3(0,y,.88f);
            foreach(var other in Physics.OverlapCapsule(center-axis,center+axis,cap.radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                if(other==cap||other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>()!=null)continue;
                if(Physics.ComputePenetration(cap,center,rotation,other,other.transform.position,other.transform.rotation,out var direction,out float depth)&&depth>.0005f)
                    hits.Add(new{y,other.name,type=other.GetType().Name,depth,bounds=other.bounds.ToString("F3"),body=other.attachedRigidbody?.name,renderers=other.GetComponentsInChildren<MeshRenderer>().Select(r=>new{r.name,r.enabled,bounds=r.bounds.ToString("F3")}).ToArray()});
            }
        }
        cap.enabled=wasEnabled;return hits;
    }
    public static object PipePath()
    {
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var data=new SerializedObject(pipe);
        return new{head=Camera.main.transform.position.ToString("F3"),poses=new[]{"stowedPose","lipPose","releasePose"}.Select(n=>{var t=(Transform)data.FindProperty(n).objectReferenceValue;return new{field=n,p=t.position.ToString("F3"),r=t.eulerAngles.ToString("F1")};}).ToArray()};
    }
    struct Vertex
    {
        public Vector3 p,n;public Vector2 uv;
        public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};
    }
    sealed class Cut
    {
        public Vector3 normal,right,up;public float offset,seed;
        public float Distance(Vector3 p)
        {
            float u=Vector3.Dot(p,right),v=Vector3.Dot(p,up);
            float coarse=(Mathf.PerlinNoise(u*19+seed,v*17+seed*.37f)-.5f)*.029f;
            float fine=(Mathf.PerlinNoise(u*73+seed*.71f,v*69+seed)-.5f)*.008f;
            return Vector3.Dot(p,normal)-offset-coarse-fine;
        }
    }
    sealed class Builder
    {
        public readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<int>[] indices;
        public readonly Dictionary<Vector3Int,Vector3> boundary=new Dictionary<Vector3Int,Vector3>();
        readonly Cut cut;readonly bool positive;
        public Builder(int submeshes,Cut cut,bool positive){indices=Enumerable.Range(0,submeshes+1).Select(_=>new List<int>()).ToArray();this.cut=cut;this.positive=positive;}
        public void Triangle(Vertex a,Vertex b,Vertex c,int sub,int depth=0)
        {
            float da=cut.Distance(a.p),db=cut.Distance(b.p),dc=cut.Distance(c.p);
            float plainA=Vector3.Dot(a.p,cut.normal)-cut.offset,plainB=Vector3.Dot(b.p,cut.normal)-cut.offset,plainC=Vector3.Dot(c.p,cut.normal)-cut.offset;
            float longest=Mathf.Max((a.p-b.p).sqrMagnitude,(b.p-c.p).sqrMagnitude,(c.p-a.p).sqrMagnitude);
            // Refine only near the split. Preserve the imported exterior and its UVs.
            if(depth<6&&longest>.000625f&&Mathf.Min(plainA,plainB,plainC)<.020f&&Mathf.Max(plainA,plainB,plainC)>-.020f)
            {
                var ab=Vertex.Lerp(a,b,.5f);var bc=Vertex.Lerp(b,c,.5f);var ca=Vertex.Lerp(c,a,.5f);
                Triangle(a,ab,ca,sub,depth+1);Triangle(ab,b,bc,sub,depth+1);Triangle(ca,bc,c,sub,depth+1);Triangle(ab,bc,ca,sub,depth+1);return;
            }
            var input=new[]{a,b,c};var distances=new[]{da,db,dc};var poly=new List<Vertex>(4);
            var previous=c;float dp=positive?dc:-dc;
            for(int i=0;i<3;i++)
            {
                var current=input[i];float d=positive?distances[i]:-distances[i];
                if((d>=0)!=(dp>=0))
                {
                    var intersection=Vertex.Lerp(previous,current,dp/(dp-d));poly.Add(intersection);
                    var p=intersection.p;var key=new Vector3Int(Mathf.RoundToInt(p.x*20000),Mathf.RoundToInt(p.y*20000),Mathf.RoundToInt(p.z*20000));boundary[key]=p;
                }
                if(d>=0)poly.Add(current);previous=current;dp=d;
            }
            for(int i=1;i+1<poly.Count;i++)Add(poly[0],poly[i],poly[i+1],sub);
        }
        void Add(Vertex a,Vertex b,Vertex c,int sub)
        {
            if(Vector3.Cross(b.p-a.p,c.p-a.p).sqrMagnitude<1e-16f)return;
            int start=vertices.Count;
            foreach(var p in new[]{a,b,c}){vertices.Add(p.p);normals.Add(p.n);uv.Add(p.uv);}
            indices[sub].AddRange(new[]{start,start+1,start+2});
        }
        public Mesh Finish()
        {
            if(boundary.Count>=3)
            {
                var points=boundary.Values.ToArray();var center=points.Aggregate(Vector3.zero,(sum,p)=>sum+p)/points.Length;
                points=points.OrderBy(p=>Mathf.Atan2(Vector3.Dot(p-center,cut.up),Vector3.Dot(p-center,cut.right))).ToArray();
                for(int i=0;i<points.Length;i++)
                {
                    Vector3 a=center,b=points[i],c=points[(i+1)%points.Length];
                    var normal=Vector3.Cross(b-a,c-a).normalized;
                    if(Vector3.Dot(normal,positive?-cut.normal:cut.normal)<0){var swap=b;b=c;c=swap;normal=-normal;}
                    Func<Vector3,Vertex> cap=p=>new Vertex{p=p,n=normal,uv=new Vector2(Vector3.Dot(p,cut.right)*2,Vector3.Dot(p,cut.up)*2)};
                    Add(cap(a),cap(b),cap(c),indices.Length-1);
                }
            }
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=indices.Length;
            for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i);
            mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
        }
    }
    static Mesh[] Divide(Mesh source,Vector3 normal,float offset,float seed)
    {
        normal.Normalize();var right=Vector3.Cross(normal,Vector3.forward).normalized;if(right.sqrMagnitude<.01f)right=Vector3.right;
        var cut=new Cut{normal=normal,right=right,up=Vector3.Cross(normal,right).normalized,offset=offset,seed=seed};
        var a=new Builder(source.subMeshCount,cut,true);var b=new Builder(source.subMeshCount,cut,false);
        var points=source.vertices;var normals=source.normals;var uv=source.uv;
        for(int sub=0;sub<source.subMeshCount;sub++)
        {
            var triangles=source.GetTriangles(sub);
            for(int i=0;i<triangles.Length;i+=3)
            {
                Func<int,Vertex> v=index=>new Vertex{p=points[index],n=normals[index],uv=uv.Length>index?uv[index]:Vector2.zero};
                var p=v(triangles[i]);var q=v(triangles[i+1]);var r=v(triangles[i+2]);a.Triangle(p,q,r,sub);b.Triangle(p,q,r,sub);
            }
        }
        return new[]{a.Finish(),b.Finish()};
    }
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        if(AssetDatabase.IsValidFolder(Art))throw new Exception("Contact fracture revision already applied; do not rebuild");
        var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Unsaved user scene changes; preserve first");
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();
        string[] names={"Front face left pressed rubble","Front face right interlocked masonry","Center crushed bearing stone","Low overlapping fracture veil","Overlapping front ceiling lip"};
        AssetDatabase.CreateFolder("Assets/_Project/Art","ContactFractures");
        var reports=new List<object>();int asset=0;
        foreach(var name in names)
        {
            var original=GameObject.Find(name);if(original==null)throw new Exception("Preserved original missing: "+name);
            var targets=manager.Elements.Where(e=>e.target.name.StartsWith(name+" broken section ")).OrderBy(e=>e.target.name).ToArray();
            if(targets.Length!=2&&targets.Length!=4)throw new Exception("Unexpected fragment count: "+name);
            var frame=targets[0].target;var raw=UnityEngine.Object.Instantiate(original.GetComponent<MeshFilter>().sharedMesh);
            var matrix=frame.worldToLocalMatrix*original.transform.localToWorldMatrix;
            raw.vertices=raw.vertices.Select(p=>matrix.MultiplyPoint3x4(p)).ToArray();raw.normals=raw.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();raw.RecalculateBounds();
            var center=raw.bounds.center;var normal=new Vector3(.71f,.69f,.15f).normalized;
            var halves=Divide(raw,normal,Vector3.Dot(center,normal)+.012f,31.7f+asset*2.1f);
            var meshes=new List<Mesh>();
            if(targets.Length==4)
            {
                for(int half=0;half<2;half++)
                {
                    // The secondary branches leave the main fault at different angles; no tiled cross pattern.
                    var n=new Vector3(half==0?-.64f:-.84f,half==0?.77f:.46f,half==0?.11f:-.18f).normalized;
                    meshes.AddRange(Divide(halves[half],n,Vector3.Dot(halves[half].bounds.center,n),78.3f+asset*3.4f+half));
                }
            }
            else meshes.AddRange(halves);
            for(int i=0;i<targets.Length;i++)
            {
                var e=targets[i];var mesh=meshes[i];mesh.name="Contact fracture "+asset;
                // All former split pieces share a frame. Convert explicitly so the migration stays correct if one was authored differently.
                var local=e.target.worldToLocalMatrix*frame.localToWorldMatrix;
                mesh.vertices=mesh.vertices.Select(p=>local.MultiplyPoint3x4(p)).ToArray();mesh.normals=mesh.normals.Select(n=>local.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();
                AssetDatabase.CreateAsset(mesh,Art+"/Contact_"+(asset++).ToString("D2")+".asset");
                e.target.GetComponent<MeshFilter>().sharedMesh=mesh;e.target.GetComponent<MeshCollider>().sharedMesh=mesh;
                var renderer=e.target.GetComponent<MeshRenderer>();var materials=original.GetComponent<MeshRenderer>().sharedMaterials.ToList();while(materials.Count<mesh.subMeshCount)materials.Add(materials[0]);renderer.sharedMaterials=materials.ToArray();
                var away=Vector3.ProjectOnPlane(frame.TransformVector(mesh.bounds.center-center),Vector3.forward).normalized;
                if(away.sqrMagnitude<.01f)away=i%2==0?Vector3.left:Vector3.right;
                e.openingOffset=e.target.parent.InverseTransformVector(away*.0018f+Vector3.down*.0007f);
                if(e.powder!=null)
                {
                    var vertices=mesh.vertices;var eye=e.target.InverseTransformPoint(Camera.main.transform.position);
                    var closest=vertices.OrderBy(p=>(p-eye).sqrMagnitude).First();e.powder.transform.localPosition=closest;
                }
                reports.Add(new{e.target.name,vertices=mesh.vertexCount,triangles=mesh.triangles.Length/3,opening=e.openingOffset.ToString("F4")});
            }
            UnityEngine.Object.DestroyImmediate(raw);
            foreach(var half in halves)if(!meshes.Contains(half))UnityEngine.Object.DestroyImmediate(half);
        }
        int archived=0;
        foreach(var e in manager.Elements)foreach(var line in e.cracks)if(line!=null){line.enabled=false;archived++;}
        foreach(var crack in UnityEngine.Object.FindObjectsByType<RubbleStressCrack>(FindObjectsInactive.Include,FindObjectsSortMode.None)){crack.ResetEffect();crack.enabled=false;}
        var serialized=new SerializedObject(manager);var array=serialized.FindProperty("elements");
        for(int i=0;i<manager.Elements.Length;i++)array.GetArrayElementAtIndex(i).FindPropertyRelative("openingOffset").vector3Value=manager.Elements[i].openingOffset;
        serialized.ApplyModifiedProperties();EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var report=new{newMeshes=asset,archivedLines=archived,fragments=reports};
        File.WriteAllText("AgentScripts/ReviewImages/AftershockRevision/Build.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
}
