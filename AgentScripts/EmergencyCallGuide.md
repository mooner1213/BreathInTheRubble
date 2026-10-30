# Emergency call system — 2026-10-30

SOS now opens a keypad with **119 prefilled**. It does not place the call.
Press **CALL 119** to attempt a connection. At the start all signal bars are
hidden, the header says NO SERVICE, and an attempt fails after 2.4 seconds with
NO SIGNAL / UNABLE TO CONNECT. RETRY CALL remains available.

The current prototype has no stage controller. `PhoneReceptionProgression`
starts an unstable 0/1-bar phase at 90 seconds. Brief one-bar openings last 1.2–2.6
seconds, outages last 1.5–3.2 seconds, and every third opening lasts 5.5–7.5 seconds
to leave an opportunity to complete a call. Durations vary on each run without
changing Unity's global random state. At 115 seconds it settles at one bar; at
120 seconds it transitions from one to two stable bars. Thresholds, duration
ranges, the five-second settling interval, and automatic recovery are Inspector fields.
One bar permits a slow 4-second connection; two bars take 3 seconds. Recovery
does not dial automatically: the player must retry. An attempt begun without
reception still fails even if service returns during that attempt.

The phone supports number keys, DEL, BACK/cancel, hang-up, and retry. Only 119 is
accepted for the prototype. Concurrent attempts are ignored. Losing reception
during dialing or a connected call drops it. BACK cancels pending coroutines.

This is a simulated connection state. Operator speech and a rescue sequence are
not part of this feature. `EmergencyCallSystem.CallConnected` is the hook for
future dialogue / game progression. Once stage events exist, disable automatic
recovery and drive `SetSignalBars(0/1/2)` from the stage controller.

## PC testing

- Play, click Game, press C: hold the phone and open the 119 screen.
- Enter: press the dialer's call / end-call action.
- Mouse: click the keypad, DEL, CALL, and BACK directly on the phone.
- PC Phone Test also has 0/1/2-bar override buttons for immediate testing.
  An override lasts until Play mode restarts. F still controls the flashlight.

## Changed files

- `Assets/_Project/Scripts/EmergencyCallSystem.cs`: call rules and connection state.
- `Assets/_Project/Scripts/PhoneReceptionProgression.cs`: adjustable timing adapter.
- `Assets/_Project/Scripts/PhoneDialerView.cs`: number screen and signal presentation.
- `Assets/_Project/Scripts/PhoneInterfaceController.cs`: SOS opens the dialer.
- `Assets/_Project/Scripts/DesktopPhoneTestDriver.cs`: Enter and pointer controls.
- `Assets/_Project/Editor/DesktopPhoneTestWindow.cs`: dial / signal test controls.
- `Assets/_Project/Prefabs/SmartphonePrototype.prefab` and
  `Assets/_Project/Scenes/ConfinedPrototype.unity`: added dialer / bars / components.
- `AgentScripts/BuildEmergencyDialer.cs`, `InspectEmergencyPhone.cs`,
  `VerifyEmergencyDialer.cs`, `PreviewEmergencyPhone.cs`: authoring / validation.
- Existing `TestDesktopPhoneControls.cs` and `TestPhoneInteractionFeature.cs`
  now verify the two-step emergency flow.

The previous in-memory scene, including unsaved edits, was preserved at
`Assets/_Project/Scenes/Backups/BeforeEmergency_20261030_111655.unity`.
Existing phone brightness, 112-degree flashlight, XR Origin, input managers and
physical tracking configuration were retained. Unity generated `.meta` files.

## Validation

- Unity compilation completed with zero errors.
- Prefilled 119, opening without dialing, hidden zero-service bars, failure/retry.
- Tracked-device UI ray clicks the new call button.
- One-bar connection, signal-loss disconnect, invalid number, digit editing,
  cancel/back, reopening, and 90/120-second progression checks passed.
- Two-bar connection also reached Connected after the normal connection delay.
- PC keyboard/mouse regression passed, including Enter confirmation/cancel,
  flashlight clicks, view/phone rotation, and physics/tracking restoration.
- Phone close-up render inspected at `Temp/EmergencyPhoneCloseup.png`.
- No real headset test was performed. Existing simulator haptic warnings remain.

## Unstable reception update

Modified `PhoneReceptionProgression.cs` and this guide; added
`AgentScripts/VerifyReceptionFluctuation.cs`. Existing zero-service call failure
and disconnection rules also apply during automatic fluctuations. PC signal
override buttons still hold a fixed bar count for testing until automatic mode
is resumed or Play mode restarts.

Validation: 12 simulated patterns passed, with 8–10 changes during the weak phase
and longest usable one-bar windows of 5.50–7.25 seconds. Checks include natural
loss canceling a pending call, no automatic redial, the stable final one-bar
bridge, exact 120-second transition to two bars, and manual override / resume.
The existing PC controls regression passed; Unity compilation reported zero errors.
