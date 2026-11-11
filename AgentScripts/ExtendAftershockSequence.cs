using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ExtendAftershockSequence
{
    public static object RefineCrackShapes()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var root=GameObject.Find("Extended Rear Fractures");int count=0;
        if(root==null)throw new Exception("Historical floating-patch authoring is superseded by BuildEmbeddedWallFractures.");
        foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Opened Crack Segment ")))
        {
            int i=int.Parse(t.name.Substring("Opened Crack Segment ".Length))-1;
            t.localPosition=new Vector3(i%2==0?-0.002f:0.002f,-0.08f+i*0.04f,0.032f);
            t.localScale=new Vector3(0.007f,0.052f,0.005f);t.localRotation=Quaternion.Euler(0f,0f,i%2==0?-7f:8f);count++;
        }
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{result="Refined newly opened cracks to thin irregular seams",segments=count};
    }
    static float Between(System.Random r,float min,float max)=>Mathf.Lerp(min,max,(float)r.NextDouble());
    static void Append(UnityEngine.Object target,string field,UnityEngine.Object[] values)
    {
        var data=new SerializedObject(target);var array=data.FindProperty(field);int first=array.arraySize;array.arraySize+=values.Length;
        for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(first+i).objectReferenceValue=values[i];data.ApplyModifiedProperties();
    }
    static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;return go;
    }
    static void Rod(Transform parent,string name,Vector3 start,Vector3 end,float radius,Material material)
    {
        var delta=end-start;var go=Shape(name,PrimitiveType.Cylinder,parent,(start+end)*0.5f,new Vector3(radius*2f,delta.magnitude*0.5f,radius*2f),material);
        go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("ConfinedPrototype required.");
        if(GameObject.Find("Extended Rear Fractures")!=null)throw new Exception("Extended fractures already exist; refusing to duplicate.");
        if(GameObject.Find("Embedded Rear Wall Fractures")!=null)throw new Exception("Embedded wall fractures already replace this historical authoring pass.");
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var bottle=UnityEngine.Object.FindFirstObjectByType<BottleLiquidVisual>().transform.root;
        var effects=director.transform.Find("Aftershock Effects");var plumeTemplate=effects.Find("Rear Fracture Dust Plume 1");
        var steel=GameObject.Find("Rear Slab Reinforcement").GetComponent<MeshRenderer>().sharedMaterial;
        var crackMaterial=GameObject.Find("Rear Crack Main").GetComponent<MeshRenderer>().sharedMaterial;
        var root=new GameObject("Extended Rear Fractures");root.transform.SetParent(GameObject.Find("Prototype Environment").transform,false);Undo.RegisterCreatedObjectUndo(root,"Author later aftershock fractures");
        var centers=new[]{new Vector3(-0.29f,0.96f,-0.40f),new Vector3(0.12f,0.83f,-0.38f),new Vector3(0.43f,0.97f,-0.39f)};
        var times=new[]{10.2f,13.4f,16.2f};var sections=new AftershockFractureSection[3];var plumes=new ParticleSystem[3];
        var sources=new[]{"Rear Inner Fracture Left","Rear Inner Fracture Center","Rear Inner Fracture Right"};
        for(int section=0;section<3;section++)
        {
            var node=new GameObject("Later Rear Fracture "+(section+1));node.transform.SetParent(root.transform,false);node.transform.position=centers[section];
            var component=node.AddComponent<AftershockFractureSection>();sections[section]=component;
            var material=GameObject.Find(sources[section]).GetComponent<MeshRenderer>().sharedMaterial;
            var covers=new Transform[4];var points=new[]{new Vector3(-0.070f,0.055f,0.020f),new Vector3(0.070f,0.055f,0.020f),new Vector3(-0.075f,-0.055f,0.025f),new Vector3(0.075f,-0.055f,0.025f)};
            var offsets=new[]{new Vector3(-0.035f,-0.018f,0.010f),new Vector3(0.035f,-0.025f,0.020f),new Vector3(-0.045f,-0.070f,0.025f),new Vector3(0.050f,-0.085f,0.040f)};
            var angles=new[]{new Vector3(-10f,4f,-7f),new Vector3(-12f,-4f,7f),new Vector3(15f,6f,-12f),new Vector3(18f,-6f,14f)};
            for(int i=0;i<4;i++)
            {
                var piece=new GameObject("Separating Concrete "+(i+1));piece.transform.SetParent(node.transform,false);piece.transform.localPosition=points[i];piece.transform.localScale=new Vector3(0.14f,0.105f,0.070f);
                piece.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i+1:00}.asset");
                var renderer=piece.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;covers[i]=piece.transform;
            }
            var crack=new GameObject("New Jagged Crack");crack.transform.SetParent(node.transform,false);
            for(int i=0;i<5;i++)
            {
                var segment=Shape("Opened Crack Segment "+(i+1),PrimitiveType.Cube,crack.transform,new Vector3(i%2==0?-0.002f:0.002f,-0.08f+i*0.04f,0.032f),new Vector3(0.007f,0.052f,0.005f),crackMaterial);
                segment.transform.localRotation=Quaternion.Euler(0f,0f,i%2==0?-7f:8f);
            }
            crack.SetActive(false);
            var rebar=new GameObject("Revealed Bent Rebar");rebar.transform.SetParent(node.transform,false);rebar.transform.localPosition=new Vector3(0f,0f,0.018f);
            float protrusion=section==1?0.055f:0.085f;
            for(int bar=0;bar<2;bar++)
            {
                float x=bar==0?-0.037f:0.038f;
                var start=new Vector3(x,-0.075f,-0.012f);var bend=new Vector3(x+(bar==0?0.012f:-0.008f),0.012f,protrusion*0.4f);
                var end=new Vector3(x+(bar==0?0.023f:0.018f),0.095f,protrusion);
                Rod(rebar.transform,"Rust Rebar "+(bar+1)+" - Root",start,bend,0.005f,steel);Rod(rebar.transform,"Rust Rebar "+(bar+1)+" - Bent Tip",bend,end,0.005f,steel);
                for(int rib=1;rib<=3;rib++)
                {
                    var center=Vector3.Lerp(bend,end,rib/4f);var direction=(end-bend).normalized;
                    Rod(rebar.transform,"Rebar Raised Rib",center-direction*0.0015f,center+direction*0.0015f,0.0062f,steel);
                }
            }
            rebar.SetActive(false);
            var plumeObject=UnityEngine.Object.Instantiate(plumeTemplate.gameObject,effects,false);plumeObject.name="Later Crack Dust "+(section+1);plumeObject.transform.position=centers[section]+Vector3.forward*0.025f;
            var plume=plumeObject.GetComponent<ParticleSystem>();plume.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=plume.main;main.maxParticles=24;plume.useAutoRandomSeed=true;plumes[section]=plume;
            var data=new SerializedObject(component);data.FindProperty("head").objectReferenceValue=Camera.main.transform;
            var c=data.FindProperty("coverPieces");var o=data.FindProperty("separationOffsets");var a=data.FindProperty("rotationOffsets");c.arraySize=o.arraySize=a.arraySize=4;
            for(int i=0;i<4;i++){c.GetArrayElementAtIndex(i).objectReferenceValue=covers[i];o.GetArrayElementAtIndex(i).vector3Value=offsets[i];a.GetArrayElementAtIndex(i).vector3Value=angles[i];}
            data.FindProperty("crackInterior").objectReferenceValue=crack;data.FindProperty("exposedRebar").objectReferenceValue=rebar.transform;data.FindProperty("fractureDust").objectReferenceValue=plume;
            data.FindProperty("opensAtSeconds").floatValue=times[section];data.FindProperty("openingSeconds").floatValue=0.65f;data.FindProperty("headClearance").floatValue=section==1?0.36f:0.40f;
            data.ApplyModifiedProperties();
        }
        Append(director,"fractureSections",sections);Append(dust,"aftershockDust",plumes);
        Append(UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>(),"roomTargets",sections.Select(s=>(UnityEngine.Object)s.transform).ToArray());
        var config=new SerializedObject(director);config.FindProperty("durationSeconds").floatValue=18f;config.FindProperty("primaryImpactCount").intValue=3;
        var impacts=config.FindProperty("impactTimes");impacts.arraySize=6;var impactTimes=new[]{3.2f,4.8f,6.6f,10.2f,13.4f,16.2f};for(int i=0;i<impacts.arraySize;i++)impacts.GetArrayElementAtIndex(i).floatValue=impactTimes[i];config.ApplyModifiedProperties();

        var pool=new SerializedObject(debris);var fragments=pool.FindProperty("fragments");int first=fragments.arraySize;const int extra=24;
        var ends=pool.FindProperty("landingPositions");var releases=pool.FindProperty("releaseTimes");var durations=pool.FindProperty("fallDurations");
        fragments.arraySize=ends.arraySize=releases.arraySize=durations.arraySize=first+extra;
        var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Aftershock/FallingConcreteInstanced.mat");var random=new System.Random(18116);Physics.SyncTransforms();
        for(int i=0;i<extra;i++)
        {
            int section=i%3;Vector3 point=Vector3.zero;
            for(int attempt=0;attempt<1000;attempt++)
            {
                point=new Vector3(centers[section].x+Between(random,-0.12f,0.12f),Between(random,0.98f,1.15f),Between(random,-0.75f,-0.54f));
                if(new Vector2(point.x-bottle.position.x,point.z-bottle.position.z).magnitude>=0.24f && new Vector2(point.x,point.z).magnitude>=0.45f)break;
                if(attempt==999)throw new Exception("Bottle clearance sampling failed.");
            }
            var go=new GameObject("Later Rear Falling Fragment "+(i+1));go.transform.SetParent(root.transform,false);go.transform.position=point;go.transform.rotation=Quaternion.Euler(i*37f,i*83f,i*53f);
            float size=i%6==0?0.068f:Between(random,0.022f,0.049f);go.transform.localScale=new Vector3(size,size*0.65f,size*1.1f);
            go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>($"Assets/_Project/Art/ProceduralRubble/AngularConcrete_{i%6+1:00}.asset");var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=concrete;renderer.shadowCastingMode=ShadowCastingMode.Off;
            var end=new Vector3(point.x,0.25f,point.z);if(Physics.Raycast(new Vector3(point.x,0.78f,point.z),Vector3.down,out var hit,1.3f))end.y=hit.point.y+size*0.4f;
            fragments.GetArrayElementAtIndex(first+i).objectReferenceValue=go.transform;ends.GetArrayElementAtIndex(first+i).vector3Value=end;
            releases.GetArrayElementAtIndex(first+i).floatValue=times[section]-0.06f+Between(random,0f,0.28f);durations.GetArrayElementAtIndex(first+i).floatValue=Between(random,0.38f,0.70f);go.SetActive(false);
        }
        pool.ApplyModifiedProperties();
        var audio=director.GetComponentInChildren<AftershockAudio>();var sound=new SerializedObject(audio);sound.FindProperty("rumbleSeconds").floatValue=18f;sound.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        return new{result="18-second aftershock, 6 impacts, 3 late rear fractures with bent rebar and 24 additional late fragments",duration=18,sections=3,lateFragments=extra,totalFragments=first+extra};
    }
}
