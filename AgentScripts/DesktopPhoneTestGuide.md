# PC phone testing

Press Play in a scene containing the prototype phone, then click the Game view.
Without a physical HMD, PC mode starts automatically and opens **PC Phone Test**.
Reopen the window from **BreathInTheRubble > PC Phone Test**.

| Input | Action |
|---|---|
| F | Pick up the phone automatically and switch its flashlight on/off; on points ahead |
| E | Pick up / return to its original resting position |
| Right mouse drag | Look around from the same seated position |
| Left mouse drag | Rotate the held phone |
| Click phone LIGHT / SOS | Activate the actual phone action |
| Wheel | Move the held phone closer / farther |
| R | Reset held phone orientation and distance |
| Q | Toggle aiming at the view direction / free rotation |
| C | Emergency call |

The editor window has equivalent action buttons and shows hold/light status.
Keyboard/mouse shortcuts only run while the Game view has focus. There is no
walking or teleportation. PC mode temporarily suspends the camera's simulated
tracking, simulator UI and controller rays, and restores them on mode exit.
Selecting the top window button returns to the original XR simulator controls.

These tools compile only in the Editor. They do not modify the scene or phone
prefab, do not enter player builds, and refuse desktop control when a physical
HMD is present. Real hardware comfort and input behavior remain untested.

Added files:

- `Assets/_Project/Scripts/DesktopPhoneTestDriver.cs`
- `Assets/_Project/Editor/DesktopPhoneTestWindow.cs`
- `Assets/_Project/Editor/DesktopPhoneTestWindow.uss`
- `AgentScripts/InspectDesktopPhone.cs`
- `AgentScripts/TestDesktopPhoneControls.cs`

Unity also regenerated `.meta` files and the solution's editor-assembly entry in
`BreathInTheRubble.slnx` after the new Editor folder was imported.

The test script injects Input System keyboard and mouse events through the same
input polling path as the user. It checks automatic pickup/light, return/pickup,
mouse screen-button activation, rotation, fixed camera position, emergency call,
and restoration of phone physics and XR simulator/camera tracking. Temporary
test input devices are removed afterward. Run it using Unity Pipeline `script run`
with entry `TestDesktopPhoneControls.Run` during Play mode.

Validation on 2026-10-30: PASS for keyboard shortcuts, mouse LIGHT button click,
phone/view rotation, fixed camera position, physics/rest-pose restoration, XR
simulator/tracking restoration, and test-window creation. Compilation: zero errors.
The background automation test temporarily routes input to the game buffer and
restores the original Input System properties afterward. Existing simulator
haptic-capability warnings remain. No physical headset test was performed.
