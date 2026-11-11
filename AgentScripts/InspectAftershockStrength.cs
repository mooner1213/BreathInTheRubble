using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using BreathInTheRubble.Prototype;
public static class InspectAftershockStrength
{
    public static object End()
    {
        var ground=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();var so=new SerializedObject(ground);
        var result=new{scene=ground.gameObject.scene.path,ground.gameObject.scene.isDirty,playing=EditorApplication.isPlaying,distance=so.FindProperty("roomShakeDistance").floatValue,degrees=so.FindProperty("roomShakeDegrees").floatValue,loadReference=so.FindProperty("loadAccelerationReference").floatValue,scale=ground.ExcitationScale,physicalHmd=DesktopPhoneTestDriver.HasPhysicalHmd,origins=UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length,inputManagers=UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>(FindObjectsSortMode.None).Length};
        File.WriteAllText("AgentScripts/ReviewImages/AftershockStrength/EndState.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object Run()
    {
        var ground=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();var so=new SerializedObject(ground);
        var result=new{scene=ground.gameObject.scene.path,playing=EditorApplication.isPlaying,distance=so.FindProperty("roomShakeDistance").floatValue,degrees=so.FindProperty("roomShakeDegrees").floatValue,scale=ground.ExcitationScale,root=so.FindProperty("environmentRoot").objectReferenceValue?.name,head=so.FindProperty("head").objectReferenceValue?.name};
        Directory.CreateDirectory("AgentScripts/ReviewImages/AftershockStrength");File.WriteAllText("AgentScripts/ReviewImages/AftershockStrength/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
