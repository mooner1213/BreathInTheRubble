using System;
using System.IO;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEngine;
using UnityEditor;

public static class VerifySettlingDust
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static object Run()
    {
        Check(Application.isPlaying && !DesktopPhoneTestDriver.HasPhysicalHmd,"Desktop Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        var awakening=UnityEngine.Object.FindFirstObjectByType<AwakeningDustEffect>();
        var initial=awakening.GetComponentsInChildren<ParticleSystem>(true);
        var shockRefs=new SerializedObject(dust).FindProperty("aftershockDust");
        var shock=Enumerable.Range(0,shockRefs.arraySize).Select(i=>(ParticleSystem)shockRefs.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        sequence.RestoreAftershock();bool originalFog=RenderSettings.fog;float originalDensity=RenderSettings.fogDensity;
        var originalMode=RenderSettings.fogMode;Color originalColor=RenderSettings.fogColor;
        bool enabled=sequence.enabled;sequence.enabled=false;
        try
        {
            foreach(var p in initial.Concat(shock))
            {
                Check(!p.main.loop && p.emission.rateOverTime.constant==0f && p.emission.rateOverDistance.constant==0f,p.name+" still emits continuously.");
                for(int i=0;i<p.emission.burstCount;i++)Check(p.emission.GetBurst(i).time==0f && p.emission.GetBurst(i).cycleCount==1,p.name+" repeats a burst.");
            }
            dust.ReplayAwakening();awakening.PlayNow();
            foreach(var p in initial)p.Simulate(20f,false,false,false);
            dust.Advance(20f);float start20=dust.HazeAmount;
            Check(dust.IsActive && dust.Opacity==1f && start20>0.5f && initial.Sum(p=>p.particleCount)>200,"Startup dust did not hold for 20 seconds.");
            Capture("Start_20s");
            foreach(var p in initial)p.Simulate(5f,false,false,false);
            dust.Advance(5f);float start25=dust.HazeAmount;
            Check(Mathf.Abs(dust.Opacity-0.5f)<0.001f && start25<start20 && start25>0f,"Startup dust does not fade between 20 and 30 seconds.");
            var block=new MaterialPropertyBlock();initial[0].GetComponent<ParticleSystemRenderer>().GetPropertyBlock(block);
            Check(block.GetColor("_BaseColor").a<0.6f && block.GetColor("_BaseColor").a>0.1f,"Particles do not fade with the haze.");
            Capture("Start_25s");dust.Advance(5f);
            Check(!dust.IsActive && dust.HazeAmount==0f && initial.Sum(p=>p.particleCount)==0,"Startup dust remains after 30 seconds.");Capture("Start_30s");
            director.Begin();director.Advance(director.DurationSeconds+0.1f);
            foreach(var p in shock)p.Simulate(20f,false,false,false);
            dust.Advance(20f);float shock20=dust.HazeAmount;
            Check(!director.IsRunning && dust.IsActive && dust.Opacity==1f && shock20>1.8f,"Aftershock End restarted/shortened the dust episode.");
            UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();Capture("Aftershock_20s");
            foreach(var p in shock)p.Simulate(5f,false,false,false);
            dust.Advance(5f);float shock25=dust.HazeAmount;
            Check(Mathf.Abs(dust.Opacity-0.5f)<0.001f && shock25<shock20 && shock25>0f,"Aftershock dust does not fade from 20 to 30 seconds.");Capture("Aftershock_25s");
            dust.Advance(5f);Check(!dust.IsActive && shock.Sum(p=>p.particleCount)==0 && dust.HazeAmount==0f,"Aftershock dust remains after 30 seconds.");Capture("Aftershock_30s");
            Check(RenderSettings.fog==originalFog && RenderSettings.fogDensity==originalDensity && RenderSettings.fogMode==originalMode && RenderSettings.fogColor==originalColor,"Fog settings were not restored.");
            director.Begin();dust.Advance(1f);director.ResetEffect();Check(!dust.IsActive && shock.Sum(p=>p.particleCount)==0,"Replay/reset leaves stale particles.");
            Debug.Log("[Settling dust verification] PASS: finite bursts, 20s hold / 10s fade, particle and fog fade together, complete clear and restore at 30s.");
            return new{result="PASS",startupDensity=new[]{start20,start25,0f},aftershockDensity=new[]{shock20,shock25,0f},checks="no loops or continuous emission, 20/25/30s timing for both episodes, particle opacity fade, reset and fog restore",captures="Temp/SettlingDustReview/"};
        }
        finally{director.ResetEffect();sequence.enabled=enabled;}
    }
    static void Capture(string label)
    {
        Directory.CreateDirectory("Temp/SettlingDustReview");var camera=Camera.main;var originalTarget=camera.targetTexture;var originalActive=RenderTexture.active;
        var target=new RenderTexture(1000,800,24);var texture=new Texture2D(1000,800,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1000,800),0,0);texture.Apply();File.WriteAllBytes("Temp/SettlingDustReview/"+label+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=originalTarget;RenderTexture.active=originalActive;UnityEngine.Object.DestroyImmediate(texture);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
