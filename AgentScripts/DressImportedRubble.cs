using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class DressImportedRubble
{
    public static object FinalizeRoof()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode only");
        foreach(var name in new[]{"Low Ceiling Rear","Low Ceiling Front"})GameObject.Find(name).transform.Find("Structural Backing - Original").GetComponent<Renderer>().enabled=false;
        var ending=UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.RescueEndingDirector>();var so=new SerializedObject(ending);var blockers=so.FindProperty("lightBlockers");int first=blockers.arraySize;
        var roofs=GameObject.Find("Prototype Environment").GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Imported Reinforced")).SelectMany(t=>t.GetComponent<LODGroup>().GetLODs()[0].renderers).ToArray();
        blockers.arraySize=first+roofs.Length;for(int i=0;i<roofs.Length;i++)blockers.GetArrayElementAtIndex(first+i).objectReferenceValue=roofs[i];so.ApplyModifiedProperties();
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{roofBlockers=roofs.Length};
    }
    public static object Finish()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode only");int created=0;
        foreach(var source in AssetDatabase.FindAssets("t:Material",new[]{"Assets/RubbleAndDebris/Material"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>)){
            var path="Assets/_Project/Art/ImportedRubble/Materials/"+source.name+"_URP.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(path)!=null)continue;
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=source.name+"_URP",enableInstancing=true};
            foreach(var pair in new[]{new[]{"_MainTex","_BaseMap"},new[]{"_BumpMap","_BumpMap"},new[]{"_MetallicGlossMap","_MetallicGlossMap"},new[]{"_OcclusionMap","_OcclusionMap"}})
                if(source.HasProperty(pair[0]))m.SetTexture(pair[1],source.GetTexture(pair[0]));
            m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.16f);m.SetFloat("_BumpScale",.8f);m.SetFloat("_OcclusionStrength",.85f);
            if(m.GetTexture("_BumpMap")!=null)m.EnableKeyword("_NORMALMAP");if(m.GetTexture("_MetallicGlossMap")!=null)m.EnableKeyword("_METALLICSPECGLOSSMAP");if(m.GetTexture("_OcclusionMap")!=null)m.EnableKeyword("_OCCLUSIONMAP");
            AssetDatabase.CreateAsset(m,path);created++;
        }
        var env=GameObject.Find("Prototype Environment");
        foreach(var t in env.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Imported "))){
            string prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);if(string.IsNullOrEmpty(prefab))continue;
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
            foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true)){
                var template=original.GetComponentsInChildren<MeshRenderer>(true).First(x=>x.name==r.name);
                r.sharedMaterials=template.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();
            }
            if(t.name=="Imported Front Collapse Bed")t.rotation=Quaternion.Euler(-74,8,0);
            if(t.name=="Imported Left Broken Wall")t.position=new Vector3(t.position.x+.12f,t.position.y,t.position.z);
            if(t.name=="Imported Right Broken Wall")t.position=new Vector3(t.position.x-.12f,t.position.y,t.position.z);
            if(t.name.Contains("Roof")){
                var s=t.parent.lossyScale;float scale=t.name.Contains("Rear")?.60f:.50f;t.localScale=new Vector3(scale/s.x,scale/s.y,scale/s.z);
                var rs=t.GetComponentsInChildren<LODGroup>()[0].GetLODs()[0].renderers;var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
                var center=t.name.Contains("Rear")?new Vector3(-.04f,1.34f,-.23f):new Vector3(.07f,1.30f,.66f);t.position+=center-b.center;
            }
        }
        foreach(var m in AssetDatabase.FindAssets("t:Material",new[]{"Assets/_Project/Art/ImportedRubble/Materials"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>)){m.SetFloat("_Cull",0);EditorUtility.SetDirty(m);}
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return new{additionalMaterials=created};
    }
    static GameObject Add(string parent,string asset,string name,Vector3 center,Vector3 rotation,float scale)
    {
        var anchor=GameObject.Find(parent);if(anchor==null)throw new Exception(parent);
        if(anchor.transform.Find(name)!=null)throw new Exception("Already dressed: "+name);
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/"+asset+".prefab");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(original,anchor.transform);Undo.RegisterCreatedObjectUndo(go,"Dress imported collapse");go.name=name;
        go.transform.rotation=Quaternion.Euler(rotation);go.transform.localScale=Vector3.one*scale;
        var rs=go.GetComponentsInChildren<MeshRenderer>(true);
        foreach(var r in rs){r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();}
        foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
        var lod=go.GetComponent<LODGroup>();var r0=lod.GetLODs()[0].renderers;var bounds=r0[0].bounds;foreach(var r in r0.Skip(1))bounds.Encapsulate(r.bounds);
        go.transform.position+=center-bounds.center;
        return go;
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode only");var scene=SceneManager.GetActiveScene();
        int hidden=0;
        foreach(var r in GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true)){
            var n=r.name;
            if(n.StartsWith("Settled Concrete Chip")||n.StartsWith("Settled Lap Fragment")||n.StartsWith("Angular Fragment")){
                Undo.RecordObject(r,"Retire floating decorative fragments");r.enabled=false;hidden++;
            }
        }
        Add("Front Rubble Seal","Piles/Rubble_Big","Imported Front Collapse Bed",new Vector3(0,.61f,.99f),new Vector3(74,8,0),.29f);
        Add("Buried Lower Body Rubble","Piles/Rubble_Medium","Imported Compacted Lower Rubble",new Vector3(-.05f,.31f,.53f),new Vector3(0,12,0),.31f);
        Add("Left Collapsed Wall Layers","WithRebar/Rebar_Wall_04","Imported Left Broken Wall",new Vector3(-.68f,.76f,.02f),new Vector3(0,5,90),.73f);
        Add("Right Collapsed Wall Layers","WithRebar/Rebar_Wall_01","Imported Right Broken Wall",new Vector3(.72f,.76f,.04f),new Vector3(0,-4,-90),.62f);
        Add("Low Ceiling Rear","WithRebar/Rebar_Wall_01","Imported Reinforced Rear Roof",new Vector3(-.04f,1.36f,-.23f),new Vector3(0,8,0),.60f);
        Add("Low Ceiling Front","WithRebar/Rebar_Wall_04","Imported Reinforced Front Roof",new Vector3(.07f,1.28f,.66f),new Vector3(2,-15,3),.50f);
        // Additional native geometry is parented to existing tremor/rescue targets. No new physics or input.
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{nativePrefabs=6,retiredFloatingDetails=hidden};
    }
}
