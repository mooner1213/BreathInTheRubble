using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class FinishGroundedArt
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var parent=GameObject.Find("Interlocked Rubble Structure").transform;if(parent.Find("Broken shaft bearing edges")!=null)throw new Exception("Already finished");
        var group=new GameObject("Broken shaft bearing edges");group.transform.SetParent(parent,false);int count=0;
        for(int level=0;level<3;level++)for(int edge=0;edge<4;edge++){
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Props/Wall_Grey_Small_0"+(edge%3+1)+".prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,group.transform);go.name="Bearing rim shard "+level+" "+edge;go.transform.rotation=Quaternion.Euler(edge%2==0?8:-7,edge*83+level*19,edge%2==0?17:-13);go.transform.localScale=Vector3.one*.86f;
            foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
            var lod=go.GetComponent<LODGroup>();var ls=lod.GetLODs();lod.enabled=false;for(int i=0;i<ls.Length;i++)foreach(var r in ls[i].renderers){r.enabled=i==0;r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;}
            var bounds=ls[0].renderers[0].bounds;foreach(var r in ls[0].renderers.Skip(1))bounds.Encapsulate(r.bounds);
            var center=new[]{new Vector3(-.43f,0,.28f),new Vector3(.43f,0,.22f),new Vector3(.04f,0,-.24f),new Vector3(-.05f,0,.78f)}[edge];center.y=1.87f+level*.4f;center.x+=(level-1)*.025f;
            go.transform.position+=center-bounds.center;count++;
        }
        var mats=new Dictionary<Material,Material>();int haze=0;
        foreach(var system in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.name.Contains("Veil")||p.name.Contains("Plume"))){
            if(!system.transform.IsChildOf(GameObject.Find("Prototype Game Flow").transform))continue;
            var renderer=system.GetComponent<ParticleSystemRenderer>();var source=renderer.sharedMaterial;if(source==null)continue;
            if(!mats.TryGetValue(source,out var material)){material=new Material(source){name=source.name+" Diffuse Grounded Dust"};var color=material.GetColor("_BaseColor");color.a*=.28f;material.SetColor("_BaseColor",color);if(material.HasProperty("_ScatteringGain"))material.SetFloat("_ScatteringGain",source.GetFloat("_ScatteringGain")*.3f);if(material.HasProperty("_AmbientScatter"))material.SetFloat("_AmbientScatter",0);if(material.HasProperty("_SoftDistance"))material.SetFloat("_SoftDistance",.16f);if(material.HasProperty("_NearFade"))material.SetFloat("_NearFade",.3f);AssetDatabase.CreateAsset(material,"Assets/_Project/Art/InterlockedRubble/"+material.name+".mat");mats.Add(source,material);}
            renderer.sharedMaterial=material;var main=system.main;main.startSizeMultiplier*=.65f;var noise=system.noise;noise.enabled=true;noise.strength=.035f;noise.frequency=.45f;noise.scrollSpeed=.08f;haze++;
        }
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var data=new SerializedObject(ending);var blockers=data.FindProperty("lightBlockers");var rs=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();blockers.arraySize=rs.Length;for(int i=0;i<rs.Length;i++)blockers.GetArrayElementAtIndex(i).objectReferenceValue=rs[i];data.ApplyModifiedProperties();
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return new{roughBearingEdges=count,diffuseDustSystems=haze,materials=mats.Count};
    }
}
