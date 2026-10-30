using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;

public static class VerifyEmergencyDialer
{
    static PhoneDialerView View=>UnityEngine.Object.FindFirstObjectByType<PhoneDialerView>();
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static object Snapshot(string check)=>new {check,time=Time.time,bars=View.CallSystem.SignalBars,state=View.CallSystem.State.ToString(),number=View.Number,open=View.IsOpen,
        visibleBars=View.GetComponentsInChildren<UnityEngine.UI.Image>(true).Count(i=>i.name.StartsWith("Signal Bar ")&&i.enabled),status=View.transform.Find("Phone Interface Canvas/Emergency Dialer/Dial Status").GetComponent<TMPro.TMP_Text>().text};

    public static object StartNoSignal()
    {
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();
        driver.EmergencyCall();
        Check(View.IsOpen && View.Number=="119","SOS did not open prefilled 119.");
        Check(View.CallSystem.State==EmergencyCallSystem.CallState.Idle,"Opening dialer placed a call.");
        View.GetComponent<PhoneReceptionProgression>().PreviewSignal(0);
        Click("Dial Confirm Call");
        Check(View.CallSystem.State==EmergencyCallSystem.CallState.Connecting,"Call button did not start connection attempt.");
        Check(!View.CallSystem.Dial("119"),"Duplicate attempt accepted while connecting.");
        return Snapshot("prefilled 119, zero bars, tracked call click, duplicate rejected");
    }

    public static object CheckFailed()
    {
        Check(View.CallSystem.State==EmergencyCallSystem.CallState.NoSignal,"Zero-service attempt must fail.");
        Check(View.GetComponentsInChildren<UnityEngine.UI.Image>(true).Where(i=>i.name.StartsWith("Signal Bar ")).All(i=>!i.enabled),"Zero-service bars are visible.");
        return Snapshot("no service fails and shows no bars");
    }

    public static object StartWeakSignal()
    {
        CheckFailed();
        View.GetComponent<PhoneReceptionProgression>().PreviewSignal(1);
        Click("Dial Confirm Call");
        Check(View.CallSystem.State==EmergencyCallSystem.CallState.Connecting,"Weak-signal retry did not start.");
        return Snapshot("one-bar retry started");
    }

    public static object CheckConnected()
    {
        Check(View.CallSystem.State==EmergencyCallSystem.CallState.Connected,"One-bar attempt did not connect.");
        return Snapshot("one bar connects to 119");
    }

    public static object OtherCases()
    {
        CheckConnected();
        View.CallSystem.SetSignalBars(0);
        Check(View.CallSystem.State==EmergencyCallSystem.CallState.NoSignal,"Signal loss must drop the call.");
        View.CallSystem.HangUp();
        Check(!View.CallSystem.Dial("112") && View.CallSystem.State==EmergencyCallSystem.CallState.InvalidNumber,"Non-119 number was accepted.");
        View.CallSystem.HangUp();
        View.DeleteDigit();Check(View.Number=="11","Delete key failed.");
        View.AppendDigit("9");Check(View.Number=="119","Number entry failed.");
        View.CallSystem.SetSignalBars(2);View.ConfirmCall();View.Close();
        Check(View.CallSystem.State==EmergencyCallSystem.CallState.Idle && !View.IsOpen,"Back did not cancel connecting call.");
        View.Open();Check(View.Number=="119","Reopening did not prefill 119.");
        var test=new GameObject("Temporary reception progression check");
        try
        {
            var rules=test.AddComponent<EmergencyCallSystem>();var progress=test.AddComponent<PhoneReceptionProgression>();
            progress.Advance(89f);Check(rules.SignalBars==0,"Reception recovered too early.");
            progress.Advance(1f);Check(rules.SignalBars==1,"90-second threshold did not produce one bar.");
            progress.Advance(30f);Check(rules.SignalBars==2,"120-second threshold did not produce two bars.");
        }
        finally{UnityEngine.Object.DestroyImmediate(test);}
        View.GetComponent<PhoneReceptionProgression>().PreviewSignal(0);View.ConfirmCall();View.Close();
        View.Open();
        return Snapshot("signal loss, invalid number, edit digits, cancel/back, reopen, 90s/120s progression all passed");
    }

    public static object StartTwoBars()
    {
        UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>().EmergencyCall();
        View.CallSystem.HangUp();View.GetComponent<PhoneReceptionProgression>().PreviewSignal(2);View.Open();
        View.ConfirmCall();return Snapshot("two-bar connection started");
    }
    public static object FinishTwoBars()
    {
        Check(View.CallSystem.State==EmergencyCallSystem.CallState.Connected && View.CallSystem.SignalBars==2,"Two bars did not connect.");
        var result=Snapshot("two-bar connection passed");View.Close();
        UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>().SetDesktopMode(false);
        return result;
    }

    static void Click(string name)
    {
        Canvas.ForceUpdateCanvases();Physics.SyncTransforms();
        var button=View.transform.Find("Phone Interface Canvas/Emergency Dialer/"+name).GetComponent<UnityEngine.UI.Button>();
        var rect=button.GetComponent<RectTransform>();var world=rect.TransformPoint(rect.rect.center);var camera=Camera.main;
        var data=new TrackedDeviceEventData(EventSystem.current){layerMask=~0,rayPoints=new System.Collections.Generic.List<Vector3>{camera.transform.position,world+(world-camera.transform.position).normalized*0.25f},button=PointerEventData.InputButton.Left};
        var results=new System.Collections.Generic.List<RaycastResult>();View.GetComponentInChildren<TrackedDeviceGraphicRaycaster>(true).Raycast(data,results);
        Check(results.Count>0 && results[0].gameObject==button.gameObject,"XR UI ray missed "+name+"; hit="+(results.Count>0?results[0].gameObject.name:"nothing"));
        data.pointerCurrentRaycast=results[0];data.pointerPressRaycast=results[0];data.eligibleForClick=true;
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
}
