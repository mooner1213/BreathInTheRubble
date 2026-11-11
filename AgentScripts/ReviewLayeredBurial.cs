using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.Threading.Tasks;
using BreathInTheRubble.Prototype;
public sealed class LayeredContactRecorder:MonoBehaviour
{
    public readonly HashSet<Collider> touching=new HashSet<Collider>();
    void OnCollisionEnter(Collision c){touching.Add(c.collider);}
    void OnCollisionStay(Collision c){touching.Add(c.collider);}
    void OnCollisionExit(Collision c){touching.Remove(c.collider);}
}
public static class ReviewLayeredBurial
{
    const string Dir="Temp/LayeredBurialReview/";
    public static object Initial()
    {
        Directory.CreateDirectory(Dir);var head=Camera.main.transform;var root=GameObject.Find("Deep Layered Collapse");var pieces=root.GetComponentsInChildren<AftershockFallingChunk>();
        var eyes=new[]{head.position,head.position+Vector3.right*.15f,head.position-Vector3.right*.15f,head.position+Vector3.up*.12f,head.position+Vector3.forward*.12f};
        var sourceVisibility=new List<object>();
        foreach(var eye in eyes)
        {
            var visible=new List<string>();
            foreach(var c in pieces){var center=c.GetComponent<Renderer>().bounds.center;var direction=center-eye;var hit=Physics.RaycastAll(eye,direction.normalized,direction.magnitude-.035f).Where(h=>h.collider.transform.IsChildOf(GameObject.Find("Prototype Environment").transform)&&h.collider.GetComponent<MeshRenderer>() is MeshRenderer r&&r.enabled).OrderBy(h=>h.distance).FirstOrDefault();if(hit.collider==null||hit.collider.GetComponent<AftershockFallingChunk>()==c)visible.Add(c.name);}
            var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var pipeVisible=new List<int>();for(int i=-1;i<=1;i++){var target=pipe.transform.position+pipe.transform.up*i*.21f;var delta=target-eye;if(!Physics.RaycastAll(eye,delta.normalized,delta.magnitude-.025f).Any(h=>h.collider.transform.IsChildOf(GameObject.Find("Prototype Environment").transform)&&h.collider.GetComponent<MeshRenderer>() is MeshRenderer r&&r.enabled))pipeVisible.Add(i);}
            sourceVisibility.Add(new{eye=eye.ToString("F3"),visibleLooseSources=visible.ToArray(),pipeVisible=pipeVisible.ToArray()});
        }
        Capture("Initial_Front",new Vector3(0,0,0),true);Capture("Initial_Ceiling",new Vector3(-58,0,0),true);Capture("Initial_Right",new Vector3(0,90,0),true);
        var result=new{chunks=pieces.Length,pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>().transform.position.ToString("F3"),sourceVisibility};File.WriteAllText(Dir+"InitialAudit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static void Capture(string name,Vector3 angles,bool lamp)
    {
        var main=Camera.main;var go=new GameObject("Temporary layered burial view"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;cam.fieldOfView=90;cam.nearClipPlane=.02f;cam.transform.SetPositionAndRotation(main.transform.position,Quaternion.Euler(angles));cam.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        if(lamp){var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.8f;light.range=4;light.shadows=LightShadows.Soft;}
        var hidden=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=hidden.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in hidden)r.forceRenderingOff=true;
        var rt=new RenderTexture(1100,850,24);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes(Dir+name+".png",pixels.EncodeToPNG());}
        finally{for(int i=0;i<hidden.Length;i++)hidden[i].forceRenderingOff=states[i];cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
    public static async Task<object> Runtime()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required");
        var seq=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool previous=seq.enabled;seq.enabled=false;seq.RestoreAftershock();
        var root=GameObject.Find("Deep Layered Collapse");var pockets=root.GetComponentsInChildren<RubbleFailurePocket>();var small=pockets.SelectMany(p=>p.Chips).ToArray();var all=root.GetComponentsInChildren<AftershockFallingChunk>();var big=all.Except(small).ToArray();
        var contacts=big.Select(c=>c.gameObject.AddComponent<LayeredContactRecorder>()).ToArray();
        var starts=big.Select(c=>c.GetComponent<Renderer>().bounds.center).ToArray();var scales=all.Select(c=>c.transform.localScale).ToArray();var poses=all.Select(c=>c.transform.localPosition).ToArray();
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool desktop=driver.DesktopEnabled,held=driver.IsHolding;bool lamp=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>().FlashlightEnabled;
        driver.SetDesktopMode(true);if(!lamp)driver.ToggleFlashlight();else if(!driver.IsHolding)driver.ToggleHold();driver.Look(new Vector2(0,85));
        float[] shots={.15f,3.52f,4.12f,5.52f,6.42f,8.42f,10.52f,14.52f,17.95f,28.5f};int next=0,frame=Time.frameCount,peakMoving=0;float time=0,last=Time.realtimeSinceStartup;var samples=new List<object>();
        seq.BeginAftershockNow();
        try
        {
            while(time<29f)
            {
                float now=Time.realtimeSinceStartup,delta=now-last;last=now;time+=delta;seq.Advance(delta);
                peakMoving=Math.Max(peakMoving,big.Count(c=>c.Released&&c.GetComponent<Rigidbody>().linearVelocity.magnitude>.3f));
                if(next<shots.Length&&time>=shots[next])
                {
                    Capture("At_"+next+"_Front",new Vector3(-15,0,0),true);Capture("At_"+next+"_Phone",new Vector3(-15,0,0),false);Capture("At_"+next+"_Ceiling",new Vector3(-56,0,0),true);
                    samples.Add(new{time,bigReleased=big.Count(c=>c.Released),smallReleased=small.Count(c=>c.Released),visible=all.Count(c=>c.GetComponent<Renderer>().enabled),pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>().transform.position.ToString("F3"),bearings=pockets.Select(p=>p.Bearings.Select(b=>new{y=b.root.position.y,b.releaseAt}).ToArray()).ToArray()});next++;
                }
                await Task.Delay(12);
            }
            var detail=big.Select((c,i)=>new{c.name,c.Released,visible=c.GetComponent<Renderer>().enabled,center=c.GetComponent<Renderer>().bounds.center.ToString("F3"),y=c.GetComponent<Renderer>().bounds.center.y,drop=starts[i].y-c.GetComponent<Renderer>().bounds.center.y,speed=c.GetComponent<Rigidbody>().linearVelocity.magnitude,gravity=c.GetComponent<Rigidbody>().useGravity,kinematic=c.GetComponent<Rigidbody>().isKinematic,contacts=contacts[i].touching.Where(t=>t!=null&&t.enabled&&t.gameObject.activeInHierarchy).Select(t=>t.name).ToArray()}).ToArray();
            var result=new{big=big.Length,small=small.Length,bigReleased=big.Count(c=>c.Released),smallReleased=small.Count(c=>c.Released),allVisible=all.All(c=>c.GetComponent<Renderer>().enabled),bigSettled=big.Count(c=>c.GetComponent<Rigidbody>().IsSleeping()||c.GetComponent<Rigidbody>().linearVelocity.magnitude<.1f),escaped=all.Where(c=>c.GetComponent<Renderer>().bounds.center.y<-.25f).Select(c=>new{c.name,position=c.transform.position.ToString("F3")}).ToArray(),stillOverhead=detail.Where(c=>c.y>.8f).ToArray(),unsupported=detail.Where(c=>c.contacts.Length==0).ToArray(),peakMoving,frames=Time.frameCount-frame,details=detail,samples};
            File.WriteAllText(Dir+"Runtime.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            Capture("Settled_Floor",new Vector3(60,0,0),true);Capture("Settled_Left",new Vector3(10,-70,0),true);
            seq.PreviewNextStage();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();ending.Advance(60);
            Capture("Rescue_AfterAccumulation",new Vector3(-68,0,0),false);
            File.WriteAllText(Dir+"Rescue.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{ending.IsComplete,ending.OpeningAmount,ending.SunlightAdmission,ending.WhiteoutAmount}));
            seq.RestoreAftershock();await Task.Delay(80);bool reset=all.Select((c,i)=>!c.Released&&Vector3.Distance(c.transform.localPosition,poses[i])<.001f&&Vector3.Distance(c.transform.localScale,scales[i])<.0001f).All(x=>x);File.WriteAllText(Dir+"Reset.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{reset}));
            return new{result.big,result.small,result.bigReleased,result.smallReleased,result.allVisible,result.bigSettled,escaped=result.escaped.Length,stillOverhead=result.stillOverhead.Length,unsupported=result.unsupported.Length,result.peakMoving,result.frames,reset};
        }
        finally{seq.RestoreAftershock();foreach(var contact in contacts)if(contact!=null)UnityEngine.Object.DestroyImmediate(contact);seq.enabled=previous;driver.Look(new Vector2(0,-85));if(!lamp&&driver.Phone.FlashlightEnabled)driver.ToggleFlashlight();if(!held)driver.ReturnPhone();if(!desktop)driver.SetDesktopMode(false);}
    }
}
