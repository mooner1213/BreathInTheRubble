using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor.Media;
using BreathInTheRubble.Prototype;

public static class VerifyAftershockTiming
{
    const string Dir="AgentScripts/ReviewImages/AftershockTiming/";
    public static async Task<object> Movie()
    {
        var go=new GameObject("Temporary episode movie validation"){hideFlags=HideFlags.HideAndDontSave};var video=go.AddComponent<UnityEngine.Video.VideoPlayer>();
        video.playOnAwake=false;video.audioOutputMode=UnityEngine.Video.VideoAudioOutputMode.None;video.url=Path.GetFullPath(Dir+"Episode.mp4");
        try
        {
            video.Prepare();var deadline=DateTime.UtcNow.AddSeconds(15);while(!video.isPrepared&&DateTime.UtcNow<deadline)await Task.Delay(25);
            if(!video.isPrepared)throw new Exception("Saved episode movie did not decode");
            var result=new{video.isPrepared,video.length,video.frameCount,video.frameRate,video.width,video.height};Save("Movie",result);return result;
        }
        finally{video.Stop();UnityEngine.Object.DestroyImmediate(go);}
    }
    static void Save(string name,object data)=>File.WriteAllText(Dir+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
    public static async Task<object> Run(bool guardPreview=true)
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var shock=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        var rumble=UnityEngine.Object.FindFirstObjectByType<AftershockAudio>().GetComponent<AudioSource>();
        var rain=UnityEngine.Object.FindFirstObjectByType<AftershockRubbleRain>();var rainRefs=new UnityEditor.SerializedObject(rain).FindProperty("joints");var rainSources=Enumerable.Range(0,rainRefs.arraySize).Select(i=>rainRefs.GetArrayElementAtIndex(i).objectReferenceValue as ParticleSystem).Where(p=>p!=null).ToArray();
        bool automatic=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();dust.Clear();
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var pieces=manager.Elements.Where(e=>e.loose!=null).Select(e=>e.loose).ToArray();
        var supports=manager.Elements.Where(e=>e.loose==null&&e.target.gameObject.activeInHierarchy&&e.target.GetComponent<Renderer>()?.enabled==true).OrderBy(e=>Vector3.Distance(e.target.GetComponent<Renderer>().bounds.center,Camera.main.transform.position)).Take(16).ToArray();
        var original=pieces.Select(c=>c.transform.localPosition).ToArray();var previousPosition=supports.Select(e=>e.target.position).ToArray();var previousRotation=supports.Select(e=>e.target.rotation).ToArray();
        var main=Camera.main;var headPosition=main.transform.position;var headRotation=main.transform.rotation;
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool desktop=driver.DesktopEnabled,held=driver.IsHolding,lit=driver.Phone.FlashlightEnabled;
        bool previousGuard=driver.GuardPreviewEnabled;
        driver.SetDesktopMode(true);if(!held)driver.ToggleHold();if(!lit)driver.ToggleFlashlight();if(guardPreview!=previousGuard)driver.ToggleGuardPreview();
        var go=new GameObject("Temporary episode review camera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.transform.SetPositionAndRotation(headPosition,headRotation);camera.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        var target=new RenderTexture(960,720,24);var pixels=new Texture2D(960,720,TextureFormat.RGBA32,false);var old=RenderTexture.active;camera.targetTexture=target;
        Action render=()=>{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,720),0,0);pixels.Apply();};
        var moments=new[]{0f,1f,3f,4.85f,5.75f,7.5f,9.8f,10.3f,12f,15f,19.8f,20.3f,25f,29.7f,30.3f,32f};
        var samples=new List<Sample>();var motion=new List<object>();int shot=0,frames=0,firstFrame=Time.frameCount,maxBeforeFive=0,headInside=0;float elapsed=0,last=0,nextVideo=0,nextTrace=0,maxHeadMove=0,maxHeadTurn=0,lastTrace=0,afterTwentyMotion=0;int initialSystems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
        var bins=new float[3];var binCounts=new int[3];MediaEncoder encoder=null;
        try
        {
            encoder=new MediaEncoder(Dir+"Episode.mp4",new VideoTrackAttributes{frameRate=new MediaRational(20),width=960,height=720,includeAlpha=false});
            render();sequence.BeginAftershockNow();last=Time.realtimeSinceStartup;
            while(elapsed<32.2f)
            {
                float now=Time.realtimeSinceStartup,delta=now-last;last=now;elapsed+=delta;sequence.Advance(delta);
                if(elapsed<5)maxBeforeFive=Math.Max(maxBeforeFive,pieces.Count(p=>p.Released));
                maxHeadMove=Mathf.Max(maxHeadMove,Vector3.Distance(headPosition,main.transform.position));maxHeadTurn=Mathf.Max(maxHeadTurn,Quaternion.Angle(headRotation,main.transform.rotation));
                foreach(var piece in pieces)if(piece.Released&&piece.GetComponent<Renderer>().bounds.size.magnitude>.2f){var c=piece.GetComponent<Collider>();if(c.enabled&&(c.ClosestPoint(headPosition)-headPosition).sqrMagnitude<.000025f)headInside++;}
                if(elapsed>=nextTrace)
                {
                    float speed=0,turn=0;float interval=Mathf.Max(.001f,elapsed-lastTrace);
                    for(int i=0;i<supports.Length;i++)
                    {
                        var t=supports[i].target;speed+=Vector3.Distance(previousPosition[i],t.position)/interval;turn+=Quaternion.Angle(previousRotation[i],t.rotation)/interval;previousPosition[i]=t.position;previousRotation[i]=t.rotation;
                    }
                    speed/=supports.Length;turn/=supports.Length;
                    if(elapsed<2){bins[0]+=speed+turn*.01f;binCounts[0]++;}else if(elapsed>=5&&elapsed<10){bins[1]+=speed+turn*.01f;binCounts[1]++;}else if(elapsed>18&&elapsed<20){bins[2]+=speed+turn*.01f;binCounts[2]++;}
                    if(elapsed>20.3f)afterTwentyMotion=Mathf.Max(afterTwentyMotion,speed+turn*.01f);
                    motion.Add(new{elapsed,speed,turn,strength=AftershockEnvelope.Motion(shock.ElapsedSeconds),haze=dust.HazeAmount,dustClock=dust.ElapsedSeconds});nextTrace=elapsed+.05f;lastTrace=elapsed;
                }
                if(elapsed<31.5f&&elapsed>=nextVideo){render();while(nextVideo<=elapsed&&nextVideo<31.5f){encoder.AddFrame(pixels);frames++;nextVideo+=.05f;}}
                if(shot<moments.Length&&elapsed>=moments[shot])
                {
                    render();File.WriteAllBytes(Dir+"At_"+shot.ToString("D2")+".png",pixels.EncodeToPNG());
                    var sample=new Sample{at=moments[shot],elapsed=elapsed,shockSeconds=shock.ElapsedSeconds,dustSeconds=dust.ElapsedSeconds,strength=AftershockEnvelope.Motion(shock.ElapsedSeconds),haze=dust.HazeAmount,opacity=dust.Opacity,dustActive=dust.IsActive,released=pieces.Count(p=>p.Released),particles=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Sum(p=>p.particleCount),stage=sequence.Stage.ToString(),rumbleVolume=rumble.volume,rumblePlaying=rumble.isPlaying,siftingParticles=rainSources.Sum(p=>p.particleCount)};samples.Add(sample);shot++;
                }
                await Task.Delay(8);
            }
            encoder.Dispose();encoder=null;
            var early=samples.First(s=>s.at==3f);var peak=samples.First(s=>s.at==7.5f);var tail=samples.First(s=>s.at==15f);var clear=samples.First(s=>s.at==30.3f);
            bool profile=samples.Where(s=>s.at<5).Any(s=>s.siftingParticles>0)&&early.haze<peak.haze&&tail.haze<peak.haze&&tail.haze>0&&!clear.dustActive&&clear.haze==0&&maxBeforeFive==0&&samples.First(s=>s.at==10.3f).released==pieces.Length&&afterTwentyMotion<.001f;
            var result=new{result=profile&&maxHeadMove<.001f&&maxHeadTurn<.01f&&headInside==0?"PASS":"FAIL",profile,guardPreview,maxBeforeFive,pieces=pieces.Length,released=pieces.Count(p=>p.Released),escaped=pieces.Where(p=>p.GetComponent<Renderer>().bounds.center.y<-.25f).Select(p=>p.name).ToArray(),headInside,maxHeadMove,maxHeadTurn,afterTwentyMotion,liveFrames=Time.frameCount-firstFrame,videoFrames=frames,videoSeconds=frames/20f,systemsBefore=initialSystems,systemsAfter=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,motionEarly=bins[0]/Math.Max(1,binCounts[0]),motionPeak=bins[1]/Math.Max(1,binCounts[1]),motionLate=bins[2]/Math.Max(1,binCounts[2]),samples,motion};Save("Runtime",result);
            sequence.RestoreAftershock();dust.Clear();await Task.Delay(80);Save("Reset",new{reset=pieces.Select((p,i)=>!p.Released&&Vector3.Distance(p.transform.localPosition,original[i])<.001f).All(x=>x),dustClear=!dust.IsActive&&dust.HazeAmount==0,allLinesHidden=manager.Elements.All(e=>e.cracks.All(c=>!c.enabled))});
            sequence.PreviewNextStage();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();ending.Advance(60);Save("Rescue",new{ending.IsComplete,ending.OpeningAmount,ending.SunlightAdmission,ending.WhiteoutAmount});
            Debug.Log("[AftershockTiming] "+result.result+"; no early collapse="+(maxBeforeFive==0)+"; peak="+peak.haze+"; dust clear="+!clear.dustActive+"; supports stopped="+afterTwentyMotion);
            return new{result.result,result.profile,result.maxBeforeFive,result.pieces,result.released,result.headInside,result.maxHeadMove,result.maxHeadTurn,result.afterTwentyMotion,result.liveFrames,result.videoFrames,result.motionEarly,result.motionPeak,result.motionLate,peakHaze=peak.haze,tailHaze=tail.haze,dustClear=!clear.dustActive,result.systemsBefore,result.systemsAfter};
        }
        finally
        {
            encoder?.Dispose();sequence.RestoreAftershock();dust.Clear();sequence.enabled=automatic;
            if(driver.GuardPreviewEnabled!=previousGuard)driver.ToggleGuardPreview();if(!lit&&driver.Phone.FlashlightEnabled)driver.ToggleFlashlight();if(!held)driver.ReturnPhone();if(!desktop)driver.SetDesktopMode(false);
            camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);
        }
    }
    public sealed class Sample
    {
        public float at,elapsed,shockSeconds,dustSeconds,strength,haze,opacity,rumbleVolume;
        public bool dustActive,rumblePlaying;public int released,particles,siftingParticles;public string stage;
    }
}
