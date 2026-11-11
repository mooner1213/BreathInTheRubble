using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using BreathInTheRubble.Prototype;
public static class VerifyCollapse360
{
    const string Dir="Temp/Collapse360Review/";
    public static object AuditPipeAudio()
    {
        var pipe=UnityEngine.Object.FindFirstObjectByType<PipeImpactAudio>();var source=(AudioSource)new SerializedObject(pipe).FindProperty("impactSource").objectReferenceValue;return new{emitter=source!=null?source.name:"none",emitterOnPhysicalRoot=source!=null&&source.transform==pipe.transform,emitterWouldMoveBody=source!=null&&pipe.transform.IsChildOf(source.transform)};
    }
    public static object ProbeBlockedQuery()
    {
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var capsule=pipe.GetComponent<CapsuleCollider>();bool enabled=capsule.enabled;var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.position=new Vector3(100,100,100);cube.transform.localScale=Vector3.one*.1f;try{Physics.SyncTransforms();var position=cube.transform.position;capsule.enabled=false;bool disabled=Physics.ComputePenetration(capsule,position,Quaternion.identity,cube.GetComponent<Collider>(),position,Quaternion.identity,out var a,out float da);bool blocked=(bool)typeof(AftershockPipeDrop).GetMethod("BlockedPose",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(pipe,new object[]{position,Quaternion.identity});capsule.enabled=true;bool active=Physics.ComputePenetration(capsule,position,Quaternion.identity,cube.GetComponent<Collider>(),position,Quaternion.identity,out var b,out float db);return new{disabled,disabledDepth=da,blocked,active,activeDepth=db};}finally{capsule.enabled=enabled;UnityEngine.Object.DestroyImmediate(cube);}
    }
    public static object PipeGeometry()
    {
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var points=pipe.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>pipe.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();var b=new Bounds(points[0],Vector3.zero);foreach(var v in points)b.Encapsulate(v);return new{meshBounds=b.ToString("F5"),renderedRadius=points.Max(v=>new Vector2(v.x-b.center.x,v.z-b.center.z).magnitude),colliderRadius=pipe.GetComponent<CapsuleCollider>().radius,colliderHeight=pipe.GetComponent<CapsuleCollider>().height,shapes=pipe.GetComponents<Collider>().Select(c=>new{type=c.GetType().Name,c.enabled,c.isTrigger,direction=c is CapsuleCollider cap?cap.direction:-1,size=c is BoxCollider box?box.size.ToString("F3"):"",radius=c is CapsuleCollider cp?cp.radius:0}).ToArray(),cachedShape=((Collider)typeof(AftershockPipeDrop).GetField("shape",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(pipe)).GetType().Name};
    }
    static void Save(string name,object value)=>File.WriteAllText(Dir+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    static void Capture(string name,Vector3 angles,bool lamp,bool actual=false)
    {
        var main=Camera.main;var go=new GameObject("Temporary structural verification camera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.CopyFrom(main);c.enabled=false;if(!actual)c.fieldOfView=90;c.transform.SetPositionAndRotation(main.transform.position,actual?main.transform.rotation:Quaternion.Euler(angles));c.GetUniversalAdditionalCameraData().renderPostProcessing=main.GetUniversalAdditionalCameraData().renderPostProcessing;
        if(lamp){var light=go.AddComponent<Light>();light.type=LightType.Point;light.intensity=.35f;light.range=4;light.shadows=LightShadows.Soft;}
        var rt=new RenderTexture(1100,850,24);var pixels=new Texture2D(1100,850,TextureFormat.RGB24,false);var old=RenderTexture.active;try{c.targetTexture=rt;c.Render();c.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1100,850),0,0);pixels.Apply();File.WriteAllBytes(Dir+name+".png",pixels.EncodeToPNG());}finally{c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
    static void Inspection(string prefix){Capture(prefix+"_Front",Vector3.zero,true);Capture(prefix+"_Right",new Vector3(0,90,0),true);Capture(prefix+"_Rear",new Vector3(0,180,0),true);Capture(prefix+"_Left",new Vector3(0,270,0),true);Capture(prefix+"_Above",new Vector3(-75,0,0),true);Capture(prefix+"_Below",new Vector3(60,0,0),true);}
    static async Task PhoneSweep(DesktopPhoneTestDriver driver)
    {
        Vector2[] angles={new Vector2(90,0),new Vector2(180,0),new Vector2(270,0),new Vector2(0,-65),new Vector2(0,50),Vector2.zero};string[] names={"Right","Rear","Left","Above","Below","Front"};float yaw=0,pitch=0;
        for(int i=0;i<angles.Length;i++){driver.Look(new Vector2((angles[i].x-yaw)/.18f,(pitch-angles[i].y)/.18f));yaw=angles[i].x;pitch=angles[i].y;await Task.Delay(45);Capture("ActualPhone_"+names[i],Vector3.zero,false,true);}
    }
    public static object Audit()
    {
        Directory.CreateDirectory(Dir);Physics.SyncTransforms();var controller=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var all=controller.Elements;var root=GameObject.Find("Independent 360 structural collapse");var colliders=root.GetComponentsInChildren<Collider>().Where(c=>c.enabled).ToArray();var overlaps=new List<object>();
        foreach(var grab in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None))foreach(var c in grab.GetComponentsInChildren<Collider>().Where(c=>c.enabled))foreach(var other in colliders)if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,other,other.transform.position,other.transform.rotation,out var direction,out float depth)&&depth>.002f)overlaps.Add(new{grab.name,surface=other.name,depth});
        var eye=Camera.main.transform.position;var directions=new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left,Vector3.up};var distances=directions.Select(d=>{var hits=Physics.RaycastAll(eye,d,3).Where(h=>{var r=h.collider.GetComponentInParent<MeshRenderer>();return r!=null&&r.enabled;}).OrderBy(h=>h.distance).ToArray();return new{direction=d.ToString("F1"),distance=hits.Length>0?hits[0].distance:-1,surface=hits.Length>0?hits[0].collider.name:"none"};}).ToArray();
        var data=new{elements=all.Length,uniquePhases=all.Select(e=>e.phase).Distinct().Count(),newFractures=all.Count(e=>e.powder!=null),crackLines=all.Sum(e=>e.cracks.Length),distances,propOverlaps=overlaps};Save("Audit",data);return data;
    }
    public static async Task<object> Runtime()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required");Directory.CreateDirectory(Dir);var seq=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool automatic=seq.enabled;seq.enabled=false;seq.RestoreAftershock();
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool desktop=driver.DesktopEnabled,held=driver.IsHolding,lit=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>().FlashlightEnabled;driver.SetDesktopMode(true);if(!driver.IsHolding)driver.ToggleHold();if(!lit)driver.ToggleFlashlight();
        var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var authored=manager.Elements.Where(e=>e.powder!=null).ToArray();var all=UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).Where(c=>c.GetComponent<Renderer>()!=null&&c.GetComponent<Renderer>().enabled).ToArray();var starts=all.Select(c=>c.GetComponent<Renderer>().bounds.center).ToArray();var positions=all.Select(c=>c.transform.localPosition).ToArray();var maxDrop=all.Select(_=>0f).ToArray();var maxTravel=all.Select(_=>0f).ToArray();var dust=authored.Select(e=>e.powder).ToArray();int initialSystems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
        int startFrame=Time.frameCount,peakPowder=0,peakCracks=0,peakMoving=0,first=0,headInsideSamples=0;var headStart=Camera.main.transform.position;float maxHeadDrift=0;float elapsed=0,last=Time.realtimeSinceStartup;var samples=new List<object>();float[] shots={1.50f,1.95f,2.43f,2.95f,3.65f,4.65f,6.55f,8.85f,11.9f,15.3f,28.5f};var restRoots=new[]{GameObject.Find("Interlocked Rubble Structure").transform,GameObject.Find("Close buried cell").transform};var rootPositions=restRoots.Select(t=>t.position).ToArray();var rootRotations=restRoots.Select(t=>t.rotation).ToArray();float commonRootTravel=0;
        try
        {
            await Task.Delay(80);Capture("Initial_Phone",Vector3.zero,false,true);Inspection("Initial");seq.BeginAftershockNow();last=Time.realtimeSinceStartup;
            while(elapsed<29)
            {
                float now=Time.realtimeSinceStartup,delta=now-last;last=now;elapsed+=delta;seq.Advance(delta);
                for(int i=0;i<all.Length;i++)if(all[i].Released){var center=all[i].GetComponent<Renderer>().bounds.center;maxDrop[i]=Mathf.Max(maxDrop[i],starts[i].y-center.y);maxTravel[i]=Mathf.Max(maxTravel[i],Vector3.Distance(starts[i],center));}
                maxHeadDrift=Mathf.Max(maxHeadDrift,Vector3.Distance(headStart,Camera.main.transform.position));foreach(var e in authored)if(e.loose.Released){var shape=e.loose.GetComponent<MeshCollider>();if(shape.enabled&&Vector3.Distance(shape.ClosestPoint(Camera.main.transform.position),Camera.main.transform.position)<.005f)headInsideSamples++;}
                peakPowder=Mathf.Max(peakPowder,dust.Sum(p=>p.particleCount));peakCracks=Mathf.Max(peakCracks,authored.Sum(e=>e.cracks.Count(c=>c.enabled)));peakMoving=Mathf.Max(peakMoving,authored.Count(e=>e.loose.Released&&e.loose.GetComponent<Rigidbody>().linearVelocity.magnitude>.25f));
                for(int i=0;i<restRoots.Length;i++)commonRootTravel=Mathf.Max(commonRootTravel,Vector3.Distance(rootPositions[i],restRoots[i].position)+Quaternion.Angle(rootRotations[i],restRoots[i].rotation));
                if(first<shots.Length&&elapsed>=shots[first]){Inspection("At_"+first);Capture("At_"+first+"_Phone",Vector3.zero,false,true);samples.Add(new{elapsed,newReleased=authored.Count(e=>e.loose.Released),allReleased=all.Count(c=>c.Released),powder=dust.Sum(p=>p.particleCount),cracks=authored.Sum(e=>e.cracks.Count(c=>c.enabled)),moving=authored.Count(e=>e.loose.Released&&e.loose.GetComponent<Rigidbody>().linearVelocity.magnitude>.25f)});if(first==7)await PhoneSweep(driver);first++;}
                await Task.Delay(12);
            }
            var detail=authored.Select(e=>{int i=Array.IndexOf(all,e.loose);var r=e.loose.GetComponent<Renderer>();return new{name=e.target.name,e.loose.Released,drop=maxDrop[i],travel=maxTravel[i],center=r.bounds.center.ToString("F3"),speed=e.loose.GetComponent<Rigidbody>().linearVelocity.magnitude};}).ToArray();
            var result=new{pieces=all.Length,released=all.Count(c=>c.Released),visible=all.Count(c=>c.GetComponent<Renderer>().enabled),escaped=all.Where(c=>c.GetComponent<Renderer>().bounds.center.y<-.25f).Select(c=>new{c.name,center=c.GetComponent<Renderer>().bounds.center.ToString("F3")}).ToArray(),newBigPieces=authored.Length,newBigMoved=detail.Count(d=>d.travel>.15f),newBigDropped=detail.Count(d=>d.drop>.12f),peakMoving,peakCracks,peakPowder,commonRootTravel,maxHeadDrift,headInsideSamples,initialSystems,finalSystems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,frames=Time.frameCount-startFrame,details=detail,samples};Save("Runtime",result);
            seq.PreviewNextStage();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();bool carriedFrozen=authored.All(e=>e.loose.GetComponent<Rigidbody>().isKinematic&&!e.loose.GetComponent<Rigidbody>().useGravity);ending.Advance(60);await Task.Delay(60);Capture("Rescue_Above",new Vector3(-68,0,0),false);Save("Rescue",new{ending.IsComplete,ending.OpeningAmount,ending.SunlightAdmission,ending.WhiteoutAmount,carriedFrozen});
            seq.RestoreAftershock();await Task.Delay(80);bool reset=all.Select((c,i)=>!c.Released&&Vector3.Distance(c.transform.localPosition,positions[i])<.001f).All(b=>b);Save("Reset",new{reset,powderCleared=dust.All(p=>p.particleCount==0),cracksHidden=authored.All(e=>e.cracks.All(c=>!c.enabled))});Debug.Log("[Collapse360Review] "+Newtonsoft.Json.JsonConvert.SerializeObject(new{result.pieces,result.released,result.newBigMoved,result.newBigDropped,result.peakCracks,result.peakPowder,result.commonRootTravel,reset}));return new{result.pieces,result.released,escaped=result.escaped.Length,result.newBigPieces,result.newBigMoved,result.newBigDropped,result.peakMoving,result.peakCracks,result.peakPowder,result.commonRootTravel,result.maxHeadDrift,result.headInsideSamples,result.initialSystems,result.finalSystems,result.frames,reset};
        }
        finally{seq.RestoreAftershock();seq.enabled=automatic;if(!lit&&driver.Phone.FlashlightEnabled)driver.ToggleFlashlight();if(!held)driver.ReturnPhone();if(!desktop)driver.SetDesktopMode(false);}
    }
}
