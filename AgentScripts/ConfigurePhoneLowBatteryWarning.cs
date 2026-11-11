using System;
using System.IO;
using System.Linq;
using BreathInTheRubble.Prototype;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigurePhoneLowBatteryWarning
{
    const string PrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";

    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit mode required.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Project/Scenes/ConfinedPrototype.unity") throw new Exception("ConfinedPrototype must be active.");
        var phone = UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>();
        if (phone == null) throw new Exception("No phone in active scene.");
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backup = "Assets/_Project/Scenes/Backups/BeforeLowBatteryWarning_" + stamp + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new Exception("Scene backup failed.");
        Directory.CreateDirectory("Temp/PhoneWarningBackups");
        File.Copy(PrefabPath, "Temp/PhoneWarningBackups/SmartphonePrototype_" + stamp + ".prefab", false);

        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Configure(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        Configure(phone.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        return new { result = "Configured 24% red battery and dismissible on-phone popup", backup };
    }

    static void Configure(GameObject phone)
    {
        var canvas = phone.transform.Find("Phone Interface Canvas");
        if (canvas == null) throw new Exception("Phone canvas missing.");
        var batteryRoot = canvas.Find("Battery 47 Percent");
        var view = phone.GetComponent<PhoneBatteryView>();
        if (batteryRoot == null || view == null) throw new Exception("Battery display missing.");
        var viewData = new SerializedObject(view);
        var graphics = batteryRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        var array = viewData.FindProperty("batteryGraphics");
        array.arraySize = graphics.Length;
        for (int i = 0; i < graphics.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = graphics[i];
        viewData.ApplyModifiedProperties();

        var banner = canvas.Find("Low Battery Warning");
        if (banner == null)
        {
            var go = new GameObject("Low Battery Warning", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            banner = go.transform;
            banner.SetParent(canvas, false);
        }
        var rect = (RectTransform)banner;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -135f);
        rect.sizeDelta = new Vector2(610f, 180f);
        var background = banner.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.20f, 0.025f, 0.025f, 1f);
        background.raycastTarget = false;

        var message = banner.Find("Low Battery Message");
        if (message == null)
        {
            message = new GameObject("Low Battery Message", typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
            message.SetParent(banner, false);
        }
        var tr = (RectTransform)message;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(18f, 12f); tr.offsetMax = new Vector2(-85f, -12f);
        var text = message.GetComponent<TMP_Text>();
        text.font = batteryRoot.GetComponentInChildren<TMP_Text>(true).font;
        text.text = "LOW BATTERY\n24% remaining\nUse the flashlight sparingly.";
        text.fontSize = 32f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.65f, 0.62f, 1f);
        text.raycastTarget = false;

        var close = banner.Find("Low Battery Dismiss");
        if (close == null)
        {
            close = new GameObject("Low Battery Dismiss", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)).transform;
            close.SetParent(banner, false);
        }
        var cr = (RectTransform)close;
        cr.anchorMin = cr.anchorMax = new Vector2(1f, 0.5f);
        cr.pivot = new Vector2(1f, 0.5f);
        cr.anchoredPosition = new Vector2(-10f, 0f);
        cr.sizeDelta = new Vector2(65f, 120f);
        var closeImage = close.GetComponent<UnityEngine.UI.Image>();
        closeImage.color = new Color(0.38f, 0.055f, 0.05f, 1f);
        closeImage.raycastTarget = true;
        var button = close.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = closeImage;
        var label = close.Find("Label");
        if (label == null)
        {
            label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
            label.SetParent(close, false);
        }
        var lr = (RectTransform)label;
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        var x = label.GetComponent<TMP_Text>();
        x.font = text.font; x.text = "X"; x.fontSize = 36f;
        x.alignment = TextAlignmentOptions.Center; x.raycastTarget = false;

        var warning = phone.GetComponent<PhoneLowBatteryWarning>();
        if (warning == null) warning = phone.AddComponent<PhoneLowBatteryWarning>();
        var data = new SerializedObject(warning);
        data.FindProperty("battery").objectReferenceValue = phone.GetComponent<PhoneBattery>();
        data.FindProperty("warningBanner").objectReferenceValue = banner.gameObject;
        data.FindProperty("dismissButton").objectReferenceValue = button;
        data.FindProperty("messageText").objectReferenceValue = text;
        data.ApplyModifiedProperties();
        banner.SetAsLastSibling();
        banner.gameObject.SetActive(false);
        if (PrefabUtility.IsPartOfPrefabInstance(warning))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(warning);
        }
    }
}
