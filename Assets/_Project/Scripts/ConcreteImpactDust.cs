using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Reuses its fracture plume for local concrete contact powder.</summary>
    public sealed class ConcreteImpactDust : MonoBehaviour
    {
        [SerializeField] ParticleSystem powder;
        float lastImpact=-10f;
        AftershockDirector aftershock;
        void Awake(){aftershock=FindFirstObjectByType<AftershockDirector>();}
        void OnCollisionEnter(Collision collision)
        {
            if(powder==null||collision.contactCount==0||collision.relativeVelocity.sqrMagnitude<.25f||Time.time-lastImpact<.32f)return;
            lastImpact=Time.time;var contact=collision.GetContact(0);
            var emit=new ParticleSystem.EmitParams{position=contact.point,applyShapeToPosition=false};
            powder.Emit(emit,Mathf.Clamp(Mathf.RoundToInt(collision.relativeVelocity.magnitude*4),3,12));
            if(aftershock!=null)aftershock.ResolvePhysicalImpact(contact.point,collision.relativeVelocity.magnitude);
        }
    }
}
