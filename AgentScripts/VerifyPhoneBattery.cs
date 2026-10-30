using System;
using BreathInTheRubble.Prototype;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class VerifyPhoneBattery
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static PhoneBattery Make(string name,System.Collections.Generic.List<GameObject> created)
    {var go=new GameObject(name);created.Add(go);return go.AddComponent<PhoneBattery>();}
    static void Near(float actual,float expected,string message){Check(Mathf.Abs(actual-expected)<0.0002f,message+": "+actual);}
    public static object Run()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required.");
        var created=new System.Collections.Generic.List<GameObject>();
        try
        {
            var idle=Make("Battery idle check",created);idle.Drain(600f,false,EmergencyCallSystem.CallState.Idle);Near(idle.RemainingPercent,47f,"Idle drain");
            idle.Drain(60f,false,EmergencyCallSystem.CallState.NoSignal);Near(idle.RemainingPercent,47f,"Failed-call screen keeps draining");
            var light=Make("Battery light check",created);light.Drain(60f,true,EmergencyCallSystem.CallState.Idle);
            Near(light.RemainingPercent,46.64f,"One-minute flashlight use");Check(light.DisplayedPercent==47,"Display drops immediately despite fractional use");
            light.Drain(120f,true,EmergencyCallSystem.CallState.Idle);Near(light.RemainingPercent,45.92f,"Three-minute flashlight use");Check(light.DisplayedPercent==46,"Integer display did not update");
            var dialing=Make("Battery dialing check",created);dialing.Drain(2.4f,false,EmergencyCallSystem.CallState.Connecting);Near(dialing.RemainingPercent,46.976f,"Failed attempt use");
            var combined=Make("Battery combined check",created);combined.Drain(180f,true,EmergencyCallSystem.CallState.Connected);Near(combined.RemainingPercent,45.2f,"Combined usage not additive");
            float paused=combined.RemainingPercent;combined.Drain(0f,true,EmergencyCallSystem.CallState.Connected);combined.Drain(-1f,true,EmergencyCallSystem.CallState.Connected);Near(combined.RemainingPercent,paused,"Zero/negative time drain");
            var fine=Make("Battery small time steps",created);for(int i=0;i<60000;i++)fine.Drain(0.001f,true,EmergencyCallSystem.CallState.Idle);
            Near(fine.RemainingPercent,46.64f,"Small-step rounding differs from one large time step");

            var viewObject=new GameObject("Battery view check");created.Add(viewObject);var view=viewObject.AddComponent<PhoneBatteryView>();
            var fillObject=new GameObject("Battery test fill",typeof(RectTransform));created.Add(fillObject);
            var textObject=new GameObject("Battery test text",typeof(RectTransform));created.Add(textObject);var label=textObject.AddComponent<TextMeshProUGUI>();
            label.font=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>().GetComponentInChildren<TMP_Text>(true).font;
            var data=new SerializedObject(view);data.FindProperty("battery").objectReferenceValue=light;data.FindProperty("percentText").objectReferenceValue=label;data.FindProperty("fillRect").objectReferenceValue=fillObject.transform;data.ApplyModifiedProperties();
            view.RefreshNow();Check(label.text=="46%","Battery text does not match stored charge");Near(((RectTransform)fillObject.transform).rect.width,41.328f,"Icon width does not match fractional charge");
            combined.Drain(100000f,true,EmergencyCallSystem.CallState.Connected);Check(combined.RemainingPercent==0f && combined.DisplayedPercent==0,"Battery not clamped at zero");
            return new{result="PASS",idle=47f,flashlightAfter60s=fine.RemainingPercent,flashlightAfter180s=light.RemainingPercent,displayAfter180s=label.text,
                failedAttemptCost=0.024f,lightAndCallAfter180s=45.2f,checks="fractional accumulation, subtle integer display, additive use, failed-call stop, zero/negative time, 60000 small steps, fractional fill, zero clamp"};
        }
        finally{foreach(var go in created)if(go!=null)UnityEngine.Object.DestroyImmediate(go);}
    }
    public static object InspectLive()
    {
        var phone=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>();var battery=phone.GetComponent<PhoneBattery>();
        return new{time=Time.time,remaining=battery.RemainingPercent,display=battery.DisplayedPercent,light=phone.FlashlightEnabled,call=phone.GetComponent<EmergencyCallSystem>().State.ToString(),
            text=phone.transform.Find("Phone Interface Canvas/Battery 47 Percent/Battery Text").GetComponent<TMP_Text>().text,
            fill=phone.transform.Find("Phone Interface Canvas/Battery 47 Percent/Battery Body/Battery Fill").GetComponent<RectTransform>().rect.width};
    }
}
