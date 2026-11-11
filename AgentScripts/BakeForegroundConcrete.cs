using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class BakeForegroundConcrete
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var root=GameObject.Find("Foreground Load Failures");int baked=0;
        foreach(var chunk in root.GetComponentsInChildren<AftershockFallingChunk>())
        {
            var path="Assets/_Project/Art/BuriedThreat/ForegroundSlab_"+(baked+1)+".asset";
            if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("Already baked");
            var leaf=chunk.transform;var piece=leaf.parent;var group=piece.parent;var renderer=leaf.GetComponent<MeshRenderer>();var center=renderer.bounds.center;
            var filter=leaf.GetComponent<MeshFilter>();var source=filter.sharedMesh;
            var matrix=Matrix4x4.Translate(-center)*leaf.localToWorldMatrix;
            var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Fixed proportion foreground concrete "+(++baked);
            mesh.vertices=source.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
            mesh.normals=source.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);
            PrefabUtility.UnpackPrefabInstance(piece.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            leaf.SetParent(group,true);leaf.SetPositionAndRotation(center,Quaternion.identity);leaf.localScale=Vector3.one;filter.sharedMesh=mesh;leaf.GetComponent<MeshCollider>().sharedMesh=mesh;
            leaf.name="Foreground gravity slab "+baked;piece.name="Archived source placement "+baked;
            var warning=group.GetComponent<AftershockLoadWarning>();var s=new SerializedObject(warning);s.FindProperty("loadedPiece").objectReferenceValue=leaf;s.ApplyModifiedProperties();
        }
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var data=new SerializedObject(ending);var p=data.FindProperty("lightBlockers");var rs=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();p.arraySize=rs.Length;for(int i=0;i<rs.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=rs[i];data.ApplyModifiedProperties();
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return new{baked,unitScaleGravityBodies=true};
    }
}
