using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TunePhoneLightBalance
{
    const string PrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";

    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Apply in Edit mode.");
        var scene=SceneManager.GetActiveScene();
        if(scene.isDirty) throw new InvalidOperationException("Save current scene edits first.");
        var phones=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PhoneWakeSequence>(true)).ToArray();
        if(phones.Length!=1)throw new InvalidOperationException("Expected one active scene phone.");
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try { Configure(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        Configure(phones[0].gameObject);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "scene="+scene.path+"|UIbrightness=55%|screenEmission=20%|screenSpill=0.08cd|flashlight=112deg/55deg/3cd";
    }

    static void Configure(GameObject phone)
    {
        var sequence=new SerializedObject(phone.GetComponent<PhoneWakeSequence>());
        sequence.FindProperty("screenEmissionColor").colorValue=new Color(0.048f,0.164f,0.280f,1f);
        sequence.FindProperty("screenOnColor").colorValue=new Color(0.010f,0.048f,0.072f,1f);
        sequence.ApplyModifiedProperties();
        var controller=new SerializedObject(phone.GetComponent<PhoneInterfaceController>());
        controller.FindProperty("screenBrightness").floatValue=0.55f;controller.ApplyModifiedProperties();
        foreach(var light in phone.GetComponentsInChildren<Light>(true))
        {
            Undo.RecordObject(light,"Balance phone lighting");
            if(light.name=="Rear Flashlight")
            {
                light.spotAngle=112f;light.innerSpotAngle=55f;light.intensity=3f;light.range=4.5f;
            }
            if(light.name=="Screen Spill")
            {
                light.intensity=0.08f;
                var spill=light.GetComponent<PhoneScreenSpillLight>();
                if(spill!=null){var data=new SerializedObject(spill);data.FindProperty("maximumIntensity").floatValue=0.08f;data.ApplyModifiedProperties();}
            }
            EditorUtility.SetDirty(light);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(phone.GetComponent<PhoneWakeSequence>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(phone.GetComponent<PhoneInterfaceController>());
        foreach(var light in phone.GetComponentsInChildren<Light>(true))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(light);
            var spill=light.GetComponent<PhoneScreenSpillLight>();if(spill!=null)PrefabUtility.RecordPrefabInstancePropertyModifications(spill);
        }
    }
}
