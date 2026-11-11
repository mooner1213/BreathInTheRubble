using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using BreathInTheRubble.Prototype;

public static class VerifyRescueEnding
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static T Ref<T>(UnityEngine.Object target,string field) where T:UnityEngine.Object => new SerializedObject(target).FindProperty(field).objectReferenceValue as T;
    static Transform[] Targets(RescueEndingDirector ending)
    {
        var p=new SerializedObject(ending).FindProperty("rubble");return Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue as Transform).ToArray();
    }
    static void Strike(PipeImpactAudio pipe,DesktopPipeTestDriver hand,PipeImpactSurface surface,bool held)
    {
        hand.SetHeld(held);if(held)Check(hand.IsHolding,"PC test hand not holding pipe.");
        typeof(PipeImpactAudio).GetField("nextImpactTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pipe,0f);
        Check(pipe.TryPlayImpact(surface,pipe.transform.position,1.8f),"Test strike was not audible.");
    }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"PC Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var gate=UnityEngine.Object.FindFirstObjectByType<RescueSignalController>();
        var call=UnityEngine.Object.FindFirstObjectByType<EmergencyCallSystem>();var reception=call.GetComponent<PhoneReceptionProgression>();var pipe=UnityEngine.Object.FindFirstObjectByType<PipeImpactAudio>();var hand=pipe.GetComponent<DesktopPipeTestDriver>();
        var desktop=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();if(desktop==null)desktop=new GameObject("Temporary desktop test driver").AddComponent<DesktopPhoneTestDriver>();
        bool wasDesktop=desktop.DesktopEnabled;if(!wasDesktop)desktop.SetDesktopMode(true);
        bool enabled=sequence.enabled;sequence.enabled=false;
        var camera=Camera.main;var cameraData=camera.GetComponent<UniversalAdditionalCameraData>();bool post=cameraData.renderPostProcessing;
        var headPosition=camera.transform.position;var headRotation=camera.transform.rotation;
        var tracked=camera.transform.root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Left Controller"||t.name=="Right Controller").ToArray();var trackedPositions=tracked.Select(t=>t.position).ToArray();
        var roofs=Targets(ending);var rests=roofs.Select(t=>t.position).ToArray();
        var target=UnityEngine.Object.FindObjectsByType<PipeImpactSurface>(FindObjectsSortMode.None).First(t=>t.Kind==PipeImpactSurface.SurfaceKind.Metal);
        try
        {
            Check(!call.RescueRequestSent,"Fresh Play Mode needed to test the no-call branch.");
            sequence.RestoreAftershock();sequence.BeginAftershockNow();sequence.Advance(18.1f);
            Check(sequence.Stage==GameStage.Isolation,"Isolation setup failed.");
            Strike(pipe,hand,target,true);Check(sequence.Stage==GameStage.Isolation,"Strike alone triggered rescue.");hand.SetHeld(false);
            gate.PreviewSearchCue();Check(gate.IsListeningWindow,"Radio opportunity is not audible.");
            Strike(pipe,hand,target,true);Check(!gate.SignalAccepted&&sequence.Stage==GameStage.Isolation,"Radio plus strike rescued without a phone request.");hand.SetHeld(false);
            gate.Advance(gate.CueDuration+.1f);Check(!gate.IsListeningWindow,"Radio opportunity did not close.");
            // Exercise the real 119 coroutine, rather than assigning a success flag.
            reception.PreviewSignal(0);Check(!call.Dial("118")&&!call.RescueRequestSent,"Invalid number registered a request.");
            reception.PreviewSignal(2);Check(call.Dial("119"),"119 attempt failed to start.");
            var timeout=DateTime.UtcNow.AddSeconds(10);
            while(!call.RescueRequestSent&&DateTime.UtcNow<timeout)await Task.Delay(50);
            Check(call.RescueRequestSent&&sequence.HasRequestedRescue,"Real 119 connection did not register a request.");
            call.HangUp();reception.PreviewSignal(0);Check(sequence.HasRequestedRescue,"Completed request was lost after hangup/network loss.");
            sequence.Advance(Mathf.Max(0f,240.1f-sequence.ElapsedSeconds));
            Check(sequence.Stage==GameStage.Isolation&&!ending.IsRunning,"Elapsed time plus phone call auto-triggered ending.");
            if(gate.IsListeningWindow)gate.Advance(gate.CueDuration+.1f);
            Strike(pipe,hand,target,true);Check(!ending.IsRunning,"Out-of-window strike was accepted.");hand.SetHeld(false);
            int searches=gate.SearchCount;sequence.Advance(18.2f);Check(gate.SearchCount>searches&&gate.IsListeningWindow,"Missed search did not repeat.");
            Check(!ending.IsRunning,"Old out-of-window strike was banked.");
            Strike(pipe,hand,target,false);Check(!ending.IsRunning,"Dropped/unheld pipe triggered rescue.");
            var startRoofs=roofs.Select(t=>t.position).ToArray();
            Strike(pipe,hand,target,true);Check(gate.SignalAccepted&&sequence.Stage==GameStage.Rescue&&ending.IsRunning,"Both conditions did not start ending.");hand.SetHeld(false);
            Check(!gate.IsListeningWindow&&cameraData.renderPostProcessing,"Search not stopped or ending post-processing missing.");
            ending.Advance(8f);float early=ending.OpeningAmount;Check(early>0&&early<1,"Early opening did not progress.");
            Check(roofs.Where((t,i)=>t.position!=startRoofs[i]).Any(),"No overhead rubble was removed.");
            ending.Advance(17f);Check(ending.OpeningAmount>early&&ending.WhiteoutAmount==0,"Opening did not progress without a white veil.");
            ending.Advance(ending.DurationSeconds);Check(ending.IsComplete&&!ending.IsRunning&&ending.WhiteoutAmount==0f&&ending.SunlightAdmission>0,"Ending did not complete in natural daylight.");
            Check(camera.transform.position==headPosition&&camera.transform.rotation==headRotation,"Ending moved the HMD camera.");
            Check(tracked.Where((t,i)=>t.position!=trackedPositions[i]).Count()==0,"Ending moved a controller.");
            Check(UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length==1,"XR Origin duplicated.");
            sequence.RestoreAftershock();Check(!ending.IsRunning&&!ending.IsComplete&&ending.WhiteoutAmount==0&&!gate.SignalAccepted,"Ending reset failed.");
            Check(cameraData.renderPostProcessing==post,"Reset did not restore camera post-processing.");
            Check(roofs.Where((t,i)=>Vector3.Distance(t.position,rests[i])>.001f).Count()==0,"Reset did not restore original roofs.");
            // Existing explicit editor preview must remain repeatable without another phone request.
            sequence.PreviewNextStage();Check(ending.IsRunning,"Editor next-stage preview did not start ending.");sequence.RestoreAftershock();
            return new{result="PASS",cases=new[]{"no call/no cue/strike rejected","radio plus held strike without request rejected","invalid number rejected","real 119 connection persisted after hangup","phone plus elapsed time rejected","phone plus strike outside radio rejected","missed opportunity repeated; old strikes not banked","unheld impact rejected","phone plus audible radio plus held strike accepted"},endingSeconds=ending.DurationSeconds,movingRubble=roofs.Length,checks="progressive roof opening/daylight, no white overlay, completion/reset/replay, original roof restoration, camera settings restored, physical HMD/controller transforms preserved, single XR Origin",frame=Time.frameCount};
        }
        finally
        {
            hand.SetHeld(false);sequence.RestoreAftershock();sequence.enabled=enabled;reception.ResumeAutomaticRecovery();
            if(!wasDesktop)desktop.SetDesktopMode(false);
        }
    }
    public static object Preview()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"PC Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();if(driver!=null&&!driver.DesktopEnabled)driver.SetDesktopMode(true);
        bool enabled=sequence.enabled;sequence.enabled=false;
        var go=new GameObject("Temporary rescue review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.transform.SetPositionAndRotation(Camera.main.transform.position,Quaternion.Euler(-22,0,0));
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1;
        // Camera-child whiteout is rendered by the real main camera in the final capture.
        try
        {
            Directory.CreateDirectory("Temp/RescueReview");sequence.RestoreAftershock();sequence.BeginAftershockNow();sequence.Advance(18.1f);
            UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();
            Save(camera,"Before");sequence.PreviewNextStage();ending.Advance(8f);Save(camera,"Opening_08s");ending.Advance(10f);Save(camera,"Opening_18s");ending.Advance(10f);Save(camera,"Brightening_28s");ending.Advance(6f);
            Save(Camera.main,"Complete_34s");
            return new{images=new[]{"Before","Opening_08s","Opening_18s","Brightening_28s","Complete_34s"}.Select(n=>"Temp/RescueReview/"+n+".png"),note="Opening images use an unlit inspection camera at the unchanged HMD position; final image uses actual main camera and whiteout."};
        }
        finally{sequence.RestoreAftershock();sequence.enabled=enabled;UnityEngine.Object.DestroyImmediate(go);}
    }
    static void Save(Camera camera,string name)
    {
        var prior=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(1000,850,24);var image=new Texture2D(1000,850,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1000,850),0,0);image.Apply();File.WriteAllBytes("Temp/RescueReview/"+name+".png",image.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
