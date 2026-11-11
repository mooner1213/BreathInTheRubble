using UnityEngine;

namespace BreathInTheRubble.Prototype
{
    /// <summary>Reusable dust plumes and lingering haze, separate from stage and input rules.</summary>
    [DisallowMultipleComponent]
    public sealed class AftershockDustEffect : MonoBehaviour
    {
        [SerializeField] ParticleSystem dustVeil;
        [SerializeField] ParticleSystem[] fracturePlumes = System.Array.Empty<ParticleSystem>();
        [SerializeField] ParticleSystem[] awakeningDust = System.Array.Empty<ParticleSystem>();
        [SerializeField] ParticleSystem[] aftershockDust = System.Array.Empty<ParticleSystem>();
        [SerializeField, Min(0)] int openingVeilCount = 28;
        [SerializeField, Min(0)] int fractureVeilCount = 22;
        [SerializeField, Min(0f)] float startingDensity = 0.9f;
        [SerializeField, Min(0f)] float collapseDensity = 1.9f;
        [SerializeField, Min(0f)] float holdSeconds = 20f;
        [SerializeField, Min(0.1f)] float fadeSeconds = 10f;
        [SerializeField] Color dustColor = new Color(0.006f, 0.006f, 0.005f, 1f);
        bool originalFog;
        FogMode originalMode;
        Color originalColor;
        float originalDensity, elapsed;
        bool captured, active, fromAftershock, fogApplied;
        bool emissionStopped;
        Layer[] awakeningLayers, aftershockLayers;
        public float HazeAmount { get; private set; }
        public float Opacity { get; private set; }
        public bool IsActive => active;
        public float ElapsedSeconds => elapsed;

        sealed class Layer
        {
            public ParticleSystem system;
            public ParticleSystemRenderer renderer;
            public Color color;
            public MaterialPropertyBlock original = new MaterialPropertyBlock();
            public MaterialPropertyBlock faded = new MaterialPropertyBlock();
            public float lastOpacity = -1f;
        }

        void Awake()
        {
            originalFog = RenderSettings.fog;
            originalMode = RenderSettings.fogMode;
            originalColor = RenderSettings.fogColor;
            originalDensity = RenderSettings.fogDensity;
            awakeningLayers = CaptureLayers(awakeningDust);
            aftershockLayers = CaptureLayers(aftershockDust);
            captured = true;
        }
        void Start() { if (!active) ReplayAwakening(); }
        void Update() { Advance(Time.deltaTime); }
        public void ReplayAwakening()
        {
            Clear(); active = true; fromAftershock = false;
            HazeAmount = startingDensity;
            Advance(0f);
        }

        public void Begin()
        {
            active = true; fromAftershock = true; elapsed = 0f; emissionStopped=false;
            if (dustVeil != null) { dustVeil.Play(); dustVeil.Emit(openingVeilCount); }
            Advance(0f);
        }
        public void Fracture()
        {
            if (!active || !fromAftershock) return;
            if (dustVeil != null) dustVeil.Emit(fractureVeilCount);
            foreach (var plume in fracturePlumes)
                if (plume != null) { plume.Play(); plume.Emit(12); }
        }
        public void End()
        {
            // The 30-second episode is measured from the start of the aftershock.
            if (aftershockLayers == null) return;
            foreach (var layer in aftershockLayers)
                if (layer != null && layer.system != null) layer.system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        public void Advance(float seconds)
        {
            if (!captured || !active) return;
            float delta = Mathf.Max(0f, seconds);
            elapsed += delta;
            if (elapsed >= (fromAftershock?AftershockEnvelope.DustEnd:holdSeconds+fadeSeconds)) { Clear(); return; }
            Opacity = fromAftershock?AftershockEnvelope.Dust(elapsed):1f-Mathf.SmoothStep(0f,1f,Mathf.Clamp01((elapsed-holdSeconds)/Mathf.Max(.1f,fadeSeconds)));
            if(fromAftershock&&!emissionStopped)
            {
                if(elapsed>=AftershockEnvelope.PeakEnd){emissionStopped=true;End();}
                else
                {
                    // A little fine debris first; the suspended cloud fills during the main failure.
                    if(dustVeil!=null)
                    {
                        var emission=dustVeil.emission;emission.enabled=true;
                        emission.rateOverTime=elapsed<5f?Mathf.Lerp(0,2,elapsed/5f):Mathf.Lerp(12,30,Mathf.Clamp01((elapsed-5)/1.5f));
                        if(!dustVeil.isPlaying)dustVeil.Play();
                    }
                }
            }
            float target = (fromAftershock ? collapseDensity : startingDensity) * Opacity;
            HazeAmount = fromAftershock ? Mathf.MoveTowards(HazeAmount, target, delta * 2.8f) : target;
            ApplyOpacity(fromAftershock ? aftershockLayers : awakeningLayers, Opacity);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = Mathf.Max(originalFog ? originalDensity : 0f, HazeAmount);
            RenderSettings.fogColor = Color.Lerp(originalColor, dustColor, Mathf.Clamp01(HazeAmount / 0.38f));
            fogApplied = true;
        }
        public void Clear()
        {
            active = false; elapsed = 0f; Opacity = 0f;emissionStopped=false;
            ClearLayers(awakeningLayers); ClearLayers(aftershockLayers);
            RestoreFog();
        }
        void RestoreFog()
        {
            HazeAmount = 0f;
            if (!captured || !fogApplied) return;
            RenderSettings.fog = originalFog; RenderSettings.fogMode = originalMode;
            RenderSettings.fogDensity = originalDensity; RenderSettings.fogColor = originalColor;
            fogApplied = false;
        }
        static Layer[] CaptureLayers(ParticleSystem[] systems)
        {
            var layers = new Layer[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;
                var renderer = systems[i].GetComponent<ParticleSystemRenderer>();
                var layer = new Layer { system = systems[i], renderer = renderer, color = renderer.sharedMaterial.GetColor("_BaseColor") };
                renderer.GetPropertyBlock(layer.original); renderer.GetPropertyBlock(layer.faded);
                layers[i] = layer;
            }
            return layers;
        }
        static void ApplyOpacity(Layer[] layers, float opacity)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                if (layer == null || layer.renderer == null || Mathf.Approximately(layer.lastOpacity, opacity)) continue;
                var color = layer.color; color.a *= opacity;
                layer.faded.SetColor("_BaseColor", color); layer.renderer.SetPropertyBlock(layer.faded);
                layer.lastOpacity = opacity;
            }
        }
        static void ClearLayers(Layer[] layers)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                if (layer == null) continue;
                if (layer.system != null) layer.system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (layer.renderer != null && layer.lastOpacity >= 0f) layer.renderer.SetPropertyBlock(layer.original);
                layer.lastOpacity = -1f;
            }
        }
        void OnDisable() { Clear(); }
    }
}
