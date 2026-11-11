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

public static class BuildBuriedPocket
{
    const string Art="Assets/_Project/Art/BuriedThreat";
    static System.Random random=new System.Random(9173);
    static float Range(float a,float b)=>(float)(a+random.NextDouble()*(b-a));
    public static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var b=rs[0].bounds;
        foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    public static GameObject Add(Transform parent,string asset,string name,Vector3 center,Vector3 angles,Vector3 size,bool collision=false)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/"+asset+".prefab");
        if(prefab==null)throw new Exception("Missing prefab "+asset);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name=name;
        foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
        var lod=go.GetComponent<LODGroup>();var levels=lod.GetLODs();lod.enabled=false;
        for(int i=0;i<levels.Length;i++)foreach(var r in levels[i].renderers)
        {
            r.enabled=i==0;
            r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")??throw new Exception("Missing URP "+m.name)).ToArray();
            r.shadowCastingMode=ShadowCastingMode.TwoSided;
        }
        var b=BoundsOf(go);go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));
        go.transform.rotation=Quaternion.Euler(angles);b=BoundsOf(go);go.transform.position+=center-b.center;
        if(collision)foreach(var r in levels[0].renderers)
        {
            var c=r.GetComponent<MeshCollider>();if(c==null)c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;c.convex=false;c.contactOffset=.001f;c.enabled=true;
        }
        return go;
    }
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("Wrong scene");
        if(GameObject.Find("Close Packed Survival Pocket")!=null)throw new Exception("Pocket already exists; do not duplicate");
        if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/_Project/Art","BuriedThreat");
        var root=new GameObject("Close Packed Survival Pocket");root.transform.SetParent(GameObject.Find("Prototype Environment").transform,false);
        var ground=new GameObject("Crushed aggregate and masonry floor");ground.transform.SetParent(root.transform,false);
        // Sloped masses rise towards the front wall; the item shelf and pipe landing remain exposed.
        Add(root.transform,"Piles/Rubble_Medium","Front compacted landing bank",new Vector3(-.19f,.21f,.91f),new Vector3(0,19,0),new Vector3(1.55f,.44f,.91f),true);
        Add(root.transform,"Piles/Rubble_Small_01","Left shoulder packed backing",new Vector3(-.94f,.76f,.20f),new Vector3(0,12,-76),new Vector3(1.25f,.43f,1.25f));
        Add(root.transform,"Piles/Rubble_Small_01","Right shoulder packed backing",new Vector3(.99f,.76f,.20f),new Vector3(0,-22,78),new Vector3(1.25f,.43f,1.27f));
        Add(root.transform,"Piles/Rubble_Medium","Front upper compressed masonry",new Vector3(.03f,.52f,1.04f),new Vector3(-26,17,-5),new Vector3(1.48f,.42f,.94f));
        Add(root.transform,"Piles/Rubble_Small_01","Rear pocket packed backing",new Vector3(.05f,.89f,-.91f),new Vector3(80,23,0),new Vector3(1.4f,.38f,.95f));
        // Small pieces seat against visible rubble with a raycast, rather than floating over a flat floor.
        Physics.SyncTransforms();
        string[] chips={"Props/Brick_Small_03","Props/Block_Small_03","Props/Wall_Grey_Small_02","Props/Brick_Small_11"};
        int seated=0;
        for(int i=0;i<58;i++)
        {
            float x=Range(-.79f,.81f),z=Range(-.59f,.92f);
            if(Mathf.Abs(x)<.28f&&z>-.22f&&z<.38f)continue;
            if((new Vector2(x-.43f,z-.17f)).magnitude<.20f||(new Vector2(x+.28f,z+.43f)).magnitude<.17f)continue;
            var hits=Physics.RaycastAll(new Vector3(x,.47f,z),Vector3.down,.65f).Where(h=>h.collider.transform.IsChildOf(GameObject.Find("Prototype Environment").transform)).OrderByDescending(h=>h.point.y).ToArray();
            float y=hits.Length>0?hits[0].point.y: .12f;
            float length=Range(.08f,.19f);
            var go=Add(ground.transform,chips[i%chips.Length],"Settled crushed floor fragment "+(++seated),new Vector3(x,y+.025f,z),new Vector3(Range(-12,12),Range(0,360),Range(-9,9)),new Vector3(length,.035f,length*.73f));
            var b=BoundsOf(go);go.transform.position+=Vector3.up*(y-b.min.y-.008f);
        }
        // Angled reinforcement ties the near rubble to the load above, rather than reading as a room wall.
        Add(root.transform,"WithRebar/Rebar_Wall_08","Left crushed diagonal reinforcement",new Vector3(-.73f,.94f,.83f),new Vector3(48,-19,22),new Vector3(.24f,.17f,1.12f));
        Add(root.transform,"WithRebar/Rebar_Wall_08","Right wedged reinforcement",new Vector3(.80f,1.02f,.78f),new Vector3(39,34,-26),new Vector3(.19f,.16f,.96f));
        Add(root.transform,"Props/Bricks_Big_02","Broken front masonry ledge",new Vector3(-.45f,.45f,1.02f),new Vector3(-15,27,13),new Vector3(.64f,.18f,.44f));
        CombineGround(ground);
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var so=new SerializedObject(ending);var blockers=so.FindProperty("lightBlockers");
        var surfaces=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        blockers.arraySize=surfaces.Length;for(int i=0;i<surfaces.Length;i++)blockers.GetArrayElementAtIndex(i).objectReferenceValue=surfaces[i];so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return new{seatedFloorFragments=seated,newRenderers=root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled),protectedPhoneAndBottlePockets=true};
    }
    static void CombineGround(GameObject root)
    {
        var sources=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
        var groups=new Dictionary<Material,List<CombineInstance>>();
        foreach(var r in sources){var mesh=r.GetComponent<MeshFilter>().sharedMesh;var materials=r.sharedMaterials;
            for(int i=0;i<mesh.subMeshCount;i++){var mat=materials[Mathf.Min(i,materials.Length-1)];if(!groups.ContainsKey(mat))groups[mat]=new List<CombineInstance>();groups[mat].Add(new CombineInstance{mesh=mesh,subMeshIndex=i,transform=root.transform.worldToLocalMatrix*r.transform.localToWorldMatrix});}}
        int index=0;foreach(var entry in groups){var mesh=new Mesh{name="Crushed floor aggregate "+(++index),indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(entry.Value.ToArray(),true,true);AssetDatabase.CreateAsset(mesh,Art+"/CrushedFloor_"+index+".asset");var go=new GameObject(mesh.name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=entry.Key;}
        foreach(var r in sources)r.enabled=false;
    }
    static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedProperties();}
    static void Number(UnityEngine.Object target,string field,float value){var s=new SerializedObject(target);s.FindProperty(field).floatValue=value;s.ApplyModifiedProperties();}
    static void Refs(UnityEngine.Object target,string field,IEnumerable<UnityEngine.Object> values){var a=values.ToArray();var s=new SerializedObject(target);var p=s.FindProperty(field);p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];s.ApplyModifiedProperties();}
    public static object Threats()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        if(GameObject.Find("Foreground Load Failures")!=null)throw new Exception("Threats already configured");
        WriteCue("LoadedConcreteStrain",2.5f,false);WriteCue("HeavyConcreteCrash",1.5f,true);
        var bank=GameObject.Find("Front upper compressed masonry");bank.transform.SetPositionAndRotation(new Vector3(.03f,.52f,1.04f),Quaternion.Euler(-26,17,-5));
        var env=GameObject.Find("Prototype Environment");var root=new GameObject("Foreground Load Failures");root.transform.SetParent(env.transform,false);
        var detached=new GameObject("Settled foreground concrete");detached.transform.SetParent(root.transform,false);
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var ds=new SerializedObject(director).FindProperty("fractureSections");var sections=Enumerable.Range(0,ds.arraySize).Select(i=>ds.GetArrayElementAtIndex(i).objectReferenceValue).ToList();
        var head=Camera.main.transform;var tracked=head.root.GetComponentsInChildren<Transform>(true);
        var protectedColliders=head.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).ToArray();
        var template=GameObject.Find("Fracture Dust Plume 1").GetComponent<ParticleSystem>();
        var chunks=new List<AftershockFallingChunk>();
        for(int i=0;i<2;i++)
        {
            float release=i==0?3.2f:6.6f;
            var group=new GameObject("Foreground fracture "+(i+1));group.transform.SetParent(root.transform,false);
            var piece=Add(group.transform,"Props/Wall_Grey_Big_01","Foreground hanging slab "+(i+1),i==0?new Vector3(-.25f,1.23f,.99f):new Vector3(.64f,1.28f,.62f),i==0?new Vector3(9,-17,-7):new Vector3(-13,31,17),i==0?new Vector3(.73f,.21f,.45f):new Vector3(.51f,.18f,.43f));
            var surface=piece.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var leaf=surface.gameObject;
            var shape=leaf.GetComponent<MeshCollider>();if(shape==null)shape=leaf.AddComponent<MeshCollider>();shape.sharedMesh=leaf.GetComponent<MeshFilter>().sharedMesh;shape.convex=true;shape.enabled=true;shape.contactOffset=.001f;
            shape.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Art/StructuralAftershock/LooseConcreteContact.physicMaterial");
            var body=leaf.AddComponent<Rigidbody>();body.mass=i==0?16:9;body.isKinematic=true;body.useGravity=false;body.linearDamping=.08f;body.angularDamping=.6f;body.maxDepenetrationVelocity=.35f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var chunk=leaf.AddComponent<AftershockFallingChunk>();Ref(chunk,"detachedRoot",detached.transform);Ref(chunk,"head",head);Ref(chunk,"leftHand",tracked.First(t=>t.name=="Left Controller"));Ref(chunk,"rightHand",tracked.First(t=>t.name=="Right Controller"));
            var upperShapes=env.GetComponentsInChildren<Collider>(true).Where(c=>c!=shape&&c.enabled&&c.bounds.min.y>.85f);
            Refs(chunk,"ignoreColliders",protectedColliders.Concat(upperShapes).Cast<UnityEngine.Object>());
            Number(chunk,"attachmentSeconds",.06f);
            var cs=new SerializedObject(chunk);cs.FindProperty("releaseAngularVelocity").vector3Value=new Vector3(i==0?.8f:-.6f,.15f,i==0?-.5f:.55f);cs.ApplyModifiedProperties();chunks.Add(chunk);
            var impact=leaf.AddComponent<AudioSource>();impact.playOnAwake=false;impact.spatialBlend=1;impact.volume=.8f;impact.minDistance=.5f;impact.maxDistance=5;impact.dopplerLevel=0;
            Ref(leaf.AddComponent<RubbleImpactAudio>(),"impactClip",AssetDatabase.LoadAssetAtPath<AudioClip>(Art+"/HeavyConcreteCrash.wav"));
            var dust=UnityEngine.Object.Instantiate(template,group.transform);dust.name="Foreground joint concrete powder "+(i+1);dust.transform.position=surface.bounds.center+Vector3.up*.04f;
            dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=dust.main;main.playOnAwake=false;main.maxParticles=30;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.8f);main.startSize=new ParticleSystem.MinMaxCurve(.025f,.09f);main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=dust.emission;emission.enabled=false;
            var section=group.AddComponent<AftershockFractureSection>();Refs(section,"fallingChunks",new[]{chunk});Ref(section,"fractureDust",dust);Number(section,"opensAtSeconds",release);sections.Add(section);
            var strainGo=new GameObject("Audible concrete strain "+(i+1));strainGo.transform.SetParent(group.transform,false);strainGo.transform.position=surface.bounds.center;
            var audio=strainGo.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.5f;audio.minDistance=.4f;audio.maxDistance=4;audio.dopplerLevel=0;
            var warning=group.AddComponent<AftershockLoadWarning>();Ref(warning,"aftershock",director);Ref(warning,"loadedPiece",piece.transform);Ref(warning,"fallingChunk",chunk);Ref(warning,"jointDust",dust);Ref(warning,"strainSource",audio);Ref(warning,"strainClip",AssetDatabase.LoadAssetAtPath<AudioClip>(Art+"/LoadedConcreteStrain.wav"));Number(warning,"startsAtSeconds",release-2.3f);Number(warning,"warningSeconds",2.3f);
        }
        Refs(director,"fractureSections",sections);
        // Tie the canopy to visible masonry at each edge; preserve the central extraction shaft.
        for(int side=-1;side<=1;side+=2)for(int j=0;j<4;j++)
            Add(root.transform,"Props/Bricks_Big_02","Compressed roof edge infill "+side+" "+j,new Vector3(side*.69f,1.40f,.03f+j*.28f),new Vector3(12,j*43,side*9),new Vector3(.31f,.16f,.35f));
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();Refs(ending,"lightBlockers",env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy));
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return new{foregroundSlabs=chunks.Count,releaseSeconds=new[]{3.26f,6.66f},totalFractures=sections.Count,localWarningSeconds=2.3f};
    }
    static void WriteCue(string name,float seconds,bool crash)
    {
        var path=Art+"/"+name+".wav";if(File.Exists(path))throw new Exception("Do not replace existing audio "+path);
        const int rate=22050;int count=(int)(rate*seconds);var rng=new System.Random(crash?210:918);float low=0;
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate;float noise=(float)rng.NextDouble()*2-1;low=Mathf.Lerp(low,noise,.075f);
                float envelope=crash?Mathf.Exp(-t*3.8f):Mathf.Pow(Mathf.Clamp01(t/seconds),.55f)*(.5f+.5f*Mathf.Sin(t*21)*Mathf.Sin(t*21));
                float resonant=Mathf.Sin(t*(crash?51:91)*Mathf.PI*2)*.2f+Mathf.Sin(t*137*Mathf.PI*2)*.08f;
                float grit=crash?noise*.27f:noise*.08f*Mathf.Pow(Mathf.Abs(Mathf.Sin(t*37)),12);
                float sample=(low*.8f+resonant+grit)*envelope*Mathf.Clamp01(t/.006f)*Mathf.Clamp01((seconds-t)/.025f);
                writer.Write((short)(Mathf.Clamp(sample,-.85f,.85f)*32767));
            }
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(AudioImporter)AssetImporter.GetAtPath(path);importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
    }
    public static object SeatLanding()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var root=GameObject.Find("Foreground Load Failures");
        const string path=Art+"/ForegroundConcreteContact.physicMaterial";
        var material=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if(material==null){material=new PhysicsMaterial("Foreground concrete friction"){staticFriction=.95f,dynamicFriction=.85f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Maximum};AssetDatabase.CreateAsset(material,path);}
        var piece=GameObject.Find("Foreground hanging slab 1");piece.transform.position+=new Vector3(-.25f,1.23f,.99f)-BoundsOf(piece).center;
        GameObject.Find("Audible concrete strain 1").transform.position=new Vector3(-.25f,1.23f,.99f);
        GameObject.Find("Foreground joint concrete powder 1").transform.position=new Vector3(-.25f,1.32f,1.06f);
        var chunk=piece.GetComponentInChildren<AftershockFallingChunk>();var s=new SerializedObject(chunk);s.FindProperty("releaseAngularVelocity").vector3Value=new Vector3(.25f,.1f,-.25f);s.ApplyModifiedProperties();chunk.GetComponent<MeshCollider>().sharedMaterial=material;
        if(GameObject.Find("Front rubble catch slab")==null){var slab=Add(root.transform,"Props/Wall_Grey_Big_01","Front rubble catch slab",new Vector3(-.26f,.33f,.99f),new Vector3(-2,12,-2),new Vector3(1.0f,.16f,.76f),true);slab.GetComponentInChildren<MeshCollider>().sharedMaterial=material;}
        if(GameObject.Find("Broken attachment neck")==null)Add(root.transform,"Props/Wall_Gray_Medium_03","Broken attachment neck",new Vector3(-.25f,1.39f,1.17f),new Vector3(20,-17,0),new Vector3(.34f,.19f,.25f));
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Horizontal matching-mesh landing slab, higher friction and bounded initial spin";
    }
}
