using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;

public static class BuildSealedBurial
{
    static int count;
    static GameObject Add(Transform parent,string asset,Vector3 center,Quaternion rotation,float scale,string name)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/"+asset+".prefab");
        if(source==null)throw new Exception(asset);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);go.name=name;go.transform.rotation=rotation;go.transform.localScale=Vector3.one*scale;
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true)){
            r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();
            if(r.sharedMaterials.Any(m=>m==null))throw new Exception("Missing material "+asset);
            r.shadowCastingMode=ShadowCastingMode.TwoSided;r.receiveShadows=true;
        }
        foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
        var group=go.GetComponent<LODGroup>();var lods=group.GetLODs();lods[lods.Length-1].screenRelativeTransitionHeight=0;group.SetLODs(lods);
        var rs=lods[0].renderers;var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);go.transform.position+=center-bounds.center;
        Undo.RegisterCreatedObjectUndo(go,"Build sealed rubble burial");count++;return go;
    }
    static Transform Group(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(go,"Build sealed rubble burial");return go.transform;}
    static void Append(SerializedObject data,string field,UnityEngine.Object[] objects){var p=data.FindProperty(field);int n=p.arraySize;p.arraySize+=objects.Length;for(int i=0;i<objects.Length;i++)p.GetArrayElementAtIndex(n+i).objectReferenceValue=objects[i];}
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("Wrong scene");
        if(GameObject.Find("Sealed Rubble Burial")!=null)throw new Exception("Burial already exists");
        string backup="Assets/_Project/Scenes/Backups/BeforeSealedBurial_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
        var env=GameObject.Find("Prototype Environment");
        int retired=0;
        foreach(var r in env.GetComponentsInChildren<MeshRenderer>(true)){
            bool shell=r.transform.parent?.name=="Concrete Shell"||r.name=="Structural Backing - Original";
            bool voidPlane=r.transform.parent?.name=="Deep Voids";
            if((shell||voidPlane)&&r.enabled){Undo.RecordObject(r,"Remove exposed enclosing wall visuals");r.enabled=false;retired++;}
        }
        var root=Group(env.transform,"Sealed Rubble Burial");var sides=Group(root,"Overlapping Side Collapse");
        var roofs=new[]{Group(root,"Burial Roof Inner Layer"),Group(root,"Burial Roof Middle Layer"),Group(root,"Burial Roof Outer Layer")};
        var random=new System.Random(71061);float J(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
        string[] piles={"Piles/Rubble_Big","Piles/Rubble_Medium","Piles/Rubble_CornerOut","Piles/Rubble_Small_03"};
        for(int layer=0;layer<3;layer++){
            int n=12+layer*2;float radius=1.38f+layer*.61f;
            for(int i=0;i<n;i++){
                float angle=(i+.5f*(layer%2))*Mathf.PI*2/n;var direction=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                var rotation=Quaternion.FromToRotation(Vector3.up,-direction)*Quaternion.Euler(0,J(-15,15),J(-5,5));
                Add(sides,piles[(i+layer)%piles.Length],direction*radius+Vector3.up*(.58f+layer*.14f),rotation,.53f+layer*.15f,"Buried side layer "+(layer+1)+" piece "+(i+1));
            }
        }
        for(int layer=0;layer<3;layer++)for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++){
            var position=new Vector3(x*.88f+(layer==1?.23f:-.08f),1.75f+layer*.4f,z*.92f+(layer==1?.31f:-.11f));
            string asset=(x+z+layer)%2==0?"WithRebar/Rebar_Wall_01":"WithRebar/Rebar_Wall_04";
            Add(roofs[layer],asset,position,Quaternion.Euler(J(-4,4),layer*28+J(-12,12),J(-4,4)),.72f+layer*.06f,"Overlapped reinforced roof "+x+" "+z);
        }
        for(int i=0;i<4;i++)Add(sides,piles[i],new Vector3(i%2==0?-.55f:.55f,-.01f,i<2?-.38f:.55f),Quaternion.Euler(0,i*69,0),.37f,"Buried floor rubble "+i);
        foreach(var light in GameObject.Find("Prototype Lighting").GetComponentsInChildren<Light>(true))light.enabled=false;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.reflectionIntensity=0;
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var data=new SerializedObject(ending);
        int first=data.FindProperty("rubble").arraySize;Append(data,"rubble",roofs.Cast<UnityEngine.Object>().ToArray());
        var starts=data.FindProperty("removalStarts");var offsets=data.FindProperty("removalOffsets");starts.arraySize=offsets.arraySize=first+3;
        float[] times={6f,20f,35f};Vector3[] moves={new Vector3(-3.6f,1.5f,.1f),new Vector3(3.9f,1.6f,.5f),new Vector3(-4.2f,1.8f,-.2f)};
        for(int i=0;i<3;i++){starts.GetArrayElementAtIndex(first+i).floatValue=times[i];offsets.GetArrayElementAtIndex(first+i).vector3Value=moves[i];}
        var sky=new Material(Shader.Find("Skybox/Procedural")){name="Natural Rescue Daylight Sky"};sky.SetFloat("_SunDisk",0);sky.SetFloat("_AtmosphereThickness",1);sky.SetFloat("_Exposure",1);sky.SetColor("_SkyTint",new Color(.55f,.6f,.66f));sky.SetColor("_GroundColor",new Color(.22f,.22f,.21f));
        AssetDatabase.CreateAsset(sky,"Assets/_Project/Art/RescueEnding/NaturalDaylightSky.mat");data.FindProperty("daylightSky").objectReferenceValue=sky;
        var sun=(Light)data.FindProperty("openingLight").objectReferenceValue;sun.intensity=1.45f;sun.color=new Color(1,.96f,.9f);sun.shadows=LightShadows.Soft;sun.shadowBias=.01f;sun.shadowNormalBias=.02f;sun.enabled=false;
        var bounce=(Light)data.FindProperty("softFill").objectReferenceValue;bounce.intensity=0;bounce.enabled=false;bounce.transform.position=new Vector3(0,1.38f,.18f);bounce.range=1.7f;
        foreach(string field in new[]{"whiteout","sunShafts","outsideSky"}){var r=(Renderer)data.FindProperty(field).objectReferenceValue;if(r!=null)r.enabled=false;}
        var volume=(Volume)data.FindProperty("endingVolume").objectReferenceValue;var profile=UnityEngine.Object.Instantiate(volume.sharedProfile);profile.name="Natural Opening Tone";
        if(profile.TryGet<ColorAdjustments>(out var color)){color.postExposure.Override(.1f);color.contrast.Override(0);}
        if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.06f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.45f);}
        if(profile.TryGet<Tonemapping>(out var tone))tone.mode.Override(TonemappingMode.Neutral);
        AssetDatabase.CreateAsset(profile,"Assets/_Project/Art/RescueEnding/NaturalOpeningVolume.asset");volume.sharedProfile=profile;volume.weight=0;
        // Include every layer so the small bounce responds to actual clearance, not the timeline.
        var blockers=env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();var p=data.FindProperty("lightBlockers");p.arraySize=blockers.Length;for(int i=0;i<blockers.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=blockers[i];data.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return new{backup,paidRubblePrefabs=count,retiredWallRenderers=retired,layers=3,roofGroups=3,blockers=blockers.Length};
    }
}
