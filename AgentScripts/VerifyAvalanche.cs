using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using BreathInTheRubble.Prototype;

public sealed class PipeMeshClearance
{
    sealed class Surface {public MeshRenderer renderer;public Vector3[] vertices;public int[] triangles;}
    Surface[] surfaces;
    bool pipeMeasured;
    Bounds pipeBounds;
    public float RenderedRadius {get;private set;}
    public PipeMeshClearance()
    {
        surfaces=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null).Select(r=>{var m=r.GetComponent<MeshFilter>().sharedMesh;return new Surface{renderer=r,vertices=m.vertices,triangles=m.triangles};}).ToArray();
    }
    public List<object> Inspect(AftershockPipeDrop pipe,float elapsed)
    {
        var capsule=pipe.GetComponent<CapsuleCollider>();var tr=pipe.transform;var scale=tr.lossyScale;
        if(!pipeMeasured){var points=pipe.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>tr.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();pipeBounds=new Bounds(points[0],Vector3.zero);foreach(var v in points)pipeBounds.Encapsulate(v);RenderedRadius=points.Max(v=>new Vector2(v.x-pipeBounds.center.x,v.z-pipeBounds.center.z).magnitude);pipeMeasured=true;}
        var center=tr.TransformPoint(pipeBounds.center);float radius=RenderedRadius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
        var axis=tr.up*(pipeBounds.extents.y-RenderedRadius)*Mathf.Abs(scale.y);var a=center-axis;var b=center+axis;
        var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Expand(radius*2);var hits=new List<object>();
        foreach(var s in surfaces)
        {
            var r=s.renderer;if(r==null||!r.enabled||!r.gameObject.activeInHierarchy||!r.bounds.Intersects(bounds))continue;
            var matrix=r.transform.localToWorldMatrix;var vertices=s.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();float minimum=float.MaxValue;
            for(int i=0;i<s.triangles.Length;i+=3)
            {
                var v0=vertices[s.triangles[i]];var v1=vertices[s.triangles[i+1]];var v2=vertices[s.triangles[i+2]];
                minimum=Mathf.Min(minimum,SegmentTriangle(a,b,v0,v1,v2));if(minimum<.00000001f)break;
            }
            float depth=radius-Mathf.Sqrt(minimum);
            if(depth>.0015f)hits.Add(new{elapsed,surface=PathOf(r.transform),depth,pipe=center.ToString("F3"),shapes=r.GetComponents<Collider>().Select(c=>new{type=c.GetType().Name,c.enabled,c.isTrigger,mesh=c is MeshCollider mc?mc.sharedMesh?.name:"",convex=c is MeshCollider meshCollider&&meshCollider.convex,ignored=Physics.GetIgnoreCollision(capsule,c),physical=Physics.ComputePenetration(capsule,capsule.transform.position,capsule.transform.rotation,c,c.transform.position,c.transform.rotation,out var dir,out float penetration),penetration}).ToArray()});
        }
        return hits;
    }
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    static float SegmentTriangle(Vector3 p,Vector3 q,Vector3 a,Vector3 b,Vector3 c)
    {
        var n=Vector3.Cross(b-a,c-a);float denominator=Vector3.Dot(n,q-p);
        if(Mathf.Abs(denominator)>.00000001f){float t=Vector3.Dot(n,a-p)/denominator;if(t>=0&&t<=1&&PointTriangle(p+(q-p)*t,a,b,c)<.00000001f)return 0;}
        return Mathf.Min(PointTriangle(p,a,b,c),PointTriangle(q,a,b,c),Segments(p,q,a,b),Segments(p,q,b,c),Segments(p,q,c,a));
    }
    static float PointTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
    {
        var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0)return ap.sqrMagnitude;
        var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return bp.sqrMagnitude;
        float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return (p-(a+ab*(d1/(d1-d3)))).sqrMagnitude;
        var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return cp.sqrMagnitude;
        float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return (p-(a+ac*(d2/(d2-d6)))).sqrMagnitude;
        float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0)return (p-(b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6))))).sqrMagnitude;
        float sum=va+vb+vc;if(Mathf.Abs(sum)<1e-20f)return Mathf.Min(ap.sqrMagnitude,bp.sqrMagnitude,cp.sqrMagnitude);
        return (p-(a+ab*(vb/sum)+ac*(vc/sum))).sqrMagnitude;
    }
    static float Segments(Vector3 p1,Vector3 q1,Vector3 p2,Vector3 q2)
    {
        var d1=q1-p1;var d2=q2-p2;var r=p1-p2;float a=Vector3.Dot(d1,d1),e=Vector3.Dot(d2,d2),f=Vector3.Dot(d2,r),s,t;
        if(a<1e-12f&&e<1e-12f)return r.sqrMagnitude;
        if(a<1e-12f){s=0;t=Mathf.Clamp01(f/e);}else{float c=Vector3.Dot(d1,r);if(e<1e-12f){t=0;s=Mathf.Clamp01(-c/a);}else{float b=Vector3.Dot(d1,d2),denom=a*e-b*b;s=Mathf.Abs(denom)>1e-20f?Mathf.Clamp01((b*f-c*e)/denom):0;t=(b*s+f)/e;if(t<0){t=0;s=Mathf.Clamp01(-c/a);}else if(t>1){t=1;s=Mathf.Clamp01((b-c)/a);}}}
        return (p1+d1*s-p2-d2*t).sqrMagnitude;
    }
}
public static class VerifyAvalanche
{
    const string Dir="Temp/AvalancheReview/";
    public static async Task<object> PipeOnly()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC play mode required");
        var seq=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();bool previous=seq.enabled;seq.enabled=false;
        var clearance=new PipeMeshClearance();var collisions=new List<object>();int samples=0,frame=Time.frameCount;float elapsed=0,last=Time.realtimeSinceStartup;bool available=false;
        try
        {
            seq.RestoreAftershock();seq.BeginAftershockNow();
            while(elapsed<Mathf.Max(7.6f,pipe.AvailableAtSeconds+1.5f))
            {
                float now=Time.realtimeSinceStartup,delta=now-last;last=now;elapsed+=delta;seq.Advance(delta);
                collisions.AddRange(clearance.Inspect(pipe,elapsed));samples++;available|=pipe.IsAvailable;
                await Task.Delay(8);
            }
            var position=pipe.transform.position;bool reachable=Vector3.Distance(position,Camera.main.transform.position)<.85f&&position.y>-.05f;
            var result=new{result=collisions.Count==0&&available&&reachable?"PASS":"FAIL",samples,frames=Time.frameCount-frame,available,position=position.ToString("F3"),reachable,pipe.LastBlockedSurface,blockedAt=pipe.BlockedAt.ToString("F3"),renderedRadius=clearance.RenderedRadius,physicalRadius=pipe.GetComponent<CapsuleCollider>().radius,geometry="rendered-radius capsule against visible rubble triangles; 1.5mm tolerance",intersections=collisions.Take(30).ToArray(),count=collisions.Count};File.WriteAllText(Dir+"PipeClearance.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{seq.RestoreAftershock();seq.enabled=previous;}
    }
    public static async Task<object> Run()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC play mode required");
        var seq=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();var chunks=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None);
        var d=new SerializedObject(debris).FindProperty("fragments");var small=Enumerable.Range(0,d.arraySize).Select(i=>(Transform)d.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var rain=UnityEngine.Object.FindFirstObjectByType<AftershockRubbleRain>();d=new SerializedObject(rain).FindProperty("joints");var showers=Enumerable.Range(0,d.arraySize).Select(i=>(ParticleSystem)d.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        bool previous=seq.enabled;seq.enabled=false;seq.RestoreAftershock();
        var starts=chunks.Select(c=>c.GetComponent<Renderer>().bounds.center).ToArray();var scales=chunks.Select(c=>c.transform.localScale).ToArray();
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool previousDesktop=driver.DesktopEnabled,previousHeld=driver.IsHolding;bool previousLight=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>().FlashlightEnabled;
        driver.SetDesktopMode(true);if(!previousLight)driver.ToggleFlashlight();else if(!driver.IsHolding)driver.ToggleHold();driver.Look(new Vector2(0,85));
        var clearance=new PipeMeshClearance();var intersections=new List<object>();var frames=new List<object>();int startFrame=Time.frameCount,maxMoving=0,maxShowerParticles=0,samples=0;
        float elapsed=0,last=Time.realtimeSinceStartup,nextSweep=0;float[] shots={2.9f,3.48f,3.76f,4.97f,5.35f,6.82f,7.16f,10.50f,17.9f};int shot=0;seq.BeginAftershockNow();
        try
        {
            while(elapsed<18.3f)
            {
                float now=Time.realtimeSinceStartup,delta=now-last;last=now;elapsed+=delta;seq.Advance(delta);
                maxMoving=Math.Max(maxMoving,chunks.Count(c=>c.Released&&c.GetComponent<Renderer>().enabled&&c.GetComponent<Rigidbody>().linearVelocity.magnitude>.2f));
                maxShowerParticles=Math.Max(maxShowerParticles,showers.Sum(p=>p.particleCount));
                if(elapsed<7.35f&&elapsed>=nextSweep){intersections.AddRange(clearance.Inspect(pipe,elapsed));nextSweep=elapsed+.025f;samples++;}
                if(shot<shots.Length&&elapsed>=shots[shot])
                {
                    Capture("Avalanche_"+shot+"_Phone",false,new Vector3(-15,0,0));Capture("Avalanche_"+shot+"_Inspection",true,new Vector3(-15,0,0));
                    frames.Add(new{elapsed,largeReleased=chunks.Count(c=>c.Released),largeMoving=chunks.Count(c=>c.Released&&c.GetComponent<Renderer>().enabled&&c.GetComponent<Rigidbody>().linearVelocity.magnitude>.2f),showerParticles=showers.Sum(p=>p.particleCount),pipe=pipe.transform.position.ToString("F3"),pipe.IsAvailable});shot++;
                }
                await Task.Delay(12);
            }
            await Task.Delay(1000);
            var drops=chunks.Select((c,i)=>starts[i].y-c.GetComponent<Renderer>().bounds.center.y).ToArray();
            int visible=chunks.Count(c=>c.GetComponent<Renderer>().enabled),settled=chunks.Count(c=>c.GetComponent<Renderer>().enabled&&(c.GetComponent<Rigidbody>().IsSleeping()||c.GetComponent<Rigidbody>().linearVelocity.magnitude<.1f));
            var escaped=chunks.Where(c=>c.GetComponent<Renderer>().enabled&&c.GetComponent<Renderer>().bounds.center.y<-.25f).Select(c=>c.name).ToArray();
            var result=new{result=intersections.Count==0&&pipe.IsAvailable&&chunks.All(c=>c.Released)&&maxMoving>=6&&maxShowerParticles>=120&&escaped.Length==0?"PASS":"FAIL",large=chunks.Length,largeReleased=chunks.Count(c=>c.Released),visibleLarge=visible,settledLarge=settled,escaped,smallPool=small.Length,visibleSmall=small.Count(t=>t.gameObject.activeInHierarchy&&t.GetComponent<Renderer>().enabled),maxSimultaneouslyMovingLarge=maxMoving,maxShowerParticles,clearanceSamples=samples,pipeIntersections=intersections.Count,intersections=intersections.Take(20).ToArray(),pipe.IsAvailable,liveFrames=Time.frameCount-startFrame,drops,details=chunks.Select((c,i)=>new{c.name,visible=c.GetComponent<Renderer>().enabled,center=c.GetComponent<Renderer>().bounds.center.ToString("F3"),drop=drops[i]}).ToArray(),frames};
            File.WriteAllText(Dir+"AvalancheVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            seq.RestoreAftershock();await Task.Delay(80);
            bool reset=chunks.Select((c,i)=>!c.Released&&Vector3.Distance(c.transform.localScale,scales[i])<.0001f&&Vector3.Distance(c.GetComponent<Renderer>().bounds.center,starts[i])<.008f).All(x=>x);
            File.WriteAllText(Dir+"AvalancheReset.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{reset,allSmallReattached=small.All(t=>t.GetComponent<Rigidbody>().isKinematic),allShowersClear=showers.All(p=>p.particleCount==0)}));
            return result;
        }
        finally
        {
            seq.RestoreAftershock();seq.enabled=previous;driver.Look(new Vector2(0,-85));if(!previousLight&&driver.Phone.FlashlightEnabled)driver.ToggleFlashlight();if(!previousHeld)driver.ReturnPhone();if(!previousDesktop)driver.SetDesktopMode(false);
        }
    }
    public static void Capture(string name,bool lamp,Vector3 angles)
    {
        var main=Camera.main;var go=new GameObject("Temporary avalanche review camera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;camera.fieldOfView=90;camera.nearClipPlane=.02f;camera.transform.SetPositionAndRotation(main.transform.position,Quaternion.Euler(angles));camera.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        if(lamp){var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.8f;light.range=4;light.shadows=LightShadows.Soft;}
        var hidden=main.transform.root.GetComponentsInChildren<Renderer>(true);var states=hidden.Select(r=>r.forceRenderingOff).ToArray();foreach(var r in hidden)r.forceRenderingOff=true;
        var rt=new RenderTexture(1100,850,24);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes(Dir+name+".png",pixels.EncodeToPNG());}
        finally{for(int i=0;i<hidden.Length;i++)hidden[i].forceRenderingOff=states[i];camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
