using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectRescueSetup
{
    public static object Run()
    {
        var scene=SceneManager.GetActiveScene();var camera=Camera.main;
        var data=camera.GetComponent<UniversalAdditionalCameraData>();
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        return new{scene=scene.path,dirty=scene.isDirty,head=camera.transform.position.ToString("F3"),camera=new{camera.nearClipPlane,camera.farClipPlane,camera.allowHDR,post=data.renderPostProcessing,volumeMask=data.volumeLayerMask.value},
            volumes=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Select(v=>new{v.name,v.weight,v.priority,profile=AssetDatabase.GetAssetPath(v.sharedProfile),effects=v.sharedProfile?.components.Select(c=>c.GetType().Name)}),
            lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Select(l=>new{l.name,l.type,l.intensity,l.range,l.shadows,p=l.transform.position.ToString("F2")}),
            overhead=all.Select(t=>t.GetComponent<Renderer>()).Where(r=>r!=null && r.gameObject.activeInHierarchy && r.bounds.max.y>1.02f && r.bounds.min.y<1.55f && r.bounds.center.magnitude<2.6f).Select(r=>new{r.name,parent=r.transform.parent?.name,b=r.bounds.ToString("F3"),p=r.transform.position.ToString("F3"),collider=r.GetComponent<Collider>()?.GetType().Name,staticObject=r.gameObject.isStatic}).Take(95),
            commonUI=AssetDatabase.IsValidFolder("Assets/DisasterTrainingSharedUI"),phonePrefab=AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>().gameObject)),
            pipe=UnityEngine.Object.FindFirstObjectByType<PipeImpactAudio>()?.name};
    }
}
