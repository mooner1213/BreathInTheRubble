using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class TuneEmbeddedWallDynamics
{
    public static object Run()
    {
        var chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None);
        var cores=GameObject.Find("Embedded Rear Wall Fractures").GetComponentsInChildren<MeshCollider>().Where(c=>c.GetComponent<Rigidbody>()==null).ToArray();
        var results=chunks.Select(chunk=>
        {
            chunk.ResetEffect();var data=new SerializedObject(chunk);var ignore=data.FindProperty("ignoreColliders");
            var colliders=Enumerable.Range(0,ignore.arraySize).Select(i=>(Collider)ignore.GetArrayElementAtIndex(i).objectReferenceValue).Where(c=>c!=null&&c.enabled&&c.gameObject.activeInHierarchy).ToList();
            var shape=chunk.GetComponent<MeshCollider>();
            foreach(var core in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if(core==shape)continue;
                // The layered scenery deliberately intersects itself. Detached cosmetic masonry collides
                // with the low rubble/floor, not overlapping structural layers or other authored skins.
                if(cores.Contains(core)||core.GetComponent<AftershockFallingChunk>()!=null||core.bounds.max.y>.72f)colliders.Add(core);
            }
            colliders=colliders.Distinct().ToList();ignore.arraySize=colliders.Count;
            for(int i=0;i<colliders.Count;i++){ignore.GetArrayElementAtIndex(i).objectReferenceValue=colliders[i];if(Application.isPlaying)Physics.IgnoreCollision(shape,colliders[i],true);}
            var anchor=(Transform)new SerializedObject(chunk.transform.parent.GetComponent<AftershockFractureSection>()).FindProperty("wallAnchor").objectReferenceValue;
            var v=anchor.TransformDirection(new Vector3(chunk.name.EndsWith(".1")?-.035f:.045f,-.08f,.065f))+Vector3.forward*.18f;data.FindProperty("releaseVelocity").vector3Value=v;
            data.FindProperty("peelDegrees").floatValue=chunk.name.EndsWith(".1")?-7f:-10f;
            data.ApplyModifiedPropertiesWithoutUndo();return new{name=chunk.name,ignored=colliders.Count,velocity=v.ToString("F3")};
        }).ToArray();
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();var debrisData=new SerializedObject(debris);
        var fragments=debrisData.FindProperty("fragments");var ends=debrisData.FindProperty("landingPositions");
        var sources=new[]{"Rear Left Fractured Panel","Rear Diagonal Reinforced Slab","Rear Inner Fracture Right"};
        for(int i=64;i<fragments.arraySize;i++)
        {
            var fragment=(Transform)fragments.GetArrayElementAtIndex(i).objectReferenceValue;
            var end=fragment.position+Vector3.forward*(i>=92?.07f:.025f);end.y=.04f;
            var hits=Physics.RaycastAll(new Vector3(end.x,1.3f,end.z),Vector3.down,1.4f).Where(h=>h.collider.attachedRigidbody==null&&h.collider.bounds.max.y<=.72f&&!h.collider.transform.IsChildOf(GameObject.Find("Embedded Rear Wall Fractures").transform)).OrderByDescending(h=>h.point.y).ToArray();
            if(hits.Length>0)end.y=hits[0].point.y+.015f;
            ends.GetArrayElementAtIndex(i).vector3Value=end;
            fragment.GetComponent<Renderer>().sharedMaterial=GameObject.Find(sources[i%3]).GetComponent<Renderer>().sharedMaterial;
        }
        debrisData.ApplyModifiedPropertiesWithoutUndo();
        if(!Application.isPlaying){EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());}
        return results;
    }
    public static async Task<object> Probe()
    {
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool enabled=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();
        var chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).OrderBy(c=>c.name).ToArray();var start=chunks.Select(c=>c.transform.position).ToArray();
        try
        {
            sequence.BeginAftershockNow();sequence.Advance(18.1f);await Task.Delay(2300);
            return chunks.Select((c,i)=>new{name=c.name,drop=start[i].y-c.transform.position.y,position=c.transform.position.ToString("F3"),released=c.Released,speed=c.GetComponent<Rigidbody>().linearVelocity.magnitude,visible=c.GetComponent<Renderer>().enabled,neighbors=Physics.OverlapBox(c.GetComponent<Renderer>().bounds.center,c.GetComponent<Renderer>().bounds.extents).Where(x=>x!=c.GetComponent<Collider>()).Select(x=>x.name).ToArray()}).ToArray();
        }
        finally{sequence.RestoreAftershock();sequence.enabled=enabled;}
    }
}
