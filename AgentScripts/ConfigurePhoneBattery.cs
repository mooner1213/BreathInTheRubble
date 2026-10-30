using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigurePhoneBattery
{
    const string PrefabPath="Assets/_Project/Prefabs/SmartphonePrototype.prefab";
    public static object Inspect()
    {
        var phone=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>();
        return new{scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,
            batteryComponents=phone.GetComponents<Component>().Where(c=>c.GetType().Name.Contains("Battery")).Select(c=>c.GetType().Name).ToArray(),
            elements=phone.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name.StartsWith("Battery")).Select(t=>new{t.name,rect=t.rect.ToString(),text=t.GetComponent<TMPro.TMP_Text>()?.text}).ToArray()};
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();var phone=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>();
        if(phone==null)throw new Exception("Phone missing.");
        if(scene.isDirty)
        {
            var path="Assets/_Project/Scenes/Backups/BeforeBattery_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
            if(!EditorSceneManager.SaveScene(scene,path,true))throw new Exception("Could not preserve unsaved scene changes.");
        }
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try{Configure(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        Configure(phone.gameObject);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Battery starts at47%; flashlight0.36pp/min; dialing0.60pp/min; connected0.24pp/min; fractional icon and integer display.";
    }
    static void Configure(GameObject phone)
    {
        var fill=phone.GetComponentsInChildren<RectTransform>(true).First(t=>t.name=="Battery Fill");
        var text=phone.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Battery Text");
        var battery=phone.GetComponent<PhoneBattery>()??phone.AddComponent<PhoneBattery>();
        if(phone.GetComponent<PhoneBatteryUsage>()==null)phone.AddComponent<PhoneBatteryUsage>();
        var view=phone.GetComponent<PhoneBatteryView>()??phone.AddComponent<PhoneBatteryView>();
        var data=new SerializedObject(view);
        data.FindProperty("battery").objectReferenceValue=battery;
        data.FindProperty("percentText").objectReferenceValue=text;
        data.FindProperty("fillRect").objectReferenceValue=fill;
        data.FindProperty("fullFillWidth").floatValue=90f;data.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
    }
}
