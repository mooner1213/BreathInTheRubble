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
public static class BuildCollapse360
{
    const string Art="Assets/_Project/Art/Collapse360";
    static int meshId;
    static Transform root,fallen;
    static readonly List<RubbleStructuralCollapse.Element> elements=new List<RubbleStructuralCollapse.Element>();
    static readonly List<Transform> rescue=new List<Transform>();
    static readonly System.Random rng=new System.Random(361927);
    static Material powderMaterial,crackMaterial;
    public static object ClearUtilityCorridor()
    {
        if(Application.isPlaying||GameObject.Find("Utility corridor offset applied")!=null)throw new Exception("Edit mode and unapplied corridor required");var group=GameObject.Find("Independent 360 structural collapse");var masses=group.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Upper compressed replacement mass")).ToArray();foreach(var mass in masses)mass.transform.position+=Vector3.back*.15f;var marker=new GameObject("Utility corridor offset applied");marker.transform.SetParent(group.transform,false);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return new{ceilingMasses=masses.Length,offset=.15f};
    }
    public static object NormalizeMotionSeeds()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");var driver=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();elements.Clear();elements.AddRange(driver.Elements);Serialize(driver);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return new{elements=elements.Count,uniquePhases=elements.Select(e=>e.phase).Distinct().Count()};
    }
    public static object BreakFrontCovers()
    {
        if(Application.isPlaying||AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Structural_104.asset")!=null)throw new Exception("Edit mode and unapplied front covers required");meshId=104;root=GameObject.Find("Independent 360 structural collapse").transform;fallen=root.Find("Persistent structural rubble");powderMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/ConcreteFracturePowder.mat");crackMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/StructuralAftershock/ConcreteStressCrack.mat");var driver=root.GetComponent<RubbleStructuralCollapse>();elements.AddRange(driver.Elements.Where(e=>e.target.name!="Overlapping front ceiling lip"&&e.target.name!="Low overlapping fracture veil"));Fracture("Low overlapping fracture veil",3.75f,4);Fracture("Overlapping front ceiling lip",4.40f,4);Serialize(driver);AppendRescue();
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var data=new SerializedObject(pipe);data.FindProperty("startsAtSeconds").floatValue=7.2f;var lip=(Transform)data.FindProperty("lipPose").objectReferenceValue;lip.position=new Vector3(0,1.34f,.88f);var release=(Transform)data.FindProperty("releasePose").objectReferenceValue;release.position=new Vector3(-.025f,1.045f,.34f);data.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{newFrontCoverPieces=8,physicalPieces=101,individualMotionElements=elements.Count,pipeStartsAt=7.2f};
    }
    public static object ExposeLowRebar()
    {
        if(Application.isPlaying||GameObject.Find("Low exposed emergency rebar")!=null)throw new Exception("Edit mode and unapplied rebar required");
        var anchor=GameObject.Find("Right leaning load slab").GetComponentsInChildren<MeshRenderer>().First(r=>r.enabled).transform;
        var path=new[]{new Vector3(.48f,.88f,.22f),new Vector3(.41f,.77f,.26f),new Vector3(.25f,.75f,.27f),new Vector3(.075f,.75f,.29f)};var samples=new List<Vector3>();
        for(int i=0;i<path.Length-1;i++)for(int j=0;j<12;j++)samples.Add(Vector3.Lerp(path[i],path[i+1],j/12f));samples.Add(path[path.Length-1]);
        var vs=new List<Vector3>();var ns=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();float distance=0;const int sides=12;
        for(int i=0;i<samples.Count;i++){if(i>0)distance+=Vector3.Distance(samples[i],samples[i-1]);var tangent=(samples[Mathf.Min(i+1,samples.Count-1)]-samples[Mathf.Max(i-1,0)]).normalized;var side=Vector3.Cross(tangent,Vector3.up).normalized;var up=Vector3.Cross(side,tangent).normalized;float radius=.0075f+.001f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(distance/.02f*Mathf.PI*2)),6);for(int k=0;k<=sides;k++){float a=k*Mathf.PI*2/sides;var normal=side*Mathf.Cos(a)+up*Mathf.Sin(a);vs.Add(anchor.InverseTransformPoint(samples[i]+normal*radius));ns.Add(anchor.localToWorldMatrix.transpose.MultiplyVector(normal).normalized);uv.Add(new Vector2(k/(float)sides,distance*3));}if(i>0)for(int k=0;k<sides;k++){int a=(i-1)*(sides+1)+k,b=a+sides+1;ts.AddRange(new[]{a,b,a+1,a+1,b,b+1});}}
        for(int end=0;end<2;end++){int row=end==0?0:(samples.Count-1)*(sides+1),index=vs.Count;vs.Add(anchor.InverseTransformPoint(samples[end==0?0:samples.Count-1]));ns.Add(anchor.localToWorldMatrix.transpose.MultiplyVector((samples[end==0?0:samples.Count-1]-samples[end==0?1:samples.Count-2]).normalized).normalized);uv.Add(Vector2.zero);for(int k=0;k<sides;k++)ts.AddRange(end==0?new[]{index,row+k+1,row+k}:new[]{index,row+k,row+k+1});}
        var mesh=new Mesh{name="Low exposed ribbed reinforcement"};mesh.SetVertices(vs);mesh.SetNormals(ns);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Art+"/LowExposedRebar.asset");
        var go=new GameObject("Low exposed emergency rebar");go.transform.SetParent(anchor,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/MetalPipe/WornSteelPipe.mat");var surface=go.AddComponent<PipeImpactSurface>();var data=new SerializedObject(surface);data.FindProperty("kind").enumValueIndex=(int)PipeImpactSurface.SurfaceKind.Metal;data.ApplyModifiedProperties();
        for(int i=0;i<path.Length-1;i++){var host=new GameObject("Metal contact "+i);host.transform.SetPositionAndRotation((path[i]+path[i+1])*.5f,Quaternion.FromToRotation(Vector3.up,path[i+1]-path[i]));host.transform.SetParent(go.transform,true);var c=host.AddComponent<CapsuleCollider>();c.radius=.0085f;c.height=Vector3.Distance(path[i],path[i+1])+.017f;c.contactOffset=.001f;}
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return "Ribbed metal striker embedded in the right structural wall, below the fallen front slabs";
    }
    public static object SynchronizeSupports()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");var driver=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();int count=0;
        foreach(var e in driver.Elements)
        {
            if(e.target==null||e.loose!=null)continue;
            var shapes=e.target.GetComponents<Collider>().Where(c=>c.enabled).ToArray();foreach(var shape in shapes)shape.enabled=false;
            var body=e.target.GetComponent<Rigidbody>();if(body==null)body=e.target.gameObject.AddComponent<Rigidbody>();
            body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            foreach(var shape in shapes)shape.enabled=true;count++;
        }
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var data=new SerializedObject(pipe);var lip=(Transform)data.FindProperty("lipPose").objectReferenceValue;lip.position=new Vector3(0,1.45f,.88f);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return new{kinematicSupports=count,physicalPipeRelease=lip.position.ToString("F3")};
    }
    public static object FitPhysicalPipe()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var shape=pipe.GetComponent<CapsuleCollider>();if(shape!=null){shape.radius=Mathf.Max(shape.radius,.027f);shape.contactOffset=.001f;}var body=pipe.GetComponent<Rigidbody>();body.maxDepenetrationVelocity=1;var contact=UnityEngine.Object.FindFirstObjectByType<TrackedRubbleDeflector>();if(contact!=null){var data=new SerializedObject(pipe);var p=data.FindProperty("ignoredColliders");var old=Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue).Concat(new UnityEngine.Object[]{contact.GetComponent<Collider>()}).Where(o=>o!=null).Distinct();Refs(pipe,"ignoredColliders",old);}EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return "Physical pipe contact encloses the 11mm rendered radius with a 16mm contact margin";
    }
    public static object RestoreFoundation()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");var env=GameObject.Find("Prototype Environment");var floor=env.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Continuous buried foundation concrete");floor.SetParent(env.transform,true);floor.gameObject.SetActive(true);foreach(var collider in floor.GetComponents<Collider>())collider.enabled=true;Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return new{floor=floor.name,footprint=floor.GetComponent<MeshRenderer>().bounds.size.ToString("F2")};
    }
    public static object AddHeadContact()
    {
        if(Application.isPlaying||GameObject.Find("Tracked rubble contact")!=null)throw new Exception("Edit mode and unapplied contact required");var go=new GameObject("Tracked rubble contact");go.layer=2;go.transform.SetParent(GameObject.Find("Prototype Environment").transform,false);go.transform.position=Camera.main.transform.position;var sphere=go.AddComponent<SphereCollider>();sphere.radius=.12f;sphere.contactOffset=.002f;var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;var driver=go.AddComponent<TrackedRubbleDeflector>();Ref(driver,"trackedHead",Camera.main.transform);var ignored=Camera.main.transform.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true)));Refs(driver,"ignored",ignored.Cast<UnityEngine.Object>());EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return "Rubble reacts to a tracked 12cm contact surface; no view transform is written";
    }
    public static object RetireOldRain()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");var driver=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();
        // Preserve the superseded window rain and its source meshes, but remove them from live playback.
        foreach(var name in new[]{"Concealed close seam chips","Deep Layered Collapse"}){var go=GameObject.Find(name);if(go!=null)go.SetActive(false);}
        var kept=driver.Elements.Where(e=>e.target!=null&&e.target.gameObject.activeInHierarchy).ToArray();var data=new SerializedObject(driver);var p=data.FindProperty("elements");for(int i=p.arraySize-1;i>=0;i--){var t=p.GetArrayElementAtIndex(i).FindPropertyRelative("target").objectReferenceValue as Transform;if(t==null||!t.gameObject.activeInHierarchy)p.DeleteArrayElementAtIndex(i);}data.ApplyModifiedProperties();
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();data=new SerializedObject(pipe);data.FindProperty("resolveLandingOnRubble").boolValue=true;var landing=(Transform)data.FindProperty("landingPose").objectReferenceValue;var pos=landing.position;pos.z=.33f;landing.position=pos;data.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{liveIndividualPieces=kept.Length,oldSourcesArchived=279,activePhysicalPieces=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).Length};
    }
    static float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
    static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedProperties();}
    static void Refs(UnityEngine.Object target,string field,IEnumerable<UnityEngine.Object> values){var array=values.ToArray();var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=array.Length;for(int i=0;i<array.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=array[i];so.ApplyModifiedProperties();}
    static Mesh Store(Mesh m){m.name="Structural rubble "+meshId;AssetDatabase.CreateAsset(m,Art+"/Structural_"+(meshId++)+".asset");return m;}
    static GameObject MeshObject(string name,Mesh mesh,Material[] mats,Vector3 center)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=center;go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=mats;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;var shape=go.AddComponent<MeshCollider>();shape.sharedMesh=mesh;shape.contactOffset=.001f;go.AddComponent<PipeImpactSurface>();return go;
    }
    static GameObject Native(string source,string name,Vector3 center,Vector3 size,Vector3 angles)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Props/"+source+".prefab");var original=prefab.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var mesh=UnityEngine.Object.Instantiate(original.GetComponent<MeshFilter>().sharedMesh);var b=mesh.bounds;var matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(angles),new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z))*Matrix4x4.Translate(-b.center);mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();mesh.normals=mesh.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();var offset=mesh.bounds.center;mesh.vertices=mesh.vertices.Select(v=>v-offset).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();var mats=original.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();return MeshObject(name,Store(mesh),mats,center);
    }
    struct V {public Vector3 p,n;public Vector2 uv;public static V Lerp(V a,V b,float t)=>new V{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
    static Mesh Slice(Mesh source,Vector3 normal,float offset,bool positive)
    {
        if(!positive){normal=-normal;offset=-offset;}var v=source.vertices;var n=source.normals;var u=source.uv;var verts=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var subs=new List<int[]>();var cut=new List<Vector3>();
        for(int sub=0;sub<source.subMeshCount;sub++)
        {
            var indices=new List<int>();var ts=source.GetTriangles(sub);
            for(int i=0;i<ts.Length;i+=3)
            {
                var input=Enumerable.Range(0,3).Select(k=>{int idx=ts[i+k];return new V{p=v[idx],n=n[idx],uv=u.Length>idx?u[idx]:Vector2.zero};}).ToArray();var poly=new List<V>();var prev=input[2];float dp=Vector3.Dot(prev.p,normal)-offset;
                foreach(var cur in input){float dc=Vector3.Dot(cur.p,normal)-offset;if((dc>=0)!=(dp>=0)){var intersection=V.Lerp(prev,cur,dp/(dp-dc));poly.Add(intersection);cut.Add(intersection.p);}if(dc>=0)poly.Add(cur);prev=cur;dp=dc;}
                int start=verts.Count;foreach(var p in poly){verts.Add(p.p);normals.Add(p.n);uv.Add(p.uv);}for(int k=1;k+1<poly.Count;k++)indices.AddRange(new[]{start,start+k,start+k+1});
            }
            subs.Add(indices.ToArray());
        }
        var unique=new List<Vector3>();foreach(var p in cut)if(!unique.Any(q=>(q-p).sqrMagnitude<.00000001f))unique.Add(p);
        if(unique.Count>=3)
        {
            var center=unique.Aggregate(Vector3.zero,(a,b)=>a+b)/unique.Count;var outward=-normal.normalized;var right=Vector3.Cross(outward,Mathf.Abs(outward.y)>.9f?Vector3.forward:Vector3.up).normalized;var up=Vector3.Cross(outward,right);unique=unique.OrderBy(p=>Mathf.Atan2(Vector3.Dot(p-center,up),Vector3.Dot(p-center,right))).ToList();int start=verts.Count;verts.Add(center);normals.Add(outward);uv.Add(Vector2.zero);foreach(var p in unique){verts.Add(p);normals.Add(outward);uv.Add(new Vector2(Vector3.Dot(p-center,right)*2,Vector3.Dot(p-center,up)*2));}var cap=new List<int>();for(int i=0;i<unique.Count;i++)cap.AddRange(new[]{start,start+1+i,start+1+(i+1)%unique.Count});subs.Add(cap.ToArray());
        }
        var result=new Mesh{indexFormat=IndexFormat.UInt32};result.SetVertices(verts);result.SetNormals(normals);result.SetUVs(0,uv);result.subMeshCount=subs.Count;for(int i=0;i<subs.Count;i++)result.SetTriangles(subs[i],i);result.RecalculateBounds();result.RecalculateTangents();return result;
    }
    static ParticleSystem Powder(Transform parent)
    {
        var go=new GameObject("Concrete fracture powder");go.transform.SetParent(parent,false);var bounds=parent.GetComponent<Renderer>().bounds;go.transform.position=bounds.center;var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=ps.main;m.playOnAwake=false;m.loop=false;m.maxParticles=80;m.simulationSpace=ParticleSystemSimulationSpace.World;m.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.85f);m.startSpeed=new ParticleSystem.MinMaxCurve(.08f,.60f);m.startSize=new ParticleSystem.MinMaxCurve(.018f,.075f);m.startColor=new ParticleSystem.MinMaxGradient(new Color(.46f,.44f,.39f,.38f),new Color(.72f,.68f,.60f,.60f));m.gravityModifier=.18f;var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.07f;var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.12f),new GradientAlphaKey(0,1)});color.color=gradient;var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.4f),new Keyframe(.35f,1),new Keyframe(1,0)));var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=powderMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return ps;
    }
    static LineRenderer[] Cracks(GameObject go)
    {
        var shape=go.GetComponent<MeshCollider>();var bounds=go.GetComponent<Renderer>().bounds;var eye=Camera.main.transform.position;var forward=(bounds.center-eye).normalized;var right=Vector3.Cross(Vector3.up,forward).normalized;if(right.sqrMagnitude<.01f)right=Vector3.right;var up=Vector3.Cross(forward,right).normalized;var result=new List<LineRenderer>();
        Physics.SyncTransforms();for(int branch=0;branch<2;branch++){var points=new List<Vector3>();for(int j=0;j<7;j++){float t=j/6f;var target=bounds.center+right*((t-.5f)*Mathf.Min(bounds.size.magnitude*.55f,.34f))+up*((t-.5f)*R(-.15f,.15f)+Mathf.Sin(j*2.3f+branch)*.012f+branch*.025f);var ray=new Ray(eye,(target-eye).normalized);if(shape.Raycast(ray,out var hit,3))points.Add(go.transform.InverseTransformPoint(hit.point+hit.normal*.0015f));}if(points.Count<2)continue;var host=new GameObject("Growing concrete crack "+branch);host.transform.SetParent(go.transform,false);var line=host.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Count;line.SetPositions(points.ToArray());line.sharedMaterial=crackMaterial;line.startColor=line.endColor=new Color(.025f,.021f,.017f,1);line.startWidth=line.endWidth=1;line.widthMultiplier=.0005f;line.numCornerVertices=1;line.shadowCastingMode=ShadowCastingMode.Off;line.enabled=false;result.Add(line);}return result.ToArray();
    }
    static AftershockFallingChunk Falling(GameObject go,Vector3 velocity,float release,bool chip=false)
    {
        var shape=go.GetComponent<MeshCollider>();shape.convex=true;shape.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Art/BuriedThreat/ForegroundConcreteContact.physicMaterial");if(chip){shape.enabled=false;var box=go.AddComponent<BoxCollider>();box.size=go.GetComponent<MeshFilter>().sharedMesh.bounds.size;box.center=go.GetComponent<MeshFilter>().sharedMesh.bounds.center;box.contactOffset=.001f;box.enabled=false;}
        var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.mass=chip?.025f:Mathf.Clamp(go.GetComponent<Renderer>().bounds.size.magnitude*8,1,12);body.angularDamping=.55f;body.linearDamping=.03f;body.maxDepenetrationVelocity=.7f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.interpolation=RigidbodyInterpolation.Interpolate;body.sleepThreshold=.015f;
        var chunk=go.AddComponent<AftershockFallingChunk>();Ref(chunk,"detachedRoot",fallen);Ref(chunk,"head",Camera.main.transform);var tracking=Camera.main.transform.root.GetComponentsInChildren<Transform>(true);Ref(chunk,"leftHand",tracking.First(t=>t.name=="Left Controller"));Ref(chunk,"rightHand",tracking.First(t=>t.name=="Right Controller"));var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var ignored=Camera.main.transform.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).Where(g=>g.gameObject!=pipe.gameObject).SelectMany(g=>g.GetComponentsInChildren<Collider>(true)));Refs(chunk,"ignoreColliders",ignored.Cast<UnityEngine.Object>());
        var data=new SerializedObject(chunk);data.FindProperty("releaseVelocity").vector3Value=velocity;data.FindProperty("releaseAngularVelocity").vector3Value=new Vector3(R(-2.1f,2.1f),R(-1,1),R(-2.5f,2.5f));data.FindProperty("headClearanceRadius").floatValue=chip?0:.08f;data.FindProperty("handClearanceRadius").floatValue=0;data.FindProperty("colliderWhileAttached").boolValue=!chip;data.FindProperty("attachmentSeconds").floatValue=.05f;data.ApplyModifiedProperties();
        var element=new RubbleStructuralCollapse.Element{target=go.transform,loose=chunk,phase=R(0,100),frequency=R(23,44),distance=chip?.001f:R(.004f,.010f),degrees=chip?.4f:R(.6f,1.8f),releaseAt=release,settlesAt=100};
        if(!chip){element.powder=Powder(go.transform);element.cracks=Cracks(go);var dust=go.AddComponent<ConcreteImpactDust>();Ref(dust,"powder",element.powder);var sound=go.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=1;sound.volume=.30f;sound.minDistance=.25f;sound.maxDistance=5;sound.dopplerLevel=0;Ref(go.AddComponent<RubbleImpactAudio>(),"impactClip",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Art/BuriedThreat/HeavyConcreteCrash.wav"));rescue.Add(go.transform);}
        elements.Add(element);return chunk;
    }
    static void Fracture(string name,float at,int parts)
    {
        var original=GameObject.Find(name);var r=original.GetComponent<MeshRenderer>();var raw=UnityEngine.Object.Instantiate(original.GetComponent<MeshFilter>().sharedMesh);var center=original.transform.position;var matrix=original.transform.localToWorldMatrix;raw.vertices=raw.vertices.Select(v=>matrix.MultiplyPoint3x4(v)-center).ToArray();raw.normals=raw.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();raw.RecalculateBounds();var pieces=new List<Mesh>{raw};Vector3[] planes={new Vector3(1,.19f,.13f).normalized,new Vector3(.13f,1,.22f).normalized};
        for(int split=0;split<(parts==4?2:1);split++){var next=new List<Mesh>();foreach(var mesh in pieces){next.Add(Slice(mesh,planes[split],split==0?-.01f:.015f,true));next.Add(Slice(mesh,planes[split],split==0?-.01f:.015f,false));}pieces=next;}
        r.enabled=false;foreach(var c in original.GetComponents<Collider>())c.enabled=false;
        for(int i=0;i<pieces.Count;i++){var mesh=pieces[i];if(mesh.vertexCount<4)continue;var mats=r.sharedMaterials.ToList();while(mats.Count<mesh.subMeshCount)mats.Add(r.sharedMaterials[0]);var go=MeshObject(name+" broken section "+i,Store(mesh),mats.ToArray(),center);var direction=Vector3.ProjectOnPlane(Camera.main.transform.position-go.GetComponent<Renderer>().bounds.center,Vector3.up).normalized;Falling(go,direction*R(.35f,.70f)+Vector3.down*.08f,at+i*R(.11f,.27f));}
    }
    static void AppendRescue()
    {
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var so=new SerializedObject(ending);var rubble=so.FindProperty("rubble");var starts=so.FindProperty("removalStarts");var offsets=so.FindProperty("removalOffsets");var aperture=so.FindProperty("apertureBlockers");int count=0;
        foreach(var t in rescue.Distinct()){int i=rubble.arraySize;rubble.arraySize++;starts.arraySize++;offsets.arraySize++;rubble.GetArrayElementAtIndex(i).objectReferenceValue=t;starts.GetArrayElementAtIndex(i).floatValue=7f+(count%8)*5f;offsets.GetArrayElementAtIndex(i).vector3Value=new Vector3(t.position.x<0?-1.8f:1.8f,.48f,.8f);int a=aperture.arraySize;aperture.arraySize++;aperture.GetArrayElementAtIndex(a).objectReferenceValue=t.GetComponent<Renderer>();count++;}so.ApplyModifiedProperties();Refs(ending,"lightBlockers",GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Cast<UnityEngine.Object>());
    }
    static void Serialize(RubbleStructuralCollapse driver)
    {
        var phases=new HashSet<float>();int seed=0;foreach(var e in elements){while(!phases.Add(e.phase))e.phase+=100f+seed*1.713f;seed++;}
        Ref(driver,"aftershock",UnityEngine.Object.FindFirstObjectByType<AftershockDirector>());var data=new SerializedObject(driver);var array=data.FindProperty("elements");array.arraySize=elements.Count;for(int i=0;i<elements.Count;i++){var e=elements[i];var p=array.GetArrayElementAtIndex(i);p.FindPropertyRelative("target").objectReferenceValue=e.target;p.FindPropertyRelative("loose").objectReferenceValue=e.loose;p.FindPropertyRelative("phase").floatValue=e.phase;p.FindPropertyRelative("frequency").floatValue=e.frequency;p.FindPropertyRelative("distance").floatValue=e.distance;p.FindPropertyRelative("degrees").floatValue=e.degrees;p.FindPropertyRelative("compression").vector3Value=e.compression;p.FindPropertyRelative("settlesAt").floatValue=e.settlesAt;p.FindPropertyRelative("releaseAt").floatValue=e.releaseAt;p.FindPropertyRelative("powder").objectReferenceValue=e.powder;var c=p.FindPropertyRelative("cracks");c.arraySize=e.cracks.Length;for(int j=0;j<e.cracks.Length;j++)c.GetArrayElementAtIndex(j).objectReferenceValue=e.cracks[j];}data.ApplyModifiedProperties();
    }
    public static object Build()
    {
        if(Application.isPlaying||AssetDatabase.IsValidFolder(Art))throw new Exception("Edit mode and unapplied build required");AssetDatabase.CreateFolder("Assets/_Project/Art","Collapse360");root=new GameObject("Independent 360 structural collapse").transform;root.SetParent(GameObject.Find("Prototype Environment").transform,false);fallen=new GameObject("Persistent structural rubble").transform;fallen.SetParent(root,false);
        powderMaterial=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Atmosphere/ScatteringDustMotes.mat"));powderMaterial.SetFloat("_AmbientScatter",.012f);powderMaterial.SetFloat("_NearFade",.035f);powderMaterial.SetFloat("_ScatteringGain",.85f);AssetDatabase.CreateAsset(powderMaterial,Art+"/ConcreteFracturePowder.mat");crackMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/StructuralAftershock/ConcreteStressCrack.mat");
        // Remove the common rigid parent wave. Every visible renderer gets its own phase below.
        var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();Refs(motion,"roomTargets",Array.Empty<UnityEngine.Object>());Refs(motion,"shakeTargets",Array.Empty<UnityEngine.Object>());Refs(motion,"ceilingTargets",Array.Empty<UnityEngine.Object>());var md=new SerializedObject(motion);md.FindProperty("desktopShakeDegrees").floatValue=0;md.FindProperty("desktopShakeDistance").floatValue=0;md.ApplyModifiedProperties();
        GameObject.Find("Close buried cell").transform.position+=Vector3.forward*.10f;
        foreach(var name in new[]{"Near left wedged slab","Near right wedged slab"})GameObject.Find(name).transform.position+=Vector3.forward*.10f;
        Fracture("Front face left pressed rubble",2.28f,4);Fracture("Front face right interlocked masonry",4.06f,4);Fracture("Center crushed bearing stone",3.19f,2);
        foreach(var name in new[]{"Near left wedged slab","Near right wedged slab"}){var go=GameObject.Find(name);var mesh=UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);var copy=MeshObject(name+" failing support",Store(mesh),go.GetComponent<MeshRenderer>().sharedMaterials,go.transform.position);go.GetComponent<MeshRenderer>().enabled=false;foreach(var c in go.GetComponents<Collider>())c.enabled=false;Falling(copy,new Vector3(name.Contains("left")?.24f:-.24f,-.12f,-.4f),name.Contains("left")?5.82f:8.36f);}
        // The old unbroken cover hid all overhead action. Keep it archived and replace its surface with independent slabs.
        var roof=GameObject.Find("Low continuous overburden");roof.GetComponent<MeshRenderer>().enabled=false;foreach(var c in roof.GetComponents<Collider>())c.enabled=false;
        Vector3[] roofCenters={new Vector3(-.23f,1.265f,.27f),new Vector3(.22f,1.29f,.27f),new Vector3(-.22f,1.27f,-.19f),new Vector3(.23f,1.30f,-.22f)};float[] roofTimes={1.78f,6.17f,9.54f,12.86f};
        for(int i=0;i<4;i++){var go=Native(i%2==0?"Wall_Gray_Medium_02":"Wall_DarkGrey_Medium_01","Overhead breaking slab "+i,roofCenters[i],new Vector3(.53f,.15f,.57f),new Vector3(R(-5,5),R(-15,15),R(-4,4)));var velocity=new Vector3(i%2==0?-.10f:.10f,-.05f,i<2?-.16f:.12f);Falling(go,velocity,roofTimes[i]);}
        // Intact deeper backing stays present after the first ceiling breaks, and settles independently.
        for(int i=0;i<6;i++){float x=(i%3-1)*.48f,z=i<3?-.23f:.33f;var go=Native(i%3==0?"Bricks_Big_02":"Wall_Grey_Big_01","Upper compressed replacement mass "+i,new Vector3(x,1.66f+R(-.04f,.04f),z),new Vector3(.65f,.24f,.76f),new Vector3(R(-7,7),R(-25,25),R(-8,8)));rescue.Add(go.transform);}
        // The rear is a second tightly packed wall, with a low bottle recess rather than an open room.
        Native("Wall_Gray_Medium_03","Rear close load wall",new Vector3(.08f,.83f,-.64f),new Vector3(.79f,.20f,.53f),new Vector3(82,9,-7));
        Native("Bricks_Big_02","Rear left locked masonry",new Vector3(-.41f,.91f,-.52f),new Vector3(.40f,.19f,.61f),new Vector3(78,-22,15));
        Native("Wall_DarkGrey_Medium_01","Rear right locked concrete",new Vector3(.44f,.88f,-.50f),new Vector3(.43f,.19f,.63f),new Vector3(87,16,-12));
        Native("Wall_Grey_Small_02","Rear bottle recess roof",new Vector3(-.29f,.63f,-.58f),new Vector3(.40f,.13f,.27f),new Vector3(4,-8,5));
        Native("Bricks_Medium_01","Rear packed lower bearing",new Vector3(.16f,.38f,-.54f),new Vector3(.55f,.20f,.32f),new Vector3(21,31,-8));
        Vector3[] sideCenters={new Vector3(-.48f,1.17f,.06f),new Vector3(.49f,1.16f,.08f),new Vector3(-.30f,1.19f,-.48f),new Vector3(.30f,1.22f,-.47f),new Vector3(0,1.24f,-.57f)};float[] times={3.58f,7.41f,5.12f,10.79f,14.34f};
        for(int i=0;i<sideCenters.Length;i++){var go=Native(i%2==0?"Wall_Grey_Small_02":"Bricks_Medium_01","Close shoulder breakaway "+i,sideCenters[i],new Vector3(.33f,.15f,.28f),new Vector3(R(-15,15),R(0,180),R(-12,12)));var inward=Vector3.ProjectOnPlane(Camera.main.transform.position-sideCenters[i],Vector3.up).normalized;Falling(go,inward*.24f+Vector3.down*.08f,times[i]);}
        Vector3[] mouths={new Vector3(-.31f,1.22f,.34f),new Vector3(.31f,1.24f,.36f),new Vector3(-.43f,1.15f,.04f),new Vector3(.43f,1.15f,.06f),new Vector3(-.25f,1.18f,-.43f),new Vector3(.24f,1.19f,-.42f)};
        for(int source=0;source<mouths.Length;source++)for(int j=0;j<12;j++){var pos=mouths[source]+new Vector3(R(-.05f,.05f),R(-.015f,.03f),R(-.04f,.04f));var go=Native(j%4==0?"Bricks_Medium_01":"Wall_Grey_Small_02","360 joint spall "+source+" "+j,pos,new Vector3(R(.018f,.055f),R(.006f,.018f),R(.018f,.045f)),new Vector3(R(0,360),R(0,360),R(0,360)));var direction=Vector3.ProjectOnPlane(Camera.main.transform.position-pos,Vector3.up).normalized;Falling(go,direction*R(.12f,.38f)+Vector3.down*.15f,source<2?1.91f+source*.48f+j*.27f:times[source-2]+R(-.15f,.9f),true);}
        // Give each existing outer piece and previously authored fragment independent local motion.
        var configured=new HashSet<Transform>(elements.Select(e=>e.target));
        foreach(var r in GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray())
        {
            var b=r.bounds;if(configured.Contains(r.transform)||b.max.y<.22f||b.min.y>3.6f||Vector3.Distance(b.center,Camera.main.transform.position)>3.8f)continue;var loose=r.GetComponent<AftershockFallingChunk>();bool deep=r.transform.IsChildOf(GameObject.Find("Deep Layered Collapse").transform);var e=new RubbleStructuralCollapse.Element{target=r.transform,loose=loose,phase=R(0,150),frequency=R(21,49),distance=R(.003f,.010f),degrees=R(.25f,1.1f),releaseAt=-1,settlesAt=R(3,11),compression=loose!=null||deep?Vector3.zero:Vector3.down*R(.008f,.035f)};if(r.name.StartsWith("Upper compressed replacement")){e.compression=Vector3.down*R(.12f,.22f);e.settlesAt=R(3.3f,8.2f);}elements.Add(e);
        }
        var driver=root.gameObject.AddComponent<RubbleStructuralCollapse>();Serialize(driver);AppendRescue();Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{independentPieces=elements.Count,newFractures=elements.Count(e=>e.releaseAt>0&&e.powder!=null),spall=72,frontMoved=.10f,meshes=meshId};
    }
}

