using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;

public static class BuildAftershockTiming
{
    const string Marker="Aftershock 5 10 20 30 timing applied";
    public static object ActivateFineRain()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var rain=UnityEngine.Object.FindFirstObjectByType<AftershockRubbleRain>();var joints=new SerializedObject(rain).FindProperty("joints");var records=new List<object>();
        for(int i=0;i<joints.arraySize;i++)
        {
            var ps=joints.GetArrayElementAtIndex(i).objectReferenceValue as ParticleSystem;if(ps==null)continue;
            bool before=ps.gameObject.activeInHierarchy;
            ps.transform.SetParent(rain.transform,true);ps.gameObject.SetActive(true);ps.GetComponent<Renderer>().enabled=true;
            ps.transform.position=new Vector3(i%2==0?-.28f:.28f,1.15f,(i/2)*.13f+.18f);
            var main=ps.main;main.gravityModifier=.18f;main.playOnAwake=false;
            EditorUtility.SetDirty(ps);records.Add(new{ps.name,wasActive=before,active=ps.gameObject.activeInHierarchy});
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        File.WriteAllText("AgentScripts/ReviewImages/AftershockTiming/FineRain.json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));return records;
    }
    public static object RefineHeadClearance()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();int count=0;
        foreach(var e in manager.Elements.Where(e=>e.loose!=null&&e.powder!=null))
        {Number(e.loose,"headClearanceRadius",.22f);count++;}
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        return new{largePieces=count,headClearance=.22f,trackingMoved=false};
    }
    public static object RefineDustDepth()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var veil=Reference<ParticleSystem>(dust,"dustVeil");
        veil.transform.position=Camera.main.transform.position+Vector3.forward*.28f+Vector3.up*.06f;
        var shape=veil.shape;shape.scale=new Vector3(.95f,.42f,.30f);
        var main=veil.main;main.startSize=new ParticleSystem.MinMaxCurve(.28f,.48f);main.maxParticles=70;
        var material=veil.GetComponent<Renderer>().sharedMaterial;
        material.SetColor("_BaseColor",new Color(.70f,.65f,.56f,.72f));material.SetFloat("_AmbientScatter",.014f);material.SetFloat("_ScatteringGain",.012f);material.SetFloat("_NearFade",.12f);
        EditorUtility.SetDirty(material);EditorUtility.SetDirty(veil);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        return new{veil.name,shader=material.shader.name,position=veil.transform.position.ToString("F3"),maxParticles=main.maxParticles};
    }
    static void Number(UnityEngine.Object target,string field,float value)
    {var so=new SerializedObject(target);so.FindProperty(field).floatValue=value;so.ApplyModifiedProperties();}
    static void Integer(UnityEngine.Object target,string field,int value)
    {var so=new SerializedObject(target);so.FindProperty(field).intValue=value;so.ApplyModifiedProperties();}
    static void Times(UnityEngine.Object target,string field,float[] values)
    {var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).floatValue=values[i];so.ApplyModifiedProperties();}
    static T Reference<T>(UnityEngine.Object target,string field)where T:UnityEngine.Object
    =>new SerializedObject(target).FindProperty(field).objectReferenceValue as T;
    public static object RefineFlow()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var head=Camera.main.transform.position;
        foreach(var e in manager.Elements.Where(e=>e.loose!=null))
        {
            var center=e.target.GetComponent<Renderer>().bounds.center;var away=Vector3.ProjectOnPlane(center-head,Vector3.up).normalized;
            bool overhead=e.target.name.StartsWith("Overhead breaking slab");bool large=e.powder!=null;
            Vector3 velocity=large?away*.17f+Vector3.right*Mathf.Sign(center.x-head.x)*(overhead?.42f:.12f):away*.035f;
            if(overhead)velocity.z=Mathf.Sign(center.z-head.z)*.24f;
            velocity.y=-.08f;
            var data=new SerializedObject(e.loose);data.FindProperty("releaseVelocity").vector3Value=e.target.parent.InverseTransformVector(velocity);data.ApplyModifiedProperties();
            if(large)
            {
                data.FindProperty("releaseAngularVelocity").vector3Value=new Vector3(Mathf.Sin(e.phase)*.32f,Mathf.Cos(e.phase)*.2f,Mathf.Sin(e.phase*1.7f)*.28f);data.ApplyModifiedProperties();
            }
        }
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();int count=0;
        if(debris!=null)
        {
            count=new SerializedObject(debris).FindProperty("fragments").arraySize;
            Times(debris,"releaseTimes",Enumerable.Range(0,count).Select(i=>Mathf.Lerp(5.2f,9.3f,i/(float)Mathf.Max(1,count-1))).ToArray());
        }
        const string art="Assets/_Project/Art/AftershockTiming";
        if(!AssetDatabase.IsValidFolder(art))AssetDatabase.CreateFolder("Assets/_Project/Art","AftershockTiming");
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var veil=Reference<ParticleSystem>(dust,"dustVeil");
        var material=AssetDatabase.LoadAssetAtPath<Material>(art+"/PeakDust.mat");
        if(material==null)
        {
            material=new Material(veil.GetComponent<Renderer>().sharedMaterial){name="Peak structural dust"};
            material.SetColor("_BaseColor",new Color(.72f,.68f,.62f,.52f));material.SetFloat("_AmbientScatter",.012f);material.SetFloat("_ScatteringGain",.045f);material.SetFloat("_NearFade",.16f);
            AssetDatabase.CreateAsset(material,art+"/PeakDust.mat");
        }
        var main=veil.main;main.startSize=new ParticleSystem.MinMaxCurve(.36f,.62f);main.startColor=new ParticleSystem.MinMaxGradient(new Color(.60f,.57f,.51f,.48f),new Color(.76f,.71f,.64f,.68f));
        veil.GetComponent<Renderer>().sharedMaterial=material;EditorUtility.SetDirty(veil);
        var plumes=new SerializedObject(dust).FindProperty("fracturePlumes");for(int i=0;i<plumes.arraySize;i++){var ps=plumes.GetArrayElementAtIndex(i).objectReferenceValue as ParticleSystem;if(ps!=null){ps.GetComponent<Renderer>().sharedMaterial=material;EditorUtility.SetDirty(ps.GetComponent<Renderer>());}}
        if(!Application.isPlaying){AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());}
        var result=new{legacyFragmentsRetimed=count,largeRelease="outward and sideways, overhead preferentially lateral; no inward launch",peakMaterial=AssetDatabase.GetAssetPath(material)};
        File.WriteAllText("AgentScripts/ReviewImages/AftershockTiming/Flow.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object SaveRefinedFlow()
    {
        if(Application.isPlaying)throw new Exception("Save in edit mode");
        const string marker="Aftershock overhead dispersion applied";
        if(GameObject.Find(marker)!=null)throw new Exception("Already applied; do not move twice");
        RefineFlow();
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();
        foreach(var e in manager.Elements.Where(e=>e.target.name.StartsWith("Overhead breaking slab")))
        {
            e.target.localScale=Vector3.one*.72f;
            var center=e.target.GetComponent<Renderer>().bounds.center;
            e.target.position+=new Vector3(Mathf.Sign(center.x)*.05f,0,Mathf.Sign(center.z)*.05f);
        }
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();
        var fragments=new SerializedObject(debris).FindProperty("fragments");int moved=0;
        for(int i=0;i<fragments.arraySize;i++)
        {
            var t=fragments.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            if(t!=null&&t.position.z>.3f){t.position+=Vector3.forward*.08f;var body=t.GetComponent<Rigidbody>();if(body!=null)body.position=t.position;moved++;}
        }
        var go=new GameObject(marker);go.transform.SetParent(GameObject.Find("Prototype Environment").transform,false);
        Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        return new{overheadScale=.72f,overheadSideOffset=.05f,attachedSmallFragmentsMoved=moved};
    }
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Preserve unsaved changes first");
        if(GameObject.Find(Marker)!=null)throw new Exception("Timing already applied; do not run twice");
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();
        Number(director,"durationSeconds",20);Times(director,"impactTimes",new[]{5.35f,6.55f,7.85f,9.15f});
        Integer(director,"openingDustCount",0);Integer(director,"impactDustCount",145);Integer(director,"lateImpactDustCount",80);
        var releases=manager.Elements.Where(e=>e.releaseAt>0).Select(e=>e.releaseAt).ToArray();float first=releases.Min(),last=releases.Max();
        var moved=new HashSet<Transform>();var pocket=GameObject.Find("Close buried cell").transform;
        string[] frontNames={"Front face left pressed rubble","Front face right interlocked masonry","Center crushed bearing stone","Low overlapping fracture veil","Overlapping front ceiling lip","Near left wedged slab","Near right wedged slab"};
        foreach(var e in manager.Elements)
        {
            if(frontNames.Any(n=>e.target.name.StartsWith(n))&&!e.target.IsChildOf(pocket))moved.Add(e.target);
            if(e.releaseAt>0)e.releaseAt=Mathf.Lerp(5.15f,9.45f,Mathf.InverseLerp(first,last,e.releaseAt));
            if(e.settlesAt<30)e.settlesAt=Mathf.Lerp(5.15f,7.65f,Mathf.InverseLerp(3f,11f,e.settlesAt));
            if(e.loose!=null)
            {
                var so=new SerializedObject(e.loose);var property=so.FindProperty("releaseVelocity");var old=property.vector3Value;
                float side=Mathf.Sign(e.target.GetComponent<Renderer>().bounds.center.x);
                property.vector3Value=new Vector3(old.x*.12f+side*.035f,Mathf.Min(old.y,-.08f),old.z*.12f);
                so.ApplyModifiedProperties();
            }
        }
        foreach(var name in frontNames){var original=GameObject.Find(name);if(original!=null&&!original.transform.IsChildOf(pocket))moved.Add(original.transform);}
        foreach(var t in moved)t.position+=Vector3.forward*.08f;
        pocket.position+=Vector3.forward*.08f;
        var data=new SerializedObject(manager);var array=data.FindProperty("elements");
        for(int i=0;i<manager.Elements.Length;i++)
        {array.GetArrayElementAtIndex(i).FindPropertyRelative("releaseAt").floatValue=manager.Elements[i].releaseAt;array.GetArrayElementAtIndex(i).FindPropertyRelative("settlesAt").floatValue=manager.Elements[i].settlesAt;}
        data.ApplyModifiedProperties();
        foreach(var section in UnityEngine.Object.FindObjectsByType<AftershockFractureSection>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            Number(section,"opensAtSeconds",Mathf.Lerp(5.3f,9.1f,Mathf.InverseLerp(2,16,section.OpensAtSeconds)));
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();
        if(debris!=null)
        {
            var p=new SerializedObject(debris).FindProperty("releaseTimes");
            Times(debris,"releaseTimes",Enumerable.Range(0,p.arraySize).Select(i=>5.1f+i*.22f).ToArray());
        }
        var rain=UnityEngine.Object.FindFirstObjectByType<AftershockRubbleRain>();
        if(rain!=null)
        {
            Times(rain,"burstTimes",new[]{.7f,1.4f,2.1f,2.8f,3.5f,4.15f,4.75f,5.3f,5.9f,6.5f,7.15f,7.9f,8.6f,9.35f,10.5f,12f,13.8f,16.1f,18f});
            Integer(rain,"chipsPerJoint",5);rain.enabled=true;EditorUtility.SetDirty(rain);
            var joints=new SerializedObject(rain).FindProperty("joints");
            for(int i=0;i<joints.arraySize;i++)
            {
                var ps=joints.GetArrayElementAtIndex(i).objectReferenceValue as ParticleSystem;if(ps==null)continue;
                var main=ps.main;main.startSpeed=new ParticleSystem.MinMaxCurve(.015f,.055f);main.gravityModifier=.65f;
                main.startSize=new ParticleSystem.MinMaxCurve(.002f,.007f);main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.3f);
                EditorUtility.SetDirty(ps);
            }
        }
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();Number(dust,"collapseDensity",1.65f);Integer(dust,"openingVeilCount",0);
        data=new SerializedObject(dust);data.FindProperty("dustColor").colorValue=new Color(.025f,.023f,.020f,1);data.ApplyModifiedProperties();
        var veil=Reference<ParticleSystem>(dust,"dustVeil");
        if(veil!=null)
        {
            var main=veil.main;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(13,22);
            main.startSize=new ParticleSystem.MinMaxCurve(.24f,.46f);main.startSpeed=new ParticleSystem.MinMaxCurve(.018f,.055f);main.gravityModifier=.008f;
            main.startColor=new ParticleSystem.MinMaxGradient(new Color(.56f,.53f,.47f,.32f),new Color(.72f,.68f,.60f,.48f));
            var noise=veil.noise;noise.enabled=true;noise.strength=.045f;noise.frequency=.55f;noise.scrollSpeed=.12f;
            EditorUtility.SetDirty(veil);
        }
        var fine=Reference<ParticleSystem>(director,"extraDust");
        if(fine!=null)
        {
            var main=fine.main;main.startLifetime=new ParticleSystem.MinMaxCurve(7,18);main.startSize=new ParticleSystem.MinMaxCurve(.0015f,.0045f);main.startSpeed=new ParticleSystem.MinMaxCurve(.015f,.06f);main.gravityModifier=.015f;
            EditorUtility.SetDirty(fine);
        }
        var marker=new GameObject(Marker);marker.transform.SetParent(GameObject.Find("Prototype Environment").transform,false);
        EditorUtility.SetDirty(manager);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var result=new{duration=20,dustEnd=30,frontMoved=.08f,movedTransforms=moved.Select(t=>t.name).ToArray(),loosePieces=releases.Length,firstRelease=manager.Elements.Where(e=>e.releaseAt>0).Min(e=>e.releaseAt),lastRelease=manager.Elements.Max(e=>e.releaseAt),initialVelocity="12% of former horizontal speed, plus 3.5cm/s lateral release; gravity retained",rainEnabled=rain!=null&&rain.enabled};
        File.WriteAllText("AgentScripts/ReviewImages/AftershockTiming/Build.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
