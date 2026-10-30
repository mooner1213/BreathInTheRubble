using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

namespace BreathInTheRubble.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class PhoneContactOnlyGrabFilter : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
    {
        [SerializeField, Min(0f)] float fallbackContactRadius = 0.04f;

        XRGrabInteractable grabInteractable;
        Collider[] phoneColliders;

        public bool canProcess => isActiveAndEnabled;

        void Awake()
        {
            CacheReferences();
        }

        void OnEnable()
        {
            CacheReferences();
            grabInteractable.selectFilters.Add(this);
            grabInteractable.hoverFilters.Add(this);
        }

        void OnDisable()
        {
            if (grabInteractable == null)
                return;

            grabInteractable.selectFilters.Remove(this);
            grabInteractable.hoverFilters.Remove(this);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return IsInteractorTouchingPhone(interactor as Component);
        }

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            return IsInteractorTouchingPhone(interactor as Component);
        }

        public bool IsInteractorTouchingPhone(Component interactorComponent)
        {
            if (interactorComponent == null)
                return false;

            var contactOrigin = interactorComponent.transform;
            var contactRadius = fallbackContactRadius;

            if (interactorComponent is NearFarInteractor nearFarInteractor &&
                nearFarInteractor.enableNearCasting &&
                nearFarInteractor.nearInteractionCaster != null)
            {
                contactOrigin = nearFarInteractor.nearInteractionCaster.effectiveCastOrigin;
                if (nearFarInteractor.nearInteractionCaster is SphereInteractionCaster sphereCaster)
                {
                    var casterScale = sphereCaster.transform.lossyScale;
                    var largestScale = Mathf.Max(Mathf.Abs(casterScale.x), Mathf.Abs(casterScale.y), Mathf.Abs(casterScale.z));
                    contactRadius = sphereCaster.castRadius * largestScale;
                }
            }

            if (contactOrigin == null)
                return false;

            var contactPosition = contactOrigin.position;
            var contactRadiusSquared = contactRadius * contactRadius;
            foreach (var phoneCollider in phoneColliders)
            {
                if (phoneCollider == null || !phoneCollider.enabled || !phoneCollider.gameObject.activeInHierarchy)
                    continue;

                var closestPoint = phoneCollider.ClosestPoint(contactPosition);
                if ((closestPoint - contactPosition).sqrMagnitude <= contactRadiusSquared)
                    return true;
            }

            return false;
        }

        void CacheReferences()
        {
            if (grabInteractable == null)
                grabInteractable = GetComponent<XRGrabInteractable>();
            phoneColliders = GetComponentsInChildren<Collider>(true);
        }
    }
}
