using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TunePhoneRearFlashlight
{
    const string PrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";

    public static string Run()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var light = root.GetComponentsInChildren<Light>(true).FirstOrDefault(item => item.name == "Rear Flashlight")
                ?? throw new InvalidOperationException("Rear Flashlight was not found.");
            light.intensity = 8f;
            light.range = 4.5f;
            light.spotAngle = 64f;
            light.innerSpotAngle = 34f;
            EditorUtility.SetDirty(light);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return $"Rear flashlight saved: intensity={light.intensity}, range={light.range}, spot={light.spotAngle}, inner={light.innerSpotAngle}.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
