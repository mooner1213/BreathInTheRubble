using System.Collections;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    public sealed class AwakeningDustEffect : MonoBehaviour
    {
        [SerializeField] ParticleSystem[] dustSystems;
        [SerializeField, Min(0f)] float startDelaySeconds = 0.15f;

        IEnumerator Start()
        {
            if (startDelaySeconds > 0f)
                yield return new WaitForSeconds(startDelaySeconds);

            PlayNow();
        }

        [ContextMenu("Preview Dust Burst")]
        public void PlayNow()
        {
            if (dustSystems == null)
                return;

            foreach (var dustSystem in dustSystems)
            {
                if (dustSystem == null)
                    continue;

                dustSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                dustSystem.Play(true);
            }
        }
    }
}
