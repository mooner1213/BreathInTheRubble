using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using BreathInTheRubble.Prototype;
public sealed class PipeTouchRecorder:MonoBehaviour
{
    public readonly List<string> contacts=new List<string>();
    void OnCollisionEnter(Collision c){if(contacts.Count>=70)return;bool held=GetComponent<DesktopPipeTestDriver>().IsHolding;for(int i=0;i<Mathf.Min(3,c.contactCount);i++){var p=c.GetContact(i);var marker=p.otherCollider.GetComponentInParent<PipeImpactSurface>();contacts.Add((held?"held ":"fall ")+c.collider.name+" / "+p.otherCollider.name+" / "+(marker!=null?marker.Kind.ToString():"none")+" at "+p.point.ToString("F2"));}}
}
public static class VerifyTightPocket
{
    public static object PropClearance()
    {
        Physics.SyncTransforms();var pieces=GameObject.Find("Tight Initial Pocket").GetComponentsInChildren<Collider>();var overlaps=new List<object>();
        foreach(var grab in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None))foreach(var shape in grab.GetComponentsInChildren<Collider>().Where(c=>c.enabled))foreach(var other in pieces)
            if(Physics.ComputePenetration(shape,shape.transform.position,shape.transform.rotation,other,other.transform.position,other.transform.rotation,out var direction,out float depth)&&depth>.001f)overlaps.Add(new{grab=grab.name,other=other.name,depth});
        var result=new{overlaps};File.WriteAllText("Temp/TightPocketReview/PropClearance.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static async Task<object> Strike(int swings=1,float waitAftershockSeconds=0)
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool automatic=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();
        var drop=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var pipe=drop.GetComponent<DesktopPipeTestDriver>();var desktop=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool desktopWas=desktop.DesktopEnabled;desktop.SetDesktopMode(true);Vector2 aimDelta=Vector2.zero;
        var impacts=drop.GetComponent<PipeImpactAudio>();var relay=drop.GetComponent<PipeRescueSignalRelay>();var source=(AudioSource)new SerializedObject(impacts).FindProperty("impactSource").objectReferenceValue;int signalCount=0;bool played=false;var surfaces=new List<string>();
        var probe=drop.gameObject.AddComponent<PipeTouchRecorder>();
        Action onSignal=()=>signalCount++;Action onImpact=()=>surfaces.Add(impacts.LastSurface.ToString());relay.SignalMade+=onSignal;impacts.ImpactPlayed+=onImpact;
        try
        {
            sequence.BeginAftershockNow();float elapsed=0,last=Time.realtimeSinceStartup;while(elapsed<Mathf.Max(drop.AvailableAtSeconds+1.5f,waitAftershockSeconds)){float now=Time.realtimeSinceStartup,delta=now-last;last=now;elapsed+=delta;sequence.Advance(delta);await Task.Delay(15);}var look=Camera.main.transform.eulerAngles;aimDelta=new Vector2(Mathf.DeltaAngle(look.y,30f)/.18f,-Mathf.DeltaAngle(look.x,8f)/.18f);desktop.Look(aimDelta);pipe.SetHeld(true);await Task.Delay(600);
            for(int swing=0;swing<Mathf.Clamp(swings,1,5);swing++){pipe.Swing();for(int i=0;i<55;i++){played|=source.isPlaying;await Task.Delay(15);}}
            var result=new{result=surfaces.Contains("Metal")&&signalCount>0&&played?"PASS":"FAIL",swings,waitAftershockSeconds,physicalImpactSurfaces=surfaces,signalCount,audioPlayed=played,pipeHeld=pipe.IsHolding,impactCount=impacts.ImpactCount,pipePosition=pipe.transform.position.ToString("F3"),constraints=pipe.GetComponent<Rigidbody>().constraints.ToString(),contacts=probe.contacts.Where(s=>s.StartsWith("held ")).ToArray(),sequence.Stage};
            File.WriteAllText("Temp/TightPocketReview/RebarStrike.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(probe);relay.SignalMade-=onSignal;impacts.ImpactPlayed-=onImpact;pipe.SetHeld(false);sequence.RestoreAftershock();sequence.enabled=automatic;desktop.Look(-aimDelta);if(!desktopWas)desktop.SetDesktopMode(false);}
    }
}
