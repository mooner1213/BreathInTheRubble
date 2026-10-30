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
