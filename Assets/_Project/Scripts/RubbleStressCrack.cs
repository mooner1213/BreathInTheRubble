using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Surface-bound, non-emissive hairline fissures that widen before a joint releases.</summary>
    public sealed class RubbleStressCrack : MonoBehaviour
    {
        [SerializeField] AftershockDirector aftershock;
        [SerializeField] LineRenderer[] lines = System.Array.Empty<LineRenderer>();
        [SerializeField, Min(0)] float startsAtSeconds = 2.6f;
        [SerializeField, Min(.1f)] float openingSeconds = .8f;
        [SerializeField, Min(.0001f)] float maximumWidth = .004f;
        public float Progress { get; private set; }
        void OnEnable()
        {
            if(aftershock!=null){aftershock.Advanced+=Apply;aftershock.EffectReset+=ResetEffect;}
            ResetEffect();
        }
        void OnDisable()
        {
            if(aftershock!=null){aftershock.Advanced-=Apply;aftershock.EffectReset-=ResetEffect;}
            ResetEffect();
        }
        public void Apply(float elapsed)
        {
            Progress=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-startsAtSeconds)/openingSeconds));
            foreach(var line in lines)if(line!=null){line.enabled=Progress>0;line.widthMultiplier=Mathf.Lerp(.0003f,maximumWidth,Progress);}
        }
        public void ResetEffect(){Progress=0;foreach(var line in lines)if(line!=null){line.enabled=false;line.widthMultiplier=0;}}
    }
}
