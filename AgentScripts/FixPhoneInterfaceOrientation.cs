using System;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class FixPhoneInterfaceOrientation
{
    const string PrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";
    const string FontPath = "Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF NoBackfaceCulling.asset";

    public static string Run()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var canvas = root.transform.Find("Phone Interface Canvas") as RectTransform
                ?? throw new InvalidOperationException("Phone Interface Canvas was not found.");
            canvas.localScale = new Vector3(-0.0001f, 0.0001f, 0.0001f);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                ?? throw new InvalidOperationException("No-backface-culling Inter font was not found.");
            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
                text.font = font;

            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return $"Corrected phone UI orientation and updated {canvas.GetComponentsInChildren<TMP_Text>(true).Length} text elements.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
