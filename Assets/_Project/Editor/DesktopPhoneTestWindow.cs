using BreathInTheRubble.Prototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BreathInTheRubble.EditorTools
{
    [InitializeOnLoad]
    public sealed class DesktopPhoneTestWindow : EditorWindow
    {
        VisualElement controls;
        Label status;
        Button holdButton, flashlightButton, aimButton;
        static DesktopPhoneTestDriver Driver => Object.FindFirstObjectByType<DesktopPhoneTestDriver>();

        static DesktopPhoneTestWindow()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                if (Object.FindFirstObjectByType<PhoneInterfaceController>() == null || DesktopPhoneTestDriver.HasPhysicalHmd) return;
                if (Driver == null)
                {
                    var root = new GameObject("Editor Desktop Phone Test");
                    root.AddComponent<DesktopPhoneTestDriver>().SetDesktopMode(true);
                }
                var previous = focusedWindow;
                Open();
                if (previous != null) previous.Focus();
            };
        }

        [MenuItem("BreathInTheRubble/PC Phone Test")]
        public static void Open()
        {
            var window = GetWindow<DesktopPhoneTestWindow>();
            window.titleContent = new GUIContent("PC Phone Test");
            window.minSize = new Vector2(320f, 650f);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Project/Editor/DesktopPhoneTestWindow.uss");
            if (stylesheet != null) rootVisualElement.styleSheets.Add(stylesheet);
            rootVisualElement.AddToClassList("phone-test");
            var title = new Label("PC 휴대폰 테스트"); title.AddToClassList("title");rootVisualElement.Add(title);
            status = new Label();status.AddToClassList("status");rootVisualElement.Add(status);
            rootVisualElement.Add(new Button(() =>
            {
                if(!EditorApplication.isPlaying){EditorApplication.EnterPlaymode();return;}
                if(Driver==null)new GameObject("Editor Desktop Phone Test").AddComponent<DesktopPhoneTestDriver>();
                Driver.SetDesktopMode(!Driver.DesktopEnabled);
            }) {text="PC 테스트 시작 / XR 시뮬레이터로 전환"});
            controls = new VisualElement();rootVisualElement.Add(controls);
            holdButton = AddButton("E · 휴대폰 집기",()=>Driver.ToggleHold());
            flashlightButton = AddButton("F · 라이트 켜기",()=>Driver.ToggleFlashlight());
            AddButton("C · 긴급통화",()=>Driver.EmergencyCall());
            AddButton("Enter · 119 통화 / 종료",()=>Driver.ConfirmEmergencyCall());
            AddButton("테스트 · 통신 0칸",()=>Driver.PreviewReception(0));
            AddButton("테스트 · 통신 1칸",()=>Driver.PreviewReception(1));
            AddButton("테스트 · 통신 2칸",()=>Driver.PreviewReception(2));
            aimButton = AddButton("Q · 시선 방향으로 비추기",()=>Driver.ToggleAim());
            AddButton("R · 휴대폰 방향 초기화",()=>Driver.ResetPhonePose());
            var help = new Label("Game 창을 클릭한 다음 조작하세요.\n\n오른쪽 마우스 드래그 · 둘러보기\n왼쪽 마우스 드래그 · 휴대폰 돌리기\n화면의 LIGHT / SOS · 마우스로 클릭\n마우스 휠 · 휴대폰 거리 조절\n\nF는 휴대폰을 자동으로 집어 라이트를 켭니다.\nE를 다시 누르면 원래 자리에 돌려놓습니다.");
            help.AddToClassList("help");rootVisualElement.Add(help);
            rootVisualElement.schedule.Execute(Refresh).Every(100);
            Refresh();
        }

        Button AddButton(string text,System.Action action)
        {
            var button = new Button(()=> {if(Driver!=null && Driver.DesktopEnabled)action();Refresh();}){text=text};controls.Add(button);return button;
        }

        void Refresh()
        {
            if(status==null)return;
            var driver=Driver;bool active=EditorApplication.isPlaying && driver!=null && driver.DesktopEnabled;
            controls.SetEnabled(active);
            status.text=!EditorApplication.isPlaying?"Play를 누르거나 위 시작 버튼을 누르세요.":DesktopPhoneTestDriver.HasPhysicalHmd?"실제 HMD 연결됨 · PC 조작 비활성":!active?"XR 시뮬레이터 조작 중":$"PC 조작 중 · {(driver.IsHolding?"집은 상태":"원래 위치")} · 라이트 {(driver.Phone.FlashlightEnabled?"ON":"OFF")}";
            holdButton.text=active && driver.IsHolding?"E · 원래 자리에 돌려놓기":"E · 휴대폰 집기";
            flashlightButton.text=active && driver.Phone.FlashlightEnabled?"F · 라이트 끄기":"F · 라이트 켜기";
            aimButton.text=active && driver.AimWithView?"Q · 자유롭게 돌리기":"Q · 시선 방향으로 비추기";
        }
    }
}
