using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using BreathInTheRubble.Prototype;

public static class VerifyBuriedThreat
{
    const string Dir="Temp/BuriedThreatReview/";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static object EditorState()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.AutoTickCommand")).First(t=>t!=null);
        var enabled=type.GetProperty("IsEnabled",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
        var interval=type.GetProperty("CurrentIntervalMs",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
        var result=new{autoTick=enabled,interval,background=Application.runInBackground};
        File.WriteAllText(Dir+"EditorState.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
    }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"PC play mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var root=GameObject.Find("Foreground Load Failures");var chunks=root.GetComponentsInChildren<AftershockFallingChunk>();var warnings=root.GetComponentsInChildren<AftershockLoadWarning>();
        var tracked=Camera.main.transform.root.GetComponentsInChildren<Transform>(true).Where(t=>t==Camera.main.transform||t.name=="Left Controller"||t.name=="Right Controller").ToArray();
        var startPositions=tracked.Select(t=>t.localPosition).ToArray();var startRotations=tracked.Select(t=>t.localRotation).ToArray();
        var starts=chunks.Select(c=>c.GetComponent<Renderer>().bounds.center).ToArray();int startFrame=Time.frameCount,systems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
        var rows=new List<object>();var minHeadGap=10f;bool warningSound=false,impactSound=false;bool available=false;float maxStrain=0;
        sequence.RestoreAftershock();sequence.BeginAftershockNow();
        Debug.Log("[BuriedThreatReview] Started real-time warning, falling concrete, item access and reset checks.");
        float[] captures={.1f,2.8f,3.43f,6.85f,17.9f};int nextCapture=0;
        var watch=System.Diagnostics.Stopwatch.StartNew();
        try
        {
            while(watch.Elapsed.TotalSeconds<32)
            {
                float t=director.ElapsedSeconds;
                maxStrain=Mathf.Max(maxStrain,warnings.Max(w=>w.StrainAmount));
                warningSound|=root.GetComponentsInChildren<AudioSource>().Any(a=>a.isPlaying);
                impactSound|=root.GetComponentsInChildren<RubbleImpactAudio>().Any(a=>a.GetComponent<AudioSource>().isPlaying);
                available|=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>().IsAvailable;
                foreach(var c in chunks){var b=c.GetComponent<Renderer>().bounds;minHeadGap=Mathf.Min(minHeadGap,Vector3.Distance(b.ClosestPoint(Camera.main.transform.position),Camera.main.transform.position));}
                if(nextCapture<captures.Length&&t>=captures[nextCapture])
                {
                    Capture("Live_"+nextCapture+"_Front",new Vector3(-13,0,0),true);
                    if(nextCapture==1||nextCapture==2)Capture("Live_"+nextCapture+"_Native",new Vector3(-13,0,0),false);
                    rows.Add(new{time=t,frame=Time.frameCount,strain=warnings.Select(w=>w.StrainAmount).ToArray(),chunks=chunks.Select(c=>new{c.Released,visible=c.GetComponent<Renderer>().enabled,center=c.GetComponent<Renderer>().bounds.center.ToString("F3"),speed=c.GetComponent<Rigidbody>().linearVelocity.magnitude}).ToArray()});nextCapture++;
                }
                if(!director.IsRunning&&t>=18f){await Task.Delay(1300);break;}
                await Task.Delay(30);
            }
            var drops=chunks.Select((c,i)=>starts[i].y-c.GetComponent<Renderer>().bounds.center.y).ToArray();
            var result=new{frames=Time.frameCount-startFrame,drops,minHeadGap,warningSound,impactSound,maxStrain,pipeAvailable=available,allReleased=chunks.All(c=>c.Released),allVisible=chunks.All(c=>c.GetComponent<Renderer>().enabled),positions=chunks.Select(c=>c.GetComponent<Renderer>().bounds.center.ToString("F3")).ToArray(),samples=rows};
            File.WriteAllText(Dir+"LiveVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            Check(Time.frameCount>startFrame+100,"Frames did not advance");Check(nextCapture==captures.Length,"Real-time sequence did not finish");Check(maxStrain>.8f&&warningSound,"Strain or audio did not advance");
            Check(chunks.All(c=>c.Released),"Foreground slab stayed attached");Check(chunks.All(c=>c.GetComponent<Renderer>().enabled),"Slab entered tracking safety radius");
            Check(drops.All(d=>d>.45f),"Foreground fall is too short or caught: "+string.Join(",",drops));Check(minHeadGap>=.24f,"Head clearance below chunk safeguard");
            Check(available,"Pipe unavailable after collapse");
            Check(impactSound,"Physical landing did not play the concrete crash");
            for(int i=0;i<tracked.Length;i++)Check(tracked[i].localPosition==startPositions[i]&&tracked[i].localRotation==startRotations[i],"Tracked pose changed: "+tracked[i].name);
            sequence.RestoreAftershock();
            await Task.Delay(80);Physics.SyncTransforms();
            Check(chunks.All(c=>!c.Released&&c.GetComponent<Rigidbody>().isKinematic),"Chunks did not reset");Check(warnings.All(w=>w.StrainAmount==0),"Load warning did not reset");
            Check(UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length==systems,"Particle systems accumulated");
            for(int i=0;i<chunks.Length;i++)Check(Vector3.Distance(chunks[i].GetComponent<Renderer>().bounds.center,starts[i])<.005f,"Reset did not recover authored pose");
            Debug.Log("[BuriedThreatReview] PASS: real frames, strain/audio, 2 visible physical falls, head/controller preservation, pipe and reset.");return result;
        }
        finally{sequence.RestoreAftershock();}
    }
    public static void Capture(string name,Vector3 angles,bool inspectionLamp)
    {
        var main=Camera.main;var go=new GameObject("Temporary threat capture"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.CopyFrom(main);cam.enabled=false;cam.nearClipPlane=.02f;cam.fieldOfView=85;cam.transform.SetPositionAndRotation(main.transform.position,Quaternion.Euler(angles));cam.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        if(inspectionLamp){var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.8f;light.range=4;light.shadows=LightShadows.Soft;}
        var hidden=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=hidden.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in hidden)r.forceRenderingOff=true;
        var rt=new RenderTexture(1100,850,24);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes(Dir+name+".png",pixels.EncodeToPNG());}
        finally{for(int i=0;i<hidden.Length;i++)hidden[i].forceRenderingOff=states[i];cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
    public static async Task<object> PhoneLightReview()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"PC play mode required");
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        Check(driver!=null,"Desktop test driver missing");
        bool previousDesktop=driver.DesktopEnabled,previousHolding=driver.IsHolding;
        bool previousLight=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>().FlashlightEnabled;
        driver.SetDesktopMode(true);if(!previousLight)driver.ToggleFlashlight();else if(!driver.IsHolding)driver.ToggleHold();driver.Look(new Vector2(0,85));
        sequence.RestoreAftershock();sequence.BeginAftershockNow();
        int next=0;var samples=new List<object>();var timer=System.Diagnostics.Stopwatch.StartNew();
        float[] times={2.8f,3.45f,6.88f,17.9f};
        try
        {
            while(timer.Elapsed.TotalSeconds<28&&next<times.Length)
            {
                if(director.ElapsedSeconds>=times[next])
                {
                    Capture("PhoneLight_"+next+"_Front",new Vector3(-15,0,0),false);
                    samples.Add(new{time=director.ElapsedSeconds,flashlight=driver.Phone.FlashlightEnabled,frame=Time.frameCount});next++;
                }
                await Task.Delay(25);
            }
            Check(next==times.Length,"Phone flashlight review did not advance");
            File.WriteAllText(Dir+"PhoneLightReview.json",Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));return samples;
        }
        finally{sequence.RestoreAftershock();driver.Look(new Vector2(0,-85));if(!previousLight&&driver.Phone.FlashlightEnabled)driver.ToggleFlashlight();if(!previousHolding)driver.ReturnPhone();if(!previousDesktop)driver.SetDesktopMode(false);}
    }
    public static async Task<object> ResetAndRescue()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"PC play mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();
        bool automatic=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();await Task.Delay(60);
        var chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None);var scales=chunks.Select(c=>c.transform.localScale).ToArray();var poses=chunks.Select(c=>c.transform.localPosition).ToArray();
        try
        {
            for(int repeat=0;repeat<3;repeat++)
            {
                sequence.BeginAftershockNow();sequence.Advance(18.1f);await Task.Delay(50);sequence.RestoreAftershock();await Task.Delay(60);Physics.SyncTransforms();
                for(int i=0;i<chunks.Length;i++){Check(Vector3.Distance(chunks[i].transform.localScale,scales[i])<.0001f,"Scale drift on "+chunks[i].name);Check(Vector3.Distance(chunks[i].transform.localPosition,poses[i])<.0001f,"Position drift on "+chunks[i].name);}
            }
            sequence.PreviewNextStage();Check(sequence.Stage==GameStage.Rescue,"Rescue preview did not start");
            foreach(float time in new[]{10f,24f,38f,52f,60f})ending.Advance(time-ending.ElapsedSeconds);
            Capture("Rescue_Final_Ceiling",new Vector3(-72,0,0),false);
            Check(ending.IsComplete&&ending.OpeningAmount>.99f&&ending.WhiteoutAmount==0,"Natural rescue view failed");Check(ending.SunlightAdmission>.25f,"New rubble blocks the rescue aperture");
            var result=new{result="PASS",repeatResets=3,gravityChunks=chunks.Length,ending.IsComplete,ending.OpeningAmount,ending.SunlightAdmission,ending.WhiteoutAmount,xrOrigins=UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length};
            File.WriteAllText(Dir+"ResetAndRescue.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{sequence.RestoreAftershock();sequence.enabled=automatic;}
    }
}
