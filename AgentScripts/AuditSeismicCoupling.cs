using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BreathInTheRubble.Prototype;
public static class AuditSeismicCoupling
{
    public static object End()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var result=new{scene=scene.path,scene.isDirty,playing=EditorApplication.isPlaying,Application.runInBackground,physicalHmd=DesktopPhoneTestDriver.HasPhysicalHmd,origins=UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length,inputManagers=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>(FindObjectsSortMode.None).Length};
        File.WriteAllText("AgentScripts/ReviewImages/SeismicCoupling/EndState.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object Run()
    {
        var root=GameObject.Find("Prototype Environment");var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var ground=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var big=manager.Elements.Where(e=>e.powder!=null).ToArray();Physics.SyncTransforms();var all=root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();int overlaps=0;float deepest=0;
        foreach(var e in big)foreach(var a in e.target.GetComponents<Collider>().Where(c=>c.enabled))foreach(var b in all)
        {
            if(a.attachedRigidbody==b.attachedRigidbody||!a.bounds.Intersects(b.bounds))continue;
            if(Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out var n,out float depth)&&depth>.0005f){overlaps++;deepest=Mathf.Max(deepest,depth);}
        }
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var ds=new SerializedObject(dust);var veil=ds.FindProperty("dustVeil").objectReferenceValue as ParticleSystem;var renderer=veil.GetComponent<ParticleSystemRenderer>();
        var active=UnityEngine.Object.FindObjectsByType<AftershockLoadWarning>(FindObjectsSortMode.None).Where(x=>x.isActiveAndEnabled).Select(x=>x.name).ToArray();
        var result=new{overlaps,deepest,boxes=big.Sum(e=>e.target.GetComponents<BoxCollider>().Length),activeWarnings=active,activePockets=UnityEngine.Object.FindObjectsByType<RubbleFailurePocket>(FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled),fractureSections=UnityEngine.Object.FindObjectsByType<AftershockFractureSection>(FindObjectsSortMode.None).Where(x=>x.isActiveAndEnabled).Select(x=>x.name),veil=new{veil.name,position=veil.transform.position.ToString(),shape=veil.shape.scale.ToString(),veil.main.maxParticles,veil.main.startLifetime.constantMin,lifeMax=veil.main.startLifetime.constantMax,material=renderer.sharedMaterial.name,renderer.enabled,layers=ds.FindProperty("aftershockDust").arraySize},hands=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<AftershockAudio>()).FindProperty("hands").arraySize,origins=UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length,inputManagers=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>(FindObjectsSortMode.None).Length};
        File.WriteAllText("AgentScripts/ReviewImages/SeismicCoupling/Audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
