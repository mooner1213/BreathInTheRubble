using System;
using System.Reflection;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class FinalizeBottlePreview
{
    public static string Run()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("Confined scene must be active in Edit mode.");
        var visual=UnityEngine.Object.FindFirstObjectByType<BottleLiquidVisual>();
        const string path="Assets/_Project/Art/ConfinedSpace/Water 45 Percent Preview.asset";
        var preview=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(preview==null)
        {
            var clone=UnityEngine.Object.Instantiate(visual.gameObject);
            try
            {
                typeof(BottleLiquidVisual).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(clone.GetComponent<BottleLiquidVisual>(),null);
                preview=UnityEngine.Object.Instantiate(clone.GetComponent<MeshFilter>().sharedMesh);preview.name="Water 45 Percent Preview";
                AssetDatabase.CreateAsset(preview,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(clone);}
        }
        visual.GetComponent<MeshFilter>().sharedMesh=preview;
        const string prefabPath="Assets/_Project/Prefabs/WaterBottle45.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try {prefab.GetComponentInChildren<BottleLiquidVisual>().GetComponent<MeshFilter>().sharedMesh=preview;PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}
        finally {PrefabUtility.UnloadPrefabContents(prefab);}
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        return "45% liquid is visible in Edit mode and Play mode; runtime volume source remains the full bottle interior.";
    }
}
