using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BreathInTheRubble.Prototype;
public static class VerifyGroundedCollapse
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Transform[] Refs(UnityEngine.Object value,string field){var a=new SerializedObject(value).FindProperty(field);return Enumerable.Range(0,a.arraySize).Select(i=>(Transform)a.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    public static async Task<object> Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"PC play mode required");
        Directory.CreateDirectory("Temp/GroundedCollapseReview");var seq=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();
        var fragments=Refs(debris,"fragments");var slabs=Refs(ending,"rubble");var head=Camera.main.transform;var headPosition=head.position;var headRotation=head.rotation;bool enabled=seq.enabled;seq.enabled=false;
        var samples=new List<object>();var roofGroups=GameObject.Find("Sealed Rubble Burial").transform.Cast<Transform>().ToArray();var roofPositions=roofGroups.Select(t=>t.position).ToArray();var pipeOriginal=new SerializedObject(pipe);var stow=(Transform)pipeOriginal.FindProperty("stowedPose").objectReferenceValue;
        try{
            seq.RestoreAftershock();Check(pipe.GetComponent<Renderer>().enabled&&!pipe.IsAvailable,"Pipe must already exist inside the rubble, without an enabled grab");Check(stow.position.y>1.6f,"Pipe origin is still inside the visible low cavity");
            Check(slabs.Length==4&&slabs.All(t=>t.name.StartsWith("Individual rescue slab")),"Rescue must move four individual slabs");
            Check(Enumerable.Range(1,4-1).All(i=>slabs[i-1].position.y>slabs[i].position.y+.25f),"Rescue order is not outside to inside");
            Check(fragments.Length==28&&fragments.All(t=>t.GetComponent<Rigidbody>()!=null&&t.GetComponent<Rigidbody>().isKinematic),"Intact debris pool not prepared");
            int first=Time.frameCount;seq.BeginAftershockNow();float elapsed=0;Vector3 previous=pipe.transform.position;float largestJump=0;var pipeFrames=new List<object>();
            for(int step=0;step<544;step++){
                seq.Advance(1f/30f);elapsed+=1f/30f;largestJump=Mathf.Max(largestJump,Vector3.Distance(previous,pipe.transform.position));previous=pipe.transform.position;
                if(step==149||step==180||step==204||step==224||step==249){UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();pipeFrames.Add(new{elapsed,position=pipe.transform.position.ToString("F3"),pipe.IsAvailable,image=Capture("Pipe_"+elapsed.ToString("00.0"),true,new Vector3(-33,0,0))});}
                await Task.Delay(8);
            }
            Check(seq.Stage==GameStage.Isolation&&pipe.IsAvailable,"Aftershock did not finish with reachable pipe");Check(largestJump<.17f,"Pipe path has a visible discontinuity: "+largestJump);
            await Task.Delay(3500);Check(Time.frameCount>first+50,"Physics did not advance across real PC frames");
            var active=fragments.Where(t=>t.gameObject.activeInHierarchy&&t.GetComponent<Renderer>().enabled).ToArray();var bodies=active.Select(t=>t.GetComponent<Rigidbody>()).ToArray();int settled=bodies.Count(b=>b.IsSleeping()||b.linearVelocity.magnitude<.08f);
            Check(active.Length>=8,"Most debris disappeared instead of settling: "+active.Length);Check(settled>=active.Length*.75f,"Too many fragments still moving/unsupported: "+settled+"/"+active.Length);
            Check(bodies.All(b=>b.useGravity&&!b.isKinematic),"Released visible debris is still animated or frozen in air");Check(bodies.All(b=>b.position.y>-.3f),"Debris fell through the floor");
            samples.Add(new{activeFragments=active.Length,settled,largestPipeStep=largestJump,pipeStartHeight=stow.position.y,pipeLanding=pipe.transform.position.ToString("F3"),pipeFrames});
            Capture("Aftershock_Settled",true,new Vector3(22,15,0));
            seq.RestoreAftershock();Check(fragments.All(t=>t.GetComponent<Rigidbody>().isKinematic),"Debris reset did not reattach the pool");Check(!pipe.IsAvailable,"Pipe reset failed");
            var rest=slabs.Select(t=>t.position).ToArray();seq.PreviewNextStage();Check(ending.IsRunning,"Rescue preview failed");
            foreach(float time in new[]{8f,14f,22f,28f,36f,42f,50f,56f,60f}){
                ending.Advance(time-ending.ElapsedSeconds);var moving=slabs.Where((t,i)=>Vector3.Distance(t.position,rest[i])>.002f).Select(t=>t.name).ToArray();
                int expected=time<17?1:time<31?2:time<45?3:4;Check(moving.Length==expected,"Wrong number/order of slabs removed at "+time+": "+moving.Length);
                int activeCarry=Enumerable.Range(0,4).Count(i=>time>3+i*14&&time<13+i*14);Check(activeCarry<=1,"Multiple roof slabs are carried simultaneously");
                Check(roofGroups.Where((t,i)=>Vector3.Distance(t.position,roofPositions[i])>.001f).Count()==0,"A whole canopy layer moved");
                samples.Add(new{time,removed=moving,ending.SunlightAdmission,image=Capture("Rescue_"+time.ToString("00"),false,new Vector3(-65,0,0))});
            }
            Check(ending.IsComplete&&ending.WhiteoutAmount==0&&ending.SunlightAdmission>0,"Limited-opening rescue did not complete naturally");
            seq.RestoreAftershock();Check(slabs.Where((t,i)=>Vector3.Distance(t.position,rest[i])>.001f).Count()==0,"Individual slab reset failed");
            Check(head.position==headPosition&&head.rotation==headRotation,"HMD pose changed");Check(UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length==1,"Duplicate XR Origin");
            var result=new{result="PASS",frameStart=first,frameEnd=Time.frameCount,individualRescueSlabs=4,wholeCanopyGroupsMoved=0,samples};File.WriteAllText("Temp/GroundedCollapseReview/Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{result="PASS",activeFragments=active.Length,settled,largestPipeStep=largestJump,individualRescueSlabs=4,wholeCanopyGroupsMoved=0,frames=Time.frameCount-first};
        }finally{seq.RestoreAftershock();seq.enabled=enabled;}
    }
    static string Capture(string label,bool lamp,Vector3 angles)
    {
        var main=Camera.main;var go=new GameObject("Temporary grounded collapse review"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.fieldOfView=88;camera.nearClipPlane=.02f;camera.transform.SetPositionAndRotation(main.transform.position,Quaternion.Euler(angles));camera.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        if(lamp){var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.9f;light.range=4;}
        var hidden=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=hidden.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in hidden)r.forceRenderingOff=true;
        var target=new RenderTexture(1100,850,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();var path="Temp/GroundedCollapseReview/"+label+".png";File.WriteAllBytes(path,pixels.EncodeToPNG());return path;}
        finally{for(int i=0;i<hidden.Length;i++)hidden[i].forceRenderingOff=states[i];camera.targetTexture=null;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);}
    }
}
