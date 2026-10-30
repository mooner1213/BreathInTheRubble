# Buried surroundings and seated lower body — 2026-10-30

Updated `Assets/_Project/Scenes/ConfinedPrototype.unity`.

- Added overlapping fractured concrete plates and rubble banks on both sides and
  behind the seated player, with smaller inner fractures and exposed steel ends.
- Added an authored seated waist/pelvis and two horizontal trousered thighs.
  These are stationary visual geometry for the trapped pose, not a tracked avatar.
- Added close rubble at both hips and across the distal thighs/knees. Repositioned
  the older front/lap slabs so some proximal trousers remain visible while the
  knees disappear beneath rubble. Resettled small fragments on the revised surfaces.
- Retained the original shell as the outer enclosure. No original asset was deleted.
- Kept the camera/rig pose and all HMD/controller tracking settings unchanged.
  Phone, emergency calls, reception, battery, bottle and persistent dust remain.

The new objects live under `Prototype Environment/Buried Surroundings`. Final
addition: 82 static renderers and 38 colliders, using existing rubble meshes and
materials wherever possible. The original rubble mesh assets were not changed.
Only the new seated body meshes and two cloth materials were created.

## Files

- Modified: `Assets/_Project/Scenes/ConfinedPrototype.unity`.
- New art: `Assets/_Project/Art/BuriedSurroundings/` — pelvis and thigh meshes,
  dusty trouser material and seam material, with Unity-generated metadata.
- Authoring: `AgentScripts/BuildBuriedSurroundings.cs` and
  `AgentScripts/RefineBuriedSurroundings.cs`.
- Inspection: `AgentScripts/InspectBuriedViews.cs` and
  `AgentScripts/VerifyBuriedSurroundings.cs`.
- Scene state before the pass was preserved in
  `Assets/_Project/Scenes/Backups/BeforeBuriedSurroundings_20261030_161815.unity`.

The authoring scripts use the live Editor; they refuse to duplicate their roots.
Run the base build, refinement, then `RefineBuriedSurroundings.FinalTouch` when
rebuilding a fresh copy. Existing lap box colliders on three retuned slabs were
disabled and matching static mesh colliders were added for surface contact.

## Validation

- Unity compilation: zero errors.
- Five rendered directions inspected from the actual initial eye position
  (0, 0.90, 0): front, left, right, rear, and down.
- `Temp/Buried_front.png`, `Buried_left.png`, `Buried_right.png`,
  `Buried_rear.png`, `Buried_down.png`: inspection renders use a temporary fill
  light; the saved game's dark lighting was not changed.
- Initial 0.12 m head sphere: no environment overlap.
- Phone and bottle: no penetration against the newly added colliders.
- Initial horizontal ray distances: left rubble 0.41 m, right rubble 0.36 m,
  rear rubble 0.64 m. Looking straight down hits the seated pelvis rather than
  the bare floor (0.52 m below the eye).
- Exactly one XR Origin and one Input Action Manager; locomotion remains disabled.
- Existing confined-space test passes: bottle select/release, 45% water volume
  under rotation, two-minute particle simulation and head clearance.
- PC phone regression passes: light toggle and screen click, hold/return,
  phone/view rotation, 119 dialer/Enter, and tracking/physics restoration.

No physical headset comfort/reach test or target-device performance measurement
was performed. Static lower-body geometry does not follow a changing seated hip
position; the room still assumes the authored trapped starting pose. Existing
simulated-controller haptic warnings remain. Play mode was stopped after tests.
