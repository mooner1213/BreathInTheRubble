using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class VerifyRearAftershockCollapse
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static object CheckTeardownSafety()
    {
        Check(Application.isPlaying,"Play Mode required.");
        UnityEngine.Object.FindFirstObjectByType<GameSequenceController>().RestoreAftershock();
        bool originalFog=RenderSettings.fog;float originalDensity=RenderSettings.fogDensity;
        var source=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).First(p=>p.name=="Fracture Dust Plume 1");
        var root=new GameObject("Temporary dust teardown verification");root.SetActive(false);
        try
        {
            var child=new GameObject("Temporary dust to destroy first");child.transform.SetParent(root.transform,false);
            var particle=child.AddComponent<ParticleSystem>();particle.GetComponent<ParticleSystemRenderer>().sharedMaterial=source.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            var effect=root.AddComponent<AftershockDustEffect>();var data=new SerializedObject(effect);
            foreach(string name in new[]{"awakeningDust","aftershockDust","fracturePlumes"}){var array=data.FindProperty(name);array.arraySize=1;array.GetArrayElementAtIndex(0).objectReferenceValue=particle;}
            data.FindProperty("dustVeil").objectReferenceValue=particle;data.ApplyModifiedProperties();
            root.SetActive(true);effect.Begin();effect.Advance(1f);
            UnityEngine.Object.DestroyImmediate(child);
            effect.End();effect.Advance(24f);effect.Clear();effect.Clear();
            Check(RenderSettings.fog==originalFog && RenderSettings.fogDensity==originalDensity,"Teardown did not restore fog.");
            UnityEngine.Object.DestroyImmediate(root);
            return new{result="PASS",checks="destroyed particle/renderer first, End/Advance/Clear twice and component teardown without missing references"};
        }
        finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);}
    }
    public static object Run()
    {
        Check(Application.isPlaying && !DesktopPhoneTestDriver.HasPhysicalHmd,"Desktop Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var dust=UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>();
        var root=GameObject.Find("Rear Aftershock Breakaway");Check(root!=null,"Rear collapse missing.");
        var all=root.GetComponentsInChildren<Transform>(true);
        var panels=all.Where(t=>t.name.StartsWith("Rear Breakaway Ceiling Panel")).ToArray();
        var fragments=all.Where(t=>t.name.StartsWith("Rear Falling Concrete Fragment")).ToArray();
        var plumes=director.GetComponentsInChildren<ParticleSystem>(true).Where(p=>p.name.StartsWith("Rear Fracture Dust Plume")).ToArray();
        Check(panels.Length==3 && fragments.Length==28 && plumes.Length==2,"Rear effect counts are wrong.");
        Check(root.GetComponentsInChildren<Collider>(true).Length==0 && root.GetComponentsInChildren<Rigidbody>(true).Length==0,"New rear debris blocks interaction.");
        var bottle=UnityEngine.Object.FindFirstObjectByType<BottleLiquidVisual>().transform.root;
        var bottleCollider=bottle.GetComponent<CapsuleCollider>();var grab=bottle.GetComponent<XRGrabInteractable>();
        Vector3 bottlePosition=bottle.position;Quaternion bottleRotation=bottle.rotation;
        bool grabEnabled=grab.enabled;float height=bottleCollider.height,radius=bottleCollider.radius;
        sequence.RestoreAftershock();
        var positions=panels.Select(t=>t.localPosition).ToArray();var rotations=panels.Select(t=>t.localRotation).ToArray();
        var fragmentIds=fragments.Select(t=>t.GetInstanceID()).ToArray();
        bool enabled=sequence.enabled;sequence.enabled=false;
        try
        {
            foreach(var piece in fragments)
                Check(new Vector2(piece.position.x-bottle.position.x,piece.position.z-bottle.position.z).magnitude>=0.239f,"New fragment crosses bottle reach space.");
            director.Begin();director.Advance(3f);
            Check(fragments.All(t=>!t.gameObject.activeSelf),"Rear fragments drop before the authored fracture.");
            director.Advance(4.6f);dust.Advance(1f);
            int visible=fragments.Count(t=>t.gameObject.activeSelf);
            Check(visible>=24,"Rear fragment shower is too sparse.");
            Check(plumes.All(p=>p.particleCount>=30 && p.main.maxParticles==40),"Rear fracture plumes did not fire on all three impacts.");
            director.Advance(director.DurationSeconds-director.ElapsedSeconds+0.1f);
            float minimumClearance=float.MaxValue;
            for(int i=0;i<panels.Length;i++)
            {
                float drop=positions[i].y-panels[i].localPosition.y;
                Check(drop>=0.13f && drop<=0.141f,"Rear ceiling does not collapse by the expected amount.");
                Check(Quaternion.Angle(panels[i].localRotation,rotations[i])>2f,"Rear ceiling does not buckle.");
                float clearance=panels[i].GetComponent<Renderer>().bounds.min.y-bottleCollider.bounds.max.y;
                minimumClearance=Mathf.Min(minimumClearance,clearance);
                Check(clearance>0.18f,"Collapsed ceiling closes the bottle opening.");
            }
            dust.Advance(24f);
            Check(Mathf.Abs(dust.Opacity-0.5f)<0.001f,"Rear dust does not share the 20/10 second episode.");
            var block=new MaterialPropertyBlock();plumes[0].GetComponent<ParticleSystemRenderer>().GetPropertyBlock(block);
            Check(block.GetColor("_BaseColor").a>0f && block.GetColor("_BaseColor").a<0.6f,"Rear dust does not fade with the existing effect.");
            dust.Advance(5f);Check(plumes.All(p=>p.particleCount==0) && !dust.IsActive,"Rear dust remains after 30 seconds.");
            for(int cycle=0;cycle<10;cycle++){director.Begin();director.Advance(director.DurationSeconds+0.1f);director.ResetEffect();}
            Check(fragments.Select(t=>t.GetInstanceID()).SequenceEqual(fragmentIds) && fragments.All(t=>!t.gameObject.activeSelf),"Rear fragment reuse/reset failed.");
            for(int i=0;i<panels.Length;i++)Check(panels[i].localPosition==positions[i] && Quaternion.Angle(panels[i].localRotation,rotations[i])<0.001f,"Rear ceiling reset failed.");
            Check(bottle.position==bottlePosition && bottle.rotation==bottleRotation && grab.enabled==grabEnabled && bottleCollider.enabled && bottleCollider.height==height && bottleCollider.radius==radius,"Bottle pose or interaction settings changed.");
            Debug.Log("[Rear aftershock verification] PASS: 3 breakaway panels, 28 pooled rear fragments, 2 rear plumes, bottle opening preserved, 30s dust cleanup and exact reset.");
            return new{result="PASS",rearPanels=panels.Length,rearFragments=fragments.Length,visibleRearFragments=visible,rearPlumes=plumes.Length,minimumBottleRoofClearanceMeters=minimumClearance,
                checks="rear ceiling drop/buckling, bottle opening and grab settings unchanged, rear dust fades by 30s, 10 replays reuse fragments and restore panels"};
        }
        finally{director.ResetEffect();sequence.enabled=enabled;}
    }
}
