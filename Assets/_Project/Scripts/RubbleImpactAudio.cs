using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    [RequireComponent(typeof(Rigidbody),typeof(AudioSource))]
    public sealed class RubbleImpactAudio : MonoBehaviour
    {
        [SerializeField] AudioClip impactClip;
        AudioSource source;
        Rigidbody body;
        float nextImpact;
        void Awake(){source=GetComponent<AudioSource>();body=GetComponent<Rigidbody>();}
        void OnCollisionEnter(Collision contact)
        {
            if(body.isKinematic||impactClip==null||Time.time<nextImpact)return;
            float speed=contact.relativeVelocity.magnitude;if(speed<.35f)return;
            source.PlayOneShot(impactClip,Mathf.Clamp(speed*.17f,.12f,.65f));nextImpact=Time.time+.3f;
        }
    }
}
