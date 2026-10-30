using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;

public static class TestDesktopPhoneControls
{
    // Pipeline commands run in an Editor update. Pump the game input buffer explicitly.
    static void PumpInput() => typeof(InputSystem).GetMethod("Update", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
        null, new[]{typeof(InputUpdateType)}, null).Invoke(null, new object[]{InputUpdateType.Dynamic});
    public static string Preview()
    {
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();
        if(!driver.DesktopEnabled)driver.SetDesktopMode(true);
        if(!driver.Phone.FlashlightEnabled)driver.ToggleFlashlight();
        return "Desktop phone in hand, flashlight on.";
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static object Run()
    {
        Require(Application.isPlaying,"Play mode required.");
        var driver=UnityEngine.Object.FindFirstObjectByType<DesktopPhoneTestDriver>();
        Require(driver!=null && driver.DesktopEnabled,"Desktop controls did not start automatically.");
        var cam=Camera.main;var cameraPosition=cam.transform.position;var cameraRotation=cam.transform.rotation;
        var phone=driver.Phone;
        driver.ReturnPhone();phone.SetFlashlight(false);phone.GetComponent<PhoneDialerView>()?.Close();
        var restPosition=phone.transform.position;var restRotation=phone.transform.rotation;
        var body=phone.GetComponent<Rigidbody>();var restKinematic=body.isKinematic;var restGravity=body.useGravity;var restCollisions=body.detectCollisions;
        var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;
        var settings=InputSystem.settings;
        var originalEditorInput=settings.editorInputBehaviorInPlayMode;
        var originalBackground=settings.backgroundBehavior;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        Keyboard keyboard=null;Mouse mouse=null;
        try
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Require(EditorWindow.focusedWindow!=null && EditorWindow.focusedWindow.GetType().Name=="GameView","Game view could not receive keyboard focus.");
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
            PumpInput();
            foreach(var key in new[]{Key.E,Key.F,Key.C,Key.Q,Key.R,Key.Enter}) {var prime=keyboard[key].wasPressedThisFrame;}
            {var prime=mouse.leftButton.wasPressedThisFrame;var release=mouse.leftButton.wasReleasedThisFrame;}
            void KeyPress(Key key)
            {
                InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(mouse);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));PumpInput();keyboard.MakeCurrent();mouse.MakeCurrent();
                Require(keyboard[key].wasPressedThisFrame,"Test keyboard input not processed: enabled="+keyboard.enabled+", focused="+Application.isFocused+", pressed="+keyboard[key].isPressed);
                driver.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());PumpInput();
            }
            KeyPress(Key.F);
            Require(driver.IsHolding && phone.FlashlightEnabled,"F failed: hold="+driver.IsHolding+", light="+phone.FlashlightEnabled+", focus="+EditorWindow.focusedWindow?.GetType().Name);
            var light=phone.GetComponentsInChildren<Light>(true).First(l=>l.name=="Rear Flashlight");
            Require(light.enabled && Vector3.Dot(light.transform.forward,cam.transform.forward)>0.98f,"Flashlight is not aimed ahead.");
            Require(Vector3.Distance(phone.transform.position,cam.transform.position)<0.55f,"Phone is outside usable desktop reach.");
            KeyPress(Key.F);Require(!phone.FlashlightEnabled,"Second F did not switch off light.");
            var button=phone.transform.Find("Phone Interface Canvas/Flashlight Button").GetComponent<RectTransform>();
            var screen=RectTransformUtility.WorldToScreenPoint(cam,button.TransformPoint(button.rect.center));
            InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1,position=screen});PumpInput();driver.SendMessage("Update");
            Require(phone.FlashlightEnabled,"Mouse click on phone LIGHT button did not turn on flashlight.");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});PumpInput();driver.SendMessage("Update");
            KeyPress(Key.F);
            var originalAngles=driver.PhoneAngles;
            InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1,delta=new Vector2(40f,20f)});PumpInput();driver.SendMessage("Update");
            Require((driver.PhoneAngles-originalAngles).sqrMagnitude>1f,"Left mouse drag did not rotate phone.");
            InputSystem.QueueStateEvent(mouse,new MouseState());PumpInput();
            var phoneRotation=phone.transform.rotation;
            InputSystem.QueueStateEvent(mouse,new MouseState{buttons=2,delta=new Vector2(50f,-15f)});PumpInput();driver.SendMessage("Update");driver.SendMessage("LateUpdate");
            Require(Quaternion.Angle(cameraRotation,cam.transform.rotation)>1f,"Right mouse drag did not look around.");
            Require(Vector3.Distance(cam.transform.position,cameraPosition)<0.0001f,"Desktop camera translated.");
            InputSystem.QueueStateEvent(mouse,new MouseState());PumpInput();
            KeyPress(Key.R);Require(driver.PhoneAngles==Vector3.zero,"R did not reset phone angles.");
            KeyPress(Key.C);Require(phone.EmergencyCallRequested,"C did not request emergency call.");
            var dialer=phone.GetComponent<PhoneDialerView>();
            Require(dialer!=null && dialer.IsOpen && dialer.Number=="119" && dialer.CallSystem.State==EmergencyCallSystem.CallState.Idle,"C must open the 119 dialer without calling.");
            KeyPress(Key.Enter);Require(dialer.CallSystem.State==EmergencyCallSystem.CallState.Connecting,"Enter did not confirm the 119 call.");
            dialer.Close();Require(dialer.CallSystem.State==EmergencyCallSystem.CallState.Idle,"Back must cancel the pending call.");
            KeyPress(Key.E);Require(!driver.IsHolding,"E did not return phone.");
            Require(Vector3.Distance(phone.transform.position,restPosition)<0.0001f && Quaternion.Angle(phone.transform.rotation,restRotation)<0.01f,"Rest pose not restored.");
            Require(body.isKinematic==restKinematic && body.useGravity==restGravity && body.detectCollisions==restCollisions,"Physics state not restored.");
            KeyPress(Key.E);Require(driver.IsHolding,"E did not pick up phone again.");
            driver.SetDesktopMode(false);
            Require(!driver.IsHolding && !driver.DesktopEnabled,"Mode switch did not release phone.");
            Require(cam.GetComponent<TrackedPoseDriver>().enabled,"Camera tracking not restored.");
            Require(UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None).Any(b=>b.GetType().Name=="XRInteractionSimulator" && b.enabled),"XR simulator not restored.");
            driver.SetDesktopMode(true);
            return new {result="PASS",keyboard="F auto pickup/light on/off; E pick/return; R reset; C emergency",mouse="LIGHT screen button click; left drag rotates phone; right drag rotates view",cameraTranslation="zero",restoration="phone rest pose, physics, XR camera tracking and simulator restored",uiWindow=Resources.FindObjectsOfTypeAll<EditorWindow>().Any(w=>w.GetType().Name=="DesktopPhoneTestWindow")};
        }
        finally
        {
            phone.GetComponent<PhoneDialerView>()?.Close();
            driver.ReturnPhone();
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);
            oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();
            settings.editorInputBehaviorInPlayMode=originalEditorInput;settings.backgroundBehavior=originalBackground;
        }
    }
}
