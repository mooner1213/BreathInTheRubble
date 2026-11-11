using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BreathInTheRubble.Prototype;

public static class BuildAftershockPipe
{
    const string Art = "Assets/_Project/Art/MetalPipe";
    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Kind(PipeImpactSurface surface, PipeImpactSurface.SurfaceKind kind)
    {
        var data=new SerializedObject(surface);data.FindProperty("kind").enumValueIndex=(int)kind;data.ApplyModifiedPropertiesWithoutUndo();
    }
    static Mesh PipeMesh()
    {
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        const int sides=24;const float length=.46f, outer=.011f, inner=.008f;
        Action<Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3,Vector3> quad=(a,b,c,d,na,nb,nc,nd)=>{
            int k=vertices.Count;vertices.AddRange(new[]{a,b,c,d});normals.AddRange(new[]{na,nb,nc,nd});uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});indices.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
        };
        for(int i=0;i<sides;i++)
        {
            float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;
            var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var m=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
            var down=Vector3.down*length/2;var up=Vector3.up*length/2;
            quad(n*outer+down,n*outer+up,m*outer+up,m*outer+down,n,n,m,m);
            quad(m*inner+down,m*inner+up,n*inner+up,n*inner+down,-m,-m,-n,-n);
            quad(n*inner+up,m*inner+up,m*outer+up,n*outer+up,Vector3.up,Vector3.up,Vector3.up,Vector3.up);
            quad(n*outer+down,m*outer+down,m*inner+down,n*inner+down,Vector3.down,Vector3.down,Vector3.down,Vector3.down);
        }
        var mesh=new Mesh{name="Hollow Steel Pipe 46cm"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Art+"/HollowSteelPipe.asset");return mesh;
    }
    static AudioClip Cue(string name, int kind)
    {
        const int rate=22050;float seconds=kind==0?.28f:kind==1?1.05f:kind==2?1.95f:.30f;
        int count=Mathf.CeilToInt(rate*seconds);var random=new System.Random(871+kind);float low=0;
        string path=Art+"/"+name+".wav";
        using(var stream=File.Create(path))using(var writer=new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate;float noise=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,noise,.28f);
                float v;
                if(kind==2)
                {
                    float envelope=Mathf.Sin(Mathf.Clamp01(t/seconds)*Mathf.PI)*(.55f+.45f*Mathf.Sin(t*29f)*Mathf.Sin(t*29f));
                    v=(low*.35f+Mathf.Sin(t*2357f)*.06f)*envelope;
                }
                else
                {
                    float hz=kind==1?780f:kind==3?460f:190f;
                    float decay=kind==1?4.6f:kind==3?16f:23f;
                    float ring=(Mathf.Sin(t*hz*2*Mathf.PI)+.42f*Mathf.Sin(t*hz*2.71f*2*Mathf.PI)+.20f*Mathf.Sin(t*hz*4.13f*2*Mathf.PI))*.32f*Mathf.Exp(-t*decay);
                    v=ring+(noise-low)*.33f*Mathf.Exp(-t*85f)+low*.2f*Mathf.Exp(-t*26f);
                    v*=Mathf.Clamp01(t/.0018f)*Mathf.Clamp01((seconds-t)/.035f);
                }
                writer.Write((short)(Mathf.Clamp(v,-.9f,.9f)*32767f));
            }
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    static Transform Pose(string name, Transform parent, Vector3 position, Vector3 euler)
    {
        var go=new GameObject(name);go.transform.SetPositionAndRotation(position,Quaternion.Euler(euler));go.transform.SetParent(parent,true);return go.transform;
    }
    static AudioSource Source(GameObject go,float volume)
    {
        var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;source.volume=volume;source.minDistance=.25f;source.maxDistance=4;source.rolloffMode=AudioRolloffMode.Logarithmic;source.dopplerLevel=0;return source;
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("ConfinedPrototype required.");
        if(UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>()!=null || AssetDatabase.IsValidFolder(Art))throw new Exception("Pipe setup already exists; refusing duplication/overwrite.");
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var ceiling=GameObject.Find("Low Ceiling Front").transform;
        var env=GameObject.Find("Prototype Environment").transform;
        var grabs=UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
        var phone=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>();var bottle=UnityEngine.Object.FindFirstObjectByType<BottleLiquidVisual>();
        if(director==null||ceiling==null||Camera.main==null||phone==null||bottle==null)throw new Exception("Existing scene setup missing.");
        string backup="Assets/_Project/Scenes/Backups/BeforeAftershockPipe_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed.");
        AssetDatabase.CreateFolder("Assets/_Project/Art","MetalPipe");
        var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/RebarRust.mat")){name="Worn Thin Steel Pipe"};
        material.SetColor("_BaseColor",new Color(.31f,.29f,.25f,1));material.SetFloat("_Metallic",.65f);material.SetFloat("_Smoothness",.32f);AssetDatabase.CreateAsset(material,Art+"/WornSteelPipe.mat");
        var friction=new PhysicsMaterial("Pipe Grip Friction"){staticFriction=.9f,dynamicFriction=.8f,bounciness=.04f,frictionCombine=PhysicsMaterialCombine.Maximum};AssetDatabase.CreateAsset(friction,Art+"/PipeFriction.physicMaterial");
        var concrete=Cue("PipeConcreteImpact",0);var metal=Cue("PipeMetalRing",1);var slide=Cue("PipeCeilingScrape",2);var landing=Cue("PipeLandingClatter",3);
        var root=new GameObject("Aftershock Pipe Release");root.transform.SetParent(director.transform,false);Undo.RegisterCreatedObjectUndo(root,"Add falling grabbable pipe");
        var stowed=Pose("Pipe Stowed Inside Ceiling",ceiling,new Vector3(-.17f,1.20f,.39f),new Vector3(75,0,-18));
        var lip=Pose("Pipe Sliding Out of Ceiling Seam",ceiling,new Vector3(-.17f,1.10f,.35f),new Vector3(48,0,-18));
        var release=Pose("Pipe Free Fall Start",root.transform,new Vector3(-.14f,.85f,.32f),new Vector3(25,0,-35));
        var landingPose=Pose("Pipe Reachable Landing",root.transform,new Vector3(-.025f,.493f,.25f),new Vector3(0,12,90));
        var pipe=new GameObject("Thin Steel Pipe (Grabbable)");pipe.transform.SetParent(root.transform,false);pipe.transform.SetPositionAndRotation(stowed.position,stowed.rotation);
        pipe.AddComponent<MeshFilter>().sharedMesh=PipeMesh();var renderer=pipe.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        var shape=pipe.AddComponent<CapsuleCollider>();shape.direction=1;shape.radius=.011f;shape.height=.46f;shape.sharedMaterial=friction;
        var body=pipe.AddComponent<Rigidbody>();body.mass=.28f;body.linearDamping=.25f;body.angularDamping=1.2f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;body.maxAngularVelocity=24;body.maxDepenetrationVelocity=1.2f;
        var grab=pipe.AddComponent<XRGrabInteractable>();grab.movementType=XRBaseInteractable.MovementType.VelocityTracking;grab.useDynamicAttach=true;grab.matchAttachPosition=true;grab.matchAttachRotation=true;grab.snapToColliderVolume=true;grab.throwOnDetach=true;grab.retainTransformParent=true;grab.attachEaseInTime=.12f;
        grab.interactionLayers=grabs.First().interactionLayers;grab.colliders.Add(shape);
        pipe.AddComponent<PhoneContactOnlyGrabFilter>();pipe.AddComponent<PhoneToggleGrabMode>();
        var movement=Source(pipe,.55f);var impactObject=new GameObject("Pipe Contact Audio");impactObject.transform.SetParent(pipe.transform,false);var source=Source(impactObject,.7f);
        var audio=pipe.AddComponent<PipeImpactAudio>();Set(audio,"impactSource",source);Set(audio,"concreteClip",concrete);Set(audio,"metalClip",metal);
        var drop=pipe.AddComponent<AftershockPipeDrop>();Set(drop,"aftershock",director);Set(drop,"stowedPose",stowed);Set(drop,"lipPose",lip);Set(drop,"releasePose",release);Set(drop,"landingPose",landingPose);Set(drop,"head",Camera.main.transform);Set(drop,"movementSource",movement);Set(drop,"slideClip",slide);Set(drop,"landingClip",landing);
        var ignored=Camera.main.transform.root.GetComponentsInChildren<Collider>(true).Concat(grabs.SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).Distinct().ToArray();
        var data=new SerializedObject(drop);var array=data.FindProperty("ignoredColliders");array.arraySize=ignored.Length;for(int i=0;i<ignored.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=ignored[i];data.ApplyModifiedPropertiesWithoutUndo();
        pipe.AddComponent<DesktopPipeTestDriver>();
        int concreteSurfaces=0,metalSurfaces=0,newSteelColliders=0;
        foreach(var c in env.GetComponentsInChildren<Collider>(true))
        {
            if(c.isTrigger||c.GetComponentInParent<XRGrabInteractable>()!=null||c.name.Contains("Seated")||c.name.Contains("Thigh")||c.name.Contains("Pelvis"))continue;
            var r=c.GetComponent<Renderer>();if(r==null||r.sharedMaterial==null)continue;
            bool steel=r.sharedMaterial.name.Contains("Rebar")||c.name.Contains("Steel")||c.name.Contains("Rebar");
            var marker=c.GetComponent<PipeImpactSurface>();if(marker==null)marker=c.gameObject.AddComponent<PipeImpactSurface>();Kind(marker,steel?PipeImpactSurface.SurfaceKind.Metal:PipeImpactSurface.SurfaceKind.Concrete);
            if(steel)metalSurfaces++;else concreteSurfaces++;
        }
        foreach(var r in env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.gameObject.activeInHierarchy && r.sharedMaterial!=null && r.sharedMaterial.name.Contains("Rebar")))
        {
            var marker=r.GetComponent<PipeImpactSurface>();if(marker==null)marker=r.gameObject.AddComponent<PipeImpactSurface>();Kind(marker,PipeImpactSurface.SurfaceKind.Metal);
            if(r.GetComponent<Collider>()==null){r.gameObject.AddComponent<MeshCollider>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;newSteelColliders++;metalSurfaces++;}
        }
        ConfigureCosmeticFiltering(drop);
        renderer.enabled=false;shape.enabled=false;grab.enabled=false;body.isKinematic=true;body.useGravity=false;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed.");
        return new{backup,pipe=pipe.name,length=.46f,diameter=.022f,availableAt=drop.AvailableAtSeconds,landing=landingPose.position.ToString("F3"),concreteSurfaces,metalSurfaces,newSteelColliders,desktop="Existing PC mode: P grab/release, J physical swing; right mouse aims as before",unchangedGrabs=grabs.Select(g=>new{g.name,p=g.transform.position.ToString("F3")})};
    }
    public static object CompleteSteelTargets()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        int added=0;
        foreach(var r in GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.gameObject.activeInHierarchy && r.sharedMaterial!=null && r.sharedMaterial.name.Contains("Rebar")))
        {
            var marker=r.GetComponent<PipeImpactSurface>();if(marker==null)marker=r.gameObject.AddComponent<PipeImpactSurface>();Kind(marker,PipeImpactSurface.SurfaceKind.Metal);
            if(r.GetComponent<Collider>()==null){r.gameObject.AddComponent<MeshCollider>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;added++;}
        }
        ConfigureCosmeticFiltering(UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>());
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{addedMetalColliders=added};
    }
    static void ConfigureCosmeticFiltering(AftershockPipeDrop drop)
    {
        var chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None);
        var steel=UnityEngine.Object.FindObjectsByType<PipeImpactSurface>(FindObjectsSortMode.None).Where(s=>s.Kind==PipeImpactSurface.SurfaceKind.Metal).Select(s=>s.GetComponent<Collider>()).Where(c=>c!=null).ToArray();
        Action<UnityEngine.Object,string,Collider[]> append=(target,field,extra)=>{
            var data=new SerializedObject(target);var array=data.FindProperty(field);
            var list=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue as Collider).Concat(extra).Where(c=>c!=null).Distinct().ToArray();
            array.arraySize=list.Length;for(int i=0;i<list.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=list[i];data.ApplyModifiedPropertiesWithoutUndo();
        };
        append(drop,"ignoredColliders",chunks.Select(c=>c.GetComponent<Collider>()).ToArray());
        foreach(var chunk in chunks)append(chunk,"ignoreColliders",steel);
    }
}
