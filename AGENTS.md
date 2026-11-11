# Project: BreathInTheRubble

## Game Overview
- Unity 6000.3.22f1
- Seated first-person VR disaster survival game
- Target device is undecided
- Use OpenXR, URP, XR Interaction Toolkit and Input System
- Single-player experience lasting approximately 4–6 minutes

## Game Rules
- The player is trapped under rubble and cannot walk or teleport.
- Preserve physical HMD and both controller tracking.
- Allow grabbing a phone, water bottle and metal pipe.
- The phone has only emergency call and flashlight interactions.
- Use 4 stages: Awakening, Aftershock, Isolation, Rescue.
- Avoid traditional HUDs and large quest panels.

## Development Rules
- Inspect existing scenes, packages and prefabs before editing.
- Do not duplicate XR Origin or Input Action Managers.
- Do not introduce device-specific SDKs without approval.
- Use small, reusable C# components and clear naming.
- Keep game rules separate from XR input and visuals.
- Never delete or overwrite existing assets without approval.
- After changes, check Unity compilation errors.
- Report modified files, test results and remaining issues.
- Work on one small feature at a time.
- Do not claim headset testing without a real device.

## Safety and Performance
- Avoid forced VR camera shaking and artificial HMD movement.
- Prefer lightweight URP-compatible effects.
- Keep an editor simulation test path available.

## UI Design Review and User Feedback
- Before creating or modifying UI, read the repository-root `design.md` and apply it as the required design review baseline. Use its current contents rather than a remembered version.
- Distinguish user requirements, confirmed decisions, project constraints, proposed standards, and undecided items. Noto Sans CJK KR Regular is the confirmed common font; proposed colors, sizes, layouts, and shared asset paths are not automatically approved or already implemented.
- Inspect the existing UI, scenes, prefabs, and shared resources. Prefer existing common Theme and Prefab assets, and apply the Rubble-specific conditions in section 12.3 alongside this project's game and XR safety rules.
- First present a concrete, reviewable assessment and proposed change, with a draft or preview when useful. Explain which `design.md` rules apply and identify relevant differences, exceptions, and decisions that need feedback.
- Obtain the user's feedback on that proposal before applying the UI design changes. Once the direction is agreed, carry out the agreed work without repeatedly requesting the same confirmation.
- If the document differs from verified implementation or current user instructions, report the discrepancy during review. Do not silently undo implemented features or label them unimplemented solely because the document is outdated.
- Do not invent timers, scores, persistent gauges, large quest panels, device-specific bindings, or unimplemented UI actions. Keep phone information on the physical phone and preserve seated interaction constraints.
- Review common UI changes against both `design.md` and related shared resources. Record temporary design values as proposals, and distinguish PC checks from actual headset validation.
