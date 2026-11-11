using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class RefineInterlockedSupports
{
    static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        foreach(var side in new[]{"Left","Right"}){
            int sign=side=="Left"?-1:1;var foot=GameObject.Find(side+" foundation slab");var block=GameObject.Find(side+" wedged upright");var plate=GameObject.Find(side+" leaning load slab");
            block.transform.rotation=Quaternion.Euler(0,sign*8,sign*3);var b=BoundsOf(block);block.transform.position+=Vector3.up*(BoundsOf(foot).max.y-.006f-b.min.y);
            b=BoundsOf(plate);plate.transform.position+=Vector3.up*(BoundsOf(block).max.y-.005f-b.min.y);
            var locking=GameObject.Find(side+" beam locking shard");locking.transform.position+=Vector3.up*(BoundsOf(plate).max.y-.02f-BoundsOf(locking).min.y);
        }
        var rear=GameObject.Find("Rear leaning cross slab");rear.transform.position+=Vector3.up*(BoundsOf(GameObject.Find("Rear load bearing rubble")).max.y-.008f-BoundsOf(rear).min.y);
        var heights=new[]{2.86f,2.46f,2.06f,1.50f};
        foreach(var t in GameObject.Find("Interlocked Rubble Structure").GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Individual rescue slab "))){int index=int.Parse(t.name.Split(' ')[3])-1;t.position=new Vector3(t.position.x,heights[index],t.position.z);}
        var ending=UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.RescueEndingDirector>();var data=new SerializedObject(ending);var blockers=data.FindProperty("apertureBlockers");
        var plugs=GameObject.Find("Interlocked Rubble Structure").GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Individual rescue slab ")).OrderBy(t=>t.name).Select(t=>t.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.enabled)).ToArray();blockers.arraySize=plugs.Length;for(int i=0;i<plugs.Length;i++)blockers.GetArrayElementAtIndex(i).objectReferenceValue=plugs[i];data.ApplyModifiedProperties();
        Physics.SyncTransforms();var debris=UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.AftershockDebris>();var dd=new SerializedObject(debris);var fragments=dd.FindProperty("fragments");
        var ceiling=GameObject.Find("Interlocked Rubble Structure").GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.bounds.max.y>1.22f).ToArray();
        for(int i=0;i<fragments.arraySize;i++){var f=(Transform)fragments.GetArrayElementAtIndex(i).objectReferenceValue;f.GetComponent<Renderer>().enabled=true;var ray=new Ray(new Vector3(f.position.x,.97f,f.position.z),Vector3.up);float distance=10;Vector3 point=f.position;foreach(var c in ceiling)if(c.Raycast(ray,out var h,.8f)&&h.distance<distance){distance=h.distance;point=h.point-Vector3.up*.009f;}f.position=point;}
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{result="Stacked foundations, bearing chips and leaning plates meet without deep overlap",leftContact=BoundsOf(GameObject.Find("Left wedged upright")).max.y-BoundsOf(GameObject.Find("Left leaning load slab")).min.y,rearBottom=BoundsOf(rear).min.y};
    }
}
