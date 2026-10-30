using System;
using System.Collections.Generic;
using BreathInTheRubble.Prototype;
using UnityEngine;

public static class VerifyReceptionFluctuation
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static object Run()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required.");
        var summaries=new List<string>();
        for(int run=0;run<12;run++)
        {
            var go=new GameObject("Temporary reception check");
            try
            {
                var calls=go.AddComponent<EmergencyCallSystem>();var progress=go.AddComponent<PhoneReceptionProgression>();
                progress.Advance(89.5f);Check(calls.SignalBars==0,"Signal before 90s.");
                progress.Advance(0.5f);Check(calls.SignalBars==1,"First one-bar opening missing.");
                // First brief window should actually interrupt a pending four-second call.
                calls.Dial("119");
                int previous=1,changes=0;float oneRun=0f,longestOneRun=0f;bool droppedAttempt=false;
                for(int step=0;step<100;step++)
                {
                    progress.Advance(0.25f);
                    Check(calls.SignalBars==0||calls.SignalBars==1,"Two bars appeared before 120s.");
                    if(calls.SignalBars!=previous){changes++;previous=calls.SignalBars;}
                    oneRun=calls.SignalBars==1?oneRun+0.25f:0f;longestOneRun=Mathf.Max(longestOneRun,oneRun);
                    if(calls.State==EmergencyCallSystem.CallState.NoSignal)droppedAttempt=true;
                }
                Check(changes>=4,"Weak interval did not alternate enough.");
                Check(longestOneRun>=5f,"No usable window for the four-second call.");
                Check(droppedAttempt,"A natural signal loss did not cancel the attempt.");
                Check(calls.State!=EmergencyCallSystem.CallState.Connecting,"Recovery automatically retried a failed call.");
                Check(calls.SignalBars==1,"Signal must settle at one bar by 115s.");
                for(int step=0;step<19;step++){progress.Advance(0.25f);Check(calls.SignalBars==1,"Final one-bar bridge dropped.");}
                progress.Advance(0.25f);Check(calls.SignalBars==2,"Expected 1 -> 2 at 120s.");
                progress.Advance(60f);Check(calls.SignalBars==2,"Two bars did not remain stable.");
                progress.PreviewSignal(0);progress.Advance(20f);Check(calls.SignalBars==0,"Manual preview was overridden.");
                progress.ResumeAutomaticRecovery();Check(calls.SignalBars==2,"Automatic recovery did not resume.");
                summaries.Add($"run{run+1}:changes={changes},longest1bar={longestOneRun:F2}s");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        return new{result="PASS",runs=summaries.ToArray(),checks="0 before90; 0/1 fluctuation; natural call interruption; usable >4s windows; stable1 at115; 1->2 at120; stable2; preview override"};
    }
}
