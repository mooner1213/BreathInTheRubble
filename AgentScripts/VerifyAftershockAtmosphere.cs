using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEngine;

public static class VerifyAftershockAtmosphere
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static Transform[] Refs(UnityEngine.Object target,string field)
    {
        var array=new SerializedObject(target).FindProperty(field);
        return Enumerable.Range(0,array.arraySize).Select(i=>(Transform)array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    }
    public static object Run()
    {
        Check(Application.isPlaying && !DesktopPhoneTestDriver.HasPhysicalHmd,"Desktop Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        var awakening=UnityEngine.Object.FindFirstObjectByType<AwakeningDustEffect>();
        Check(dust!=null,"New dust atmosphere is missing.");
        var head=Camera.main.transform;Vector3 headPosition=head.position;Quaternion headRotation=head.rotation;
        var hands=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Left Controller" || t.name=="Right Controller").ToArray();
        var handPositions=hands.Select(t=>t.position).ToArray();var handRotations=hands.Select(t=>t.rotation).ToArray();
        sequence.RestoreAftershock();
        bool baselineFog=RenderSettings.fog;float baselineDensity=RenderSettings.fogDensity;Color baselineColor=RenderSettings.fogColor;var baselineMode=RenderSettings.fogMode;
        bool wasEnabled=sequence.enabled;sequence.enabled=false;
        try
        {
            var startup=awakening.GetComponentsInChildren<ParticleSystem>(true);
            awakening.PlayNow();dust.ReplayAwakening();dust.Advance(0.65f);
            foreach(var system in startup)system.Simulate(0.65f,false,true,false);
            int initialCount=startup.Sum(p=>p.particleCount);
            Check(initialCount>=250 && dust.HazeAmount>0.2f,"Startup dust is too sparse.");
            Check(startup.All(p=>p.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name=="BreathInTheRubble/Light Scattering Dust"),"Startup dust no longer uses the light-reactive scattering material.");
            foreach(var system in startup)system.Simulate(60f,false,true,false);
            Check(startup.All(p=>!p.main.loop) && startup.Sum(p=>p.particleCount)==0,"Finite startup dust remains after one minute.");
            dust.Advance(30f);Check(dust.HazeAmount==0f && RenderSettings.fog==baselineFog,"Startup fog does not restore.");

            var ceilings=Refs(motion,"ceilingTargets");var walls=Refs(motion,"shakeTargets");
            var positions=ceilings.Select(t=>t.localPosition).ToArray();var rotations=ceilings.Select(t=>t.localRotation).ToArray();
            var wallPositions=walls.Select(t=>t.localPosition).ToArray();
            director.Begin();
            motion.Apply(2.9f,2.9f/director.DurationSeconds,1f);
            Check(motion.AppliedMaximumDrop==0f,"Ceiling collapses before fracture.");
            Check(walls.Select((t,i)=>Vector3.Distance(t.localPosition,wallPositions[i])).Max()>0.006f,"Environment shaking remains too weak.");
            motion.Apply(3.5f,3.5f/director.DurationSeconds,1f);float firstDrop=motion.AppliedMaximumDrop;
            Check(firstDrop>0.025f,"First fracture has no abrupt visible drop.");
            motion.Apply(4.4f,4.4f/director.DurationSeconds,1f);float plateau=motion.AppliedMaximumDrop;
            motion.Apply(4.6f,4.6f/director.DurationSeconds,1f);
            Check(Mathf.Abs(motion.AppliedMaximumDrop-plateau)<0.00001f,"Ceiling slides between fractures.");
            motion.Apply(5.2f,5.2f/director.DurationSeconds,1f);
            Check(motion.AppliedMaximumDrop>firstDrop+0.025f,"Second fracture does not release the ceiling.");
            motion.Apply(director.DurationSeconds,1f,0f);
            Check(motion.AppliedMaximumDrop<=motion.MaximumConfiguredDrop+0.00001f && motion.AppliedMaximumDrop>0.10f,"Final collapse range is wrong.");
            Check(ceilings.Where((t,i)=>Quaternion.Angle(t.localRotation,rotations[i])>1f).Any(),"Slabs do not buckle.");
            director.ResetEffect();
            for(int i=0;i<ceilings.Length;i++)Check(ceilings[i].localPosition==positions[i] && Quaternion.Angle(ceilings[i].localRotation,rotations[i])<0.001f,"Ceiling restore failed.");

            var fragments=Refs(debris,"fragments");Check(fragments.Length>=64,"Existing pooled fragments are missing.");
            director.Begin();director.Advance(7.4f);dust.Advance(0.8f);
            int visible=fragments.Count(t=>t.gameObject.activeSelf);
            Check(visible>=40,"Too few falling fragments survive safety checks.");
            Check(fragments.All(t=>t.GetComponent<Collider>()==null && t.GetComponent<Rigidbody>()==null),"Cosmetic fragments add physics cost or block grabbing.");
            var shockRefs=new SerializedObject(dust).FindProperty("aftershockDust");
            var shockSystems=Enumerable.Range(0,shockRefs.arraySize).Select(i=>(ParticleSystem)shockRefs.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            Check(shockSystems.Sum(p=>p.particleCount)>=600,"Aftershock dust is insufficient.");
            Check(dust.HazeAmount>=1.8f && RenderSettings.fogDensity>=1.8f,"Aftershock does not produce dense haze.");
            director.Advance(director.DurationSeconds-director.ElapsedSeconds+0.1f);dust.Advance(4f);
            float lingering=dust.HazeAmount;
            Check(!director.IsRunning && Mathf.Abs(lingering-1.9f)<0.0001f,"Dust does not hold during the first 20 seconds.");
            dust.Advance(40f);
            Check(dust.HazeAmount==0f && RenderSettings.fog==baselineFog && RenderSettings.fogMode==baselineMode && RenderSettings.fogDensity==baselineDensity && RenderSettings.fogColor==baselineColor,"Fog settings leak after settling.");

            int particleSystems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
            var ids=fragments.Select(t=>t.GetInstanceID()).ToArray();
            for(int cycle=0;cycle<10;cycle++){director.Begin();director.Advance(7.5f);director.ResetEffect();}
            Check(UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==particleSystems,"Repeated aftershocks create emitters.");
            Check(Refs(debris,"fragments").Select(t=>t.GetInstanceID()).SequenceEqual(ids),"Repeated aftershocks create fragments.");
            Check(fragments.All(t=>!t.gameObject.activeSelf),"Reset leaves debris visible.");
            Check(head.position==headPosition && head.rotation==headRotation,"Effect moved the HMD camera.");
            for(int i=0;i<hands.Length;i++)Check(hands[i].position==handPositions[i] && hands[i].rotation==handRotations[i],"Effect moved a tracked controller.");
            Debug.Log("[Aftershock atmosphere verification] PASS: startup/ambient dust, dense lingering haze, staged fracture, 64 pooled fragments, exact restore, unchanged tracked poses.");
            return new{result="PASS",initialParticles=initialCount,visibleFragments=visible,pooledFragments=fragments.Length,shockParticleCapacity=shockSystems.Sum(p=>p.main.maxParticles),lingeringDensityAfter4s=lingering,
                checks="finite light-reactive startup dust, fracture plateaus and buckling, stronger environment shake, 20s haze hold, fog restore, 10 replays reuse emitters/fragments, HMD/controller poses unchanged within effects"};
        }
        finally{director.ResetEffect();sequence.enabled=wasEnabled;}
    }
}
