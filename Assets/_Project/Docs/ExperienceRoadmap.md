# BreathInTheRubble — experience direction

Persistent design notes from the user, 2026-10-30.

- Seated single-player VR, roughly 4–6 minutes. Four stages: Awakening,
  Aftershock, Isolation, Rescue. No walking or teleportation.
- Preserve physical head and controller tracking. Convey shaking with moving
  environmental fragments, structural audio and light controller haptics.
- During aftershock impacts the player protects the head with both arms. Infer
  this from both tracked controllers near the sides/crown of the tracked HMD.
  If unprotected, show a brief red peripheral hit effect with a clear center.
- Aftershock: warning rumble, dense lingering dust, repeated showers of pooled
  falling/rolling concrete pieces, structural movement, and staged ceiling collapse. Preserve reachable phone
  and water bottle pockets. Never drive the HMD pose for the effect.
- Later: occasional distant human screams. Use stage/tension-dependent scheduling
  and a minimum interval; do not continuously spam random screams.
- Later: rats briefly crossing rubble/crevices along authored routes. No rat model,
  movement or scream audio is implemented now. `FutureIncidentHooks` is available
  as a future extension; it is not connected in the current scene.
- Phone: SOS opens a keypad prefilled with 119; a second call action attempts it.
  Zero reception fails. 90–115 seconds: unstable 0/1 bars with usable windows;
  115–120 seconds: one bar; 120+ seconds: two bars. Recovery does not auto-redial.
- Battery slowly drains from flashlight and call use. When the displayed charge
  reaches 24% or lower, color its number and complete icon red and show a small
  on-phone popup until dismissed. Keep emergency-call controls usable. Restore
  the original colors above 24%; warn again on the next transition to low charge.

The ConfinedPrototype scene now has the aftershock components connected. The
PC Phone Test window shows stage, elapsed time and guarded/unprotected impacts.
Its 24% battery button holds and wakes the phone and can reopen a dismissed
warning for repeated testing. Runtime warnings appear once per low-charge episode.

The current sequence starts an eighteen-second aftershock at 75 game seconds.
The first impact is at 3.2 seconds, leaving time to raise both hands. The original
4.8/6.6-second impacts are followed by secondary impacts at 10.2/13.4/16.2 seconds.
A successful 119 connection now records a sent rescue request, retained after
hangup or later reception loss. At 240 game seconds, outside rescue-radio cues
begin during Isolation: eight audible seconds followed by eighteen quiet seconds
before the next opportunity. These are initial pacing values. Rescue requires a
new held-pipe impact while a cue is actually playing AND the sent request; time
and a successful phone call alone no longer trigger Rescue. Missed opportunities
repeat, and earlier/out-of-window or unheld-pipe impacts are not banked.

The 60-second rescue ending uses outside radio/work audio only, with no visible
rescuers or camera movement. Nineteen existing overhead rubble objects lift and
move aside in sequence. Daylight enters the opening, grows with the cleared
area. Shadowed directional sunlight and HDR bloom now appear through the first
opening; glare and final whiteout follow cleared area rather than a delayed
eleven-second timer. Major slabs take 9.5 seconds per removal, with layered slabs
carried out together so a hidden ceiling layer does not block the new opening.
An ending-only HDR pipeline/renderer supplies the missing post-process resources;
the original Performance pipeline stays unchanged. URP
exposure and lightweight quarter-resolution bloom enable during the ending only;
reset restores the original camera and rubble state. Prototype radio/noise and
work WAVs are replaceable; intelligible operator/rescuer dialogue remains later
audio work. The editor's Ending Start/Restart button bypasses gameplay conditions
for visual review. See SunlitRescueRevision.md for the latest visual revision and
RescueEndingReport.md for the original condition implementation.

Awakening starts with 0.6 seconds of blackout and a 3.4-second soft opening-eye
reveal. This is a camera-child overlay; head/controller tracking is never moved.
The overlay also covers the phone canvas and switches its renderer off when done.
The phone screen now wakes at startup and finishes its existing 0.7-second fade
before the first visible eyelid slit. The alarm still begins at five seconds.
For the first four seconds, a wider screen glow lights the foreground dust from
the phone's resting position. It fades back to the normal screen spill over the
next two seconds. The slit loses its extra blackout sooner so the blue-lit haze
can be seen while the eyelids are still visibly opening. The phone stays grabbable
at its existing resting position; the flashlight starts off.

The atmosphere uses light-scattering URP particles. The phone spotlight illuminates
dust within its real cone, including distance falloff and light shadows. Soft
depth intersections, rounded/noisy density and near-eye fading hide billboard
edges. The room keeps its black ambient/fog; dust no longer adds a self-lit grey
fog sheet. A very faint cool light through rubble and minimal residual scattering
leave occasional dust silhouettes with the flashlight off. All dust systems use finite bursts with no loops or continuous emission.
Startup haze density is 0.9, aftershock density is 1.9. Each episode holds its haze for 20 seconds, fades over the next 10 seconds and
clears its particles at 30 seconds. Smaller, dimmer flecks drift slowly downward.
During the aftershock, 116 reused concrete fragments (64 original, 28 rear and
24 later rear fragments) fall in the opening and subsequent fracture waves.
Aftershock fine dust bursts are 225 at onset, 150 on each primary impact and 45
on each later impact (25% more). The broad veil emits 28 initially and 22 per
primary fracture. These use bounded capacities of 760 fine particles and 90
veil particles; the shared 20-second hold / 10-second settling clock is unchanged.
There are eight local fracture dust plumes (three front, two rear and three
later cracks), plus the denser near-field veil. Its 30-second dust clock starts
when the aftershock begins; ending the eighteen-second event does not restart that clock.
Particle opacity and fog fade together and restore the original fog settings.

Ceiling panels release in three accelerating breakaways, with staggered tilt and
pauses between impacts. The maximum authored drop is 14 cm, reduced around the
tracked head by the clearance calculation. Wall movement is now 1.8 cm plus
impact pulses, compared with 3 mm before. Fifteen additional structure groups,
including the front rubble seal, now share a coherent room tremor. Desktop
testing adds a small fast camera vibration through the desktop adapter; that
adapter is disabled for physical HMDs. In VR, effects do not modify tracked
HMD/controller poses. The 116 small debris pieces have no colliders. Six larger
wall skins use gravity and convex collisions against low rubble and the floor,
ignoring tracked devices and grabbable props. Cosmetic debris hides if it approaches tracked
devices. The original scene, lighting assets, XR rig and phone interactions
remain available in the pre-pass backup.

The bottle-side rear roof now has three additional breakaway panels. They buckle
and drop with the existing fracture impacts, using the original ceiling concrete
material. The 28 additional fragments fall beside the bottle pocket with at least
24 cm horizontal clearance from its center; the new roof geometry retains about
25 cm above the bottle top at full collapse in the desktop inspection. These
cosmetic additions do not add colliders or modify the bottle grab setup.

Three later rear fracture sections open at 10.2/13.4/16.2 seconds. They now cut
the actual existing rear wall meshes, preserving the original exterior surfaces,
UVs and material before failure. Each wall sheds two asymmetric concrete skins:
a short attachment-edge failure is followed by gravity, tumbling and contact
with low rubble. Reinforcing steel remains fixed inside the wall, with both ends
embedded in the surviving concrete. It becomes visible as concrete falls away;
it never unfolds or grows toward the player. Small chips and local dust originate
at those same fracture surfaces. The 52 rear chips land on low rubble surfaces
and use the existing wall palette. The former floating four-part patches are
preserved inactive. Steel and stationary wall geometry are consolidated per
section to reduce renderer and collider counts. The original ceiling completes its primary collapse on the original
three impacts; it is not stretched across the later waves. Rumble and room
vibration continue through the longer event and fade out at the end. Exposed
cracks/steel remain visible in Isolation and restore on an editor replay/reset.

Tune timing and comfort on a real device before treating these values as final.
