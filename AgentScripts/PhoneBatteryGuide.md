# Subtle phone battery usage — 2026-10-30

The phone starts at 47%. Usage accumulates in double precision rather than
subtracting whole percentage points on button presses.

| Activity | Percentage points per minute |
|---|---:|
| Flashlight on | 0.36 |
| Connecting / searching during a call attempt | 0.60 |
| Connected call | 0.24 |
| Idle, dialer open, failed-call screen | 0 |

Flashlight use stacks with the current call state. A 2.4-second failed attempt
costs 0.024 percentage points. After the attempt fails or is canceled, call
consumption stops. Consumption follows scaled game time, so pausing does not
drain the battery. Values are adjustable on `PhoneBattery` in the Inspector.

The number uses a whole-point ceiling, so small usage does not immediately change
47% to 46%. The battery icon follows fractional charge. Both are refreshed at
most once per game second and the text changes only when the displayed number
changes. This avoids a constantly ticking number or extra UI dirtying every frame.
Remaining charge is clamped at zero. This pass tracks charge and updates the
readout; it does not add phone shutdown or new game-over rules.

## Files

- `Assets/_Project/Scripts/PhoneBattery.cs`: accounting and configurable rates.
- `Assets/_Project/Scripts/PhoneBatteryUsage.cs`: existing light/call state adapter.
- `Assets/_Project/Scripts/PhoneBatteryView.cs`: existing number/icon presentation.
- `Assets/_Project/Prefabs/SmartphonePrototype.prefab` and
  `Assets/_Project/Scenes/ConfinedPrototype.unity`: component/reference wiring.
- `AgentScripts/ConfigurePhoneBattery.cs`: live-Editor configuration.
- `AgentScripts/VerifyPhoneBattery.cs`: accounting and UI checks.
- `AgentScripts/TestPhoneInteractionFeature.cs`: finds battery readout by object
  name rather than assuming its text stays at 47% forever.

## Validation

Unity compilation: zero errors. Battery checks and existing PC controls regression
passed. Checks cover no idle/failed-screen consumption, additive light/call use,
zero/negative time, fractional icon width, clamping at zero, and 60,000 tiny steps
matching a single 60-second step.

- Light for 60 simulated seconds: 46.64%, still displayed as 47%.
- Light for 180 simulated seconds: 45.92%, displayed as 46%.
- Light plus connected call for 180 simulated seconds: 45.2%.
- Live phone observation: 46.9955% while display remained 47% and icon width was
  already slightly below its initial 42.3 units.

The accelerated calculations above are not minutes of physical headset testing.
No physical headset test was performed. Existing simulator haptic warnings remain.
