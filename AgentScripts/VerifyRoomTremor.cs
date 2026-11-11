using System;
using System.Linq;
using System.Threading.Tasks;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEngine;

public static class VerifyRoomTremor
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static async Task<object> Run()
    {
        Check(Application.isPlaying && !DesktopPhoneTestDriver.HasPhysicalHmd,"Desktop Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();
        Check(driver!=null && driver.DesktopEnabled,"PC driver is not enabled.");
        sequence.RestoreAftershock();await Task.Delay(80);
        var camera=Camera.main.transform;Vector3 restPosition=camera.position;Quaternion restRotation=camera.rotation;
        var array=new SerializedObject(motion).FindProperty("roomTargets");
        var targets=Enumerable.Range(0,array.arraySize).Select(i=>(Transform)array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        Check(targets.Length>=10 && targets.Any(t=>t.name=="Front Rubble Seal"),"Front/peripheral structure is not connected.");
        var positions=targets.Select(t=>t.position).ToArray();var rotations=targets.Select(t=>t.rotation).ToArray();
        var hands=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Left Controller" || t.name=="Right Controller").ToArray();
        var handPositions=hands.Select(t=>t.position).ToArray();var handRotations=hands.Select(t=>t.rotation).ToArray();
        float cameraDistance=0f,cameraDegrees=0f,roomDistance=0f;
        int startFrame=Time.frameCount;
        try
        {
            sequence.BeginAftershockNow();sequence.Advance(2.2f);
            for(int sample=0;sample<18;sample++)
            {
                await Task.Delay(35);
                cameraDistance=Mathf.Max(cameraDistance,Vector3.Distance(camera.position,restPosition));
                cameraDegrees=Mathf.Max(cameraDegrees,Quaternion.Angle(camera.rotation,restRotation));
                roomDistance=Mathf.Max(roomDistance,Vector3.Distance(targets[0].position,positions[0]));
                Quaternion rotation=targets[0].rotation*Quaternion.Inverse(rotations[0]);
                Vector3 translation=targets[0].position-rotation*positions[0];
                for(int i=1;i<targets.Length;i++)
                    Check(Vector3.Distance(targets[i].position,rotation*positions[i]+translation)<0.0002f,"Room structure is shaking out of phase.");
            }
            Check(Time.frameCount>startFrame+5,"Game did not advance during tremor sampling.");
            Check(cameraDistance>0.001f && cameraDegrees>0.25f,"PC view does not visibly vibrate.");
            Check(roomDistance>0.004f,"Front rubble does not move with the room.");
            for(int i=0;i<hands.Length;i++)Check(Vector3.Distance(hands[i].position,handPositions[i])<0.0001f && Quaternion.Angle(hands[i].rotation,handRotations[i])<0.001f,"Tremor moved controller tracking.");
            sequence.RestoreAftershock();await Task.Delay(80);
            Check(Vector3.Distance(camera.position,restPosition)<0.00001f && Quaternion.Angle(camera.rotation,restRotation)<0.001f,"PC view does not return to its baseline.");
            for(int i=0;i<targets.Length;i++)Check(Vector3.Distance(targets[i].position,positions[i])<0.0001f && Quaternion.Angle(targets[i].rotation,rotations[i])<0.001f,"Room does not restore exactly.");
            Check(motion.DesktopShakeOffset==Vector3.zero && motion.DesktopShakeEuler==Vector3.zero,"Residual PC shake remains after restore.");

            driver.SetDesktopMode(false);await Task.Delay(200);
            Vector3 xrPosition=camera.position;Quaternion xrRotation=camera.rotation;
            sequence.BeginAftershockNow();sequence.Advance(2.2f);await Task.Delay(250);
            Check(Vector3.Distance(camera.position,xrPosition)<0.0001f && Quaternion.Angle(camera.rotation,xrRotation)<0.001f,"Room tremor drives the XR simulation camera.");
            sequence.RestoreAftershock();driver.SetDesktopMode(true);await Task.Delay(80);
            Debug.Log("[Room tremor verification] PASS: live PC camera vibration, coherent front/wall motion, exact restore, controller poses unchanged, XR simulation camera unchanged.");
            return new{result="PASS",sampledFrames=Time.frameCount-startFrame,maxDesktopPositionMeters=cameraDistance,maxDesktopAngleDegrees=cameraDegrees,maxFrontRubbleDistanceMeters=roomDistance,groups=targets.Length,
                checks="actual frame sampling, PC view vibration, coherent space motion, exact restore, unchanged controller poses, unchanged XR simulation camera; no physical headset tested"};
        }
        finally{sequence.RestoreAftershock();if(!driver.DesktopEnabled)driver.SetDesktopMode(true);}
    }
}
