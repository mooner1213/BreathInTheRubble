using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using BreathInTheRubble.Prototype;
public static class VerifyPhoneLitAwakening
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static object Run()
    {
        Check(Application.isPlaying&&!DesktopPhoneTestDriver.HasPhysicalHmd,"Desktop Play Mode required");
        var captures=JArray.Parse(File.ReadAllText("Temp/PhoneLitAwakeningReview/Startup.json"));
        Check(captures.Count==7,"Real startup capture did not complete");
        Check(captures.All(f=>(bool)f["phoneAwake"]&&!(bool)f["held"]&&!(bool)f["flashlight"]),"Startup requires picking up the phone or turning on its torch");
        Check((double)captures[0]["maximum"]==0f,"Startup does not begin completely black");
        var slit=captures.Single(f=>Math.Abs((double)f["targetTime"]-1.5)<.01);var full=captures.Single(f=>Math.Abs((double)f["targetTime"]-4.2)<.01);
        Check((double)slit["opening"]>0f&&(double)slit["opening"]<.5f&&(double)slit["centerMean"]>.02,"The first slit still contains only invisible darkness");
        Check((double)full["opening"]==1f&&(double)full["mean"]>(double)slit["mean"],"Actual startup does not reveal a wider view");
        var phone=UnityEngine.Object.FindFirstObjectByType<PhoneWakeSequence>();var settings=new SerializedObject(phone);
        Check(settings.FindProperty("screenOnAtStart").boolValue&&settings.FindProperty("wakeDelaySeconds").floatValue==5f,"Screen/alarm start timing changed incorrectly");
        Check(Time.timeSinceLevelLoad>=6.1f,"Run after the 6-second opening glow settles");
        var spill=phone.GetComponentInChildren<PhoneScreenSpillLight>();var spillSettings=new SerializedObject(spill);var spillLight=(Light)spillSettings.FindProperty("spillLight").objectReferenceValue;
        Check(Mathf.Abs(spillSettings.FindProperty("maximumIntensity").floatValue-.08f)<.0001f&&Mathf.Abs(spillLight.spotAngle-120f)<.001f&&spillLight.intensity<=.0801f,"The opening glow remains boosted during normal play");
        int wakes=0;Action onWake=()=>wakes++;phone.Awakened+=onWake;phone.WakeNow();phone.WakeNow();phone.Awakened-=onWake;Check(wakes==0,"Already-awake screen restarts its wake event");
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var data=new SerializedObject(director);var fine=(ParticleSystem)data.FindProperty("extraDust").objectReferenceValue;
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var atmosphere=new SerializedObject(dust);var veil=(ParticleSystem)atmosphere.FindProperty("dustVeil").objectReferenceValue;
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();bool enabled=sequence.enabled;sequence.enabled=false;sequence.RestoreAftershock();
        try
        {
            director.Begin();Check(fine.particleCount==225&&veil.particleCount==28,"Aftershock opening dust was not increased");
            director.Advance(3.3f);Check(fine.particleCount==375&&veil.particleCount==50,"Impact does not emit its denser dust burst");
            director.Advance(4f);Check(fine.particleCount==675&&veil.particleCount==90,"Primary waves or bounded veil capacity mismatch");
            Check(fine.main.maxParticles==760&&veil.main.maxParticles==90&&!fine.main.loop&&!veil.main.loop,"Dense dust is not finite and bounded");
            director.Advance(11f);Check(!director.IsRunning&&director.DurationSeconds==18f,"Dust increase changed the aftershock duration");
            dust.Advance(30f);Check(!dust.IsActive&&fine.particleCount==0&&veil.particleCount==0,"Increased dust does not clear");
            return new{result="PASS",startupFrames=captures.Count,screenAwakeFrom=(double)captures[0]["gameTime"],slitCenterBrightness=(double)slit["centerMean"],fullCenterBrightness=(double)full["centerMean"],settledScreenSpill=spillLight.intensity,shockFineBurst=225,shockImpactBurst=150,shockVeilCapacity=90,checks="actual untouched startup, lit central slit, phone stays at rest / torch off, alarm delay preserved, glow settles by 6s, idempotent wake, 25% more bounded shock dust, 18s shock and 30s cleanup"};
        }
        finally{sequence.RestoreAftershock();sequence.enabled=enabled;}
    }
}
