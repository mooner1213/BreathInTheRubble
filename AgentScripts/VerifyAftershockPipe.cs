using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using BreathInTheRubble.Prototype;

public static class VerifyAftershockPipe
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static object Run()
    {
        Check(Application.isPlaying && !DesktopPhoneTestDriver.HasPhysicalHmd,"PC Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var drop=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();
        var grab=drop.GetComponent<XRGrabInteractable>();var body=drop.GetComponent<Rigidbody>();var shape=drop.GetComponent<Collider>();var audio=drop.GetComponent<PipeImpactAudio>();
        var head=Camera.main.transform;var position=head.position;var rotation=head.rotation;
        bool sequenceEnabled=sequence.enabled;sequence.enabled=false;
        var simulation=Physics.simulationMode;
        try
        {
            Physics.simulationMode=SimulationMode.Script;
            sequence.RestoreAftershock();
            Check(!drop.IsAvailable && !grab.enabled && !shape.enabled && !drop.GetComponent<Renderer>().enabled,"Pipe visible/grabbable before event.");
            sequence.BeginAftershockNow();sequence.Advance(4.9f);
            Check(!drop.IsRevealed,"Pipe revealed before ceiling opens.");
            sequence.Advance(.3f);Check(drop.IsRevealed && !drop.IsAvailable && !grab.enabled,"Sliding phase invalid.");
            var sliding=drop.transform.position;
            sequence.Advance(1.5f);Check(drop.transform.position.y<sliding.y && !drop.IsAvailable,"Pipe is not gradually descending.");
            sequence.Advance(1.3f);Physics.SyncTransforms();
            Check(drop.IsAvailable && grab.enabled && shape.enabled && !body.isKinematic && body.useGravity,"Landed pipe is not a physical grab target.");
            Check(Vector3.Distance(drop.transform.position,drop.LandingPose.position)<.002f,"Pipe missed reachable landing.");
            var landing=drop.transform.position;
            for(int i=0;i<250;i++)Physics.Simulate(.02f);
            var settled=drop.transform.position;
            Check(Vector3.Distance(landing,settled)<.16f,"Pipe rolled out of intended resting space: "+settled);
            Check(settled.y>.35f && Vector3.Distance(head.position,settled)<.65f,"Pipe is out of seated reach.");
            Check(Vector3.Distance(settled,UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>().transform.position)>.16f,"Pipe obstructs phone.");
            // A temporary contact interactor exercises selection without moving tracked controllers.
            var manager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
            var hand=new GameObject("Temporary XRI pipe contact test");hand.transform.position=settled;
            var trigger=hand.AddComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.035f;
            var handBody=hand.AddComponent<Rigidbody>();handBody.isKinematic=true;handBody.useGravity=false;
            var interactor=hand.AddComponent<XRDirectInteractor>();interactor.interactionManager=manager;
            try
            {
                Check(drop.GetComponent<PhoneContactOnlyGrabFilter>().IsInteractorTouchingPhone(interactor),"Contact filter rejected near hand.");
                manager.SelectEnter((IXRSelectInteractor)interactor,(IXRSelectInteractable)grab);
                Check(grab.isSelected && body.constraints==RigidbodyConstraints.None,"XRI selection failed or resting constraint persisted: selected="+grab.isSelected+", constraints="+body.constraints);
                manager.SelectExit((IXRSelectInteractor)interactor,(IXRSelectInteractable)grab);Check(!grab.isSelected && !body.isKinematic,"XRI release failed.");
            }
            finally{UnityEngine.Object.DestroyImmediate(hand);}
            sequence.Advance(11f);Check(sequence.Stage==GameStage.Isolation && drop.IsAvailable,"Isolation did not retain the pipe.");
            Check(UnityEngine.Object.FindObjectsByType<AftershockFallingChunk>(FindObjectsSortMode.None).All(c=>Physics.GetIgnoreCollision(shape,c.GetComponent<Collider>())),"Cosmetic masonry can push the held pipe.");
            Check(head.position==position && head.rotation==rotation,"Tracked camera was moved.");
            Check(UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length==1,"XR Origin duplicated.");
            var targets=UnityEngine.Object.FindObjectsByType<PipeImpactSurface>(FindObjectsSortMode.None);
            Check(targets.Any(t=>t.Kind==PipeImpactSurface.SurfaceKind.Metal)&&targets.Any(t=>t.Kind==PipeImpactSurface.SurfaceKind.Concrete),"Target materials missing.");
            var concrete=targets.First(t=>t.name=="Right Wall");
            Check(!audio.TryPlayImpact(concrete,concrete.transform.position,.01f),"Resting contact produced sound.");
            var tests=new System.Collections.Generic.List<object>();
            // Real physics collisions against two differently marked surfaces in an isolated test pocket.
            foreach(var kind in new[]{PipeImpactSurface.SurfaceKind.Concrete,PipeImpactSurface.SurfaceKind.Metal})
            {
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary pipe collision verification";wall.transform.position=new Vector3(5,2,5.22f);wall.transform.localScale=new Vector3(1,1,.04f);
                var marker=wall.AddComponent<PipeImpactSurface>();var data=new SerializedObject(marker);data.FindProperty("kind").enumValueIndex=(int)kind;data.ApplyModifiedPropertiesWithoutUndo();
                typeof(PipeImpactAudio).GetField("nextImpactTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(audio,0f);
                body.useGravity=false;body.position=new Vector3(5,2,5);body.rotation=Quaternion.identity;body.linearVelocity=Vector3.forward*2;body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
                int before=audio.ImpactCount;
                try
                {
                    for(int i=0;i<15;i++)Physics.Simulate(.02f);
                    Check(audio.ImpactCount==before+1 && audio.LastSurface==kind,"Physical "+kind+" collision did not play once.");
                    Check(!audio.TryPlayImpact(marker,body.position,2f),"Cooldown allowed repeated sound.");
                    tests.Add(new{kind=kind.ToString(),physicalContact=true,oneSound=true});
                }
                finally{UnityEngine.Object.DestroyImmediate(wall);}
            }
            sequence.RestoreAftershock();Check(!grab.enabled && !shape.enabled && !drop.IsAvailable && audio.ImpactCount==0,"Replay reset failed.");
            sequence.BeginAftershockNow();sequence.Advance(8f);Check(drop.IsAvailable,"Second release did not work.");
            var desktop=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();bool wasDesktop=desktop.DesktopEnabled;
            if(!wasDesktop)desktop.SetDesktopMode(true);
            var pipeDriver=drop.GetComponent<DesktopPipeTestDriver>();pipeDriver.SetHeld(true);
            Check(pipeDriver.IsHolding && body.constraints==RigidbodyConstraints.None && !grab.enabled,"PC hold did not release resting constraints.");
            pipeDriver.Swing();pipeDriver.SetHeld(false);Check(!pipeDriver.IsHolding && grab.enabled && body.useGravity,"PC release did not restore XR grab/physics.");
            if(!wasDesktop)desktop.SetDesktopMode(false);
            return new{result="PASS",landing=landing.ToString("F3"),settledAfter5s=settled.ToString("F3"),headDistance=Vector3.Distance(position,settled),availableAt=drop.AvailableAtSeconds,collisionTests=tests,checks="hidden start, timed slide/tip/fall, physical settling, XRI select/release, Isolation persistence, speed threshold/cooldown, real concrete/metal collisions, replay, single XR Origin, unchanged HMD pose"};
        }
        finally{Physics.simulationMode=simulation;sequence.RestoreAftershock();sequence.enabled=sequenceEnabled;}
    }
    public static object Preview()
    {
        Check(Application.isPlaying && !DesktopPhoneTestDriver.HasPhysicalHmd,"PC Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var drop=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();if(!driver.DesktopEnabled)driver.SetDesktopMode(true);
        sequence.enabled=false;sequence.RestoreAftershock();sequence.BeginAftershockNow();sequence.Advance(8f);
        var go=new GameObject("Temporary pipe inspection camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.nearClipPlane=.01f;
        camera.transform.SetPositionAndRotation(Camera.main.transform.position,Quaternion.Euler(58,0,0));
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=2;light.intensity=.6f;light.shadows=LightShadows.None;
        try
        {
            Directory.CreateDirectory("Temp/PipeReview");
            UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();
            Save(camera,"Temp/PipeReview/Landed_InspectionLight.png");
            sequence.RestoreAftershock();sequence.BeginAftershockNow();sequence.Advance(6.9f);UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();camera.transform.rotation=Quaternion.Euler(-15,0,0);Save(camera,"Temp/PipeReview/Sliding_InspectionLight.png");
            sequence.RestoreAftershock();sequence.BeginAftershockNow();sequence.Advance(8f);
            driver.ToggleFlashlight();driver.Look(new Vector2(0,-120));
            return new{images=new[]{"Temp/PipeReview/Landed_InspectionLight.png","Temp/PipeReview/Sliding_InspectionLight.png"},note="Temporary camera/light only for structure review; main HMD unchanged. Left in own Play session for follow-up verification."};
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static void Save(Camera camera,string path)
    {
        var target=new RenderTexture(1100,900,24);var previous=RenderTexture.active;var output=new Texture2D(1100,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;output.ReadPixels(new Rect(0,0,1100,900),0,0);output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(output);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
