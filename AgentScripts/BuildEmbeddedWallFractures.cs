using System;
using System.Collections.Generic;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Clips the existing wall meshes, preserving their exterior silhouette, normals and UVs.</summary>
public static class BuildEmbeddedWallFractures
{
    struct Vertex
    {
        public Vector3 p, n; public Vector2 uv;
        public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};
    }
    struct Triangle { public Vertex a,b,c; public int material; }
    class Solid { public List<Triangle> triangles=new List<Triangle>(); }
    static Solid Read(Mesh mesh)
    {
        var p=mesh.vertices;var n=mesh.normals;var uv=mesh.uv;var s=new Solid();var t=mesh.triangles;
        for(int i=0;i<t.Length;i+=3)
        {
            Func<int,Vertex> v=k=>new Vertex{p=p[t[k]],n=n[t[k]],uv=uv.Length==p.Length?uv[t[k]]:Vector2.zero};
            s.triangles.Add(new Triangle{a=v(i),b=v(i+1),c=v(i+2),material=0});
        }
        return s;
    }
    static Solid Clip(Solid source,Vector3 normal,float distance,bool negative)
    {
        var output=new Solid();var cut=new List<Vector3>();
        normal=normal.normalized;
        foreach(var tri in source.triangles)
        {
            var polygon=new List<Vertex>();var input=new[]{tri.a,tri.b,tri.c};
            for(int i=0;i<3;i++)
            {
                var a=input[i];var b=input[(i+1)%3];float da=Vector3.Dot(normal,a.p)-distance;float db=Vector3.Dot(normal,b.p)-distance;
                bool ina=negative?da<=0.000001f:da>=-0.000001f;bool inb=negative?db<=0.000001f:db>=-0.000001f;
                if(ina)polygon.Add(a);
                if(ina!=inb)
                {
                    var cross=Vertex.Lerp(a,b,da/(da-db));polygon.Add(cross);
                    if(!cut.Any(p=>(p-cross.p).sqrMagnitude<0.00000001f))cut.Add(cross.p);
                }
            }
            for(int i=1;i+1<polygon.Count;i++)
                if(Vector3.Cross(polygon[i].p-polygon[0].p,polygon[i+1].p-polygon[0].p).sqrMagnitude>0.000000000001f)
                    output.triangles.Add(new Triangle{a=polygon[0],b=polygon[i],c=polygon[i+1],material=tri.material});
        }
        if(cut.Count>=3)
        {
            var center=cut.Aggregate(Vector3.zero,(a,b)=>a+b)/cut.Count;var outward=negative?normal:-normal;
            var tangent=Vector3.Cross(Mathf.Abs(outward.y)<.9f?Vector3.up:Vector3.right,outward).normalized;var bitangent=Vector3.Cross(outward,tangent);
            cut=cut.OrderBy(p=>Mathf.Atan2(Vector3.Dot(p-center,bitangent),Vector3.Dot(p-center,tangent))).ToList();
            Func<Vector3,Vertex> cap=p=>new Vertex{p=p,n=outward,uv=new Vector2(Vector3.Dot(p,tangent),Vector3.Dot(p,bitangent))};
            for(int i=0;i<cut.Count;i++)output.triangles.Add(new Triangle{a=cap(center),b=cap(cut[i]),c=cap(cut[(i+1)%cut.Count]),material=1});
        }
        return output;
    }
    static Mesh Bake(Solid solid,Transform source,Vector3 pivot,string name)
    {
        var mesh=new Mesh{name=name};var positions=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var indices=new[]{new List<int>(),new List<int>()};
        var normalMatrix=source.localToWorldMatrix.inverse.transpose;
        foreach(var tri in solid.triangles)
        {
            foreach(var v in new[]{tri.a,tri.b,tri.c})
            {
                indices[tri.material].Add(positions.Count);positions.Add(source.TransformPoint(v.p)-pivot);normals.Add(normalMatrix.MultiplyVector(v.n).normalized);uvs.Add(v.uv);
            }
        }
        mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.subMeshCount=2;
        for(int i=0;i<2;i++)mesh.SetTriangles(indices[i],i);mesh.RecalculateBounds();
        var path="Assets/_Project/Art/Aftershock/EmbeddedWallFractures/"+name+".asset";
        if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("Refusing to overwrite "+path);
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Vector3 Center(Solid solid,Transform source)
    {
        var p=solid.triangles.SelectMany(t=>new[]{t.a.p,t.b.p,t.c.p}).Select(p=>source.TransformPoint(p)).ToArray();
        var b=new Bounds(p[0],Vector3.zero);foreach(var point in p)b.Encapsulate(point);return b.center;
    }
    static GameObject Surface(string name,Transform parent,Mesh mesh,Material exterior,Material interior,Vector3 localPosition)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=localPosition;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=new[]{exterior,interior};return go;
    }
    static void Rod(Transform parent,Transform wall,string name,Vector3 a,Vector3 b,float radius,Material material)
    {
        var start=wall.TransformPoint(a);var end=wall.TransformPoint(b);var delta=end-start;
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent,false);go.transform.position=(start+end)*.5f;go.transform.rotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);go.transform.localScale=new Vector3(radius*2,delta.magnitude*.5f,radius*2);
        go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
    {
        var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var scene=SceneManager.GetActiveScene();if(!scene.path.EndsWith("/ConfinedPrototype.unity"))throw new Exception("Wrong scene");
        if(GameObject.Find("Embedded Rear Wall Fractures")!=null)throw new Exception("Already authored; refusing duplication");
        const string folder="Assets/_Project/Art/Aftershock/EmbeddedWallFractures";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Aftershock","EmbeddedWallFractures");
        var root=new GameObject("Embedded Rear Wall Fractures");root.transform.SetParent(GameObject.Find("Prototype Environment").transform,false);Undo.RegisterCreatedObjectUndo(root,"Embed rear wall fractures");
        var detached=new GameObject("Detached Wall Debris");detached.transform.SetParent(root.transform,false);
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();var debrisData=new SerializedObject(debris);
        var steel=GameObject.Find("Rear Slab Reinforcement").GetComponent<MeshRenderer>().sharedMaterial;
        var inner=GameObject.Find("Rear Inner Fracture Center").GetComponent<MeshRenderer>().sharedMaterial;
        var sourceNames=new[]{"Rear Left Fractured Panel","Rear Diagonal Reinforced Slab","Rear Inner Fracture Right"};
        var times=new[]{10.2f,13.4f,16.2f};var sections=new List<AftershockFractureSection>();var anchors=new List<Transform>();var reports=new List<object>();
        var safety=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.attachedRigidbody!=null || c.transform.root.name.Contains("XR") || c.GetComponentsInParent<Component>(true).Any(x=>x!=null&&x.GetType().Name.Contains("XRGrabInteractable"))).ToArray();
        var head=Camera.main.transform;var dso=new SerializedObject(debris);var left=dso.FindProperty("leftHand").objectReferenceValue;var right=dso.FindProperty("rightHand").objectReferenceValue;
        for(int index=0;index<3;index++)
        {
            var source=GameObject.Find(sourceNames[index]);var wall=source.transform;anchors.Add(wall);
            var original=source.GetComponent<MeshFilter>().sharedMesh;var exterior=source.GetComponent<MeshRenderer>().sharedMaterial;var bounds=original.bounds;
            float cutZ=bounds.max.z-bounds.size.z*.37f;float xmin=bounds.min.x+bounds.size.x*.15f,xmax=bounds.max.x-bounds.size.x*.16f;
            float ymin=bounds.min.y+bounds.size.y*.20f,ymax=bounds.max.y-bounds.size.y*.13f;
            var whole=Read(original);var fixedParts=new List<Solid>{Clip(whole,Vector3.forward,cutZ,true)};var remaining=Clip(whole,Vector3.forward,cutZ,false);
            // Asymmetric diagonal edges remain part of the original wall around the opening.
            var normals=new[]{new Vector3(-1,.11f,0),new Vector3(1,-.17f,0),new Vector3(-.13f,-1,0),new Vector3(.16f,1,0),new Vector3(.7f,.7f,0)};
            var points=new[]{new Vector3(xmin,bounds.center.y,0),new Vector3(xmax,bounds.center.y,0),new Vector3(bounds.center.x,ymin,0),new Vector3(bounds.center.x,ymax,0),new Vector3(xmax-.05f,ymax-.04f,0)};
            for(int i=0;i<normals.Length;i++)
            {
                float distance=Vector3.Dot(normals[i].normalized,points[i]);var frame=Clip(remaining,normals[i],distance,false);
                if(frame.triangles.Count>0)fixedParts.Add(frame);remaining=Clip(remaining,normals[i],distance,true);
            }
            var sectionObject=new GameObject("Embedded Wall Fracture "+(index+1));sectionObject.transform.SetParent(root.transform,false);sectionObject.transform.position=wall.position;
            var section=sectionObject.AddComponent<AftershockFractureSection>();sections.Add(section);
            int fixedTriangles=0;
            for(int i=0;i<fixedParts.Count;i++)
            {
                var mesh=Bake(fixedParts[i],wall,wall.position,$"Wall{index+1}_Fixed{i+1}");fixedTriangles+=mesh.triangles.Length/3;
                var go=Surface("Original Wall Core and Edge "+(i+1),sectionObject.transform,mesh,exterior,inner,Vector3.zero);
                go.AddComponent<MeshCollider>().sharedMesh=mesh;
            }
            var split=new Vector3(.85f,index==1?-.53f:.48f,0).normalized;float splitDistance=Vector3.Dot(split,new Vector3(bounds.center.x,bounds.center.y,cutZ));
            var solids=new[]{Clip(remaining,split,splitDistance,true),Clip(remaining,split,splitDistance,false)};
            var chunks=new List<AftershockFallingChunk>();var initialIgnored=new List<string>();
            for(int piece=0;piece<2;piece++)
            {
                var center=Center(solids[piece],wall);var mesh=Bake(solids[piece],wall,center,$"Wall{index+1}_FallingSkin{piece+1}");
                var go=Surface("Falling Wall Masonry "+(index+1)+"."+(piece+1),sectionObject.transform,mesh,exterior,inner,center-wall.position);
                var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;collider.contactOffset=.002f;
                var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.mass=index==1?2.2f:.8f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.maxDepenetrationVelocity=.5f;body.angularDamping=.16f;
                var chunk=go.AddComponent<AftershockFallingChunk>();chunks.Add(chunk);
                var data=new SerializedObject(chunk);data.FindProperty("detachedRoot").objectReferenceValue=detached.transform;
                data.FindProperty("head").objectReferenceValue=head;data.FindProperty("leftHand").objectReferenceValue=left;data.FindProperty("rightHand").objectReferenceValue=right;
                // Peel about the actual upper attachment, not the center of a radial arrangement.
                var pivot=wall.TransformPoint(new Vector3(wall.InverseTransformPoint(center).x,ymax,cutZ));
                data.FindProperty("attachmentPoint").vector3Value=pivot-wall.position;data.FindProperty("attachmentAxis").vector3Value=wall.TransformDirection(Vector3.right);
                data.FindProperty("delaySeconds").floatValue=piece==0?0f:.27f;data.FindProperty("attachmentSeconds").floatValue=piece==0?.15f:.23f;
                data.FindProperty("peelDegrees").floatValue=piece==0?-4f:-7f;
                data.FindProperty("releaseVelocity").vector3Value=wall.TransformDirection(new Vector3(piece==0?-.035f:.045f,-.08f,.065f));
                data.FindProperty("releaseAngularVelocity").vector3Value=wall.TransformDirection(new Vector3(-.7f,piece==0?-.3f:.4f,piece==0?-.4f:.55f));
                var ignores=safety.ToList();ignores.AddRange(source.GetComponents<Collider>());
                Physics.SyncTransforms();
                foreach(var other in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                {
                    if(other==collider||other.isTrigger||other.transform.IsChildOf(root.transform)||ignores.Contains(other))continue;
                    if(Physics.ComputePenetration(collider,go.transform.position,go.transform.rotation,other,other.transform.position,other.transform.rotation,out var direction,out var depth)&&depth>.008f)
                    {ignores.Add(other);initialIgnored.Add(go.name+" / "+other.name+" ("+depth.ToString("F3")+"m)");}
                }
                var array=data.FindProperty("ignoreColliders");array.arraySize=ignores.Distinct().Count();int k=0;foreach(var c in ignores.Distinct())array.GetArrayElementAtIndex(k++).objectReferenceValue=c;data.ApplyModifiedPropertiesWithoutUndo();
            }
            var rebar=new GameObject("Steel Embedded in Original Wall");rebar.transform.SetParent(sectionObject.transform,false);
            float z=cutZ+.007f/wall.lossyScale.z;
            for(int bar=0;bar<2;bar++)
            {
                float x=Mathf.Lerp(xmin,xmax,bar==0?.30f:.68f);
                var a=new Vector3(x,ymin-.09f,z);var b=new Vector3(x+(bar==0?.012f:-.015f),ymax-.13f,z);var c=new Vector3(x+(bar==0?.03f:-.02f),ymax+.06f,z+.009f/wall.lossyScale.z);
                Rod(rebar.transform,wall,"Embedded Ribbed Rebar "+(bar+1),a,b,.004f,steel);Rod(rebar.transform,wall,"Short Bent Rebar End "+(bar+1),b,c,.004f,steel);
                var wa=wall.TransformPoint(a);var wb=wall.TransformPoint(b);float length=(wb-wa).magnitude;int ribs=Mathf.FloorToInt(length/.023f);
                for(int rib=1;rib<ribs;rib++)
                {
                    var p=Vector3.Lerp(a,b,(float)rib/ribs);var dir=(b-a).normalized;Rod(rebar.transform,wall,"Steel Rib",p-dir*.0013f/wall.lossyScale.y,p+dir*.0013f/wall.lossyScale.y,.005f,steel);
                }
            }
            Rod(rebar.transform,wall,"Embedded Transverse Tie",new Vector3(xmin-.06f,bounds.center.y,z-.003f/wall.lossyScale.z),new Vector3(xmax+.06f,bounds.center.y+.025f,z-.003f/wall.lossyScale.z),.0035f,steel);
            var plume=GameObject.Find("Later Crack Dust "+(index+1)).GetComponent<ParticleSystem>();plume.transform.SetParent(sectionObject.transform,true);
            var fracturePoint=wall.TransformPoint(new Vector3(bounds.center.x,Mathf.Lerp(ymin,ymax,.6f),cutZ+.03f));plume.transform.position=fracturePoint;plume.transform.rotation=Quaternion.identity;
            var sectionData=new SerializedObject(section);sectionData.FindProperty("wallAnchor").objectReferenceValue=wall;sectionData.FindProperty("exposedRebar").objectReferenceValue=rebar.transform;sectionData.FindProperty("fractureDust").objectReferenceValue=plume;
            sectionData.FindProperty("opensAtSeconds").floatValue=times[index];sectionData.FindProperty("openingSeconds").floatValue=.75f;
            var chunkArray=sectionData.FindProperty("fallingChunks");chunkArray.arraySize=2;for(int i=0;i<2;i++)chunkArray.GetArrayElementAtIndex(i).objectReferenceValue=chunks[i];sectionData.ApplyModifiedPropertiesWithoutUndo();
            Undo.RecordObject(source.GetComponent<Renderer>(),"Retain original wall renderer");source.GetComponent<Renderer>().enabled=false;
            foreach(var collider in source.GetComponents<Collider>()){Undo.RecordObject(collider,"Retain original wall collider");collider.enabled=false;}
            // Small chips now originate at this wall's failing surface.
            var fragments=debrisData.FindProperty("fragments");var ends=debrisData.FindProperty("landingPositions");var releases=debrisData.FindProperty("releaseTimes");var durations=debrisData.FindProperty("fallDurations");
            for(int chip=0;chip<8;chip++)
            {
                int pool=92+chip*3+index;var fragment=(Transform)fragments.GetArrayElementAtIndex(pool).objectReferenceValue;fragment.SetParent(root.transform,true);
                var local=new Vector3(Mathf.Lerp(xmin,xmax,.15f+chip*.10f),Mathf.Lerp(ymin,ymax,.25f+(chip%3)*.24f),cutZ+.035f);
                var point=wall.TransformPoint(local);fragment.position=point;
                // Keep the existing deterministic lightweight pool, with a short gravity arc down the rear wall.
                var end=point+new Vector3((chip%2==0?-.01f:.015f),-.6f,.025f);end.y=.18f;
                if(Physics.Raycast(point+Vector3.forward*.015f,Vector3.down,out var hit,1.5f))end.y=hit.point.y+.012f;
                ends.GetArrayElementAtIndex(pool).vector3Value=end;releases.GetArrayElementAtIndex(pool).floatValue=times[index]+.04f+chip*.043f;durations.GetArrayElementAtIndex(pool).floatValue=.38f+(chip%3)*.08f;
            }
            reports.Add(new{wall=source.name,fixedTriangles,spalls=2,fracturePoint=fracturePoint.ToString("F3"),initialOverlapIgnores=initialIgnored.ToArray()});
        }
        debrisData.ApplyModifiedPropertiesWithoutUndo();
        var directorData=new SerializedObject(director);var sectionRefs=directorData.FindProperty("fractureSections");sectionRefs.arraySize=3;for(int i=0;i<3;i++)sectionRefs.GetArrayElementAtIndex(i).objectReferenceValue=sections[i];directorData.ApplyModifiedPropertiesWithoutUndo();
        var motionData=new SerializedObject(motion);var targets=motionData.FindProperty("roomTargets");var kept=new List<Transform>();
        for(int i=0;i<targets.arraySize;i++){var t=(Transform)targets.GetArrayElementAtIndex(i).objectReferenceValue;if(t!=null&&!t.name.StartsWith("Later Rear Fracture "))kept.Add(t);}
        foreach(var t in anchors)if(!kept.Contains(t))kept.Add(t);targets.arraySize=kept.Count;for(int i=0;i<kept.Count;i++)targets.GetArrayElementAtIndex(i).objectReferenceValue=kept[i];motionData.ApplyModifiedPropertiesWithoutUndo();
        var old=GameObject.Find("Extended Rear Fractures");old.name="Archived Floating Fractures - Inactive";old.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        return new{result="Existing wall meshes split into fixed concrete and six gravity-driven skins; steel is permanently embedded",walls=reports.ToArray(),roomTargets=kept.Count,safetyIgnoredColliders=safety.Length,oldAssets="Preserved inactive"};
    }
}
