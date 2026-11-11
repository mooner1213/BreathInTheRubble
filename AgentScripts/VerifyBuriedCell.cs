using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using BreathInTheRubble.Prototype;
public static class VerifyBuriedCell
{
    const string Dir="Temp/BuriedCellReview/";
    static void Save(string name,object value)=>File.WriteAllText(Dir+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    static Collider[] CloseShapes()=>GameObject.Find("Close buried cell").GetComponentsInChildren<Collider>().Where(c=>c.enabled).ToArray();
    public static object Audit()
    {
        Directory.CreateDirectory(Dir);Physics.SyncTransforms();var close=CloseShapes();var overlaps=new List<object>();
        foreach(var grab in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None))foreach(var c in grab.GetComponentsInChildren<Collider>().Where(c=>c.enabled))foreach(var other in close)if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,other,other.transform.position,other.transform.rotation,out var normal,out float depth)&&depth>.001f)overlaps.Add(new{item=grab.name,surface=other.name,depth});
        var eye=Camera.main.transform.position;var rays=new[]{new Vector3(0,0,1),new Vector3(-.5f,0,1).normalized,new Vector3(.5f,0,1).normalized,Vector3.left,Vector3.right,Vector3.up}.Select(d=>{var hits=Physics.RaycastAll(eye,d,3).Where(h=>h.collider.GetComponent<MeshRenderer>() is MeshRenderer r&&r.enabled).OrderBy(h=>h.distance).ToArray();return new{direction=d.ToString("F2"),distance=hits.Length>0?hits[0].distance:-1,surface=hits.Length>0?hits[0].collider.name:"none"};}).ToArray();
        var offsets=new[]{Vector3.zero,new Vector3(.12f,0,0),new Vector3(-.12f,0,0),new Vector3(0,.1f,0),new Vector3(0,0,.1f)};var h=new GameObject("Temporary head clearance probe");var sphere=h.AddComponent<SphereCollider>();sphere.radius=.08f;var headOverlaps=new List<object>();try{foreach(var offset in offsets)foreach(var c in close)if(Physics.ComputePenetration(sphere,eye+offset,Quaternion.identity,c,c.transform.position,c.transform.rotation,out var direction,out float depth)&&depth>.001f)headOverlaps.Add(new{offset=offset.ToString("F2"),c.name,depth});}finally{UnityEngine.Object.DestroyImmediate(h);}
        var sources=GameObject.Find("Concealed close seam chips").GetComponentsInChildren<AftershockFallingChunk>();var visibleSources=offsets.Select(offset=>new{offset=offset.ToString("F2"),visible=sources.Where(p=>Visible(eye+offset,p)).Select(p=>p.name).ToArray()}).ToArray();
        var result=new{rays,propOverlaps=overlaps,headOverlaps,visibleSources};Save("Clearance",result);return result;
    }
    static bool Visible(Vector3 eye,AftershockFallingChunk piece)
    {
        var center=piece.GetComponent<Renderer>().bounds.center;var delta=center-eye;var hits=Physics.RaycastAll(eye,delta.normalized,delta.magnitude-.003f).Where(h=>{var r=h.collider.GetComponentInParent<MeshRenderer>();return r!=null&&r.enabled&&h.collider.gameObject!=piece.gameObject;}).OrderBy(h=>h.distance).ToArray();return hits.Length==0;
    }
    static void Capture(string name,Vector3 angles)
    {
        var main=Camera.main;var go=new GameObject("Temporary actual lens capture"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.CopyFrom(main);c.enabled=false;c.transform.SetPositionAndRotation(main.transform.position,Quaternion.Euler(angles));c.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        var rt=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;try{c.targetTexture=rt;c.Render();c.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Dir+name+".png",pixels.EncodeToPNG());}finally{c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
    public static async Task<object> Runtime()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required");var seq=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool automatic=seq.enabled;seq.enabled=false;seq.RestoreAftershock();
        var d=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool desktop=d.DesktopEnabled,held=d.IsHolding,lit=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>().FlashlightEnabled;d.SetDesktopMode(true);if(!d.IsHolding)d.ToggleHold();if(!lit)d.ToggleFlashlight();
        var all=GameObject.Find("Close buried cell").GetComponentsInChildren<AftershockFallingChunk>();var samples=new List<object>();var startPos=all.Select(c=>c.transform.localPosition).ToArray();int firstFrame=Time.frameCount;var nativeRoof=GameObject.Find("Low continuous overburden").transform;var roofRest=nativeRoof.position;float elapsed=0,last=Time.realtimeSinceStartup;var shots=new[]{3.1f,3.4f,3.7f,4.5f,5.2f,7.2f,10.1f,13.1f,16.4f,28.5f};int next=0,visiblePeak=0;
        try
        {
            await Task.Delay(100);Capture("InitialPhone",Vector3.zero);Capture("InitialPhoneUp",new Vector3(-38,0,0));seq.BeginAftershockNow();last=Time.realtimeSinceStartup;
            while(elapsed<29){float now=Time.realtimeSinceStartup;float delta=now-last;last=now;elapsed+=delta;seq.Advance(delta);visiblePeak=Mathf.Max(visiblePeak,all.Count(c=>c.Released&&Visible(Camera.main.transform.position,c)));if(next<shots.Length&&elapsed>=shots[next]){Capture("Quake_"+next,Vector3.zero);samples.Add(new{elapsed,released=all.Count(c=>c.Released),visible=all.Count(c=>c.Released&&Visible(Camera.main.transform.position,c))});next++;}await Task.Delay(12);}
            int released=all.Count(c=>c.Released),escaped=all.Count(c=>c.GetComponent<Renderer>().bounds.center.y<-.25f),renderers=all.Count(c=>c.GetComponent<Renderer>().enabled),settled=all.Count(c=>c.GetComponent<Rigidbody>().IsSleeping()||c.GetComponent<Rigidbody>().linearVelocity.magnitude<.1f);
            seq.PreviewNextStage();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();ending.Advance(60);await Task.Delay(80);Capture("RescuePhoneUp",new Vector3(-68,0,0));var rescue=new{ending.IsComplete,ending.OpeningAmount,ending.SunlightAdmission,closeRoofMoved=Vector3.Distance(nativeRoof.position,roofRest)};Save("Rescue",rescue);
            seq.RestoreAftershock();await Task.Delay(80);bool reset=all.Select((c,i)=>!c.Released&&Vector3.Distance(c.transform.localPosition,startPos[i])<.001f).All(v=>v)&&Vector3.Distance(nativeRoof.position,roofRest)<.001f;
            var result=new{pieces=all.Length,released,escaped,renderers,settled,visiblePeak,reset,frames=Time.frameCount-firstFrame,samples};Save("Runtime",result);Debug.Log("[BuriedCellReview] "+Newtonsoft.Json.JsonConvert.SerializeObject(new{pieces=all.Length,released,escaped,renderers,visiblePeak,reset}));return result;
        }
        finally{seq.RestoreAftershock();seq.enabled=automatic;if(!lit&&d.Phone.FlashlightEnabled)d.ToggleFlashlight();if(!held)d.ReturnPhone();if(!desktop)d.SetDesktopMode(false);}
    }
    public static async Task<object> Preview()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required");var seq=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool automatic=seq.enabled;seq.enabled=false;seq.RestoreAftershock();var d=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool desktop=d.DesktopEnabled,held=d.IsHolding,lit=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>().FlashlightEnabled;d.SetDesktopMode(true);if(!d.IsHolding)d.ToggleHold();if(!lit)d.ToggleFlashlight();try{int f=Time.frameCount;await Task.Delay(150);Capture("InitialPhone",Vector3.zero);Capture("InitialPhoneUp",new Vector3(-38,0,0));return new{frames=Time.frameCount-f};}finally{seq.enabled=automatic;if(!lit&&d.Phone.FlashlightEnabled)d.ToggleFlashlight();if(!held)d.ReturnPhone();if(!desktop)d.SetDesktopMode(false);}
    }
}
