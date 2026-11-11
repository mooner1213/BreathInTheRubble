using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using BreathInTheRubble.Prototype;
public static class BuildPressureCollapse
{
    public static object NormalizeContactAsset()
    {
        const string oldPath="Assets/_Project/Art/StructuralAftershock/LooseConcreteContact.physicsMaterial";
        const string path="Assets/_Project/Art/StructuralAftershock/LooseConcreteContact.physicMaterial";
        if(AssetDatabase.LoadMainAssetAtPath(oldPath)!=null){var error=AssetDatabase.MoveAsset(oldPath,path);if(!string.IsNullOrEmpty(error))throw new Exception(error);}
        var material=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);if(material==null)throw new Exception("Physics material failed to resolve");
        var chunks=GameObject.Find("Upper Joint Collapse").GetComponentsInChildren<AftershockFallingChunk>();foreach(var chunk in chunks)chunk.GetComponent<MeshCollider>().sharedMaterial=material;
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{asset=path,material.staticFriction,material.dynamicFriction,references=chunks.Length};
    }
    public static object CloseLandingPerimeter()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");int count=0;
        foreach(var group in GameObject.Find("Overlapping Side Collapse").transform.Cast<Transform>().Where(t=>t.name.StartsWith("Buried side layer 1 piece ")))
        foreach(var surface in group.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))
        {
            var shape=surface.GetComponent<MeshCollider>();if(shape==null)shape=surface.gameObject.AddComponent<MeshCollider>();shape.sharedMesh=surface.GetComponent<MeshFilter>().sharedMesh;shape.convex=false;shape.contactOffset=.001f;shape.enabled=true;count++;
        }
        foreach(var chunk in GameObject.Find("Upper Joint Collapse").GetComponentsInChildren<AftershockFallingChunk>())chunk.GetComponent<Rigidbody>().maxDepenetrationVelocity=.35f;
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{actualOuterRubbleColliders=count};
    }
    public static object SeatShowersAtUpperSeams()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");foreach(var ps in GameObject.Find("Compressed Upper Rubble").GetComponentsInChildren<ParticleSystem>()){var p=ps.transform.position;ps.transform.position=new Vector3(p.x,1.46f,p.z);}var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Four mesh showers start at the underside of the compressed upper rubble";
    }
    public static object FitFallChannels()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        Vector3[] centers={new Vector3(.75f,1.38f,.70f),new Vector3(-.75f,1.38f,.66f),new Vector3(-.68f,1.38f,-.50f),new Vector3(.68f,1.38f,-.50f)};
        for(int i=0;i<4;i++){var piece=GameObject.Find("Upper loose concrete "+(i+1));piece.transform.position+=centers[i]-BoundsOf(piece).center;var chunk=piece.GetComponentInChildren<AftershockFallingChunk>();var data=new SerializedObject(chunk);data.FindProperty("releaseVelocity").vector3Value=Vector3.zero;data.FindProperty("releaseAngularVelocity").vector3Value=new Vector3(.3f,.15f,-.25f);data.ApplyModifiedProperties();var b=BoundsOf(piece);GameObject.Find("Gravity chips from upper seam "+(i+1)).transform.position=new Vector3(b.center.x,b.min.y-.018f,b.center.z);}
        var catchRoot=new GameObject("Physical rubble landing beds");catchRoot.transform.SetParent(GameObject.Find("Prototype Environment").transform,false);int count=0;
        foreach(var center in new[]{new Vector3(-.85f,0,.65f),new Vector3(.85f,0,.7f),new Vector3(-.8f,0,-.5f),new Vector3(.8f,0,-.5f)})
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Piles/Rubble_Medium.prefab"),catchRoot.transform);go.name="Concrete and brick landing bed "+(++count);go.transform.rotation=Quaternion.Euler(0,count*71,0);foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;var lod=go.GetComponent<LODGroup>();var levels=lod.GetLODs();lod.enabled=false;for(int i=0;i<levels.Length;i++)foreach(var r in levels[i].renderers){r.enabled=i==0;r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();}
            var bounds=BoundsOf(go);go.transform.localScale*=1.0f/Mathf.Max(bounds.size.x,bounds.size.z);bounds=BoundsOf(go);go.transform.position+=new Vector3(center.x,.10f-bounds.extents.y,center.z)-bounds.center;
            foreach(var r in levels[0].renderers){var shape=r.GetComponent<MeshCollider>();if(shape==null)shape=r.gameObject.AddComponent<MeshCollider>();shape.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;shape.contactOffset=.001f;shape.enabled=true;}
        }
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{fallChannels=centers.Select(c=>c.ToString()).ToArray(),landingBeds=count,floorBounds=GameObject.Find("Floor Slab").GetComponent<Collider>().bounds.ToString()};
    }
    public static object LoosenUpperJoints()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        const string path="Assets/_Project/Art/StructuralAftershock/LooseConcreteContact.physicMaterial";
        var material=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);if(material==null){material=new PhysicsMaterial("Loose fractured concrete"){staticFriction=.32f,dynamicFriction=.27f,bounciness=.025f,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};AssetDatabase.CreateAsset(material,path);}
        var chunks=GameObject.Find("Upper Joint Collapse").GetComponentsInChildren<AftershockFallingChunk>();int index=0;
        foreach(var chunk in chunks){chunk.GetComponent<MeshCollider>().sharedMaterial=material;index++;}
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return new{loosenedUpperJoints=index};
    }
    static void Ref(UnityEngine.Object o,string field,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedProperties();}
    static void Refs(UnityEngine.Object o,string field,IEnumerable<UnityEngine.Object> values){var list=values.ToArray();var s=new SerializedObject(o);var a=s.FindProperty(field);a.arraySize=list.Length;for(int i=0;i<list.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=list[i];s.ApplyModifiedProperties();}
    static Bounds BoundsOf(GameObject o){var rs=o.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static GameObject Add(Transform parent,string asset,string name,Vector3 center,Vector3 angles,float width,bool collider)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Props/"+asset+".prefab"),parent);go.name=name;go.transform.rotation=Quaternion.Euler(angles);
        foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
        var lod=go.GetComponent<LODGroup>();var levels=lod.GetLODs();lod.enabled=false;
        for(int i=0;i<levels.Length;i++)foreach(var r in levels[i].renderers){r.enabled=i==0;r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;}
        var b=BoundsOf(go);go.transform.localScale*=width/Mathf.Max(b.size.x,b.size.z);b=BoundsOf(go);go.transform.position+=center-b.center;
        if(collider)foreach(var r in levels[0].renderers){var c=r.GetComponent<MeshCollider>();if(c==null)c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;c.contactOffset=.001f;c.enabled=true;}
        return go;
    }
    static void KeepShaftClear(GameObject go,Vector3 outward)
    {
        // Keep the existing individual rescue route open after its four slabs are removed.
        for(int attempt=0;attempt<24;attempt++){var b=BoundsOf(go);if(b.max.x<-.29f||b.min.x>.29f||b.max.z<-.07f||b.min.z>.61f)return;go.transform.position+=outward*.025f;}
    }
    public static object Run()
    {
        if(Application.isPlaying||GameObject.Find("Compressed Upper Rubble")!=null)throw new Exception("Edit mode / new setup required");
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var env=GameObject.Find("Prototype Environment");
        var root=new GameObject("Compressed Upper Rubble");root.transform.SetParent(GameObject.Find("Burial Roof Inner Layer").transform,true);
        var random=new System.Random(7613);float Rand(float min,float max)=>min+(float)random.NextDouble()*(max-min);int packed=0;var looseCandidates=new List<GameObject>();
        string[] assets={"Wall_Gray_Medium_02","Blocks_Big_01","Bricks_Big_02","Wall_DarkGrey_Medium_01","Blocks_Big_02","Wall_Gray_Medium_03"};
        // A close inner collar carries into an outer mass. Each column meets its next piece.
        for(int ring=0;ring<2;ring++)for(int column=0;column<14;column++)
        {
            float angle=(column+.23f*ring)*Mathf.PI*2/14;var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            var center=new Vector3(Mathf.Cos(angle)*(.61f+ring*.38f),0,.27f+Mathf.Sin(angle)*(.72f+ring*.37f));float top=1.48f+Rand(-.015f,.015f);
            for(int layer=0;layer<4;layer++)
            {
                bool loose=ring==0&&layer==0&&new[]{1,6,9,12}.Contains(column);
                var piece=Add(root.transform,loose?"Wall_Gray_Medium_0"+(looseCandidates.Count%3+1):assets[(column+layer*2+ring)%assets.Length],"Compressed bearing fragment "+(++packed),center+Vector3.up*top,new Vector3(Rand(-17,17),Rand(0,360),Rand(-15,15)),loose?.32f:Rand(.44f,.59f),true);
                KeepShaftClear(piece,outward);var b=BoundsOf(piece);piece.transform.position+=Vector3.up*(top-b.min.y-.007f);b=BoundsOf(piece);top=b.max.y;center=Vector3.Lerp(center,new Vector3(b.center.x,0,b.center.z),.65f);center+=outward*Rand(-.025f,.025f);
                if(loose)looseCandidates.Add(piece);
            }
        }
        // Loose existing chunks now release immediately, with no scripted pivot motion.
        foreach(var chunk in UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None))
        {
            var s=new SerializedObject(chunk);s.FindProperty("attachmentSeconds").floatValue=.065f;s.FindProperty("peelDegrees").floatValue=0;s.FindProperty("releaseVelocity").vector3Value=Vector3.zero;s.FindProperty("releaseAngularVelocity").vector3Value=new Vector3(.3f,.15f,-.25f);s.ApplyModifiedProperties();var body=chunk.GetComponent<Rigidbody>();body.linearDamping=.025f;body.angularDamping=.18f;body.maxDepenetrationVelocity=1.2f;
            if(chunk.transform.parent.name.Contains("Medium concrete breakaway 5")){var b=chunk.GetComponent<Renderer>().bounds;chunk.transform.parent.position+=new Vector3(.68f,1.34f,.52f)-b.center;}
        }
        var events=new GameObject("Upper Joint Collapse");events.transform.SetParent(env.transform,false);var detached=new GameObject("Settled Upper Fragments");detached.transform.SetParent(events.transform,false);
        var positions=looseCandidates.Select(p=>BoundsOf(p).center).ToArray();
        var oldRefs=new SerializedObject(director).FindProperty("fractureSections");var sections=Enumerable.Range(0,oldRefs.arraySize).Select(i=>oldRefs.GetArrayElementAtIndex(i).objectReferenceValue).ToList();
        var head=Camera.main.transform;var tracked=head.root.GetComponentsInChildren<Transform>(true);var protectedShapes=head.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).ToArray();
        for(int i=0;i<positions.Length;i++)
        {
            var sectionGo=new GameObject("Upper pressure joint "+(i+1));sectionGo.transform.SetParent(events.transform,false);sectionGo.transform.position=root.transform.position;
            var piece=looseCandidates[i];piece.name="Upper loose concrete "+(i+1);piece.transform.SetParent(sectionGo.transform,true);var surface=piece.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var shape=surface.GetComponent<MeshCollider>();shape.convex=true;var body=surface.gameObject.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.mass=1.4f;body.linearDamping=.025f;body.angularDamping=.18f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.maxDepenetrationVelocity=1.2f;
            var chunk=surface.gameObject.AddComponent<AftershockFallingChunk>();Ref(chunk,"detachedRoot",detached.transform);Ref(chunk,"head",head);Ref(chunk,"leftHand",tracked.First(t=>t.name=="Left Controller"));Ref(chunk,"rightHand",tracked.First(t=>t.name=="Right Controller"));Refs(chunk,"ignoreColliders",protectedShapes);
            var cs=new SerializedObject(chunk);cs.FindProperty("attachmentSeconds").floatValue=.05f;cs.FindProperty("releaseAngularVelocity").vector3Value=new Vector3(.5f,-.2f,.4f);cs.ApplyModifiedProperties();
            var audio=surface.gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.55f;audio.minDistance=.3f;audio.maxDistance=4;audio.dopplerLevel=0;Ref(surface.gameObject.AddComponent<RubbleImpactAudio>(),"impactClip",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Art/MetalPipe/PipeConcreteImpact.wav"));
            var section=sectionGo.AddComponent<AftershockFractureSection>();Ref(section,"wallAnchor",root.transform);Refs(section,"fallingChunks",new UnityEngine.Object[]{chunk});var ss=new SerializedObject(section);ss.FindProperty("opensAtSeconds").floatValue=new[]{3.35f,5.0f,6.85f,10.6f}[i];ss.ApplyModifiedProperties();sections.Add(section);
        }
        Refs(director,"fractureSections",sections);
        var showers=new List<ParticleSystem>();string[] chipAssets={"Brick_Small_03","Wall_Grey_Small_02","Block_Small_03","Brick_Small_11"};
        for(int i=0;i<4;i++)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Props/"+chipAssets[i]+".prefab");var sourceRenderer=source.GetComponent<LODGroup>().GetLODs()[0].renderers[0];
            var go=new GameObject("Gravity chips from upper seam "+(i+1));go.transform.SetParent(root.transform,false);go.transform.position=positions[i]+Vector3.up*.07f;go.transform.rotation=Quaternion.Euler(90,0,0);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=18;main.maxParticles=36;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(.55f,1.15f);main.startSpeed=new ParticleSystem.MinMaxCurve(.1f,.55f);main.startSize=new ParticleSystem.MinMaxCurve(.12f,.3f);main.startRotation3D=true;main.startRotationX=new ParticleSystem.MinMaxCurve(0,6.28f);main.startRotationY=new ParticleSystem.MinMaxCurve(0,6.28f);main.startRotationZ=new ParticleSystem.MinMaxCurve(0,6.28f);main.startColor=new Color(.72f,.68f,.61f,1);main.gravityModifier=.95f;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=19;shape.radius=.055f;var spin=ps.rotationOverLifetime;spin.enabled=true;spin.separateAxes=true;spin.x=new ParticleSystem.MinMaxCurve(-2,3);spin.y=new ParticleSystem.MinMaxCurve(-3,2);spin.z=new ParticleSystem.MinMaxCurve(-2,2);var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(.85f,1),new Keyframe(1,0)));
            var collision=ps.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;collision.quality=ParticleSystemCollisionQuality.Medium;collision.bounce=.12f;collision.dampen=.6f;collision.lifetimeLoss=.35f;collision.radiusScale=.6f;collision.maxCollisionShapes=64;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=sourceRenderer.GetComponent<MeshFilter>().sharedMesh;renderer.sharedMaterials=sourceRenderer.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;showers.Add(ps);
        }
        var rain=root.AddComponent<AftershockRubbleRain>();Ref(rain,"aftershock",director);Refs(rain,"joints",showers.Cast<UnityEngine.Object>());
        LoosenUpperJoints();FitFallChannels();SeatShowersAtUpperSeams();CloseLandingPerimeter();
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();Refs(ending,"lightBlockers",env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Cast<UnityEngine.Object>());
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{packedUpperFragments=packed,largeGravityChunks=9,upperMeshShowers=4,scriptedRotation=false};
    }
}
