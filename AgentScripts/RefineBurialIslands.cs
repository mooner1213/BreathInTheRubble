using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BreathInTheRubble.Prototype;
public static class RefineBurialIslands
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");var group=GameObject.Find("Independent unsupported stone islands");
        var ignored=Camera.main.transform.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).Distinct().ToArray();
        int count=0;foreach(var chunk in group.GetComponentsInChildren<AftershockFallingChunk>())
        {
            var s=new SerializedObject(chunk);var refs=s.FindProperty("ignoreColliders");refs.arraySize=ignored.Length;for(int i=0;i<ignored.Length;i++)refs.GetArrayElementAtIndex(i).objectReferenceValue=ignored[i];s.ApplyModifiedPropertiesWithoutUndo();count++;
        }
        Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(group.scene);EditorSceneManager.SaveScene(group.scene);return new{count,ignored=ignored.Length};
    }
}
