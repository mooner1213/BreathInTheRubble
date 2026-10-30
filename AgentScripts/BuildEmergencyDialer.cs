using System;
using System.Linq;
using BreathInTheRubble.Prototype;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildEmergencyDialer
{
    const string PrefabPath="Assets/_Project/Prefabs/SmartphonePrototype.prefab";
    static TMP_FontAsset font;
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();
        var phone=UnityEngine.Object.FindFirstObjectByType<PhoneInterfaceController>();
        if(phone==null)throw new Exception("Active scene phone missing.");
        // Preserve the current in-memory scene, including edits that predate this feature.
        if(!AssetDatabase.IsValidFolder("Assets/_Project/Scenes/Backups"))AssetDatabase.CreateFolder("Assets/_Project/Scenes","Backups");
        string backup="Assets/_Project/Scenes/Backups/BeforeEmergency_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Could not preserve current scene.");
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try{Configure(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        Configure(phone.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "119 dialer added to "+scene.path+" and smartphone prefab; preserved="+backup;
    }

    static void Configure(GameObject phone)
    {
        var canvas=phone.transform.Find("Phone Interface Canvas") as RectTransform;
        if(canvas==null)throw new Exception("Phone canvas missing.");
        font=canvas.GetComponentsInChildren<TMP_Text>(true).First().font;
        var system=phone.GetComponent<EmergencyCallSystem>()??phone.AddComponent<EmergencyCallSystem>();
        var progression=phone.GetComponent<PhoneReceptionProgression>()??phone.AddComponent<PhoneReceptionProgression>();
        var progressionData=new SerializedObject(progression);progressionData.FindProperty("callSystem").objectReferenceValue=system;progressionData.ApplyModifiedProperties();
        var existing=canvas.Find("Emergency Dialer");
        if(existing!=null)
        {
            var controllerData=new SerializedObject(phone.GetComponent<PhoneInterfaceController>());
            controllerData.FindProperty("emergencyDialer").objectReferenceValue=phone.GetComponent<PhoneDialerView>();controllerData.ApplyModifiedProperties();return;
        }
        var barsRoot=Rect("Reception Bars",canvas,new Vector2(18f,650f),new Vector2(84f,48f));
        var bars=new UnityEngine.UI.Image[4];
        for(int i=0;i<4;i++)
        {
            bars[i]=Image("Signal Bar "+(i+1),barsRoot,new Vector2(-27+i*18f,-12f+i*4f),new Vector2(12f,12f+i*8f),new Color(0.65f,0.88f,0.72f),false);
            bars[i].enabled=false;
        }
        var header=canvas.Find("Emergency Header").GetComponent<TMP_Text>();header.text="NO SERVICE";
        var panel=Image("Emergency Dialer",canvas,new Vector2(0f,-50f),new Vector2(660f,1230f),new Color(0.008f,0.025f,0.034f,1f),true).rectTransform;
        Text("Dialer Title",panel,new Vector2(0f,490f),new Vector2(580f,70f),"EMERGENCY CALL",46f);
        var number=Text("Dial Number",panel,new Vector2(-30f,365f),new Vector2(460f,130f),"119",112f);
        var delete=Button("Dial Delete",panel,new Vector2(255f,365f),new Vector2(95f,90f),"DEL",32f,new Color(0.08f,0.15f,0.18f));
        var status=Text("Dial Status",panel,new Vector2(0f,250f),new Vector2(580f,112f),"NO MOBILE NETWORK",36f);
        status.textWrappingMode=TextWrappingModes.Normal;
        var digits=new UnityEngine.UI.Button[12];string[] labels={"1","2","3","4","5","6","7","8","9","*","0","#"};
        for(int i=0;i<12;i++)digits[i]=Button("Dial Key "+labels[i],panel,new Vector2(-190f+(i%3)*190f,95f-(i/3)*132f),new Vector2(160f,110f),labels[i],56f,new Color(0.055f,0.105f,0.13f));
        var call=Button("Dial Confirm Call",panel,new Vector2(0f,-455f),new Vector2(550f,112f),"CALL 119",48f,new Color(0.045f,0.31f,0.18f));
        var back=Button("Dial Back",panel,new Vector2(0f,-570f),new Vector2(370f,76f),"BACK",36f,new Color(0.075f,0.13f,0.16f));
        var dialer=phone.GetComponent<PhoneDialerView>()??phone.AddComponent<PhoneDialerView>();
        var data=new SerializedObject(dialer);
        void Ref(string name,UnityEngine.Object value)=>data.FindProperty(name).objectReferenceValue=value;
        Ref("callSystem",system);Ref("dialerPanel",panel.gameObject);Ref("numberText",number);Ref("statusText",status);Ref("serviceText",header);
        Ref("callButton",call);Ref("callButtonText",call.GetComponentInChildren<TMP_Text>());Ref("backButton",back);Ref("deleteButton",delete);
        var home=data.FindProperty("homeControls");home.arraySize=2;home.GetArrayElementAtIndex(0).objectReferenceValue=canvas.Find("Flashlight Button").gameObject;home.GetArrayElementAtIndex(1).objectReferenceValue=canvas.Find("Emergency Call Button").gameObject;
        var keys=data.FindProperty("digitButtons");keys.arraySize=12;for(int i=0;i<12;i++)keys.GetArrayElementAtIndex(i).objectReferenceValue=digits[i];
        var signal=data.FindProperty("signalBars");signal.arraySize=4;for(int i=0;i<4;i++)signal.GetArrayElementAtIndex(i).objectReferenceValue=bars[i];
        data.ApplyModifiedProperties();
        var controller=new SerializedObject(phone.GetComponent<PhoneInterfaceController>());controller.FindProperty("emergencyDialer").objectReferenceValue=dialer;controller.ApplyModifiedProperties();
        panel.gameObject.SetActive(false);
        canvas.Find("Cracked Glass Overlay")?.SetAsLastSibling();
    }

    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 position,Vector2 size,Color color,bool raycast)
    {
        var rect=Rect(name,parent,position,size);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=raycast;return image;
    }
    static TMP_Text Text(string name,Transform parent,Vector2 position,Vector2 size,string value,float fontSize)
    {
        var rect=Rect(name,parent,position,size);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=fontSize;text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;text.color=new Color(0.8f,0.9f,0.93f);text.raycastTarget=false;return text;
    }
    static UnityEngine.UI.Button Button(string name,Transform parent,Vector2 position,Vector2 size,string value,float fontSize,Color color)
    {
        var image=Image(name,parent,position,size,color,true);var button=image.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
        Text("Label",image.transform,Vector2.zero,size-Vector2.one*8f,value,fontSize);return button;
    }
}
