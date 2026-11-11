using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TuneSettlingDust
{
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("ConfinedPrototype required.");
        var effect=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        var awakening=UnityEngine.Object.FindFirstObjectByType<AwakeningDustEffect>();
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var initial=awakening.GetComponentsInChildren<ParticleSystem>(true);
        var shock=director.GetComponentsInChildren<ParticleSystem>(true);
        foreach(var system in initial.Concat(shock))
        {
            system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            bool veil=system.name.Contains("Veil"),plume=system.name.Contains("Plume");
            bool motes=system.name=="Suspended Dust Motes";
            var main=system.main;main.loop=false;main.duration=0.2f;main.prewarm=false;main.playOnAwake=false;
            main.startLifetime=30f;main.startSpeed=new ParticleSystem.MinMaxCurve(0.001f,plume?0.045f:0.008f);
            main.gravityModifier=0.00008f;
            main.startSize=new ParticleSystem.MinMaxCurve(veil?0.45f:plume?0.22f:motes?0.0015f:0.003f,veil?0.90f:plume?0.56f:motes?0.004f:0.014f);
            float alpha=veil?0.21f:plume?0.30f:motes?0.18f:0.25f;
            main.startColor=new ParticleSystem.MinMaxGradient(new Color(0.57f,0.57f,0.54f,alpha*0.55f),new Color(0.72f,0.71f,0.67f,alpha));
            var emission=system.emission;emission.rateOverTime=0f;emission.rateOverDistance=0f;
            int burst=system.name=="Initial Dust Cloud"?220:motes?90:system.name=="Dense Dust Veil"?24:0;
            emission.SetBursts(burst>0?new[]{new ParticleSystem.Burst(0f,(short)burst)}:Array.Empty<ParticleSystem.Burst>());
            var noise=system.noise;noise.enabled=true;noise.strength=0.008f;noise.frequency=0.18f;noise.scrollSpeed=0.015f;noise.octaveCount=1;
            var velocity=system.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
            velocity.x=new ParticleSystem.MinMaxCurve(0.0015f);velocity.y=new ParticleSystem.MinMaxCurve(-0.007f);velocity.z=new ParticleSystem.MinMaxCurve(0.002f);
            var size=system.sizeOverLifetime;size.enabled=veil||plume;
            if(size.enabled)size.size=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0f,0.95f,1f,1.15f));
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(0.95f,0.95f,0.92f),0f),new GradientColorKey(new Color(0.86f,0.86f,0.82f),1f)},
                new[]{new GradientAlphaKey(plume?0.1f:0.6f,0f),new GradientAlphaKey(0.8f,0.04f),new GradientAlphaKey(0.8f,0.67f),new GradientAlphaKey(0f,1f)});
            var color=system.colorOverLifetime;color.enabled=true;color.color=gradient;
            EditorUtility.SetDirty(system);
        }
        var data=new SerializedObject(effect);
        data.FindProperty("startingDensity").floatValue=0.65f;
        data.FindProperty("holdSeconds").floatValue=20f;data.FindProperty("fadeSeconds").floatValue=10f;
        void Set(string field,ParticleSystem[] systems)
        {
            var array=data.FindProperty(field);array.arraySize=systems.Length;
            for(int i=0;i<systems.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=systems[i];
        }
        Set("awakeningDust",initial);Set("aftershockDust",shock);data.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{result="Finite settling dust: hold 20s, fade 10s, no continuous emission",startup=initial.Length,aftershock=shock.Length};
    }
}
