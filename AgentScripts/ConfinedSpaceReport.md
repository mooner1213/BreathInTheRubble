# Confined space pass — 2026-10-30

Open `Assets/_Project/Scenes/ConfinedPrototype.unity` and enter Play mode. The original
`Prototype.unity`, original materials and smartphone prefab were preserved.

## Changes

- Initial seated offset: 1.02 m → 0.90 m, keeping Device tracking and both controllers.
- Side walls moved inward; overhead slabs lowered; more rubble across the lap,
  hips and knees. The phone pocket remains accessible.
- Fine concrete grain, settled chips, and embedded reinforcement improve contact
  and surface detail without importing assets or adding SDKs.
- Initial cloud lifetime: 24–40 s. Motes and soft haze continuously replenish,
  capped at 90 and 28 particles respectively. Original black start and phone lighting remain.
- A 26 cm grabbable PET bottle rests behind the left shoulder at (-0.28, 0.30, -0.43).
  Its sealed water volume is 45%, and its surface stays level as the bottle rotates.

## Added files

- `Assets/_Project/Scenes/ConfinedPrototype.unity`
- `Assets/_Project/Prefabs/WaterBottle45.prefab`
- `Assets/_Project/Scripts/BottleLiquidVisual.cs`
- `Assets/_Project/Art/ConfinedConcrete.shader`
- `Assets/_Project/Art/ConfinedSpace/`: concrete and particle material variants,
  bottle materials, bottle/label meshes, and the 45% edit-time preview mesh.
- `AgentScripts/InspectConfinedSetup.cs`, `BuildConfinedSpace.cs`,
  `RefineConfinedSpace.cs`, `FinalizeBottlePreview.cs`, `VerifyConfinedSpace.cs`.

Unity generated corresponding `.meta` files. Existing unrelated working changes
were retained. `BuildConfinedSpace` creates a new variant and refuses to replace it;
the refinement and preview scripts apply to that variant only.

## Validation

- Unity compilation: zero errors. Final console: zero errors.
- Repository-wide `git diff --check` reports trailing spaces in pre-existing
  changes to `Assets/XR/Settings/OpenXRPackageSettings.asset`; that unrelated file
  was not edited by this pass.
- Play loop advanced from frame 3867 to 17735 (10.57 s → 48.12 s game time).
- Exactly one XR Origin and one Input Action Manager; locomotion providers disabled.
- Starting head position passes a 0.12 m radius environment overlap check.
- Bottle selects and releases through XR Interaction Manager, and remains on its ledge.
- Calculated water fractions at 0°, 45°, 90°, 140°, 180°:
  45.01%, 45.01%, 45.01%, 45.00%, 44.99%; cap remains horizontal.
- Particle simulation at 120 seconds retains 90 motes and 28 haze particles.
  This is an accelerated particle check, not a two-minute real-time headset test.
- Existing phone regression passes: contact-only pickup, both poke hands,
  flashlight and emergency call, unchanged camera pose during interaction.
- Front/rear render inspection performed with temporary diagnostic lighting:
  `Temp/ConfinedFrontPreview.png`, `Temp/ConfinedRearPreview.png`.
  These images are brighter than the intentionally dark saved game lighting.

## Remaining limits

No physical headset test or device performance measurement was performed. The
simulator reports unsupported haptic capability warnings. Reach, comfort, and
head clearance while leaning still need checking on the eventual target device.
The bottle is sealed; water spilling/drinking is outside this pass.

The Editor is left in Edit mode with ConfinedPrototype open. Temporary runtime
inspection objects and Play-mode overrides are removed.
