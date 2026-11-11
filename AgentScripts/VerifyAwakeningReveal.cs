using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using BreathInTheRubble.Prototype;
public static class VerifyAwakeningReveal
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static async Task<object> Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"Desktop Play Mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool enabled=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();
        var reveal=UnityEngine.Object.FindFirstObjectByType<AwakeningReveal>();var renderer=(MeshRenderer)new SerializedObject(reveal).FindProperty("eyelidRenderer").objectReferenceValue;
        var head=Camera.main.transform;var pose=head.position;var rotation=head.rotation;var hands=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Left Controller"||t.name=="Right Controller").ToArray();var positions=hands.Select(t=>t.position).ToArray();var angles=hands.Select(t=>t.rotation).ToArray();
        var phone=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>();bool light=phone.FlashlightEnabled;
        int firstFrame=Time.frameCount;int materialId=renderer.sharedMaterial.GetInstanceID();
        try
        {
            Check(renderer.GetComponent<Collider>()==null&&Camera.main.GetUniversalAdditionalCameraData().requiresDepthTexture,"Overlay collision or depth configuration is wrong");
            Check(renderer.sortingOrder>20&&renderer.sharedMaterial.renderQueue==5000,"Phone canvas can render over closed eyes");
            // Let the frame containing this script's in-memory compilation finish before timing live updates.
            await Task.Delay(100);reveal.Replay();Check(reveal.Opening==0f&&renderer.enabled,"Replay did not close the eyes");await Task.Delay(280);Check(reveal.Opening==0f,"Blackout does not hold");
            await Task.Delay(850);float partial=reveal.Opening;Check(partial>0f&&partial<.5f&&renderer.enabled,"Actual frames do not open the eyelids gradually");
            await Task.Delay(Mathf.CeilToInt((reveal.DurationSeconds-1.13f+.2f)*1000));Check(reveal.IsComplete&&!renderer.enabled,"Opening overlay did not disappear");
            Check(Time.frameCount>firstFrame+10,"No live frames observed");
            Check(Vector3.Distance(head.position,pose)<.0001f&&Quaternion.Angle(head.rotation,rotation)<.001f,"Opening animation drives head pose");
            for(int i=0;i<hands.Length;i++)Check(Vector3.Distance(hands[i].position,positions[i])<.0001f&&Quaternion.Angle(hands[i].rotation,angles[i])<.001f,"Opening animation moves controller tracking");
            for(int i=0;i<10;i++){reveal.Replay();reveal.Advance(100f);Check(reveal.IsComplete&&!renderer.enabled,"Reveal replay did not finish");}
            Check(renderer.sharedMaterial.GetInstanceID()==materialId&&phone.FlashlightEnabled==light,"Reveal allocates new materials or changes phone controls");
            return new{result="PASS",duration=reveal.DurationSeconds,sampledFrames=Time.frameCount-firstFrame,partialOpeningAt1_13s=partial,checks="real-time blackout / opening / complete, overlay order, no colliders, no material allocation on 10 replays, unchanged head/controller poses and phone light"};
        }
        finally{reveal.Complete();sequence.RestoreAftershock();sequence.enabled=enabled;}
    }
}
