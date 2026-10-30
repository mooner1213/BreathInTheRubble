using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
public static class RefineConfinedSpace
{
    public static string Run()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("Confined scene must be active in Edit mode.");
        var environment=GameObject.Find("Prototype Environment").transform;
        var all=environment.GetComponentsInChildren<Transform>();
        void Place(string name,Vector3 pos,Vector3 scale) {var t=all.First(x=>x.name==name);t.position=pos;t.localScale=scale;}
        Place("Lower Body Rubble",new Vector3(-0.10f,0.32f,0.47f),new Vector3(0.73f,0.48f,0.76f));
        Place("Front Compression Slab",new Vector3(-0.12f,0.48f,0.60f),new Vector3(0.83f,0.17f,0.45f));
        Place("Right Thigh Rubble",new Vector3(0.12f,0.36f,0.50f),new Vector3(0.30f,0.22f,0.34f));
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/_Project/Art/ConfinedSpace"}))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if(m.HasProperty("_GrainScale")) {m.SetFloat("_GrainScale",240f);EditorUtility.SetDirty(m);}
        }
        foreach(var system in environment.GetComponentsInChildren<ParticleSystem>())
        {
            var renderer=system.GetComponent<ParticleSystemRenderer>();var source=renderer.sharedMaterial;
            var path="Assets/_Project/Art/ConfinedSpace/"+source.name+" Soft.mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(source){name=source.name+" Soft"};AssetDatabase.CreateAsset(m,path);}
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_BlendModePreserveSpecular",0f);m.SetFloat("_Smoothness",0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            renderer.sharedMaterial=m;EditorUtility.SetDirty(m);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        return "Refined fine concrete grain; removed square particle specular highlights; kept the phone pocket clear.";
    }
}
