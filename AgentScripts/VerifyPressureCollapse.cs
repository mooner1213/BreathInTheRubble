using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using BreathInTheRubble.Prototype;
public static class VerifyPressureCollapse
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static async Task<object> Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"PC Play Mode required");Directory.CreateDirectory("Temp/PressureCollapseReview");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool enabled=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();
        var head=Camera.main.transform;var headPosition=head.position;var headRotation=head.rotation;var chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).OrderBy(c=>c.transform.parent.name).ToArray();var starts=chunks.Select(c=>c.GetComponent<Renderer>().bounds.center).ToArray();var releasePositions=new Vector3[chunks.Length];var releaseTime=new float[chunks.Length];var released=new bool[chunks.Length];var samples=new List<object>();var showers=GameObject.Find("Compressed Upper Rubble").GetComponentsInChildren<ParticleSystem>();int initialSystems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;int frame=Time.frameCount,maxParticles=0;float maximumDrop=0;
        try
        {
            Check(chunks.Length==9&&showers.Length==4,"Pressure setup missing");Capture("Packed_Before_Up",new Vector3(-53,0,0));Capture("Packed_Before_Left",new Vector3(-32,-45,0));sequence.BeginAftershockNow();
            for(int step=0;step<362;step++)
            {
                sequence.Advance(.05f);float time=(step+1)*.05f;maximumDrop=Mathf.Max(maximumDrop,UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>().AppliedMaximumDrop);
                for(int i=0;i<chunks.Length;i++)if(chunks[i].Released&&!released[i]){released[i]=true;releasePositions[i]=chunks[i].transform.position;releaseTime[i]=time;}
                maxParticles=Math.Max(maxParticles,showers.Sum(p=>p.particleCount));
                if(step==65||step==73||step==99||step==111||step==139||step==155||step==215||step==285||step==359)
                {
                    UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();Capture("At_"+time.ToString("00.0")+"_Up",new Vector3(-53,0,0));if(step==73||step==155)Capture("At_"+time.ToString("00.0")+"_Left",new Vector3(-32,-45,0));
                    samples.Add(new{time,particles=showers.Sum(p=>p.particleCount),chunks=chunks.Select((c,i)=>new{index=i,c.name,c.Released,visible=c.GetComponent<Renderer>().enabled,kinematic=c.GetComponent<Rigidbody>().isKinematic,gravity=c.GetComponent<Rigidbody>().useGravity,y=c.GetComponent<Renderer>().bounds.center.y,drop=starts[i].y-c.GetComponent<Renderer>().bounds.center.y,speed=c.GetComponent<Rigidbody>().linearVelocity.magnitude}).ToArray()});
                }
                await Task.Delay(20);
            }
            await Task.Delay(1800);var drops=chunks.Select((c,i)=>starts[i].y-c.GetComponent<Renderer>().bounds.center.y).ToArray();var sleeping=chunks.Select(c=>c.GetComponent<Rigidbody>().IsSleeping()).ToArray();
            var result=new{maximumDrop,largeChunks=chunks.Length,allReleased=chunks.All(c=>c.Released),drops,sleeping,visible=chunks.Select(c=>c.GetComponent<Renderer>().enabled).ToArray(),maxParticles,frameCount=Time.frameCount-frame,samples};File.WriteAllText("Temp/PressureCollapseReview/Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            Check(chunks.All(c=>c.Released),"A large fragment stayed attached");Check(chunks.Where(c=>c.GetComponent<Renderer>().enabled).All(c=>c.GetComponent<Rigidbody>().useGravity&&!c.GetComponent<Rigidbody>().isKinematic),"A visible released chunk is frozen");Check(drops.All(d=>d>.7f),"A large fragment remained in an overhead joint: "+string.Join(",",drops));Check(chunks.All(c=>c.GetComponent<Renderer>().bounds.center.y<.4f),"A fragment remained overhead");Check(maxParticles>0,"Upper joint shower did not run");Check(Time.frameCount>frame+50,"Physics frames did not advance");Check(maximumDrop>.11f,"Ceiling stopped descending");Check(head.position==headPosition&&head.rotation==headRotation,"Tracked camera changed");
            Check(chunks.All(c=>c.GetComponent<Renderer>().bounds.center.y>-.2f),"A large fragment escaped the rubble floor");Check(chunks.All(c=>c.GetComponent<Rigidbody>().IsSleeping()||c.GetComponent<Rigidbody>().linearVelocity.magnitude<.08f),"A large fragment did not settle");
            Capture("Settled_Up",new Vector3(-53,0,0));sequence.RestoreAftershock();Check(chunks.All(c=>!c.Released&&c.GetComponent<Rigidbody>().isKinematic),"Large fragments did not reset");Check(showers.All(p=>p.particleCount==0),"Chips did not reset");Check(UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length==initialSystems,"Particle systems accumulated");return new{result="PASS",largeChunks=chunks.Length,drops,maxParticles,maximumDrop,liveFrames=Time.frameCount-frame};
        }
        finally{sequence.RestoreAftershock();sequence.enabled=enabled;}
    }
    public static void Capture(string name,Vector3 angle)
    {
        var main=Camera.main;var go=new GameObject("Temporary pressure review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.nearClipPlane=.02f;camera.fieldOfView=85;camera.transform.SetPositionAndRotation(main.transform.position,Quaternion.Euler(angle));camera.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.8f;light.range=4;
        var hidden=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=hidden.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in hidden)r.forceRenderingOff=true;
        var rt=new RenderTexture(1100,850,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var active=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes("Temp/PressureCollapseReview/"+name+".png",pixels.EncodeToPNG());}
        finally{for(int i=0;i<hidden.Length;i++)hidden[i].forceRenderingOff=states[i];camera.targetTexture=null;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
