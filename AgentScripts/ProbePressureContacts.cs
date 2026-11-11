using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using BreathInTheRubble.Prototype;
public sealed class PressureContactRecorder:MonoBehaviour
{
    public readonly HashSet<string> contacts=new HashSet<string>();
    void OnCollisionStay(Collision hit){contacts.Add(hit.collider.transform.parent.name+"/"+hit.collider.name+" "+hit.GetContact(0).point.ToString("F2"));}
}
public static class ProbePressureContacts
{
    public static async Task<object> Run()
    {
        var seq=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool enabled=seq.enabled;seq.enabled=false;seq.RestoreAftershock();var chunks=GameObject.Find("Upper Joint Collapse").GetComponentsInChildren<AftershockFallingChunk>();var probes=chunks.Select(c=>c.gameObject.AddComponent<PressureContactRecorder>()).ToArray();
        try{seq.BeginAftershockNow();for(int i=0;i<320;i++){seq.Advance(.05f);await Task.Delay(20);}await Task.Delay(1000);return chunks.Select((c,i)=>new{joint=c.transform.parent?.name,c.Released,position=c.transform.position.ToString("F3"),contacts=probes[i].contacts.Take(10).ToArray()}).ToArray();}
        finally{seq.RestoreAftershock();seq.enabled=enabled;foreach(var p in probes)UnityEngine.Object.Destroy(p);}
    }
}
