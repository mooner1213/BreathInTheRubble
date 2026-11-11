using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
using BreathInTheRubble.EditorTools;
public static class ConfigurePhoneLitAwakening
{
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var scene=SceneManager.GetActiveScene();var backup="Assets/_Project/Scenes/Backups/BeforePhoneLitAwakening_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
        var phone=UnityEngine.Object.FindFirstObjectByType<PhoneWakeSequence>();var wake=new SerializedObject(phone);wake.FindProperty("screenOnAtStart").boolValue=true;wake.ApplyModifiedProperties();
        var spill=phone.GetComponentInChildren<PhoneScreenSpillLight>();var light=new SerializedObject(spill);float oldIntensity=light.FindProperty("maximumIntensity").floatValue;light.FindProperty("maximumIntensity").floatValue=.08f;
        light.FindProperty("awakeningIntensityMultiplier").floatValue=5f;light.FindProperty("awakeningHoldSeconds").floatValue=4f;light.FindProperty("awakeningFadeSeconds").floatValue=2f;
        light.FindProperty("awakeningOuterAngle").floatValue=150f;light.FindProperty("awakeningInnerAngle").floatValue=108f;light.ApplyModifiedProperties();
        var spillLight=(Light)new SerializedObject(phone).FindProperty("screenSpillLight").objectReferenceValue;
        var spot=new SerializedObject(spillLight);spot.FindProperty("m_SpotAngle").floatValue=120f;spot.FindProperty("m_InnerSpotAngle").floatValue=82f;spot.ApplyModifiedProperties();
        var reveal=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<AwakeningReveal>());reveal.FindProperty("openingSeconds").floatValue=3.4f;reveal.ApplyModifiedProperties();
        AssetDatabase.ImportAsset("Assets/_Project/Art/AwakeningEyelids.shader",ImportAssetOptions.ForceUpdate);
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var data=new SerializedObject(director);data.FindProperty("openingDustCount").intValue=225;data.FindProperty("impactDustCount").intValue=150;data.FindProperty("lateImpactDustCount").intValue=45;data.ApplyModifiedProperties();
        var fine=(ParticleSystem)data.FindProperty("extraDust").objectReferenceValue;var main=fine.main;main.maxParticles=760;
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var atmosphere=new SerializedObject(dust);atmosphere.FindProperty("openingVeilCount").intValue=28;atmosphere.FindProperty("fractureVeilCount").intValue=22;atmosphere.ApplyModifiedProperties();
        var veil=(ParticleSystem)atmosphere.FindProperty("dustVeil").objectReferenceValue;var veilMain=veil.main;veilMain.maxParticles=90;
        var startingVeil=GameObject.Find("Dense Dust Veil").GetComponent<ParticleSystem>();startingVeil.useAutoRandomSeed=false;startingVeil.randomSeed=33826;EditorUtility.SetDirty(startingVeil);
        // Keep the existing bounded spawn volume and 30-second settling episode.
        EditorUtility.SetDirty(fine);EditorUtility.SetDirty(veil);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        AwakeningStartupCapture.Arm();
        return new{backup,screenOnAtStart=true,alarmDelay=wake.FindProperty("wakeDelaySeconds").floatValue,screenFade=wake.FindProperty("screenWakeFadeSeconds").floatValue,previousSpill=oldIntensity,restingScreenSpill=.08f,awakeningScreenSpill=.4f,glowHoldSeconds=4,glowFadeSeconds=2,openingDuration=4f,shockFineBursts=new[]{225,150,45},shockVeilBursts=new[]{28,22},fineCapacity=760,veilCapacity=90,startupCaptureArmed=true};
    }
}
