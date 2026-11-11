using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEngine;

public static class VerifyAftershockFoundation
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Ref(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(property).objectReferenceValue = value;
        data.ApplyModifiedProperties();
    }

    public static object Run()
    {
        Check(Application.isPlaying, "Play Mode required.");
        var sequence = UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();
        var director = UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();
        var guard = UnityEngine.Object.FindFirstObjectByType<HeadProtectionDetector>();
        var feedback = UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>();
        var motion = UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var driver = UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();
        Check(sequence != null && director != null && guard != null && feedback != null && motion != null, "Scene foundation missing.");
        Check(driver != null && driver.DesktopEnabled && !DesktopPhoneTestDriver.HasPhysicalHmd, "Desktop-only test required.");
        var head = Camera.main.transform;
        Vector3 headPosition = head.position;
        Quaternion headRotation = head.rotation;
        sequence.RestoreAftershock();
        var targets = new SerializedObject(motion).FindProperty("ceilingTargets");
        var ceiling = Enumerable.Range(0, targets.arraySize).Select(i => (Transform)targets.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var positions = ceiling.Select(t => t.localPosition).ToArray();

        Check(guard.EvaluatePose(Vector3.zero, Vector3.right, new Vector3(-0.15f, 0.1f, 0f), new Vector3(0.15f, 0.1f, 0f)), "Two hands at head should protect.");
        Check(!guard.EvaluatePose(Vector3.zero, Vector3.right, new Vector3(-0.15f, -0.4f, 0f), new Vector3(0.15f, 0.1f, 0f)), "One lowered hand should not protect.");
        Check(!guard.EvaluatePose(Vector3.zero, Vector3.right, new Vector3(-0.6f, 0.1f, 0f), new Vector3(0.15f, 0.1f, 0f)), "Distant hands should not protect.");
        guard.SetEditorPreview(true, false); guard.AdvanceProtection(0.2f);
        sequence.BeginAftershockNow(); sequence.Advance(3.3f);
        Check(sequence.Stage == GameStage.Aftershock && director.IsRunning && director.UnprotectedImpacts == 1, "Unprotected impact not resolved.");
        Check(feedback.Opacity > 0f, "Unprotected impact has no red feedback.");
        Check(motion.AppliedMaximumDrop > 0f && motion.AppliedMaximumDrop <= motion.MaximumConfiguredDrop + 0.0001f, "Ceiling settling is absent or exceeds configured limit.");
        Check(ceiling.Where((t, i) => t.localPosition != positions[i]).Any(), "No environment target moved.");
        Check(GameObject.Find("Aftershock Structural Audio").GetComponent<AudioSource>().isPlaying, "Aftershock audio is not playing.");
        Check(GameObject.Find("Additional Aftershock Dust").GetComponent<ParticleSystem>().particleCount > 0, "Aftershock dust did not emit.");
        Check(sequence.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Cosmetic Falling Pebble") && t.gameObject.activeSelf), "No falling debris became active.");
        sequence.PreviewNextStage();
        Check(sequence.Stage == GameStage.Isolation && !director.IsRunning && feedback.Opacity == 0f, "Next-stage preview did not end aftershock.");
        for (int i = 0; i < ceiling.Length; i++) Check(ceiling[i].localPosition == positions[i], "Restore did not restore ceiling.");
        if (!driver.GuardPreviewEnabled) driver.ToggleGuardPreview();
        guard.SetEditorPreview(true, driver.GuardPreviewEnabled); guard.AdvanceProtection(0.2f);
        Check(guard.IsProtected, "PC guard toggle did not protect.");
        sequence.BeginAftershockNow(); sequence.Advance(director.DurationSeconds-0.2f);
        Check(director.GuardedImpacts == director.ImpactCount && director.UnprotectedImpacts == 0 && feedback.Opacity == 0f, "Protected aftershock caused damage or missed checks.");
        sequence.Advance(0.3f);
        Check(sequence.Stage == GameStage.Isolation && !director.IsRunning, "Aftershock did not finish naturally.");
        sequence.PreviewNextStage(); sequence.Advance(0f);
        Check(sequence.Stage == GameStage.Rescue, "Rescue preview did not persist.");
        sequence.RestoreAftershock(); sequence.Advance(0f);
        Check(sequence.Stage == GameStage.Isolation, "Restore remains stuck in Rescue preview.");
        if (driver.GuardPreviewEnabled) driver.ToggleGuardPreview();
        guard.SetEditorPreview(true, false); guard.AdvanceProtection(0.2f);

        var temporary = new GameObject("Temporary automatic sequence verification");
        sequence.enabled = false;
        try
        {
            var clock = temporary.AddComponent<GameSequenceController>();
            Ref(clock, "aftershock", director);
            clock.Advance(74.99f);
            Check(clock.Stage == GameStage.Awakening && !director.IsRunning, "Automatic aftershock starts early.");
            clock.Advance(0.02f);
            Check(clock.Stage == GameStage.Aftershock && director.ElapsedSeconds < 0.02f, "75-second crossing uses wrong effect time.");
            clock.Advance(director.DurationSeconds+0.1f);
            Check(clock.Stage == GameStage.Isolation && !director.IsRunning && director.UnprotectedImpacts == director.ImpactCount, "Automatic aftershock did not reach Isolation.");
            clock.Advance(200f);
            Check(clock.Stage == GameStage.Isolation, "Rescue triggered without a successful call.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(temporary);
            sequence.enabled = true;
            sequence.RestoreAftershock();
        }
        Check(head.position == headPosition && head.rotation == headRotation, "Aftershock moved the HMD camera.");
        Check(UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None).Length == 1, "XR Origin count changed.");
        return new { result = "PASS", frame = Time.frameCount, duration=director.DurationSeconds,impacts=director.ImpactCount,checks = "scene wiring, head pose classification, PC guard toggle, all guarded/unprotected impacts, red feedback, limited settling, debris, dust, audio playback state, restore, four-stage preview, automatic 75-second start, no-call Rescue gate, unchanged HMD pose and single XR Origin" };
    }
}
