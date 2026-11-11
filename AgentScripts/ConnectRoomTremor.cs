using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConnectRoomTremor
{
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        if(motion==null)throw new Exception("Aftershock motion missing.");
        var names=new[]{"Front Rubble Seal","Left Wall","Right Wall","Rear Wall","Left Collapsed Wall Layers","Right Collapsed Wall Layers","Rear Left Fractured Panel","Rear Right Stacked Slab","Left Wall Crack","Right Wall Crack","Rear Crack Main","Rear Crack Branch A","Rear Crack Branch B"};
        var targets=names.Select(n=>GameObject.Find(n)?.transform??throw new Exception("Structure missing: "+n)).ToArray();
        var data=new SerializedObject(motion);var array=data.FindProperty("roomTargets");array.arraySize=targets.Length;
        for(int i=0;i<targets.Length;i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue=targets[i];
            foreach(var t in targets[i].GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
        }
        data.FindProperty("roomShakeDistance").floatValue=0.012f;data.FindProperty("roomShakeDegrees").floatValue=0.28f;
        data.FindProperty("desktopShakeDegrees").floatValue=0.85f;data.FindProperty("desktopShakeDistance").floatValue=0.004f;
        data.ApplyModifiedProperties();
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{result="Connected coherent room tremor and desktop-only view vibration",groups=targets.Length,renderers=targets.Sum(t=>t.GetComponentsInChildren<MeshRenderer>(true).Length)};
    }
}
