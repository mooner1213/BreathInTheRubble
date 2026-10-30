# Phone light balance — 2026-10-30

Applied to `Assets/_Project/Scenes/ConfinedPrototype.unity` and the shared
`Assets/_Project/Prefabs/SmartphonePrototype.prefab` through the live Editor.

- Screen interface opacity/brightness: 1 → 0.55. A serialized `screenBrightness`
  field in `Assets/_Project/Scripts/PhoneInterfaceController.cs` preserves this
  setting when the screen wakes.
- Screen emission RGB: (0.240, 0.820, 1.400) → (0.048, 0.164, 0.280).
- Screen spill maximum intensity: 0.35 → 0.08 cd.
- Rear flashlight: outer cone 64° → 112°, inner cone 34° → 55°.
- Rear flashlight intensity: 8 → 3 cd to reduce close-range clipping; range stays 4.5 m.

`AgentScripts/TunePhoneLightBalance.cs` applies these values to the active phone
and prefab without replacing the UI, materials or interaction setup.

Validation: Unity compilation completed with zero errors. Existing desktop input
regression passed (F/E/C/R, screen LIGHT click, rotation, XR/physics restoration).
The playing frame counter was advancing. Actual game-camera render was inspected
at `Assets/Temp/PhoneWideLightPreview.png`; no diagnostic lighting was added.
Play mode was stopped afterward and the Run In Background override was removed.
No physical headset testing was performed.
