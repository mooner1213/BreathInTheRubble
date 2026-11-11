using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor.Media;
using BreathInTheRubble.Prototype;

public static class VerifyContactRevision
{
    const string Dir="AgentScripts/ReviewImages/AftershockRevision/";
    static void Save(string name,object data)=>File.WriteAllText(Dir+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
    public static async Task<object> Run()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC play mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool automatic=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();
        var pieces=manager.Elements.Where(e=>e.loose!=null).Select(e=>e.loose).ToArray();
        var starts=pieces.Select(c=>c.transform.localPosition).ToArray();
        var tracked=Camera.main.transform;var headPosition=tracked.position;var headRotation=tracked.rotation;
        var supports=manager.Elements.Where(e=>e.loose==null&&e.target.GetComponent<Renderer>()?.enabled==true&&e.target.gameObject.activeInHierarchy).OrderBy(e=>Vector3.Distance(e.target.GetComponent<Renderer>().bounds.center,headPosition)).Take(16).ToArray();
        var previousPosition=supports.Select(e=>e.target.localPosition).ToArray();var previousRotation=supports.Select(e=>e.target.localRotation).ToArray();
        var activeCounts=new int[supports.Length];var quietCounts=new int[supports.Length];var signatures=new string[supports.Length];var maxAngle=new float[supports.Length];
        var traces=new List<object>();int minMoving=supports.Length,maxMoving=0,samples=0,maxLines=0,peakPowder=0,headInside=0;float headDrift=0,headTurn=0;
        int systems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool desktop=driver.DesktopEnabled,held=driver.IsHolding,lit=driver.Phone.FlashlightEnabled;
        driver.SetDesktopMode(true);if(!driver.IsHolding)driver.ToggleHold();if(!lit)driver.ToggleFlashlight();
        var rigRenderers=tracked.root.GetComponentsInChildren<Renderer>(true);var forced=rigRenderers.Select(r=>r.forceRenderingOff).ToArray();
        var go=new GameObject("Temporary contact review camera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.transform.SetPositionAndRotation(headPosition,headRotation);camera.fieldOfView=70;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing;
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.2f;light.range=4;light.shadows=LightShadows.Soft;
        var rt=new RenderTexture(960,720,24);var pixels=new Texture2D(960,720,TextureFormat.RGBA32,false);var previous=RenderTexture.active;camera.targetTexture=rt;
        MediaEncoder encoder=null;int frames=0,firstFrame=Time.frameCount;float elapsed=0,last=Time.realtimeSinceStartup,nextVideo=0,nextSample=0;int shot=0;float[] shots={0,1.6f,2.2f,3.4f,5.5f,8.5f,14.5f,28.5f};
        Action<bool> render=inspection=>
        {
            light.enabled=inspection;camera.fieldOfView=inspection?70:Camera.main.fieldOfView;
            for(int i=0;i<rigRenderers.Length;i++)rigRenderers[i].forceRenderingOff=inspection||forced[i];
            camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,720),0,0);pixels.Apply();
            for(int i=0;i<rigRenderers.Length;i++)rigRenderers[i].forceRenderingOff=forced[i];
        };
        try
        {
            encoder=new MediaEncoder(Dir+"Motion.mp4",new VideoTrackAttributes{frameRate=new MediaRational(20),width=960,height=720,includeAlpha=false});
            render(true); // Warm the render before the first scored capture.
            sequence.BeginAftershockNow();last=Time.realtimeSinceStartup;
            while(elapsed<29)
            {
                float now=Time.realtimeSinceStartup;float delta=now-last;last=now;elapsed+=delta;sequence.Advance(delta);
                maxLines=Math.Max(maxLines,manager.Elements.Sum(e=>e.cracks.Count(c=>c!=null&&c.enabled)));
                peakPowder=Math.Max(peakPowder,manager.Elements.Where(e=>e.powder!=null).Sum(e=>e.powder.particleCount));
                headDrift=Mathf.Max(headDrift,Vector3.Distance(headPosition,tracked.position));headTurn=Mathf.Max(headTurn,Quaternion.Angle(headRotation,tracked.rotation));
                foreach(var c in pieces.Where(c=>c.Released&&c.GetComponent<Renderer>().bounds.size.magnitude>.2f))
                {var shape=c.GetComponent<Collider>();if(shape.enabled&&(shape.ClosestPoint(tracked.position)-tracked.position).sqrMagnitude<.000025f)headInside++;}
                if(elapsed>=nextSample&&elapsed<17)
                {
                    int moving=0;var angles=new float[supports.Length];
                    for(int i=0;i<supports.Length;i++)
                    {
                        var t=supports[i].target;bool active=Vector3.Distance(t.localPosition,previousPosition[i])>.000015f||Quaternion.Angle(t.localRotation,previousRotation[i])>.008f;
                        if(active){activeCounts[i]++;moving++;}else quietCounts[i]++;
                        signatures[i]+=active?"1":"0";previousPosition[i]=t.localPosition;previousRotation[i]=t.localRotation;
                        angles[i]=Quaternion.Angle(supports[i].restRotation,t.localRotation);maxAngle[i]=Mathf.Max(maxAngle[i],angles[i]);
                    }
                    minMoving=Math.Min(minMoving,moving);maxMoving=Math.Max(maxMoving,moving);samples++;traces.Add(new{elapsed,moving,angles});nextSample=elapsed+.05f;
                }
                if(elapsed<=18&&elapsed>=nextVideo)
                {
                    render(true);while(nextVideo<=elapsed&&nextVideo<18){encoder.AddFrame(pixels);frames++;nextVideo+=.05f;}
                }
                if(shot<shots.Length&&elapsed>=shots[shot])
                {
                    render(true);File.WriteAllBytes(Dir+"Contact_"+shot+"_Inspection.png",pixels.EncodeToPNG());
                    render(false);File.WriteAllBytes(Dir+"Contact_"+shot+"_Phone.png",pixels.EncodeToPNG());shot++;
                }
                await Task.Delay(8);
            }
            encoder.Dispose();encoder=null;
            var patterns=supports.Select((e,i)=>new{name=e.target.name,active=activeCounts[i],quiet=quietCounts[i],peakDegrees=maxAngle[i],signature=signatures[i]}).ToArray();
            int escaped=pieces.Count(c=>c.GetComponent<Renderer>().bounds.center.y<-.25f);
            bool motion=patterns.All(p=>p.quiet>0)&&patterns.Count(p=>p.active>0)>=8&&patterns.Select(p=>p.signature).Distinct().Count()>=8&&minMoving<maxMoving;
            var result=new{result=maxLines==0&&escaped==0&&headDrift<.001f&&headTurn<.01f&&headInside==0&&pieces.All(c=>c.Released)&&motion?"PASS":"FAIL",pieces=pieces.Length,released=pieces.Count(c=>c.Released),escaped,maxLines,peakPowder,headDrift,headTurn,headInside,liveFrames=Time.frameCount-firstFrame,videoFrames=frames,videoSeconds=frames/20f,supports=patterns,minMoving,maxMoving,samples,systemsBefore=systems,systemsAfter=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,traces};
            Save("Runtime",result);
            sequence.PreviewNextStage();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();ending.Advance(60);await Task.Delay(70);Save("Rescue",new{ending.IsComplete,ending.OpeningAmount,ending.SunlightAdmission,ending.WhiteoutAmount});
            sequence.RestoreAftershock();await Task.Delay(80);Save("Reset",new{allPiecesReset=pieces.Select((c,i)=>!c.Released&&Vector3.Distance(c.transform.localPosition,starts[i])<.001f).All(x=>x),allPowderClear=manager.Elements.Where(e=>e.powder!=null).All(e=>e.powder.particleCount==0),allLinesHidden=manager.Elements.All(e=>e.cracks.All(c=>!c.enabled))});
            Debug.Log("[ContactRevision] "+result.result+"; lines="+maxLines+"; released="+result.released+"; movement patterns="+patterns.Select(p=>p.signature).Distinct().Count());
            return new{result.result,result.pieces,result.released,result.escaped,result.maxLines,result.peakPowder,result.headDrift,result.headTurn,result.headInside,result.liveFrames,result.videoFrames,result.minMoving,result.maxMoving,result.samples,distinctPatterns=patterns.Select(p=>p.signature).Distinct().Count(),result.systemsBefore,result.systemsAfter};
        }
        finally
        {
            encoder?.Dispose();sequence.RestoreAftershock();sequence.enabled=automatic;
            if(!lit&&driver.Phone.FlashlightEnabled)driver.ToggleFlashlight();if(!held)driver.ReturnPhone();if(!desktop)driver.SetDesktopMode(false);
            for(int i=0;i<rigRenderers.Length;i++)if(rigRenderers[i]!=null)rigRenderers[i].forceRenderingOff=forced[i];
            camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
