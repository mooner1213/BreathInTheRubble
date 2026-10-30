using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class InspectConfinedSetup
{
    public static object Run() => new {
        scene = SceneManager.GetActiveScene().path,
        camera = Camera.main.transform.position.ToString(),
        lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(l => new {l.name,l.enabled,l.intensity,l.range,position=l.transform.position.ToString()}).ToArray(),
        origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>().RequestedTrackingOriginMode.ToString(),
        grab = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().interactionLayers.value,
        materials = AssetDatabase.FindAssets("t:Material",new[]{"Assets/_Project"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).Select(m=>new {m.name,shader=m.shader.name,texture=m.mainTexture!=null?m.mainTexture.name:"none"}).ToArray()
    };
}
