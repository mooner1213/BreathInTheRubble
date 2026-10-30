using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class BuildPhoneInterface
{
    const string PhonePrefabPath = "Assets/_Project/Prefabs/SmartphonePrototype.prefab";
    const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
    const string FontPath = "Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF NoBackfaceCulling.asset";

    static readonly Color ScreenColor = new(0.008f, 0.025f, 0.034f, 0.985f);
    static readonly Color ScreenAccent = new(0.24f, 0.88f, 1f, 1f);
    static readonly Color CrackColor = new(0.76f, 0.91f, 0.96f, 0.44f);

    public static string Run()
    {
        ConfigurePhonePrefab();
        ConfigurePrototypeScene();
        AssetDatabase.SaveAssets();
        return "Phone interface built: hold-to-grab hand tracking, cracked world-space screen, 47% battery, LIGHT/SOS buttons, rear flashlight, and XR UI input.";
    }

    static void ConfigurePhonePrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PhonePrefabPath);
        try
        {
            var grab = Require<XRGrabInteractable>(root);
            var wake = Require<BreathInTheRubble.Prototype.PhoneWakeSequence>(root);
            var attach = root.GetComponentsInChildren<Transform>(true).First(item => item.name == "Grab Attach");
            attach.localPosition = new Vector3(0f, -0.025f, -0.075f);
            attach.localRotation = Quaternion.identity;
            grab.attachTransform = attach;
            grab.useDynamicAttach = false;
            grab.attachEaseInTime = 0.12f;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.trackScale = false;
            grab.throwOnDetach = false;

            var holdGrab = root.GetComponent<BreathInTheRubble.Prototype.PhoneToggleGrabMode>();
            if (holdGrab != null)
                holdGrab.enabled = true;
            var contactFilter = root.GetComponent<BreathInTheRubble.Prototype.PhoneContactOnlyGrabFilter>();
            if (contactFilter == null)
                contactFilter = root.AddComponent<BreathInTheRubble.Prototype.PhoneContactOnlyGrabFilter>();
            contactFilter.enabled = true;
            var presenter = root.GetComponent<BreathInTheRubble.Prototype.PhoneFocusPresenter>();
            if (presenter == null)
                presenter = root.AddComponent<BreathInTheRubble.Prototype.PhoneFocusPresenter>();
            presenter.enabled = false;

            var rearFlashlight = root.GetComponentsInChildren<Light>(true).First(item => item.name == "Screen Flashlight");
            rearFlashlight.name = "Rear Flashlight";
            rearFlashlight.transform.localPosition = new Vector3(-0.022f, 0.038f, 0.0105f);
            rearFlashlight.transform.localRotation = Quaternion.identity;
            rearFlashlight.type = LightType.Spot;
            rearFlashlight.color = new Color(0.88f, 0.94f, 1f, 1f);
            rearFlashlight.intensity = 8f;
            rearFlashlight.range = 4.5f;
            rearFlashlight.spotAngle = 64f;
            rearFlashlight.innerSpotAngle = 34f;
            rearFlashlight.shadows = LightShadows.Soft;
            rearFlashlight.shadowStrength = 0.78f;
            rearFlashlight.enabled = false;

            var oldCanvas = root.transform.Find("Phone Interface Canvas");
            if (oldCanvas != null)
                UnityEngine.Object.DestroyImmediate(oldCanvas.gameObject);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var canvasObject = new GameObject(
                "Phone Interface Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster),
                typeof(TrackedDeviceGraphicRaycaster));
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.SetParent(root.transform, false);
            canvasRect.localPosition = new Vector3(0f, 0f, -0.00605f);
            canvasRect.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasRect.localScale = new Vector3(-0.0001f, 0.0001f, 0.0001f);
            canvasRect.sizeDelta = new Vector2(680f, 1430f);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 10f;
            var canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            var trackedRaycaster = canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>();
            trackedRaycaster.ignoreReversedGraphics = false;
            trackedRaycaster.checkFor2DOcclusion = false;
            trackedRaycaster.checkFor3DOcclusion = false;

            var background = CreateImage("Screen Background", canvasRect, ScreenColor, false);
            Stretch(background.rectTransform);
            var dismissButton = background.gameObject.AddComponent<UnityEngine.UI.Button>();
            dismissButton.targetGraphic = background;
            dismissButton.interactable = false;
            dismissButton.transition = UnityEngine.UI.Selectable.Transition.None;
            dismissButton.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };

            CreateTopDetails(canvasRect, font);
            var flashlightButton = CreatePhoneButton(
                "Flashlight Button", canvasRect, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(38f, 42f), new Vector2(250f, 238f), new Color(0.08f, 0.18f, 0.23f, 0.96f));
            var flashlightLabel = CreateText("Label", flashlightButton.transform, font, "LIGHT", 54f, Color.white);
            SetRect(flashlightLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 78f));
            CreateFlashlightIcon(flashlightButton.transform);

            var emergencyButton = CreatePhoneButton(
                "Emergency Call Button", canvasRect, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-38f, 42f), new Vector2(250f, 238f), new Color(0.55f, 0.055f, 0.045f, 0.98f));
            var emergencyLabel = CreateText("Label", emergencyButton.transform, font, "SOS", 62f, Color.white);
            SetRect(emergencyLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 92f));
            CreateEmergencyIcon(emergencyButton.transform);

            CreateCrackedGlass(canvasRect);

            var controller = root.GetComponent<BreathInTheRubble.Prototype.PhoneInterfaceController>();
            if (controller == null)
                controller = root.AddComponent<BreathInTheRubble.Prototype.PhoneInterfaceController>();
            var presenter = root.GetComponent<BreathInTheRubble.Prototype.PhoneFocusPresenter>();
            controller.EditorConfigure(
                wake,
                rearFlashlight,
                canvasGroup,
                flashlightButton.GetComponent<UnityEngine.UI.Button>(),
                emergencyButton.GetComponent<UnityEngine.UI.Button>(),
                flashlightButton,
                emergencyButton,
                flashlightLabel,
                emergencyLabel,
                dismissButton,
                presenter);

            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, PhonePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void ConfigurePrototypeScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem eventSystem;
        if (eventSystems.Length == 0)
        {
            var eventSystemObject = new GameObject("XR UI EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }
        else
        {
            eventSystem = eventSystems[0];
            if (eventSystem.GetComponent<XRUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        foreach (var extra in eventSystems.Skip(1))
            Debug.LogWarning($"Additional EventSystem left unchanged at {extra.name}; scene should retain exactly one EventSystem.");

        foreach (var interactor in UnityEngine.Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            interactor.enableUIInteraction = true;

        EditorUtility.SetDirty(eventSystem.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void CreateTopDetails(RectTransform canvasRect, TMP_FontAsset font)
    {
        var title = CreateText("Emergency Header", canvasRect, font, "EMERGENCY", 34f, new Color(0.6f, 0.8f, 0.86f, 0.78f));
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -34f), new Vector2(250f, 60f), new Vector2(0f, 1f));
        title.alignment = TextAlignmentOptions.TopLeft;

        var battery = CreateRect("Battery 47 Percent", canvasRect);
        SetRect(battery, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -30f), new Vector2(210f, 58f), Vector2.one);
        var body = CreateRect("Battery Body", battery);
        SetRect(body, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(104f, 38f), new Vector2(0f, 0.5f));
        CreateBorder(body, 4f, new Color(0.78f, 0.9f, 0.93f, 0.9f));
        var fill = CreateImage("Battery Fill", body, new Color(0.3f, 0.9f, 0.62f, 0.95f), false);
        SetRect(fill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(42.3f, 24f), new Vector2(0f, 0.5f));
        var cap = CreateImage("Battery Cap", battery, new Color(0.78f, 0.9f, 0.93f, 0.9f), false);
        SetRect(cap.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(109f, 0f), new Vector2(6f, 18f), new Vector2(0f, 0.5f));
        var percent = CreateText("Battery Text", battery, font, "47%", 36f, Color.white);
        SetRect(percent.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(82f, 52f), new Vector2(1f, 0.5f));
        percent.alignment = TextAlignmentOptions.MidlineRight;
    }

    static UnityEngine.UI.Image CreatePhoneButton(string name, RectTransform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        var image = CreateImage(name, parent, color, true);
        SetRect(image.rectTransform, anchor, anchor, position, size, pivot);
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        return image;
    }

    static void CreateFlashlightIcon(Transform parent)
    {
        var head = CreateImage("Flashlight Head", parent, new Color(0.75f, 0.94f, 1f, 0.95f), false);
        SetRect(head.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(78f, 30f));
        var handle = CreateImage("Flashlight Handle", parent, new Color(0.75f, 0.94f, 1f, 0.95f), false);
        SetRect(handle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(38f, 56f));
        foreach (var offset in new[] { -42f, 0f, 42f })
        {
            var ray = CreateImage("Light Ray", parent, new Color(0.45f, 0.9f, 1f, 0.75f), false);
            SetRect(ray.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(offset, 95f), new Vector2(5f, 28f));
            ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, offset * 0.35f);
        }
    }

    static void CreateEmergencyIcon(Transform parent)
    {
        var line = CreateImage("Emergency Pulse", parent, new Color(1f, 0.82f, 0.76f, 0.9f), false);
        SetRect(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 66f), new Vector2(132f, 5f));
        var vertical = CreateImage("Emergency Marker", parent, new Color(1f, 0.82f, 0.76f, 0.9f), false);
        SetRect(vertical.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 66f), new Vector2(5f, 40f));
    }

    static void CreateCrackedGlass(RectTransform parent)
    {
        var overlay = CreateRect("Cracked Glass Overlay", parent);
        Stretch(overlay);

        CreateCrackCluster(overlay, new Vector2(75f, 230f), new[]
        {
            (118f, 12f, 3.2f), (92f, 58f, 3f), (145f, 107f, 3.6f), (86f, 154f, 2.8f),
            (110f, 202f, 3.2f), (72f, 252f, 2.6f), (130f, 304f, 3.1f)
        });
        CreateCrackCluster(overlay, new Vector2(-205f, -110f), new[]
        {
            (168f, 18f, 3.5f), (126f, 67f, 3f), (92f, 116f, 2.6f), (152f, 166f, 3.2f),
            (105f, 218f, 2.8f), (138f, 274f, 3.2f), (84f, 330f, 2.4f)
        });

        CreateLine(overlay, new Vector2(-85f, 520f), 520f, 78f, 3f, CrackColor);
        CreateLine(overlay, new Vector2(128f, -410f), 430f, 102f, 2.6f, new Color(CrackColor.r, CrackColor.g, CrackColor.b, 0.32f));
        CreateLine(overlay, new Vector2(-250f, 385f), 250f, 24f, 2.4f, new Color(CrackColor.r, CrackColor.g, CrackColor.b, 0.28f));
    }

    static void CreateCrackCluster(RectTransform parent, Vector2 center, (float length, float angle, float thickness)[] branches)
    {
        var impact = CreateImage("Crack Impact", parent, new Color(0.9f, 0.97f, 1f, 0.68f), false);
        SetRect(impact.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), center, new Vector2(11f, 11f));
        foreach (var branch in branches)
            CreateLine(parent, center, branch.length, branch.angle, branch.thickness, CrackColor);
    }

    static void CreateLine(RectTransform parent, Vector2 start, float length, float angle, float thickness, Color color)
    {
        var line = CreateImage("Glass Crack", parent, color, false);
        var radians = angle * Mathf.Deg2Rad;
        var center = start + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * length * 0.5f;
        SetRect(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), center, new Vector2(length, thickness));
        line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    static void CreateBorder(RectTransform parent, float thickness, Color color)
    {
        foreach (var item in new[]
        {
            ("Top", new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(parent.sizeDelta.x, thickness)),
            ("Bottom", new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(parent.sizeDelta.x, thickness)),
            ("Left", new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(thickness, parent.sizeDelta.y)),
            ("Right", new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(thickness, parent.sizeDelta.y))
        })
        {
            var line = CreateImage(item.Item1, parent, color, false);
            SetRect(line.rectTransform, item.Item2, item.Item2, item.Item3, item.Item4);
        }
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, string value, float fontSize, Color color)
    {
        var rect = CreateRect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    static UnityEngine.UI.Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var item = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)item.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static T Require<T>(GameObject root) where T : Component
    {
        return root.GetComponent<T>() ?? throw new InvalidOperationException($"{typeof(T).Name} is missing from {PhonePrefabPath}.");
    }
}
