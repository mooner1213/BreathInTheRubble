using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BuildRearAftershockCollapse
{
    public static object MatchRoofMaterial()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var material=GameObject.Find("Rear Ceiling Broken Corner").GetComponent<MeshRenderer>().sharedMaterial;
        var root=GameObject.Find("Rear Aftershock Breakaway");
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("Rear Breakaway Ceiling Panel")))renderer.sharedMaterial=material;
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{result="Rear panels matched to existing ceiling concrete",material=material.name};
    }
    static float Between(System.Random r,float min,float max)=>Mathf.Lerp(min,max,(float)r.NextDouble());
    static void Append(UnityEngine.Object target,string name,UnityEngine.Object[] values)
    {
        var data=new SerializedObject(target);var array=data.FindProperty(name);int start=array.arraySize;array.arraySize+=values.Length;
        for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(start+i).objectReferenceValue=values[i];
        data.ApplyModifiedProperties();
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("ConfinedPrototype required.");
        if(GameObject.Find("Rear Aftershock Breakaway")!=null)throw new Exception("Rear collapse already exists; refusing to duplicate.");
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        var bottle=UnityEngine.Object.FindFirstObjectByType<BottleLiquidVisual>().transform.root;
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Aftershock/FallingConcreteInstanced.mat");
        var roofMaterial=GameObject.Find("Rear Ceiling Broken Corner").GetComponent<MeshRenderer>().sharedMaterial;
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Art/ProceduralRubble/AngularConcrete_03.asset");
        if(director==null||motion==null||debris==null||dust==null||material==null||mesh==null)throw new Exception("Existing aftershock setup missing.");

        var root=new GameObject("Rear Aftershock Breakaway");root.transform.SetParent(GameObject.Find("Prototype Environment").transform,false);
        Undo.RegisterCreatedObjectUndo(root,"Add rear collapse detail");
        var positions=new[]{new Vector3(-0.39f,1.045f,-0.61f),new Vector3(0.02f,1.06f,-0.59f),new Vector3(0.41f,1.06f,-0.63f)};
        var scales=new[]{new Vector3(0.36f,0.08f,0.25f),new Vector3(0.32f,0.09f,0.28f),new Vector3(0.38f,0.08f,0.26f)};
        var panels=new Transform[positions.Length];
        for(int i=0;i<panels.Length;i++)
        {
            var go=new GameObject("Rear Breakaway Ceiling Panel "+(i+1));go.transform.SetParent(root.transform,false);
            go.transform.position=positions[i];go.transform.localScale=scales[i];go.transform.rotation=Quaternion.Euler(8f+i*3f,i*13f-14f,i%2==0?9f:-8f);
            go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i+2:00}.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=roofMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
            panels[i]=go.transform;
        }
        Append(motion,"ceilingTargets",panels);

        const int additionalFragments=28;var random=new System.Random(13927);
        var data=new SerializedObject(debris);var fragments=data.FindProperty("fragments");int first=fragments.arraySize;
        var ends=data.FindProperty("landingPositions");var releases=data.FindProperty("releaseTimes");var durations=data.FindProperty("fallDurations");
        fragments.arraySize=ends.arraySize=releases.arraySize=durations.arraySize=first+additionalFragments;
        Physics.SyncTransforms();
        for(int i=0;i<additionalFragments;i++)
        {
            Vector3 point=Vector3.zero;
            for(int attempt=0;attempt<1000;attempt++)
            {
                point=new Vector3(Between(random,-0.59f,0.52f),Between(random,1.01f,1.17f),Between(random,-0.76f,-0.34f));
                var bottleDelta=new Vector2(point.x-bottle.position.x,point.z-bottle.position.z);
                if(bottleDelta.magnitude>=0.24f && new Vector2(point.x,point.z).magnitude>=0.45f)break;
                if(attempt==999)throw new Exception("Could not preserve bottle clearance.");
            }
            var go=new GameObject("Rear Falling Concrete Fragment "+(i+1));go.transform.SetParent(root.transform,false);go.transform.position=point;
            go.transform.rotation=Quaternion.Euler(Between(random,0f,360f),Between(random,0f,360f),Between(random,0f,360f));
            float size=i%7==0?Between(random,0.065f,0.085f):Between(random,0.022f,0.050f);
            go.transform.localScale=new Vector3(size,size*Between(random,0.45f,0.85f),size*1.15f);
            go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i%6+1:00}.asset");
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            var end=new Vector3(point.x,0.25f,point.z);
            if(Physics.Raycast(new Vector3(end.x,0.78f,end.z),Vector3.down,out var ground,1.3f))end.y=ground.point.y+size*0.4f;
            fragments.GetArrayElementAtIndex(first+i).objectReferenceValue=go.transform;
            ends.GetArrayElementAtIndex(first+i).vector3Value=end;
            releases.GetArrayElementAtIndex(first+i).floatValue=new[]{3.12f,4.72f,6.52f}[i%3]+Between(random,0f,0.23f);
            durations.GetArrayElementAtIndex(first+i).floatValue=Between(random,0.35f,0.64f);
            go.SetActive(false);
        }
        data.ApplyModifiedProperties();

        var effects=director.transform.Find("Aftershock Effects");
        var template=effects.Find("Fracture Dust Plume 1");
        var plumes=new ParticleSystem[2];
        for(int i=0;i<plumes.Length;i++)
        {
            var go=UnityEngine.Object.Instantiate(template.gameObject,effects,false);go.name="Rear Fracture Dust Plume "+(i+1);
            go.transform.position=i==0?new Vector3(-0.41f,1.01f,-0.65f):new Vector3(0.34f,1.04f,-0.66f);
            var system=go.GetComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.maxParticles=40;system.useAutoRandomSeed=true;
            plumes[i]=system;
        }
        Append(dust,"fracturePlumes",plumes);Append(dust,"aftershockDust",plumes);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{result="Rear breakaway ceiling, rear fragment showers and fracture dust added",rearPanels=panels.Length,additionalFragments,totalFragments=first+additionalFragments,rearDustPlumes=plumes.Length,bottleClearRadius=0.24f};
    }
}
