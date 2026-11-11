using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using BreathInTheRubble.Prototype;
public static class RefineBuriedPocket
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        GameObject.Find("Front upper compressed masonry").transform.SetPositionAndRotation(new Vector3(.03f,.52f,1.04f),Quaternion.Euler(-26,17,-5));
        GameObject.Find("Left shoulder packed backing").transform.rotation=Quaternion.Euler(0,12,-76);
        GameObject.Find("Right shoulder packed backing").transform.rotation=Quaternion.Euler(0,-22,78);
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Pile textured faces oriented into the survival pocket";
    }
    public static object FallChannel()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var piece=GameObject.Find("Foreground hanging slab 1");var renderer=piece.GetComponentsInChildren<MeshRenderer>().First(r=>r.enabled);
        var change=new Vector3(-.25f,1.23f,.99f)-renderer.bounds.center;piece.transform.position+=change;
        var group=piece.transform.parent;var dust=group.GetComponentInChildren<ParticleSystem>();dust.transform.position+=change;
        GameObject.Find("Audible concrete strain 1").transform.position+=change;
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Foreground slab shifted 21 cm forward, outside idle-controller sweep";
    }
}
