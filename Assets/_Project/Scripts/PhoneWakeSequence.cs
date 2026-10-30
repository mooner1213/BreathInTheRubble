using System.Collections;
using System;
using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    public sealed class PhoneWakeSequence : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Timing")]
        [SerializeField, Min(0f)] float wakeDelaySeconds = 5f;
        [SerializeField, Range(1, 6)] int alarmPulseCount = 3;
        [SerializeField, Min(0.05f)] float alarmPulseInterval = 0.42f;

        [Header("Phone References")]
        [SerializeField] Renderer screenRenderer;
        [SerializeField] Light screenLight;
        [SerializeField] Light screenSpillLight;
        [SerializeField] PhoneScreenSpillLight screenSpillController;
        [SerializeField] AudioSource alarmAudioSource;

        [Header("Screen")]
        [SerializeField, Min(0.05f)] float screenWakeFadeSeconds = 0.7f;
        [SerializeField] Color screenOffColor = new Color(0.002f, 0.004f, 0.005f, 1f);
        [SerializeField] Color screenOnColor = new Color(0.025f, 0.12f, 0.18f, 1f);
        [SerializeField, ColorUsage(true, true)] Color screenEmissionColor = new Color(0.18f, 0.95f, 1.35f, 1f);

        Material runtimeScreenMaterial;
        AudioClip alarmClip;
        Coroutine screenWakeRoutine;
        float screenSpillTargetIntensity;
        bool isAwake;

        public bool IsAwake => isAwake;

        public event Action Awakened;

        void Awake()
        {
            if (screenRenderer != null)
                runtimeScreenMaterial = screenRenderer.material;

            SetScreenVisual(0f);
            if (screenLight != null)
                screenLight.enabled = false;
            if (screenSpillController != null)
            {
                screenSpillController.SetEmissionLevel(0f);
            }
            else if (screenSpillLight != null)
            {
                screenSpillTargetIntensity = screenSpillLight.intensity;
                screenSpillLight.intensity = 0f;
                screenSpillLight.enabled = false;
            }

            alarmClip = CreateAlarmClip();
        }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(wakeDelaySeconds);
            WakeNow();

            if (alarmAudioSource == null || alarmClip == null)
                yield break;

            for (var pulse = 0; pulse < alarmPulseCount; pulse++)
            {
                alarmAudioSource.PlayOneShot(alarmClip, 0.75f);
                yield return new WaitForSeconds(alarmPulseInterval);
            }
        }

        public void WakeNow()
        {
            if (isAwake)
                return;

            isAwake = true;
            Awakened?.Invoke();
            screenWakeRoutine = StartCoroutine(FadeScreenOn());
        }

        public void SetFlashlight(bool enabled)
        {
            if (screenLight != null)
                screenLight.enabled = enabled;
        }

        IEnumerator FadeScreenOn()
        {
            if (screenSpillController != null)
            {
                screenSpillController.SetEmissionLevel(0f);
            }
            else if (screenSpillLight != null)
            {
                screenSpillLight.intensity = 0f;
                screenSpillLight.enabled = true;
            }

            var elapsed = 0f;
            while (elapsed < screenWakeFadeSeconds)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / screenWakeFadeSeconds);
                var easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                SetScreenVisual(easedProgress);
                if (screenSpillController != null)
                    screenSpillController.SetEmissionLevel(easedProgress);
                else if (screenSpillLight != null)
                    screenSpillLight.intensity = screenSpillTargetIntensity * easedProgress;
                yield return null;
            }

            SetScreenVisual(1f);
            if (screenSpillController != null)
                screenSpillController.SetEmissionLevel(1f);
            else if (screenSpillLight != null)
                screenSpillLight.intensity = screenSpillTargetIntensity;
            screenWakeRoutine = null;
        }

        void SetScreenVisual(float amount)
        {
            if (runtimeScreenMaterial == null)
                return;

            amount = Mathf.Clamp01(amount);
            runtimeScreenMaterial.SetColor(BaseColorId, Color.Lerp(screenOffColor, screenOnColor, amount));
            runtimeScreenMaterial.SetColor(EmissionColorId, Color.Lerp(Color.black, screenEmissionColor, amount));
            if (amount > 0.001f)
                runtimeScreenMaterial.EnableKeyword("_EMISSION");
            else
                runtimeScreenMaterial.DisableKeyword("_EMISSION");
        }

        static AudioClip CreateAlarmClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.22f;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var index = 0; index < sampleCount; index++)
            {
                var time = index / (float)sampleRate;
                var frequency = time < duration * 0.5f ? 880f : 1175f;
                var attack = Mathf.Clamp01(time / 0.012f);
                var release = Mathf.Clamp01((duration - time) / 0.035f);
                var envelope = Mathf.Min(attack, release);
                samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.38f;
            }

            var clip = AudioClip.Create("Prototype Phone Alarm", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDestroy()
        {
            if (screenWakeRoutine != null)
                StopCoroutine(screenWakeRoutine);
            if (runtimeScreenMaterial != null)
                ReleaseRuntimeObject(runtimeScreenMaterial);
            if (alarmClip != null)
                ReleaseRuntimeObject(alarmClip);
        }

        static void ReleaseRuntimeObject(UnityEngine.Object item)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(item);
                return;
            }
#endif
            Destroy(item);
        }
    }
}
