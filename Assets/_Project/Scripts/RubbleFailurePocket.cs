using System;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>One deep joint: successive bearing layers settle, fracture, and leave permanent rubble.</summary>
    public sealed class RubbleFailurePocket : MonoBehaviour
    {
        [Serializable] public sealed class Bearing
        {
            public Transform root;
            public AftershockFallingChunk[] pieces=Array.Empty<AftershockFallingChunk>();
            public LineRenderer[] cracks=Array.Empty<LineRenderer>();
            public float releaseAt;
            public float settlesAt;
            public float settleSeconds=1.1f;
            public Vector3 settleOffset;
            [NonSerialized] public Vector3 restPosition;
            [NonSerialized] public Quaternion restRotation;
            [NonSerialized] public bool sounded;
        }
        [SerializeField] AftershockDirector aftershock;
        [SerializeField] Bearing[] bearings=Array.Empty<Bearing>();
        [SerializeField] AftershockFallingChunk[] chips=Array.Empty<AftershockFallingChunk>();
        [SerializeField] float[] chipReleaseTimes=Array.Empty<float>();
        [SerializeField] AudioSource strainSource;
        [SerializeField] AudioClip strainClip;
        bool captured;
        public Bearing[] Bearings=>bearings;
        public AftershockFallingChunk[] Chips=>chips;
        void Awake(){Capture();}
        void Capture()
        {
            if(captured)return;
            foreach(var b in bearings)if(b.root!=null){b.restPosition=b.root.localPosition;b.restRotation=b.root.localRotation;}
            captured=true;
        }
        void OnEnable(){Capture();if(aftershock!=null){aftershock.Advanced+=Apply;aftershock.EffectReset+=ResetEffect;}}
        void OnDisable(){if(aftershock!=null){aftershock.Advanced-=Apply;aftershock.EffectReset-=ResetEffect;}ResetEffect();}
        public void Apply(float elapsed)
        {
            Capture();
            foreach(var b in bearings)
            {
                if(b.root==null)continue;
                float settle=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-b.settlesAt)/Mathf.Max(.1f,b.settleSeconds)));
                float strain=b.releaseAt>0?Mathf.Clamp01((elapsed-b.releaseAt+.85f)/.85f):0;
                b.root.localPosition=b.restPosition+b.settleOffset*settle;
                b.root.localRotation=b.restRotation*Quaternion.Euler(Mathf.Sin(elapsed*37f)*strain*.18f,0,strain*1.8f);
                if(!b.sounded&&strain>0){b.sounded=true;if(strainSource!=null&&strainClip!=null)strainSource.PlayOneShot(strainClip,.45f);}
                for(int i=0;i<b.pieces.Length;i++)if(b.pieces[i]!=null&&b.releaseAt>0)b.pieces[i].Apply(elapsed-b.releaseAt-i*.073f);
                foreach(var line in b.cracks)if(line!=null){line.enabled=strain>0&&(b.releaseAt<=0||elapsed<b.releaseAt+.07f);line.widthMultiplier=Mathf.Lerp(.0006f,.006f,strain);}
            }
            for(int i=0;i<chips.Length;i++)if(chips[i]!=null&&i<chipReleaseTimes.Length)chips[i].Apply(elapsed-chipReleaseTimes[i]);
        }
        public void ResetEffect()
        {
            if(!captured)return;
            foreach(var b in bearings)
            {
                if(b.root!=null){b.root.localPosition=b.restPosition;b.root.localRotation=b.restRotation;}
                foreach(var p in b.pieces)if(p!=null)p.ResetEffect();
                foreach(var line in b.cracks)if(line!=null)line.enabled=false;
                b.sounded=false;
            }
            foreach(var chip in chips)if(chip!=null)chip.ResetEffect();
            if(strainSource!=null)strainSource.Stop();
        }
    }
}
