using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    public sealed class PhoneScreenSpillLight : MonoBehaviour
    {
        [SerializeField] Light spillLight;
        [SerializeField, Min(0f)] float maximumIntensity = 0.55f;
        [SerializeField, Min(0.001f)] float probeRadius = 0.004f;
        [SerializeField, Min(0f)] float blockedDistance = 0.004f;
        [SerializeField, Min(0.001f)] float clearDistance = 0.035f;
        [SerializeField, Range(0.1f, 1f)] float fadeResponseExponent = 0.35f;
        [SerializeField, Range(0f, 1f)] float minimumOcclusionIncidence = 0.975f;
        [SerializeField, Range(0f, 1f)] float minimumDownwardFacingForOverlap = 0.995f;
        [SerializeField] LayerMask occlusionMask = ~0;

        readonly RaycastHit[] raycastHits = new RaycastHit[8];
        float requestedLevel;

        void Awake()
        {
            if (spillLight == null)
                spillLight = GetComponent<Light>();

            SetEmissionLevel(0f);
        }

        void LateUpdate()
        {
            UpdateLightOutput();
        }

        public void SetEmissionLevel(float level)
        {
            requestedLevel = Mathf.Clamp01(level);
            UpdateLightOutput();
        }

        void UpdateLightOutput()
        {
            if (spillLight == null)
                return;

            if (requestedLevel <= 0.001f)
            {
                spillLight.intensity = 0f;
                spillLight.enabled = false;
                return;
            }

            spillLight.enabled = true;
            spillLight.intensity = maximumIntensity * requestedLevel * MeasureClearance();
        }

        float MeasureClearance()
        {
            var origin = spillLight.transform.position;
            var direction = spillLight.transform.forward;
            var phoneRoot = transform.root;

            var hitCount = Physics.SphereCastNonAlloc(
                origin,
                probeRadius,
                direction,
                raycastHits,
                clearDistance,
                occlusionMask,
                QueryTriggerInteraction.Ignore);

            var nearestDistance = float.PositiveInfinity;
            for (var index = 0; index < hitCount; index++)
            {
                var hitCollider = raycastHits[index].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(phoneRoot))
                    continue;
                if (raycastHits[index].distance <= 0.0001f && Physics.gravity.sqrMagnitude > 0.001f)
                {
                    var downwardFacing = Vector3.Dot(direction, Physics.gravity.normalized);
                    if (downwardFacing < minimumDownwardFacingForOverlap)
                        continue;
                }
                var incidence = Vector3.Dot(-direction, raycastHits[index].normal);
                if (incidence < minimumOcclusionIncidence)
                    continue;

                nearestDistance = Mathf.Min(nearestDistance, raycastHits[index].distance);
            }

            if (float.IsPositiveInfinity(nearestDistance))
                return 1f;

            var clearance = Mathf.InverseLerp(blockedDistance, clearDistance, nearestDistance);
            return Mathf.Pow(clearance, fadeResponseExponent);
        }

        void OnValidate()
        {
            clearDistance = Mathf.Max(clearDistance, blockedDistance + 0.001f);
            fadeResponseExponent = Mathf.Clamp(fadeResponseExponent, 0.1f, 1f);
            minimumOcclusionIncidence = Mathf.Clamp01(minimumOcclusionIncidence);
            minimumDownwardFacingForOverlap = Mathf.Clamp01(minimumDownwardFacingForOverlap);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (spillLight == null)
                return;

            Gizmos.color = new Color(0.35f, 0.75f, 1f, 0.65f);
            Gizmos.DrawLine(
                spillLight.transform.position,
                spillLight.transform.position + spillLight.transform.forward * clearDistance);
        }
#endif
    }
}
