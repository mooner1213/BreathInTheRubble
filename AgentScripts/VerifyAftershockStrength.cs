using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine.Rendering.Universal;
using BreathInTheRubble.Prototype;

public static class VerifyAftershockStrength
{
    const string Dir="AgentScripts/ReviewImages/AftershockStrength/";
    static void Save(string name,object data)=>File.WriteAllText(Dir+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
    public static async Task<object> MovieAndRescue()
    {
        var go=new GameObject("Temporary seismic movie validation"){hideFlags=HideFlags.HideAndDontSave};var video=go.AddComponent<UnityEngine.Video.VideoPlayer>();
        video.playOnAwake=false;video.audioOutputMode=UnityEngine.Video.VideoAudioOutputMode.None;video.url=Path.GetFullPath(Dir+"Episode.mp4");
        try
        {
            video.Prepare();var deadline=DateTime.UtcNow.AddSeconds(12);while(!video.isPrepared&&DateTime.UtcNow<deadline)await Task.Delay(25);
            if(!video.isPrepared)throw new Exception("Saved video did not decode");
            var movie=new{video.isPrepared,video.length,video.frameCount,video.frameRate,video.width,video.height};Save("Movie",movie);
            var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool enabled=sequence.enabled;sequence.enabled=false;
            try{sequence.RestoreAftershock();sequence.PreviewNextStage();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();ending.Advance(60);var rescue=new{ending.IsComplete,ending.OpeningAmount,ending.SunlightAdmission,ending.WhiteoutAmount};Save("Rescue",rescue);return new{movie,rescue};}
            finally{sequence.RestoreAftershock();sequence.enabled=enabled;}
        }
        finally{video.Stop();UnityEngine.Object.DestroyImmediate(go);}
    }
    public static async Task<object> Control()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var shock=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var ground=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();
        bool automatic=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();ground.ExcitationScale=0;
        int first=Time.frameCount;float elapsed=0,last=Time.time,maxMotion=0;var samples=new List<object>();
        try
        {
            sequence.BeginAftershockNow();
            while(elapsed<20.3f)
            {
                float now=Time.time,delta=now-last;last=now;elapsed+=delta;sequence.Advance(delta);
                maxMotion=Mathf.Max(maxMotion,ground.GroundOffset.magnitude);
                await Task.Delay(10);
            }
            var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();
            var released=(bool[])typeof(AftershockDebris).GetField("released",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(debris);
            var result=new{result=manager.Fractures==0&&released.All(x=>!x)&&!pipe.IsRevealed&&maxMotion==0?"PASS":"FAIL",elapsed,fractures=manager.Fractures,legacyReleased=released.Count(x=>x),pipeRevealed=pipe.IsRevealed,maxMotion,liveFrames=Time.frameCount-first,driven=ground.DrivenSupports};Save("ZeroExcitation",result);return result;
        }
        finally{sequence.RestoreAftershock();ground.ExcitationScale=1;sequence.enabled=automatic;}
    }
    public static async Task<object> Run()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var shock=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var ground=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();
        // Desktop mouse deltas must not turn the test view while measuring environment motion.
        var previousFocus=EditorWindow.focusedWindow;SceneView.lastActiveSceneView?.Focus();
        var pieces=manager.Elements.Where(e=>e.loose!=null&&e.failureResistance>0).ToArray();var big=pieces.Where(e=>e.powder!=null).ToArray();
        var main=Camera.main;var head=main.transform.position;var headRotation=main.transform.rotation;
        var tracked=UnityEngine.Object.FindObjectsByType<UnityEngine.InputSystem.XR.TrackedPoseDriver>(FindObjectsSortMode.None).Select(x=>x.transform).ToArray();var trackedPositions=tracked.Select(t=>t.position).ToArray();
        var root=GameObject.Find("Prototype Environment");
        var floors=root.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer&&(r.name.Contains("foundation")||r.name.Contains("receiving concrete plate")||r.name.Contains("landing bed"))).Select(r=>r.transform).ToArray();var floorStarts=floors.Select(t=>t.position).ToArray();var floorMove=new float[floors.Length];
        var local=manager.Elements.Where(e=>e.loose==null&&e.target!=null&&e.target.gameObject.activeInHierarchy).Take(20).ToArray();
        bool automatic=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();ground.ExcitationScale=1;
        var anchors=manager.Elements.Where(e=>e.loose==null&&e.target!=null&&e.target.GetComponent<Renderer>()?.enabled==true)
            .Select(e=>e.target.GetComponent<Renderer>()).Where(r=>{var p=main.WorldToViewportPoint(r.bounds.center);return p.z>.2f&&p.x>.05f&&p.x<.95f&&p.y>.05f&&p.y<.95f;}).Take(12).ToArray();
        var anchorRest=anchors.Select(r=>main.WorldToScreenPoint(r.bounds.center)).ToArray();
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool desktop=driver.DesktopEnabled,held=driver.IsHolding,lit=driver.Phone.FlashlightEnabled,guard=driver.GuardPreviewEnabled;
        driver.SetDesktopMode(true);if(!held)driver.ToggleHold();if(!lit)driver.ToggleFlashlight();if(guard)driver.ToggleGuardPreview();
        var go=new GameObject("Temporary seismic review camera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.transform.SetPositionAndRotation(head,headRotation);camera.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        camera.GetUniversalAdditionalCameraData().requiresDepthOption=main.GetUniversalAdditionalCameraData().requiresDepthOption;
        camera.GetUniversalAdditionalCameraData().requiresColorOption=main.GetUniversalAdditionalCameraData().requiresColorOption;
        var target=new RenderTexture(960,720,24);var pixels=new Texture2D(960,720,TextureFormat.RGBA32,false);var old=RenderTexture.active;camera.targetTexture=target;
        Action render=()=>{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,720),0,0);pixels.Apply();};
        var moments=new[]{0f,1f,3f,4.85f,5.75f,7.5f,9.8f,10.3f,12f,15f,19.8f,20.3f,25f,29.7f,30.3f,32f};var shots=new List<object>();var trace=new List<object>();var releasedAt=new Dictionary<string,float>();var releaseInfo=new List<object>();
        int frame=Time.frameCount,videoFrames=0,shot=0,headInside=0,early=0;float elapsed=0,last=0,nextVideo=0,nextTrace=0,maxTrackedMove=0,maxHeadTurn=0,maxReleaseSpeed=0,maxHorizontalSpeed=0;
        int systemsBefore=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;MediaEncoder encoder=null;
        try
        {
            encoder=new MediaEncoder(Dir+"Episode.mp4",new VideoTrackAttributes{frameRate=new MediaRational(20),width=960,height=720,includeAlpha=false});
            sequence.BeginAftershockNow();last=Time.time;
            while(elapsed<32.2f)
            {
                float now=Time.time,delta=now-last;last=now;elapsed+=delta;sequence.Advance(delta);
                if(elapsed<5)early=Math.Max(early,pieces.Count(e=>e.loose.Released));
                for(int i=0;i<tracked.Length;i++)maxTrackedMove=Mathf.Max(maxTrackedMove,Vector3.Distance(tracked[i].position,trackedPositions[i]));
                maxHeadTurn=Mathf.Max(maxHeadTurn,Quaternion.Angle(headRotation,main.transform.rotation));
                for(int i=0;i<floors.Length;i++)floorMove[i]=Mathf.Max(floorMove[i],Vector3.Distance(floors[i].position,floorStarts[i]));
                foreach(var e in pieces)if(e.loose.Released&&!releasedAt.ContainsKey(e.target.name))
                {
                    releasedAt[e.target.name]=elapsed;maxReleaseSpeed=Mathf.Max(maxReleaseSpeed,e.loose.ReleaseVelocity.magnitude);
                    releaseInfo.Add(new{e.target.name,elapsed,e.damage,e.failureResistance,velocity=e.loose.ReleaseVelocity.ToString("F4")});
                }
                foreach(var e in big)if(e.loose.Released)
                {
                    var b=e.target.GetComponent<Rigidbody>();maxHorizontalSpeed=Mathf.Max(maxHorizontalSpeed,Vector3.ProjectOnPlane(b.linearVelocity,Vector3.up).magnitude);
                    foreach(var c in e.target.GetComponents<Collider>())if(c.enabled&&(c.ClosestPoint(head)-head).sqrMagnitude<.000025f)headInside++;
                }
                if(elapsed>=nextTrace)
                {
                    trace.Add(new{elapsed,ground=ground.GroundOffset.ToString("F5"),groundDistance=ground.GroundOffset.magnitude,groundDegrees=Quaternion.Angle(Quaternion.identity,ground.GroundRotation),screenMotion=anchors.Select((r,i)=>(main.WorldToScreenPoint(r.bounds.center)-anchorRest[i]).magnitude).DefaultIfEmpty(0).Average(),acceleration=ground.GroundAcceleration.magnitude,load=ground.Load,fractures=manager.Fractures,individual=local.Select(e=>e.response).ToArray(),haze=dust.HazeAmount});nextTrace=elapsed+.08f;
                }
                if(elapsed<31.5f&&elapsed>=nextVideo){render();while(nextVideo<=elapsed&&nextVideo<31.5f){encoder.AddFrame(pixels);videoFrames++;nextVideo+=.05f;}}
                if(shot<moments.Length&&elapsed>=moments[shot])
                {
                    render();File.WriteAllBytes(Dir+"At_"+shot.ToString("D2")+".png",pixels.EncodeToPNG());
                    shots.Add(new{at=moments[shot],elapsed,released=pieces.Count(e=>e.loose.Released),haze=dust.HazeAmount,dustActive=dust.IsActive,ground=ground.GroundOffset.ToString("F5"),stage=sequence.Stage.ToString()});
                    if(shot==5)
                    {
                        foreach(float angle in new[]{-90f,90f,180f}){camera.transform.rotation=headRotation*Quaternion.Euler(0,angle,0);render();File.WriteAllBytes(Dir+"PeakSide_"+angle+".png",pixels.EncodeToPNG());}
                        camera.transform.rotation=headRotation;
                    }
                    shot++;
                }
                await Task.Delay(8);
            }
            encoder.Dispose();encoder=null;
            bool complete=pieces.All(e=>e.loose.Released),clear=!dust.IsActive&&dust.HazeAmount==0;
            var result=new{result=early==0&&complete&&clear&&maxTrackedMove<.001f&&maxHeadTurn<.01f&&headInside==0&&maxReleaseSpeed<=.161f?"PASS":"FAIL",early,complete,clear,pieces=pieces.Length,released=pieces.Count(e=>e.loose.Released),firstFailure=releasedAt.Values.DefaultIfEmpty(-1).Min(),lastFailure=releasedAt.Values.DefaultIfEmpty(-1).Max(),maxReleaseSpeed,maxHorizontalSpeed,headInside,maxTrackedMove,maxHeadTurn,liveFrames=Time.frameCount-frame,videoFrames,videoSeconds=videoFrames/20f,ground.DrivenSupports,floors=floors.Select((f,i)=>new{f.name,maxDisplacement=floorMove[i]}).ToArray(),systemsBefore,systemsAfter=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,shots,trace,releaseInfo};Save("Runtime",result);
            sequence.RestoreAftershock();dust.Clear();await Task.Delay(100);Save("Reset",new{allAttached=pieces.All(e=>!e.loose.Released&&e.damage==0),groundRestored=ground.GroundOffset==Vector3.zero,dustClear=!dust.IsActive,linesHidden=manager.Elements.All(e=>e.cracks.All(c=>c==null||!c.enabled))});
            return new{result.result,result.early,result.complete,result.clear,result.firstFailure,result.lastFailure,result.maxReleaseSpeed,result.maxHorizontalSpeed,result.headInside,result.maxTrackedMove,result.maxHeadTurn,result.liveFrames,result.DrivenSupports,result.floors,result.videoSeconds};
        }
        finally
        {
            encoder?.Dispose();sequence.RestoreAftershock();dust.Clear();sequence.enabled=automatic;
            if(driver.GuardPreviewEnabled!=guard)driver.ToggleGuardPreview();if(!lit&&driver.Phone.FlashlightEnabled)driver.ToggleFlashlight();if(!held)driver.ReturnPhone();if(!desktop)driver.SetDesktopMode(false);
            camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);
            if(previousFocus!=null)previousFocus.Focus();
        }
    }
}
