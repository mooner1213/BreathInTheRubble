using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using BreathInTheRubble.Prototype;
public static class SealStructuralGaps
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");int count=0;
        foreach(var e in Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText("Temp/StructuralAftershockReview/GapDirections.json")))
        {
            var direction=new Vector3((float)e[0],(float)e[1],(float)e[2]);var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Props/Wall_Gray_Medium_02.prefab"),GameObject.Find("Burial Roof Outer Layer").transform);go.name="Overlapping outer roof wedge "+(++count);go.transform.rotation=Quaternion.FromToRotation(Vector3.up,-direction);go.transform.localScale=Vector3.one*1.55f;
            foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;var lod=go.GetComponent<LODGroup>();var levels=lod.GetLODs();lod.enabled=false;for(int i=0;i<levels.Length;i++)foreach(var r in levels[i].renderers){r.enabled=i==0;r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;}
            var bounds=levels[0].renderers[0].bounds;foreach(var r in levels[0].renderers.Skip(1))bounds.Encapsulate(r.bounds);go.transform.position+=new Vector3(0,.9f,0)+direction*2.2f-bounds.center;
        }
        var ending=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>());var list=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();var refs=ending.FindProperty("lightBlockers");refs.arraySize=list.Length;for(int i=0;i<list.Length;i++)refs.GetArrayElementAtIndex(i).objectReferenceValue=list[i];ending.ApplyModifiedProperties();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{outerPaidWedges=count};
    }
}
