using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BreathInTheRubble.Prototype
{
    public sealed class RescueEndingDirector : MonoBehaviour
    {
        [SerializeField] GameSequenceController sequence;
        [SerializeField] Transform[] rubble = Array.Empty<Transform>();
        [SerializeField] Vector3[] removalOffsets = Array.Empty<Vector3>();
        [SerializeField] float[] removalStarts = Array.Empty<float>();
        [SerializeField, Min(1f)] float removalSeconds = 9.5f;
        [SerializeField, Min(1f)] float durationSeconds = 60f;
        [SerializeField] Light openingLight;
        [SerializeField] Light softFill;
        [SerializeField] MeshRenderer outsideSky;
        [SerializeField] MeshRenderer whiteout;
        [SerializeField] MeshRenderer sunShafts;
        [SerializeField] Renderer[] lightBlockers = Array.Empty<Renderer>();
        [SerializeField] Renderer[] apertureBlockers = Array.Empty<Renderer>();
        [SerializeField] Volume endingVolume;
        [SerializeField] UniversalAdditionalCameraData view;
        [SerializeField] UniversalRenderPipelineAsset endingPipeline;
        [SerializeField] Material daylightSky;
        [SerializeField] AudioSource workSource;
        [SerializeField] AudioClip workClip;
        [SerializeField] AudioSource responseSource;
        [SerializeField] AudioClip responseClip;
        Vector3[] positions;
        Quaternion[] rotations;
        Collider[][] colliders;
        bool[][] colliderStates;
        Rigidbody[][] carriedBodies;
        bool[][] bodyKinematicStates, bodyGravityStates;
        CollisionDetectionMode[][] bodyDetectionStates;
        MaterialPropertyBlock whiteProperties;
        ColorAdjustments exposure;
        Bloom bloom;
        bool captured;
        bool cameraStateCaptured, previousPostProcessing, previousDepthTexture, previousWorkLoop;
        Light previousSun;
        bool sunStateCaptured;
        RenderPipelineAsset previousQualityPipeline;
        bool pipelineStateCaptured;
        Material previousSky;
        CameraClearFlags previousClearFlags;
        float lightAdvanceSeconds;
        bool recoveryCuePlayed;
        public bool IsRunning { get; private set; }
        public bool IsComplete { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public float OpeningAmount { get; private set; }
        public float WhiteoutAmount { get; private set; }
        public float SunlightAdmission { get; private set; }
        public float GlareAmount { get; private set; }
        public float DurationSeconds => durationSeconds;
        public event Action Completed;
        void OnEnable()
        {
            if (sequence != null) { sequence.StageChanged += OnStage; sequence.Advanced += Advance; sequence.SequenceReset += ResetEnding; }
            SetLight(0f); SetWhite(0f);
        }
        void OnDisable()
        {
            if (sequence != null) { sequence.StageChanged -= OnStage; sequence.Advanced -= Advance; sequence.SequenceReset -= ResetEnding; }
            ResetEnding();
        }
        void OnStage(GameStage stage) { if (stage == GameStage.Rescue) Begin(); }
        public void Begin()
        {
            if (IsRunning || IsComplete) return;
            positions = new Vector3[rubble.Length]; rotations = new Quaternion[rubble.Length];
            colliders = new Collider[rubble.Length][]; colliderStates = new bool[rubble.Length][];
            carriedBodies=new Rigidbody[rubble.Length][];bodyKinematicStates=new bool[rubble.Length][];bodyGravityStates=new bool[rubble.Length][];bodyDetectionStates=new CollisionDetectionMode[rubble.Length][];
            for (int i = 0; i < rubble.Length; i++)
            {
                if (rubble[i] == null) continue;
                positions[i] = rubble[i].position; rotations[i] = rubble[i].rotation;
                colliders[i] = rubble[i].GetComponentsInChildren<Collider>(true); colliderStates[i] = new bool[colliders[i].Length];
                for (int j = 0; j < colliders[i].Length; j++) { colliderStates[i][j] = colliders[i][j].enabled; colliders[i][j].enabled = false; }
                carriedBodies[i]=rubble[i].GetComponentsInChildren<Rigidbody>(true);bodyKinematicStates[i]=new bool[carriedBodies[i].Length];bodyGravityStates[i]=new bool[carriedBodies[i].Length];bodyDetectionStates[i]=new CollisionDetectionMode[carriedBodies[i].Length];
                for(int j=0;j<carriedBodies[i].Length;j++){var body=carriedBodies[i][j];bodyKinematicStates[i][j]=body.isKinematic;bodyGravityStates[i][j]=body.useGravity;bodyDetectionStates[i][j]=body.collisionDetectionMode;if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}if(body.collisionDetectionMode==CollisionDetectionMode.ContinuousDynamic)body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.isKinematic=true;body.useGravity=false;}
            }
            captured = true; IsRunning = true; recoveryCuePlayed = false; ElapsedSeconds = lightAdvanceSeconds = 0f;
            if (endingPipeline != null)
            {
                previousQualityPipeline = QualitySettings.renderPipeline; pipelineStateCaptured = true;
                QualitySettings.renderPipeline = endingPipeline;
            }
            previousSun = RenderSettings.sun; sunStateCaptured = true;
            previousSky = RenderSettings.skybox;
            if (daylightSky != null) RenderSettings.skybox = daylightSky;
            if (openingLight != null && openingLight.type == LightType.Directional) RenderSettings.sun = openingLight;
            if (view != null) { previousPostProcessing = view.renderPostProcessing; previousDepthTexture = view.requiresDepthTexture; previousClearFlags = view.GetComponent<Camera>().clearFlags; cameraStateCaptured = true; view.renderPostProcessing = true; view.requiresDepthTexture = true; if (daylightSky != null) view.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox; }
            if (endingVolume != null && endingVolume.profile != null)
            {
                endingVolume.profile.TryGet(out exposure); endingVolume.profile.TryGet(out bloom);
            }
            if (workSource != null && workClip != null) { previousWorkLoop = workSource.loop; workSource.loop = true; workSource.clip = workClip; workSource.Play(); }
            Apply();
        }
        public void Advance(float seconds)
        {
            if (!IsRunning) return;
            ElapsedSeconds = Mathf.Min(durationSeconds, ElapsedSeconds + Mathf.Max(0f, seconds));
            lightAdvanceSeconds = Mathf.Max(0f, seconds);
            Apply();
            float lastCarry = 0f;
            for (int i = 0; i < rubble.Length; i++) lastCarry = Mathf.Max(lastCarry, (i < removalStarts.Length ? removalStarts[i] : 0f) + removalSeconds);
            if (!recoveryCuePlayed && ElapsedSeconds >= lastCarry)
            {
                recoveryCuePlayed = true;
                if (workSource != null) workSource.Stop();
                if (responseSource != null && responseClip != null) responseSource.PlayOneShot(responseClip);
            }
            if (ElapsedSeconds < durationSeconds) return;
            IsRunning = false; IsComplete = true;
            if (workSource != null) workSource.Stop();
            Completed?.Invoke();
        }
        void Apply()
        {
            float total = 0f, weightSum = 0f;
            for (int i = 0; i < rubble.Length; i++)
            {
                if (rubble[i] == null) continue;
                float starts = i < removalStarts.Length ? removalStarts[i] : 3f + i * .7f;
                float raw = Mathf.Clamp01((ElapsedSeconds - starts) / removalSeconds);
                float t = Mathf.SmoothStep(0f, 1f, raw);
                var offset = i < removalOffsets.Length ? removalOffsets[i] : Vector3.up;
                // Free one edge, lift just clear of its supports, then hand-carry this individual slab.
                float lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(raw / .38f));
                float carry = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.32f, 1f, raw));
                rubble[i].position = positions[i] + new Vector3(offset.x * carry, offset.y * lift, offset.z * carry);
                float edge = Mathf.Sin(Mathf.Clamp01(raw / .45f) * Mathf.PI) * 5f;
                rubble[i].rotation = rotations[i] * Quaternion.Euler(t * (i % 2 == 0 ? 5f : -4f) + edge, t * 5f, t * 3f);
                float weight = 1f;
                total += t * weight; weightSum += weight;
            }
            OpeningAmount = weightSum > 0f ? total / weightSum : 0f;
            SunlightAdmission = Mathf.MoveTowards(SunlightAdmission, MeasureOpening(), lightAdvanceSeconds * .2f);
            // Geometry and real shadows reveal daylight. Removal progress must never whiten a closed cavity.
            GlareAmount = SunlightAdmission;
            SetLight(OpeningAmount);
            if (endingVolume != null) endingVolume.weight = 1f;
            if (exposure != null) exposure.postExposure.value = .1f;
            if (bloom != null) { bloom.intensity.value = .06f; bloom.scatter.value = .45f; bloom.threshold.value = 1.2f; }
            SetWhite(0f);
            if (sunShafts != null) sunShafts.enabled = false;
            if (workSource != null) workSource.volume = .48f;
        }
        void SetLight(float amount)
        {
            if (openingLight != null)
            {
                openingLight.enabled = (IsRunning || IsComplete) && amount > .001f;
                openingLight.intensity = 1.45f;
            }
            if (softFill != null) { softFill.enabled = SunlightAdmission > 0f; softFill.intensity = Mathf.Pow(SunlightAdmission, .7f) * .09f; }
            if (outsideSky != null) outsideSky.enabled = false;
        }
        float MeasureOpening()
        {
            if (OpeningAmount <= .0001f) return 0f;
            int clear = 0;
            // A fixed sample area keeps head tracking from changing the authored ending pace.
            var origin = new Vector3(0f, .92f, .15f);
            for (int x = 0; x < 7; x++) for (int z = 0; z < 5; z++)
            {
                var destination = apertureBlockers.Length > 0
                    ? new Vector3(Mathf.Lerp(-.22f, .22f, x / 6f), 3.2f, Mathf.Lerp(.05f, .49f, z / 4f))
                    : new Vector3(Mathf.Lerp(-.7f, .7f, x / 6f), 2.8f, Mathf.Lerp(-.35f, 1f, z / 4f));
                bool blocked = false;
                foreach (var surface in apertureBlockers.Length > 0 ? apertureBlockers : lightBlockers)
                {
                    if (surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy) continue;
                    var localOrigin = surface.transform.InverseTransformPoint(origin);
                    var localEnd = surface.transform.InverseTransformPoint(destination);
                    var localRay = new Ray(localOrigin, (localEnd - localOrigin).normalized);
                    if (surface.localBounds.IntersectRay(localRay, out float distance) && distance < Vector3.Distance(localOrigin, localEnd)) { blocked = true; break; }
                }
                if (!blocked) clear++;
            }
            return clear / 35f;
        }
        void SetWhite(float amount)
        {
            WhiteoutAmount = amount;
            if (whiteout == null) return;
            whiteout.enabled = amount > .0001f;
            if (whiteProperties == null) whiteProperties = new MaterialPropertyBlock();
            whiteProperties.SetFloat("_Opacity", amount); whiteout.SetPropertyBlock(whiteProperties);
            // This mesh is drawn before grading; prevent exposure from clipping a partial
            // veil to solid white while rubble is still being removed.
            whiteProperties.SetFloat("_WhiteLevel", exposure != null ? Mathf.Pow(2f, -exposure.postExposure.value) : 1f);
            whiteout.SetPropertyBlock(whiteProperties);
        }
        public void ResetEnding()
        {
            if (captured)
                for (int i = 0; i < rubble.Length; i++)
                {
                    if (rubble[i] == null) continue;
                    rubble[i].SetPositionAndRotation(positions[i], rotations[i]);
                    for (int j = 0; j < colliders[i].Length; j++) if (colliders[i][j] != null) colliders[i][j].enabled = colliderStates[i][j];
                    if(carriedBodies!=null&&carriedBodies[i]!=null)for(int j=0;j<carriedBodies[i].Length;j++){var body=carriedBodies[i][j];if(body==null)continue;body.isKinematic=bodyKinematicStates[i][j];body.useGravity=bodyGravityStates[i][j];body.collisionDetectionMode=bodyDetectionStates[i][j];}
                }
            captured = IsRunning = IsComplete = false; ElapsedSeconds = OpeningAmount = SunlightAdmission = GlareAmount = lightAdvanceSeconds = 0f;
            SetLight(0f); SetWhite(0f);
            if (endingVolume != null) endingVolume.weight = 0f;
            if (sunShafts != null) sunShafts.enabled = false;
            if (workSource != null) { workSource.Stop(); workSource.loop = previousWorkLoop; }
            if (responseSource != null) responseSource.Stop();
            if (cameraStateCaptured && view != null) { view.renderPostProcessing = previousPostProcessing; view.requiresDepthTexture = previousDepthTexture; view.GetComponent<Camera>().clearFlags = previousClearFlags; }
            cameraStateCaptured = false;
            if (sunStateCaptured) { RenderSettings.sun = previousSun; RenderSettings.skybox = previousSky; }
            sunStateCaptured = false;
            if (pipelineStateCaptured) QualitySettings.renderPipeline = previousQualityPipeline;
            pipelineStateCaptured = false;
        }
    }
}
