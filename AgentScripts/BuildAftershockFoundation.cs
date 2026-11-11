using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public static class BuildAftershockFoundation
{
    const string Art="Assets/_Project/Art/Aftershock";
    static SerializedObject Data(UnityEngine.Object target)=>new SerializedObject(target);
    static void Refs(UnityEngine.Object target,params (string name,UnityEngine.Object value)[] values)
    {var d=Data(target);foreach(var item in values)d.FindProperty(item.name).objectReferenceValue=item.value;d.ApplyModifiedProperties();}
    static void ArrayRefs(UnityEngine.Object target,string name,UnityEngine.Object[] values)
    {var d=Data(target);var p=d.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];d.ApplyModifiedProperties();}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("ConfinedPrototype must be active.");
        if(GameObject.Find("Prototype Game Flow")!=null)throw new Exception("Game flow already exists; refusing to duplicate it.");
        var requiredNames=new[]{"Left Controller","Right Controller","Prototype Environment","Low Ceiling Rear","Low Ceiling Front","Overhead Broken Slab Left","Overhead Broken Slab Right","Broken Ceiling Lip","Rear Ceiling Broken Corner","Left Inner Broken Face","Right Inner Broken Face","Rear Inner Fracture Center"};
        foreach(var name in requiredNames)if(GameObject.Find(name)==null)throw new Exception("Required scene object missing: "+name);
        if(Camera.main==null || UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>()==null)throw new Exception("Main camera or phone missing.");
        if(Shader.Find("BreathInTheRubble/Head Impact Edges")==null)throw new Exception("Impact shader missing.");
        for(int i=1;i<=6;i++)if(AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i:00}.asset")==null)throw new Exception("Debris mesh missing: "+i);
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ConfinedSpace/DustParticle Soft.mat")==null)throw new Exception("Dust material missing.");
        var backup="Assets/_Project/Scenes/Backups/BeforeAftershock_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Could not preserve current scene.");
        if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/_Project/Art","Aftershock");
        var phone=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>();
        var camera=Camera.main;var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var left=all.First(t=>t.name=="Left Controller");var right=all.First(t=>t.name=="Right Controller");
        var root=new GameObject("Prototype Game Flow");Undo.RegisterCreatedObjectUndo(root,"Create game sequence foundation");
        var guard=root.AddComponent<HeadProtectionDetector>();Refs(guard,("head",camera.transform),("leftHand",left),("rightHand",right));
        var hit=root.AddComponent<HeadImpactFeedback>();
        var overlay=GameObject.CreatePrimitive(PrimitiveType.Quad);overlay.name="Head Impact Edge Overlay";overlay.transform.SetParent(camera.transform,false);
        float distance=Mathf.Max(0.11f,camera.nearClipPlane+0.02f);overlay.transform.localPosition=Vector3.forward*distance;overlay.transform.localScale=Vector3.one*distance*12f;
        UnityEngine.Object.DestroyImmediate(overlay.GetComponent<Collider>());
        var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/HeadImpactEdges.mat");
        if(material==null){material=new Material(Shader.Find("BreathInTheRubble/Head Impact Edges")){name="Temporary Red Head Impact"};AssetDatabase.CreateAsset(material,Art+"/HeadImpactEdges.mat");}
        var edge=overlay.GetComponent<MeshRenderer>();edge.sharedMaterial=material;edge.shadowCastingMode=ShadowCastingMode.Off;edge.receiveShadows=false;edge.enabled=false;
        Refs(hit,("edgeRenderer",edge));

        var motion=root.AddComponent<AftershockEnvironmentMotion>();Refs(motion,("head",camera.transform));
        var ceilingNames=new[]{"Low Ceiling Rear","Low Ceiling Front","Overhead Broken Slab Left","Overhead Broken Slab Right","Broken Ceiling Lip","Rear Ceiling Broken Corner"};
        var ceiling=ceilingNames.Select(n=>all.First(t=>t.name==n)).ToArray();
        var shake=new[]{"Left Inner Broken Face","Right Inner Broken Face","Rear Inner Fracture Center"}.Select(n=>all.First(t=>t.name==n)).ToArray();
        foreach(var t in ceiling.Concat(shake))t.gameObject.isStatic=false;
        ArrayRefs(motion,"ceilingTargets",ceiling);ArrayRefs(motion,"shakeTargets",shake);

        var effects=new GameObject("Aftershock Effects");effects.transform.SetParent(root.transform,false);
        var dustObject=new GameObject("Additional Aftershock Dust");dustObject.transform.SetParent(effects.transform,false);dustObject.transform.position=new Vector3(0f,1.03f,0.28f);
        var dust=dustObject.AddComponent<ParticleSystem>();dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=dust.main;main.loop=false;main.playOnAwake=false;main.duration=8f;main.startLifetime=new ParticleSystem.MinMaxCurve(5f,9f);main.startSpeed=new ParticleSystem.MinMaxCurve(0.01f,0.05f);main.startSize=new ParticleSystem.MinMaxCurve(0.01f,0.065f);main.maxParticles=150;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=0.001f;main.startColor=new Color(0.65f,0.60f,0.51f,0.42f);
        var emission=dust.emission;emission.rateOverTime=0f;emission.SetBursts(new ParticleSystem.Burst[0]);var shape=dust.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(0.95f,0.30f,0.75f);
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0f),new GradientColorKey(new Color(0.8f,0.75f,0.65f),1f)},new[]{new GradientAlphaKey(0f,0f),new GradientAlphaKey(0.6f,0.12f),new GradientAlphaKey(0.28f,0.65f),new GradientAlphaKey(0f,1f)});var color=dust.colorOverLifetime;color.enabled=true;color.color=gradient;
        var dustRenderer=dust.GetComponent<ParticleSystemRenderer>();dustRenderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ConfinedSpace/DustParticle Soft.mat");dustRenderer.shadowCastingMode=ShadowCastingMode.Off;

        var debris=root.AddComponent<AftershockDebris>();Refs(debris,("head",camera.transform),("leftHand",left),("rightHand",right));
        var starts=new[]{new Vector3(-0.43f,1.10f,0.47f),new Vector3(0.50f,1.10f,0.53f),new Vector3(-0.48f,1.10f,-0.47f),new Vector3(0.48f,1.10f,-0.49f),new Vector3(-0.46f,1.12f,0.54f),new Vector3(0.52f,1.12f,0.47f)};
        var pieces=new Transform[starts.Length];var landings=new Vector3[starts.Length];
        var chunkMaterial=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>().First(r=>r.sharedMaterial!=null&&r.sharedMaterial.name.Contains("Concrete")).sharedMaterial;
        Physics.SyncTransforms();
        for(int i=0;i<starts.Length;i++)
        {
            var piece=new GameObject("Cosmetic Falling Pebble "+(i+1));piece.transform.SetParent(effects.transform,false);piece.transform.position=starts[i];piece.transform.rotation=Quaternion.Euler(i*31f,i*53f,i*19f);piece.transform.localScale=Vector3.one*(0.018f+i%3*0.006f);
            piece.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i+1:00}.asset");piece.AddComponent<MeshRenderer>().sharedMaterial=chunkMaterial;
            var landing=new Vector3(starts[i].x,0.30f,starts[i].z);if(Physics.Raycast(new Vector3(landing.x,0.82f,landing.z),Vector3.down,out var floor,1f))landing.y=floor.point.y+0.014f;
            landings[i]=landing;pieces[i]=piece.transform;piece.SetActive(false);
        }
        ArrayRefs(debris,"fragments",pieces);var debrisData=Data(debris);var points=debrisData.FindProperty("landingPositions");points.arraySize=landings.Length;for(int i=0;i<landings.Length;i++)points.GetArrayElementAtIndex(i).vector3Value=landings[i];debrisData.ApplyModifiedProperties();

        var audioObject=new GameObject("Aftershock Structural Audio");audioObject.transform.SetParent(root.transform,false);audioObject.transform.position=new Vector3(0f,1.02f,0.53f);
        var source=audioObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1f;source.volume=0.40f;source.minDistance=0.3f;source.maxDistance=3f;source.dopplerLevel=0f;
        var audio=audioObject.AddComponent<AftershockAudio>();ArrayRefs(audio,"hands",left.GetComponentsInChildren<HapticImpulsePlayer>(true).Concat(right.GetComponentsInChildren<HapticImpulsePlayer>(true)).ToArray());
        var director=root.AddComponent<AftershockDirector>();Refs(director,("protection",guard),("hitFeedback",hit),("environmentMotion",motion),("debris",debris),("audioCue",audio),("extraDust",dust));
        var sequence=root.AddComponent<GameSequenceController>();Refs(sequence,("reception",phone.GetComponent<PhoneReceptionProgression>()),("calls",phone.GetComponent<EmergencyCallSystem>()),("aftershock",director));

        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        return "Game stages + 75s aftershock, three protection checks, 4.5cm ceiling settling, dust and six pebbles, red edge feedback wired. Backup="+backup;
    }
}
