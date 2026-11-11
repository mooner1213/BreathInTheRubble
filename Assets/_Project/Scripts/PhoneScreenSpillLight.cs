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

        [Header("Awakening glow")]
        [SerializeField, Min(1f)] float awakeningIntensityMultiplier = 5f;
        [SerializeField, Min(0f)] float awakeningHoldSeconds = 4f;
        [SerializeField, Min(0.1f)] float awakeningFadeSeconds = 2f;
        [SerializeField, Range(1f, 179f)] float awakeningOuterAngle = 150f;
        [SerializeField, Range(0f, 179f)] float awakeningInnerAngle = 108f;

        readonly RaycastHit[] raycastHits = new RaycastHit[8];
        float requestedLevel;
        float restingOuterAngle, restingInnerAngle;
        double awakeningBeganAt;
        bool awakeningGlow;

        void Awake()
        {
            if (spillLight == null)
                spillLight = GetComponent<Light>();

            if (spillLight != null)
            {
                restingOuterAngle = spillLight.spotAngle;
                restingInnerAngle = spillLight.innerSpotAngle;
            }

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

        public void BeginAwakeningGlow()
        {
            awakeningBeganAt = Time.timeAsDouble;
            awakeningGlow = true;
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
            float glow = 0f;
            if (awakeningGlow)
            {
                float age = (float)(Time.timeAsDouble - awakeningBeganAt);
                glow = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - awakeningHoldSeconds) / awakeningFadeSeconds));
                spillLight.spotAngle = Mathf.Lerp(restingOuterAngle, awakeningOuterAngle, glow);
                spillLight.innerSpotAngle = Mathf.Lerp(restingInnerAngle, awakeningInnerAngle, glow);
                if (glow <= 0f) awakeningGlow = false;
            }
            spillLight.intensity = maximumIntensity * requestedLevel * MeasureClearance() * Mathf.Lerp(1f, awakeningIntensityMultiplier, glow);
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
