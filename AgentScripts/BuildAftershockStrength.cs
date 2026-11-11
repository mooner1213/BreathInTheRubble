using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BreathInTheRubble.Prototype;
public static class BuildAftershockStrength
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var ground=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();var s=new SerializedObject(ground);
        s.FindProperty("roomShakeDistance").floatValue=.055f;s.FindProperty("roomShakeDegrees").floatValue=.7f;
        // Stronger motion must not exhaust the existing bearings before the requested 5–10 second failure window.
        s.FindProperty("loadAccelerationReference").floatValue=3.85f;s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(ground);
        EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);EditorSceneManager.SaveScene(ground.gameObject.scene);
        var result=new{distance=.055f,degrees=.7f,loadReference=3.85f,envelope="squared smoothstep 0–5; full 5–10; decay 10–20"};
        File.WriteAllText("AgentScripts/ReviewImages/AftershockStrength/Build.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
