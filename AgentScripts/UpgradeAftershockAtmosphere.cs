using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class UpgradeAftershockAtmosphere
{
    const string Art = "Assets/_Project/Art/Aftershock/";
    static float Between(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
    static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value)
    { var data = new SerializedObject(target); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedProperties(); }
    static void Number(UnityEngine.Object target, string name, float value)
    { var data = new SerializedObject(target); data.FindProperty(name).floatValue = value; data.ApplyModifiedProperties(); }

    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit mode required.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Project/Scenes/ConfinedPrototype.unity") throw new Exception("ConfinedPrototype must be active.");
        var director = UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var awakening = UnityEngine.Object.FindFirstObjectByType<AwakeningDustEffect>();
        var motion = UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var debris = UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();
        if (director == null || awakening == null || motion == null || debris == null) throw new Exception("Existing aftershock foundation is required.");
        var fine = Material("AtmosphericDustUnlit", "Assets/_Project/Art/Effects/DustSoft.png");
        var veil = Material("AtmosphericVeilUnlit", "Assets/_Project/Art/Effects/DustVeilNoise.png");

        var cloud = awakening.GetComponentsInChildren<ParticleSystem>(true).First(p => p.name == "Initial Dust Cloud");
        Configure(cloud, fine, new Vector3(0f,0.96f,0.48f), new Vector3(1.16f,0.52f,0.75f), 0.008f,0.038f,12f,22f,440,0.52f,false,0f,360);
        var motes = awakening.GetComponentsInChildren<ParticleSystem>(true).First(p => p.name == "Suspended Dust Motes");
        Configure(motes, fine, new Vector3(0f,0.95f,0.43f),new Vector3(1.28f,0.56f,0.85f),0.003f,0.009f,12f,20f,240,0.64f,true,12f,170);
        var ambientVeil = awakening.GetComponentsInChildren<ParticleSystem>(true).First(p => p.name == "Dense Dust Veil");
        Configure(ambientVeil, veil, new Vector3(0f,0.94f,0.48f),new Vector3(1.30f,0.52f,0.70f),0.28f,0.62f,9f,15f,48,0.30f,true,3f,34);
        Number(awakening,"startDelaySeconds",0.02f);

        var effects = director.transform.Find("Aftershock Effects");
        var fineDust = effects.Find("Additional Aftershock Dust").GetComponent<ParticleSystem>();
        Configure(fineDust, fine, new Vector3(0f,1.06f,0.47f),new Vector3(1.30f,0.30f,0.85f),0.006f,0.040f,5f,11f,600,0.64f,false,0f,0);
        var fineMain = fineDust.main; fineMain.startSpeed = new ParticleSystem.MinMaxCurve(0.08f,0.28f); fineMain.gravityModifier=0.018f;
        var dense = FindOrCreate(effects,"Aftershock Dense Dust Veil");
        Configure(dense,veil,new Vector3(0f,0.98f,0.46f),new Vector3(1.25f,0.52f,0.65f),0.38f,0.85f,7f,13f,72,0.62f,false,5f,0);
        var plumes = new ParticleSystem[3];
        var points = new[]{new Vector3(-0.36f,1.12f,0.64f),new Vector3(0.37f,1.15f,0.62f),new Vector3(0.02f,1.11f,0.86f)};
        for(int i=0;i<plumes.Length;i++)
        {
            plumes[i]=FindOrCreate(effects,"Fracture Dust Plume "+(i+1));
            Configure(plumes[i],veil,points[i],new Vector3(0.40f,0.10f,0.26f),0.18f,0.48f,3.5f,7f,24,0.60f,false,0f,0);
            var main=plumes[i].main;main.startSpeed=new ParticleSystem.MinMaxCurve(0.06f,0.22f);main.gravityModifier=0.025f;
        }
        var atmosphere = director.GetComponent<AftershockDustEffect>();
        if(atmosphere==null)atmosphere=director.gameObject.AddComponent<AftershockDustEffect>();
        Ref(atmosphere,"dustVeil",dense);
        var atmosphereData=new SerializedObject(atmosphere);var plumeArray=atmosphereData.FindProperty("fracturePlumes");plumeArray.arraySize=plumes.Length;
        for(int i=0;i<plumes.Length;i++)plumeArray.GetArrayElementAtIndex(i).objectReferenceValue=plumes[i];
        atmosphereData.FindProperty("dustColor").colorValue=new Color(0.43f,0.42f,0.39f,1f);
        atmosphereData.ApplyModifiedProperties();Ref(director,"dustAtmosphere",atmosphere);
        Number(motion,"ceilingDrop",0.14f);Number(motion,"shakeDistance",0.018f);Number(motion,"shakeDegrees",0.9f);Number(motion,"slabTiltDegrees",2.8f);
        ConfigureDebris(debris,effects);
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        return new{result="Dense startup dust + ambient haze; 64 pooled fragments, 3 fracture plumes, staged breakaway slabs, 6x environment shake, lingering aftershock haze",scene=scene.path};
    }

    static Material Material(string name,string texture)
    {
        string path=Art+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material!=null){material.SetColor("_BaseColor",new Color(0.78f,0.77f,0.74f,0.9f));EditorUtility.SetDirty(material);return material;}
        var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var map=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
        if(shader==null || map==null)throw new Exception("Dust shader or texture missing.");
        material=new Material(shader){name=name};
        material.SetTexture("_BaseMap",map);material.SetColor("_BaseColor",new Color(0.78f,0.77f,0.74f,0.9f));
        material.SetFloat("_Surface",1f);material.SetFloat("_Blend",0f);material.SetFloat("_ZWrite",0f);
        material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);material.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_Cull",(float)CullMode.Off);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
        AssetDatabase.CreateAsset(material,path);return material;
    }

    static ParticleSystem FindOrCreate(Transform parent,string name)
    {
        var existing=parent.Find(name);if(existing!=null)return existing.GetComponent<ParticleSystem>();
        var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Add fracture dust");go.transform.SetParent(parent,false);return go.AddComponent<ParticleSystem>();
    }

    static void Configure(ParticleSystem system,Material material,Vector3 position,Vector3 box,float minSize,float maxSize,float minLife,float maxLife,int limit,float alpha,bool loop,float rate,int burst)
    {
        Undo.RecordObject(system,"Strengthen collapse dust");system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        system.transform.position=position;
        var main=system.main;main.loop=loop;main.duration=8f;main.playOnAwake=false;main.prewarm=false;
        main.startLifetime=new ParticleSystem.MinMaxCurve(minLife,maxLife);main.startSize=new ParticleSystem.MinMaxCurve(minSize,maxSize);
        main.startSpeed=new ParticleSystem.MinMaxCurve(0.006f,0.035f);main.startRotation=new ParticleSystem.MinMaxCurve(0f,Mathf.PI*2f);
        main.startColor=new ParticleSystem.MinMaxGradient(new Color(0.66f,0.65f,0.61f,alpha*0.65f),new Color(0.82f,0.80f,0.75f,alpha));
        main.gravityModifier=0f;main.maxParticles=limit;main.simulationSpace=ParticleSystemSimulationSpace.World;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
        var emission=system.emission;emission.enabled=true;emission.rateOverTime=rate;emission.SetBursts(burst>0?new[]{new ParticleSystem.Burst(0f,(short)burst)}:Array.Empty<ParticleSystem.Burst>());
        var shape=system.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=box;
        var noise=system.noise;noise.enabled=true;noise.quality=ParticleSystemNoiseQuality.Low;noise.strength=0.045f;noise.frequency=0.55f;noise.scrollSpeed=0.12f;noise.octaveCount=1;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(0.95f,0.94f,0.91f),0f),new GradientColorKey(new Color(0.80f,0.79f,0.75f),1f)},
            new[]{new GradientAlphaKey(0.42f,0f),new GradientAlphaKey(0.9f,0.08f),new GradientAlphaKey(0.60f,0.65f),new GradientAlphaKey(0f,1f)});
        var color=system.colorOverLifetime;color.enabled=true;color.color=gradient;
        var size=system.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0f,0.65f),new Keyframe(0.3f,1f),new Keyframe(1f,1.3f)));
        var renderer=system.GetComponent<ParticleSystemRenderer>();renderer.enabled=true;renderer.sharedMaterial=material;
        renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.alignment=ParticleSystemRenderSpace.View;renderer.sortMode=ParticleSystemSortMode.Distance;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
        renderer.minParticleSize=0f;renderer.maxParticleSize=0.8f;
    }

    static void ConfigureDebris(AftershockDebris debris,Transform parent)
    {
        var data=new SerializedObject(debris);var array=data.FindProperty("fragments");
        var previous=Enumerable.Range(0,array.arraySize).Select(i=>(Transform)array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var source=previous.First(t=>t!=null).GetComponent<MeshRenderer>().sharedMaterial;
        var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"FallingConcreteInstanced.mat");
        if(material==null){material=new Material(source){name="FallingConcreteInstanced",enableInstancing=true};AssetDatabase.CreateAsset(material,Art+"FallingConcreteInstanced.mat");}
        const int count=64;var random=new System.Random(7423);
        array.arraySize=count;var ends=data.FindProperty("landingPositions");ends.arraySize=count;
        var times=data.FindProperty("releaseTimes");times.arraySize=count;var durations=data.FindProperty("fallDurations");durations.arraySize=count;
        Physics.SyncTransforms();
        for(int i=0;i<count;i++)
        {
            Transform t=i<previous.Length?previous[i]:null;
            if(t==null){var go=new GameObject("Cosmetic Falling Pebble "+(i+1));Undo.RegisterCreatedObjectUndo(go,"Pool falling rubble");t=go.transform;t.SetParent(parent,false);go.AddComponent<MeshFilter>();go.AddComponent<MeshRenderer>();}
            t.gameObject.SetActive(false);
            float x=Between(random,-0.62f,0.62f),z=Between(random,0.34f,0.88f);
            if(i%7==0){x=Between(random,-0.68f,0.68f);z=Between(random,-0.58f,-0.28f);}
            t.position=new Vector3(x,Between(random,1.12f,1.27f),z);
            t.rotation=Quaternion.Euler(Between(random,0f,360f),Between(random,0f,360f),Between(random,0f,360f));
            float size=i%8==0?Between(random,0.07f,0.105f):Between(random,0.018f,0.055f);
            t.localScale=new Vector3(size,size*Between(random,0.35f,0.9f),size*Between(random,0.6f,1.3f));
            t.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i%6+1:00}.asset");
            var renderer=t.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
            var end=new Vector3(x+Between(random,-0.10f,0.10f),0.25f,z+Between(random,-0.12f,0.12f));
            if(Physics.Raycast(new Vector3(end.x,0.82f,end.z),Vector3.down,out var ground,1.4f))end.y=ground.point.y+size*0.35f;
            array.GetArrayElementAtIndex(i).objectReferenceValue=t;ends.GetArrayElementAtIndex(i).vector3Value=end;
            times.GetArrayElementAtIndex(i).floatValue=i<10?Between(random,0.35f,2.65f):new[]{3.15f,4.75f,6.55f}[(i-10)%3]+Between(random,0f,0.22f);
            durations.GetArrayElementAtIndex(i).floatValue=Between(random,0.36f,0.70f);
        }
        data.ApplyModifiedProperties();
    }
}
