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
public static class VerifyStructuralAftershock
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Bounds VisibleBounds(Transform target){var rs=target.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.size.sqrMagnitude>.01f).ToArray();var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);return bounds;}
    static Transform[] Targets(AftershockEnvironmentMotion motion){var a=new SerializedObject(motion).FindProperty("ceilingTargets");return Enumerable.Range(0,a.arraySize).Select(i=>(Transform)a.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    public static async Task<object> Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"PC Play Mode required");Directory.CreateDirectory("Temp/StructuralAftershockReview");
        var seq=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool previous=seq.enabled;seq.enabled=false;
        var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();var ceiling=Targets(motion);var chunks=GameObject.Find("Structural Aftershock Breakaway").GetComponentsInChildren<AftershockFallingChunk>(true);var cracks=UnityEngine.Object.FindObjectsByType<RubbleStressCrack>(FindObjectsSortMode.None);var head=Camera.main.transform;var headPosition=head.position;var headRotation=head.rotation;
        seq.RestoreAftershock();var starts=ceiling.Select(t=>t.position).ToArray();var chunkStarts=chunks.Select(c=>c.transform.position).ToArray();var samples=new List<object>();float minimumGap=10,maximumDrop=0;int firstFrame=Time.frameCount;
        try{
            Check(ceiling.Length==8&&chunks.Length==5&&cracks.Length==3,"Structural setup missing");Check(chunks.All(c=>c.GetComponent<MeshCollider>().sharedMesh==c.GetComponent<MeshFilter>().sharedMesh&&c.GetComponent<MeshCollider>().convex),"Large collider/visual mismatch");
            Capture("Before_Up",new Vector3(-53,0,0));Capture("Before_LeftJoint",new Vector3(-30,-46,0));
            seq.BeginAftershockNow();float elapsed=0;
            for(int step=0;step<362;step++){
                seq.Advance(.05f);elapsed+=.05f;maximumDrop=Mathf.Max(maximumDrop,motion.AppliedMaximumDrop);
                foreach(var target in ceiling){var b=VisibleBounds(target);if(head.position.x>=b.min.x&&head.position.x<=b.max.x&&head.position.z>=b.min.z&&head.position.z<=b.max.z)minimumGap=Mathf.Min(minimumGap,b.min.y-head.position.y);}
                if(step==57||step==79||step==119||step==155||step==285||step==359){UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();Capture("At_"+elapsed.ToString("00.0")+"_Up",new Vector3(-53,0,0));Capture("At_"+elapsed.ToString("00.0")+"_Left",new Vector3(-25,-48,0));samples.Add(new{elapsed,drop=motion.AppliedMaximumDrop,cracks=cracks.Select(c=>c.Progress).ToArray(),fragments=chunks.Select((c,i)=>new{c.name,c.Released,drop=chunkStarts[i].y-c.transform.position.y,visible=c.GetComponent<Renderer>().enabled,position=c.transform.position.ToString("F3")}).ToArray()});}
                await Task.Delay(16);
            }
            await Task.Delay(3000);
            Check(seq.Stage==GameStage.Isolation,"Aftershock did not finish");Check(maximumDrop>.105f&&maximumDrop<=.141f,"Ceiling drop absent or too large: "+maximumDrop);Check(minimumGap>=.235f,"Head clearance violated: "+minimumGap);
            var drops=chunks.Select((c,i)=>chunkStarts[i].y-c.transform.position.y).ToArray();Check(chunks.All(c=>c.Released),"Not all structural chunks released");Check(drops.Count(d=>d>.3f)>=3,"Large pieces did not fall: "+string.Join(",",drops));Check(cracks.All(c=>c.Progress==1&&c.GetComponentsInParent<Transform>().Length>0),"Cracks did not open");
            Check(head.position==headPosition&&head.rotation==headRotation,"Tracked camera moved");Check(Time.frameCount>firstFrame+50,"Physics did not run across real frames");
            var lowered=ceiling.Select((t,i)=>starts[i].y-t.position.y).ToArray();Capture("After_Settled_Up",new Vector3(-53,0,0));
            seq.RestoreAftershock();Check(ceiling.Where((t,i)=>Vector3.Distance(t.position,starts[i])>.001f).Count()==0,"Ceiling did not restore");Check(chunks.All(c=>!c.Released&&c.GetComponent<Rigidbody>().isKinematic),"Large rubble reset failed");Check(cracks.All(c=>c.Progress==0),"Crack reset failed");
            var result=new{result="PASS",maximumDrop,minimumHeadGap=minimumGap,loweredTargets=lowered,largeFragmentDrops=drops,stressCracks=cracks.Length,frames=Time.frameCount-firstFrame,samples};File.WriteAllText("Temp/StructuralAftershockReview/Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{result="PASS",maximumDrop,minimumHeadGap=minimumGap,largeFragmentDrops=drops,stressCracks=cracks.Length,frames=Time.frameCount-firstFrame};
        }finally{seq.RestoreAftershock();seq.enabled=previous;}
    }
    static void Capture(string name,Vector3 angle)
    {
        var main=Camera.main;var go=new GameObject("Temporary structural review camera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.nearClipPlane=.02f;camera.fieldOfView=85;camera.transform.SetPositionAndRotation(main.transform.position,Quaternion.Euler(angle));var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.8f;light.range=4;
        var xr=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=xr.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in xr)r.forceRenderingOff=true;
        var particles=UnityEngine.Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None);var particleStates=particles.Select(p=>p.forceRenderingOff).ToArray();foreach(var r in particles)r.forceRenderingOff=true;
        var rt=new RenderTexture(1100,850,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var active=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes("Temp/StructuralAftershockReview/"+name+".png",pixels.EncodeToPNG());}
        finally{for(int i=0;i<xr.Length;i++)xr[i].forceRenderingOff=states[i];for(int i=0;i<particles.Length;i++)particles[i].forceRenderingOff=particleStates[i];camera.targetTexture=null;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
