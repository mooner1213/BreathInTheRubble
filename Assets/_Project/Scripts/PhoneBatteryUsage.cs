using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Maps existing phone activity to battery accounting, using game time.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhoneBattery))]
    public sealed class PhoneBatteryUsage : MonoBehaviour
    {
        PhoneBattery battery;
        PhoneInterfaceController phone;
        EmergencyCallSystem calls;
        void Awake()
        {
            battery = GetComponent<PhoneBattery>();
            phone = GetComponent<PhoneInterfaceController>();
            calls = GetComponent<EmergencyCallSystem>();
        }
        void Update()
        {
            battery.Drain(Time.deltaTime, phone != null && phone.FlashlightEnabled,
                calls != null ? calls.State : EmergencyCallSystem.CallState.Idle);
        }
    }
}
