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

public static class BuildAvalanche
{
    const string Art="Assets/_Project/Art/AftershockAvalanche";
    static void Folder(){if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/_Project/Art","AftershockAvalanche");}
    static void Save(){var s=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();}
    static void Ref(UnityEngine.Object o,string field,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedProperties();}
    static void Refs(UnityEngine.Object o,string field,IEnumerable<UnityEngine.Object> values){var a=values.ToArray();var s=new SerializedObject(o);var p=s.FindProperty(field);p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];s.ApplyModifiedProperties();}
    static void Number(UnityEngine.Object o,string field,float value){var s=new SerializedObject(o);s.FindProperty(field).floatValue=value;s.ApplyModifiedProperties();}
    static void Vector(UnityEngine.Object o,string field,Vector3 value){var s=new SerializedObject(o);s.FindProperty(field).vector3Value=value;s.ApplyModifiedProperties();}
    static GameObject Piece(Transform parent,string prefab,string name,Vector3 center,Vector3 size,Vector3 angles,string meshName,bool collision=false)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/"+prefab+".prefab");
        var source=asset.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var src=source.GetComponent<MeshFilter>().sharedMesh;var mesh=UnityEngine.Object.Instantiate(src);
        var b=src.bounds;var matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(angles),new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z))*Matrix4x4.Translate(-b.center);
        var verts=src.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();mesh.vertices=verts;mesh.normals=src.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();var offset=mesh.bounds.center;mesh.vertices=verts.Select(v=>v-offset).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name=meshName;
        var path=Art+"/"+meshName+".asset";if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("Asset already exists: "+path);AssetDatabase.CreateAsset(mesh,path);
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=center;go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")??throw new Exception("Missing URP material "+m.name)).ToArray();renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
        if(collision){var shape=go.AddComponent<MeshCollider>();shape.sharedMesh=mesh;shape.contactOffset=.001f;}
        return go;
    }
    public static object FixPipe()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");Folder();
        if(GameObject.Find("Pipe underside bearing notch")!=null)throw new Exception("Notch already exists");
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var data=new SerializedObject(pipe);
        var fields=new[]{"stowedPose","lipPose","releasePose"};var positions=new[]{new Vector3(-.06f,1.075f,.58f),new Vector3(-.06f,1.075f,.44f),new Vector3(-.045f,.96f,.37f)};
        for(int i=0;i<3;i++){var t=(Transform)data.FindProperty(fields[i]).objectReferenceValue;t.SetPositionAndRotation(positions[i],Quaternion.Euler(0,12,90));}
        data.FindProperty("slideSeconds").floatValue=1.25f;data.FindProperty("tipSeconds").floatValue=.33f;data.FindProperty("fallSeconds").floatValue=.38f;data.FindProperty("settleSeconds").floatValue=.30f;data.ApplyModifiedProperties();
        pipe.transform.SetPositionAndRotation(positions[0],Quaternion.Euler(0,12,90));var body=pipe.GetComponent<Rigidbody>();body.position=pipe.transform.position;body.rotation=pipe.transform.rotation;
        var root=new GameObject("Pipe underside bearing notch");root.transform.SetParent(GameObject.Find("Interlocked Rubble Structure").transform,false);
        for(int i=0;i<2;i++)
        {
            var go=Piece(root.transform,"Props/Wall_Grey_Small_02","Broken pipe bearing lip "+(i+1),new Vector3(i==0?-.29f:.17f,1.0f,.69f),new Vector3(.13f,.115f,.29f),Vector3.zero,"PipeNotch_"+i,true);
            var b=go.GetComponent<Renderer>().bounds;go.transform.position+=Vector3.up*(1.059f-b.max.y);
        }
        Save();return new{stowed=positions[0].ToString("F3"),ceilingCrossingRemoved=true,availableAt=pipe.AvailableAtSeconds};
    }
    public static object Collapse()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");Folder();
        if(GameObject.Find("Aftershock Avalanche")!=null)throw new Exception("Already configured");
        var env=GameObject.Find("Prototype Environment");var root=new GameObject("Aftershock Avalanche");root.transform.SetParent(env.transform,false);
        var settled=new GameObject("Settled avalanche chunks");settled.transform.SetParent(root.transform,false);
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var ds=new SerializedObject(director).FindProperty("fractureSections");var sections=Enumerable.Range(0,ds.arraySize).Select(i=>ds.GetArrayElementAtIndex(i).objectReferenceValue).ToList();
        var head=Camera.main.transform;var tracked=head.root.GetComponentsInChildren<Transform>(true);
        var protectedShapes=head.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).ToArray();
        var powder=GameObject.Find("Fracture Dust Plume 1").GetComponent<ParticleSystem>();
        var chunks=new List<AftershockFallingChunk>();
        void Attach(GameObject piece,float time,int index,bool warning)
        {
            var group=new GameObject("Avalanche fracture "+index);group.transform.SetParent(root.transform,false);piece.transform.SetParent(group.transform,true);
            var shape=piece.GetComponent<MeshCollider>();if(shape==null)shape=piece.AddComponent<MeshCollider>();shape.sharedMesh=piece.GetComponent<MeshFilter>().sharedMesh;shape.convex=true;shape.contactOffset=.001f;shape.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Art/BuriedThreat/ForegroundConcreteContact.physicMaterial");
            var body=piece.AddComponent<Rigidbody>();body.mass=index%3==0?8:3;body.useGravity=false;body.isKinematic=true;body.linearDamping=.06f;body.angularDamping=.5f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.maxDepenetrationVelocity=.3f;
            var chunk=piece.AddComponent<AftershockFallingChunk>();Ref(chunk,"detachedRoot",settled.transform);Ref(chunk,"head",head);Ref(chunk,"leftHand",tracked.First(t=>t.name=="Left Controller"));Ref(chunk,"rightHand",tracked.First(t=>t.name=="Right Controller"));
            Refs(chunk,"ignoreColliders",protectedShapes.Concat(env.GetComponentsInChildren<Collider>(true).Where(c=>c!=shape&&c.enabled&&c.bounds.min.y>.87f)).Cast<UnityEngine.Object>());
            Number(chunk,"attachmentSeconds",.05f);Vector(chunk,"releaseAngularVelocity",new Vector3(.22f*(index%2==0?1:-1),.12f,-.28f));
            var audio=piece.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.32f;audio.minDistance=.4f;audio.maxDistance=4;audio.dopplerLevel=0;
            Ref(piece.AddComponent<RubbleImpactAudio>(),"impactClip",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Art/BuriedThreat/HeavyConcreteCrash.wav"));
            var dust=UnityEngine.Object.Instantiate(powder,group.transform);dust.name="Avalanche fracture powder "+index;dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);dust.transform.position=piece.GetComponent<Renderer>().bounds.center;
            var main=dust.main;main.playOnAwake=false;main.maxParticles=12;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.7f);main.startSize=new ParticleSystem.MinMaxCurve(.03f,.08f);main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=dust.emission;emission.enabled=false;
            var section=group.AddComponent<AftershockFractureSection>();Refs(section,"fallingChunks",new[]{chunk});Ref(section,"fractureDust",dust);Number(section,"opensAtSeconds",time);
            var ss=new SerializedObject(section);ss.FindProperty("releaseDustCount").intValue=5;ss.ApplyModifiedProperties();sections.Add(section);chunks.Add(chunk);
            if(warning)
            {
                var host=new GameObject("Loaded avalanche joint "+index);host.transform.SetParent(group.transform,false);host.transform.position=dust.transform.position;var strain=host.AddComponent<AudioSource>();strain.playOnAwake=false;strain.spatialBlend=1;strain.volume=.32f;strain.minDistance=.4f;strain.maxDistance=4;strain.dopplerLevel=0;
                var cue=group.AddComponent<AftershockLoadWarning>();Ref(cue,"aftershock",director);Ref(cue,"loadedPiece",piece.transform);Ref(cue,"fallingChunk",chunk);Ref(cue,"jointDust",dust);Ref(cue,"strainSource",strain);Ref(cue,"strainClip",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Art/BuriedThreat/LoadedConcreteStrain.wav"));Number(cue,"startsAtSeconds",Mathf.Max(.1f,time-1.1f));Number(cue,"warningSeconds",1.1f);Vector(cue,"slipOffset",new Vector3(0,-.02f,0));Vector(cue,"tiltDegrees",new Vector3(0,0,index%2==0?3:-3));
            }
        }
        // Eight visible bearing pieces become the falling pieces themselves.
        var infill=GameObject.Find("Foreground Load Failures").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.transform.parent.name.StartsWith("Compressed roof edge infill")).OrderBy(r=>r.transform.parent.name).ToArray();
        for(int i=0;i<infill.Length;i++)
        {
            var source=infill[i];var center=source.bounds.center;var mesh=UnityEngine.Object.Instantiate(source.GetComponent<MeshFilter>().sharedMesh);var matrix=Matrix4x4.Translate(-center)*source.transform.localToWorldMatrix;
            mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();mesh.normals=mesh.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name="Released bearing masonry "+i;AssetDatabase.CreateAsset(mesh,Art+"/ReleasedBearing_"+i+".asset");
            var piece=new GameObject("Falling roof masonry "+(i+1));piece.transform.SetParent(root.transform,false);piece.transform.position=center;piece.AddComponent<MeshFilter>().sharedMesh=mesh;piece.AddComponent<MeshRenderer>().sharedMaterials=source.sharedMaterials;
            source.enabled=false;foreach(var shape in source.GetComponents<Collider>())shape.enabled=false;
            Attach(piece,new[]{3.20f,3.42f,4.80f,4.98f,6.60f,6.84f,9.20f,10.20f}[i],i,false);
        }
        var centers=new[]{new Vector3(-.85f,1.22f,.95f),new Vector3(.78f,1.23f,.99f),new Vector3(.16f,1.32f,1.28f),new Vector3(-.67f,1.25f,1.28f),new Vector3(.86f,1.30f,.20f),new Vector3(-.86f,1.28f,-.18f),new Vector3(.82f,1.27f,-.32f),new Vector3(-.15f,1.39f,1.42f),new Vector3(.62f,1.36f,1.37f)};
        var sizes=new[]{new Vector3(.50f,.13f,.36f),new Vector3(.60f,.14f,.40f),new Vector3(.64f,.18f,.38f),new Vector3(.58f,.14f,.42f),new Vector3(.48f,.14f,.42f),new Vector3(.45f,.13f,.40f),new Vector3(.56f,.15f,.45f),new Vector3(.74f,.17f,.43f),new Vector3(.60f,.15f,.39f)};
        float[] times={3.28f,3.36f,4.82f,4.92f,5.10f,6.60f,6.70f,9.20f,10.30f};
        for(int i=0;i<centers.Length;i++){var go=Piece(root.transform,i%3==0?"Props/Wall_Gray_Medium_03":"Props/Wall_Grey_Big_01","Avalanche concrete slab "+(i+1),centers[i],sizes[i],new Vector3(i%2==0?7:-8,17+i*21,i%2==0?4:-5),"AvalancheSlab_"+i);Attach(go,times[i],8+i,i==2||i==4||i==7);}
        Refs(director,"fractureSections",sections);
        // Keep neighboring released masonry from ejecting each other at shared attachment edges.
        foreach(var chunk in chunks){var so=new SerializedObject(chunk);var p=so.FindProperty("ignoreColliders");var old=Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue);Refs(chunk,"ignoreColliders",old.Concat(chunks.Where(c=>c!=chunk).Select(c=>(UnityEngine.Object)c.GetComponent<Collider>())).Distinct());}
        AddSmallDebris(root.transform);AddShowers(root.transform);
        Number(UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>(),"collapseDensity",.78f);
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();Refs(ending,"lightBlockers",env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy));
        Save();return new{newFallingMasonry=chunks.Count,totalLargeChunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).Length,smallPhysicalFragments=84,meshShowerJoints=10,burstWaves=30,collapseFogDensity=.78f};
    }
    static void AddSmallDebris(Transform root)
    {
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();var data=new SerializedObject(debris);var p=data.FindProperty("fragments");var originals=Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue).ToList();
        var oldTimes=data.FindProperty("releaseTimes");var times=Enumerable.Range(0,oldTimes.arraySize).Select(i=>oldTimes.GetArrayElementAtIndex(i).floatValue).ToList();
        Mesh[] meshes=new Mesh[3];Material[][] materials=new Material[3][];
        for(int j=0;j<3;j++)
        {
            var template=Piece(root,new[]{"Props/Brick_Small_03","Props/Wall_Grey_Small_02","Props/Block_Small_03"}[j],"Chip source "+j,new Vector3(0,-5,0),new Vector3(.08f+j*.02f,.04f+j*.012f,.06f+j*.02f),Vector3.zero,"AvalancheChip_"+j);
            meshes[j]=template.GetComponent<MeshFilter>().sharedMesh;materials[j]=template.GetComponent<MeshRenderer>().sharedMaterials;template.SetActive(false);
        }
        float[] waves={2.5f,3.08f,3.20f,3.38f,3.58f,4.60f,4.80f,5.0f,6.40f,6.60f,6.80f,9.1f,10.15f,13.10f};
        for(int i=0;i<56;i++)
        {
            int side=i%2==0?-1:1;var go=new GameObject("Loose avalanche chip "+(i+1));go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(new Vector3(side*(.53f+(i%3)*.055f),1.18f+(i%4)*.025f,.08f+(i%7)*.12f),Quaternion.Euler(i*31,i*73,i*43));go.AddComponent<MeshFilter>().sharedMesh=meshes[i%3];go.AddComponent<MeshRenderer>().sharedMaterials=materials[i%3];originals.Add(go.transform);times.Add(waves[i/4]+(i%4)*.022f);
        }
        Refs(debris,"fragments",originals);data=new SerializedObject(debris);p=data.FindProperty("releaseTimes");p.arraySize=times.Count;for(int i=0;i<times.Count;i++)p.GetArrayElementAtIndex(i).floatValue=times[i];data.ApplyModifiedProperties();
    }
    static void AddShowers(Transform root)
    {
        var rain=UnityEngine.Object.FindFirstObjectByType<AftershockRubbleRain>();var data=new SerializedObject(rain);var p=data.FindProperty("joints");var systems=Enumerable.Range(0,p.arraySize).Select(i=>(ParticleSystem)p.GetArrayElementAtIndex(i).objectReferenceValue).ToList();var template=systems[0];
        var centers=new[]{new Vector3(-.66f,1.20f,.93f),new Vector3(.12f,1.30f,1.04f),new Vector3(.65f,1.20f,.93f),new Vector3(-.70f,1.22f,.12f),new Vector3(.70f,1.22f,.13f),new Vector3(.05f,1.25f,-.69f)};
        for(int i=0;i<centers.Length;i++){var ps=UnityEngine.Object.Instantiate(template,root);ps.name="Dense gravity shower "+(i+1);ps.transform.position=centers[i];systems.Add(ps);}
        foreach(var ps in systems){ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.maxParticles=72;main.startLifetime=new ParticleSystem.MinMaxCurve(.75f,1.5f);main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,.6f);main.startSize=new ParticleSystem.MinMaxCurve(.16f,.39f);main.gravityModifier=1;var shape=ps.shape;shape.radius=.075f;shape.angle=16;var emission=ps.emission;emission.enabled=false;}
        Refs(rain,"joints",systems);data=new SerializedObject(rain);data.FindProperty("chipsPerJoint").intValue=8;
        float[] waves={2.40f,2.78f,3.05f,3.20f,3.38f,3.58f,3.84f,4.28f,4.62f,4.80f,4.98f,5.22f,5.58f,6.12f,6.40f,6.60f,6.82f,7.08f,7.44f,8.20f,9.20f,10.20f,10.45f,11.50f,12.80f,13.40f,13.70f,14.60f,15.50f,16.20f};
        p=data.FindProperty("burstTimes");p.arraySize=waves.Length;for(int i=0;i<waves.Length;i++)p.GetArrayElementAtIndex(i).floatValue=waves[i];data.ApplyModifiedProperties();
    }
    public static object FinishLandingAndShowers()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var root=GameObject.Find("Aftershock Avalanche").transform;
        if(GameObject.Find("Buried avalanche foundation")!=null)throw new Exception("Foundation already exists");
        var foundation=new GameObject("Buried avalanche foundation");foundation.transform.SetParent(root,false);
        var centers=new[]{new Vector3(-.80f,0,.05f),new Vector3(.80f,0,.05f),new Vector3(0,0,1.25f),new Vector3(0,0,-.80f)};
        for(int i=0;i<centers.Length;i++)Piece(foundation.transform,"Props/Wall_Grey_Big_01","Buried receiving concrete plate "+i,centers[i],i<2?new Vector3(1.4f,.12f,1.7f):new Vector3(1.9f,.12f,1.3f),new Vector3(0,i*19,0),"ReceivingFloor_"+i,true);
        var rain=UnityEngine.Object.FindFirstObjectByType<AftershockRubbleRain>();var data=new SerializedObject(rain);var p=data.FindProperty("joints");
        for(int i=0;i<p.arraySize;i++)
        {
            var ps=(ParticleSystem)p.GetArrayElementAtIndex(i).objectReferenceValue;var position=ps.transform.position;position.y=1.09f+(i%3)*.025f;ps.transform.position=position;
            var main=ps.main;main.startSize=new ParticleSystem.MinMaxCurve(.28f,.60f);main.startLifetime=new ParticleSystem.MinMaxCurve(.85f,1.55f);
        }
        data.FindProperty("chipsPerJoint").intValue=10;data.ApplyModifiedProperties();
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();Refs(ending,"lightBlockers",GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy));
        Save();return new{visibleMeshReceivingPlates=4,showerChipsPerJoint=10,source="underside of visible rubble, below the closed canopy"};
    }
}
