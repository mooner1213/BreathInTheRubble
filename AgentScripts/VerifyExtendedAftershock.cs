using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class VerifyExtendedAftershock
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static object Run()
    {
        Check(Application.isPlaying && !DesktopPhoneTestDriver.HasPhysicalHmd,"Desktop Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();var motion=UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var guard=UnityEngine.Object.FindFirstObjectByType<HeadProtectionDetector>();
        var root=GameObject.Find("Embedded Rear Wall Fractures");Check(root!=null,"Embedded wall fractures not connected.");
        var sections=root.GetComponentsInChildren<AftershockFractureSection>(true).OrderBy(s=>s.OpensAtSeconds).ToArray();
        var fragments=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Later Rear Falling Fragment")).ToArray();
        Check(director.DurationSeconds==18f && director.ImpactCount==6 && sections.Length==3 && fragments.Length==24,"Extended timeline configuration mismatch.");
        var chunks=root.GetComponentsInChildren<AftershockFallingChunk>(true);
        Check(chunks.Length==6 && chunks.All(c=>c.GetComponent<MeshCollider>().convex && c.GetComponent<Rigidbody>().isKinematic),"Wall skins are not attached convex physics chunks.");
        var bottle=UnityEngine.Object.FindFirstObjectByType<BottleLiquidVisual>().transform.root;var grab=bottle.GetComponent<XRGrabInteractable>();
        Vector3 bottlePosition=bottle.position;Quaternion bottleRotation=bottle.rotation;bool grabEnabled=grab.enabled;
        sequence.RestoreAftershock();bool enabled=sequence.enabled;sequence.enabled=false;
        var source=GameObject.Find("Aftershock Structural Audio").GetComponent<AudioSource>();
        void Advance(float seconds){sequence.Advance(seconds);dust.Advance(seconds);}
        try
        {
            guard.SetEditorPreview(true,true);guard.AdvanceProtection(0.2f);
            sequence.BeginAftershockNow();int clip=source.clip.GetInstanceID();
            Check(source.clip.length>=17.99f && source.isPlaying,"Rumble does not cover the longer aftershock.");
            Advance(8.1f);
            Check(sequence.Stage==GameStage.Aftershock && director.IsRunning,"Aftershock still ends at 8 seconds.");
            Check(director.GuardedImpacts==3 && sections.All(s=>s.Progress==0f),"Original impacts or late reveal timing changed.");
            Check(motion.AppliedMaximumDrop>0.10f,"Initial ceiling no longer collapses fully during the original phase.");
            Advance(1.8f);Check(fragments.All(t=>!t.gameObject.activeSelf) && sections.All(s=>!s.RebarVisible),"Late damage appears early.");
            Advance(1.1f);Check(sections[0].Progress==1f && sections[0].RebarVisible && sections[1].Progress==0f,"First new crack does not reveal steel.");
            int firstWave=fragments.Count(t=>t.gameObject.activeSelf);Check(firstWave>=7,"First late fragment wave is missing.");
            Advance(3.2f);Check(sections[1].Progress==1f && sections[1].RebarVisible && sections[2].Progress==0f,"Second new crack is not sequenced.");
            Advance(2.9f);Check(sections.All(s=>s.Progress==1f && s.RebarVisible),"Final rear crack does not reveal its steel.");
            int visible=fragments.Count(t=>t.gameObject.activeSelf);Check(visible>=22,"Later rear fragment showers are sparse.");
            Advance(0.85f);Check(director.IsRunning,"Aftershock ends before the authored tail.");
            Advance(0.10f);Check(!director.IsRunning && sequence.Stage==GameStage.Isolation,"Aftershock does not finish at 18 seconds.");
            Check(director.GuardedImpacts==6 && director.UnprotectedImpacts==0,"Extended protection impacts are not resolved.");
            Check(!source.isPlaying && motion.DesktopShakeOffset==Vector3.zero && motion.DesktopShakeEuler==Vector3.zero,"Rumble/view vibration remains after completion.");
            Check(chunks.All(c=>Physics.GetIgnoreCollision(c.GetComponent<Collider>(),bottle.GetComponent<Collider>())),"Masonry can push the bottle.");
            dust.Advance(6.95f);Check(Mathf.Abs(dust.Opacity-0.5f)<0.001f,"Longer aftershock changes the 25s dust fade.");
            dust.Advance(5f);Check(!dust.IsActive,"Later fracture dust is not cleared by 30 seconds.");
            var ids=fragments.Select(t=>t.GetInstanceID()).ToArray();int systems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
            for(int i=0;i<10;i++){sequence.BeginAftershockNow();Advance(18.1f);sequence.RestoreAftershock();}
            Check(fragments.Select(t=>t.GetInstanceID()).SequenceEqual(ids) && fragments.All(t=>!t.gameObject.activeSelf),"Later fragment pool is not reused/reset.");
            Check(sections.All(s=>s.Progress==0f && !s.RebarVisible) && chunks.All(c=>!c.Released && c.GetComponent<Rigidbody>().isKinematic),"Wall skins do not reattach on reset.");
            Check(source.clip.GetInstanceID()==clip && UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==systems,"Replays allocate replacement audio or emitters.");
            Check(bottle.position==bottlePosition && bottle.rotation==bottleRotation && grab.enabled==grabEnabled,"Bottle pose or grab state changed.");
            Debug.Log("[Extended aftershock verification] PASS: 18s duration, 6 guarded impacts, staged cracks/rebar, late fragment waves, 30s dust cleanup and replay reuse.");
            return new{result="PASS",duration=director.DurationSeconds,impacts=director.ImpactCount,fractureSections=sections.Length,visibleLaterFragments=visible,firstLaterWave=firstWave,
                checks="8s no longer ends event; 10.2/13.4/16.2s fracture/rebar sequence; original collapse preserved; bottle reach space; audio duration; 30s dust fade; 10 replays reuse/reset"};
        }
        finally{sequence.RestoreAftershock();guard.SetEditorPreview(false,false);sequence.enabled=enabled;}
    }
}
