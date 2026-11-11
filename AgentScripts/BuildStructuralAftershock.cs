using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using BreathInTheRubble.Prototype;

public static class BuildStructuralAftershock
{
    const string Art="Assets/_Project/Art/StructuralAftershock";
    static void Reference(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedProperties();}
    static void References(UnityEngine.Object target,string field,IEnumerable<UnityEngine.Object> values){var list=values.ToArray();var so=new SerializedObject(target);var a=so.FindProperty(field);a.arraySize=list.Length;for(int i=0;i<list.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=list[i];so.ApplyModifiedProperties();}
    static void Number(UnityEngine.Object target,string field,float value){var so=new SerializedObject(target);so.FindProperty(field).floatValue=value;so.ApplyModifiedProperties();}
    public static object AttachSeamChips()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var data=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<AftershockDebris>());var pieces=data.FindProperty("fragments");
        for(int i=0;i<pieces.arraySize;i++){var f=(Transform)pieces.GetArrayElementAtIndex(i).objectReferenceValue;f.SetParent(GameObject.Find(f.position.x<0?"Left beam locking shard":"Right beam locking shard").transform,true);}
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{attachedSeamChips=pieces.arraySize};
    }
    static GameObject Add(Transform parent,string path,Vector3 center,Vector3 rotation,float scale)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/"+path+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.transform.rotation=Quaternion.Euler(rotation);go.transform.localScale=Vector3.one*scale;
        foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
        var lod=go.GetComponent<LODGroup>();var ls=lod.GetLODs();lod.enabled=false;for(int i=0;i<ls.Length;i++)foreach(var r in ls[i].renderers){r.enabled=i==0;r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;}
        var bounds=ls[0].renderers[0].bounds;foreach(var r in ls[0].renderers.Skip(1))bounds.Encapsulate(r.bounds);go.transform.position+=center-bounds.center;return go;
    }
    static RubbleStressCrack Crack(GameObject target,AftershockDirector director,Material material,Vector3 a,Vector3 b,float onset,string name)
    {
        var shape=target.GetComponentsInChildren<MeshCollider>(true).First(c=>c.enabled);var host=shape.transform;
        var lines=new List<LineRenderer>();var points=new List<Vector3>();
        void Flush(){if(points.Count>1){var go=new GameObject(name+" segment "+lines.Count);go.transform.SetParent(host,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Count;line.SetPositions(points.ToArray());line.sharedMaterial=material;line.numCornerVertices=2;line.numCapVertices=2;line.startWidth=line.endWidth=1;line.widthMultiplier=0;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;lines.Add(line);}points.Clear();}
        for(int i=0;i<23;i++){float t=i/22f;var p=Vector3.Lerp(a,b,t);p.z+=Mathf.Sin(i*1.71f)*.012f;p.x+=Mathf.Sin(i*2.37f)*.006f;
            if(shape.Raycast(new Ray(new Vector3(p.x,.85f,p.z),Vector3.up),out var hit,1.5f))points.Add(host.InverseTransformPoint(hit.point+hit.normal*.0009f));else Flush();}
        Flush();if(lines.Count==0)throw new Exception("No crack surface: "+name);
        var effect=new GameObject(name);effect.transform.SetParent(host,false);var crack=effect.AddComponent<RubbleStressCrack>();Reference(crack,"aftershock",director);References(crack,"lines",lines.Cast<UnityEngine.Object>());Number(crack,"startsAtSeconds",onset);Number(crack,"openingSeconds",.85f);Number(crack,"maximumWidth",.0045f);return crack;
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");if(AssetDatabase.IsValidFolder(Art))throw new Exception("Structural aftershock already configured");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("Wrong scene");
        AssetDatabase.CreateFolder("Assets/_Project/Art","StructuralAftershock");
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var ceilingNames=new[]{"Burial Roof Inner Layer","Individual rescue slab 4 - closest","Shaft front lintel","Shaft rear lintel","Shaft left bearing edge","Shaft right bearing edge","Left beam locking shard","Right beam locking shard"};
        References(motion,"ceilingTargets",ceilingNames.Select(n=>(UnityEngine.Object)GameObject.Find(n).transform));Number(motion,"ceilingDrop",.14f);Number(motion,"minimumHeadGap",.24f);Number(motion,"slabTiltDegrees",1.5f);Number(motion,"breakawaySeconds",.48f);
        var root=new GameObject("Structural Aftershock Breakaway");root.transform.SetParent(GameObject.Find("Prototype Environment").transform,false);var detached=new GameObject("Settled Large Aftershock Fragments");detached.transform.SetParent(root.transform,false);
        var poses=new[]{new Vector3(-.69f,1.27f,.61f),new Vector3(.68f,1.28f,.67f),new Vector3(.52f,1.31f,-.45f),new Vector3(-.57f,1.35f,-.25f),new Vector3(.53f,1.40f,.08f)};
        string[] anchors={"Left beam locking shard","Right beam locking shard","Shaft rear lintel","Shaft left bearing edge","Shaft right bearing edge"};float[] times={3.2f,4.8f,6.6f,10.2f,13.4f};
        string[] assets={"Props/Wall_Gray_Medium_02","Props/Wall_Gray_Medium_03","Props/Wall_Gray_Medium_01","Props/Wall_Gray_Medium_03","Props/Wall_Grey_Small_01"};float[] scales={.78f,.75f,.76f,.46f,.58f};
        var protectedColliders=Camera.main.transform.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).ToArray();
        var chunks=new List<AftershockFallingChunk>();var sections=new List<AftershockFractureSection>();var plumes=new List<ParticleSystem>();
        var template=GameObject.Find("Fracture Dust Plume 1").GetComponent<ParticleSystem>();
        for(int i=0;i<poses.Length;i++)
        {
            var anchor=GameObject.Find(anchors[i]);var group=new GameObject("Structural joint failure "+(i+1));group.transform.SetParent(root.transform,false);group.transform.position=anchor.transform.position;
            var piece=Add(group.transform,assets[i],poses[i],new Vector3(12+i*6,23+i*29,i%2==0?-11:13),scales[i]);piece.name=(i<3?"Large concrete breakaway ":"Medium concrete breakaway ")+(i+1);
            var surface=piece.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var leaf=surface.gameObject;var shape=leaf.GetComponent<MeshCollider>();if(shape==null)shape=leaf.AddComponent<MeshCollider>();shape.sharedMesh=leaf.GetComponent<MeshFilter>().sharedMesh;shape.convex=true;shape.contactOffset=.001f;shape.enabled=true;
            var body=leaf.AddComponent<Rigidbody>();body.mass=i<3?5f:1.2f;body.isKinematic=true;body.useGravity=false;body.linearDamping=.12f;body.angularDamping=.65f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.maxDepenetrationVelocity=.35f;
            var chunk=leaf.AddComponent<AftershockFallingChunk>();Reference(chunk,"detachedRoot",detached.transform);Reference(chunk,"head",Camera.main.transform);var tracking=Camera.main.transform.root.GetComponentsInChildren<Transform>(true);Reference(chunk,"leftHand",tracking.First(t=>t.name=="Left Controller"));Reference(chunk,"rightHand",tracking.First(t=>t.name=="Right Controller"));
            References(chunk,"ignoreColliders",protectedColliders.Concat(anchor.GetComponentsInChildren<Collider>(true)).Cast<UnityEngine.Object>());
            var data=new SerializedObject(chunk);data.FindProperty("attachmentPoint").vector3Value=new Vector3(i%2==0?.18f:-.18f,.03f,.08f);data.FindProperty("attachmentAxis").vector3Value=Vector3.forward;data.FindProperty("releaseVelocity").vector3Value=new Vector3(i%2==0?-.12f:.12f,-.12f,.06f);data.FindProperty("releaseAngularVelocity").vector3Value=new Vector3(.6f,.4f,i%2==0?-.8f:.8f);data.FindProperty("attachmentSeconds").floatValue=.55f;data.FindProperty("peelDegrees").floatValue=i%2==0?12:-12;data.ApplyModifiedProperties();
            var audio=leaf.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.65f;audio.minDistance=.3f;audio.maxDistance=4;audio.dopplerLevel=0;var contact=leaf.AddComponent<RubbleImpactAudio>();Reference(contact,"impactClip",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Art/MetalPipe/PipeConcreteImpact.wav"));
            var dust=UnityEngine.Object.Instantiate(template,group.transform);dust.name="Structural fracture powder "+(i+1);dust.transform.position=poses[i];dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=dust.main;main.playOnAwake=false;main.maxParticles=24;main.startLifetime=new ParticleSystem.MinMaxCurve(.7f,1.4f);main.startSize=new ParticleSystem.MinMaxCurve(.04f,.13f);main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=dust.emission;emission.enabled=false;
            var section=group.AddComponent<AftershockFractureSection>();Reference(section,"wallAnchor",anchor.transform);References(section,"fallingChunks",new UnityEngine.Object[]{chunk});Reference(section,"fractureDust",dust);Number(section,"opensAtSeconds",times[i]);Number(section,"openingSeconds",.65f);
            sections.Add(section);chunks.Add(chunk);plumes.Add(dust);
        }
        References(director,"fractureSections",sections.Cast<UnityEngine.Object>());
        var atmosphere=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var a=new SerializedObject(atmosphere);var old=a.FindProperty("aftershockDust");var systems=Enumerable.Range(0,old.arraySize).Select(i=>old.GetArrayElementAtIndex(i).objectReferenceValue).Concat(plumes.Cast<UnityEngine.Object>()).ToArray();References(atmosphere,"aftershockDust",systems);
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Dark concrete stress fissure"};material.SetColor("_BaseColor",new Color(.008f,.007f,.006f,1));AssetDatabase.CreateAsset(material,Art+"/ConcreteStressCrack.mat");
        var cracks=new[]{Crack(GameObject.Find("Individual rescue slab 4 - closest"),director,material,new Vector3(-.27f,0,.27f),new Vector3(.25f,0,.29f),2.55f,"Ceiling underside stress crack"),
            Crack(GameObject.Find("Shaft front lintel"),director,material,new Vector3(-.22f,0,.65f),new Vector3(.26f,0,.68f),4.1f,"Front lintel stress crack"),
            Crack(GameObject.Find("Left beam locking shard"),director,material,new Vector3(-.57f,0,.02f),new Vector3(-.57f,0,.49f),5.85f,"Left wedged beam stress crack")};
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();References(ending,"lightBlockers",GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Cast<UnityEngine.Object>());
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return new{ceilingTargets=ceilingNames,ceilingDrop=.14f,minimumHeadGap=.24f,largeFragments=3,mediumFragments=2,stressCracks=cracks.Length,fracturePowder=plumes.Count};
    }
}
