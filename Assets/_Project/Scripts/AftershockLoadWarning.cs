using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Local load strain before an authored fracture. Never moves tracking or runs a second clock.</summary>
    [DisallowMultipleComponent]
    public sealed class AftershockLoadWarning : MonoBehaviour
    {
        [SerializeField] AftershockDirector aftershock;
        [SerializeField] Transform loadedPiece;
        [SerializeField] AftershockFallingChunk fallingChunk;
        [SerializeField] ParticleSystem jointDust;
        [SerializeField] AudioSource strainSource;
        [SerializeField] AudioClip strainClip;
        [SerializeField, Min(0f)] float startsAtSeconds=1.2f;
        [SerializeField, Min(.1f)] float warningSeconds=1.8f;
        [SerializeField] Vector3 slipOffset=new Vector3(0,-.035f,-.025f);
        [SerializeField] Vector3 tiltDegrees=new Vector3(0,0,5);
        Vector3 restPosition;
        Quaternion restRotation;
        bool captured, sounded;
        int dustStep;
        public float StrainAmount { get; private set; }
        void Awake(){Capture();}
        void Capture()
        {
            if(captured||loadedPiece==null)return;
            restPosition=loadedPiece.localPosition;restRotation=loadedPiece.localRotation;captured=true;
        }
        void OnEnable()
        {
            Capture();
            if(aftershock!=null){aftershock.Advanced+=Apply;aftershock.EffectReset+=ResetEffect;}
        }
        void OnDisable()
        {
            if(aftershock!=null){aftershock.Advanced-=Apply;aftershock.EffectReset-=ResetEffect;}
            ResetEffect();
        }
        public void Apply(float elapsed)
        {
            Capture();if(!captured||elapsed<startsAtSeconds)return;
            StrainAmount=Mathf.Clamp01((elapsed-startsAtSeconds)/warningSeconds);
            if(!sounded){sounded=true;if(strainSource!=null&&strainClip!=null)strainSource.PlayOneShot(strainClip,.7f);}
            // Once detached the falling body is owned exclusively by PhysX.
            if(fallingChunk!=null&&fallingChunk.Released)return;
            float strain=StrainAmount*StrainAmount;
            float tremor=Mathf.Sin(elapsed*47f)*Mathf.Sin(StrainAmount*Mathf.PI)*.0035f;
            loadedPiece.localPosition=restPosition+slipOffset*strain+Vector3.up*tremor;
            loadedPiece.localRotation=restRotation*Quaternion.Euler(tiltDegrees*strain);
            int step=Mathf.FloorToInt(StrainAmount*6f);
            if(step>dustStep){dustStep=step;if(jointDust!=null)jointDust.Emit(3);}
        }
        public void ResetEffect()
        {
            if(captured&&loadedPiece!=null){loadedPiece.localPosition=restPosition;loadedPiece.localRotation=restRotation;}
            if(strainSource!=null)strainSource.Stop();
            if(jointDust!=null)jointDust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            StrainAmount=0;sounded=false;dustStep=0;
        }
    }
}
