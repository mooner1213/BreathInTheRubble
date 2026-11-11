using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class TuneRescueSunlight
{
    static T Ref<T>(UnityEngine.Object target,string field) where T:UnityEngine.Object => new SerializedObject(target).FindProperty(field).objectReferenceValue as T;
    public static object Inspect()
    {
        var urp=UniversalRenderPipeline.asset;
        return new{pipeline=urp?.name,hdr=urp?.supportsHDR,mainShadows=urp?.supportsMainLightShadows,shadowDistance=urp?.shadowDistance,additionalShadows=urp?.supportsAdditionalLightShadows,sun=RenderSettings.sun?.name,
            lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Select(l=>new{l.name,l.enabled,l.type,l.intensity}),
            camera=Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing};
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("ConfinedPrototype required.");
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();if(ending==null)throw new Exception("Existing ending missing.");
        var urp=UniversalRenderPipeline.asset;if(urp==null||!urp.supportsMainLightShadows)throw new Exception("URP and main-light shadows required.");
        var shader=Shader.Find("BreathInTheRubble/Rescue Sun Shafts");if(shader==null)throw new Exception("Sun shaft shader missing.");
        var backup="Assets/_Project/Scenes/Backups/BeforeSlowerSunlitRescue_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed.");
        var data=new SerializedObject(ending);data.FindProperty("removalSeconds").floatValue=9.5f;data.FindProperty("durationSeconds").floatValue=60f;
        const string hdrPath="Assets/_Project/Art/RescueEnding/RescueHDRPipeline.asset";
        var hdr=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(hdrPath);
        if(hdr==null){hdr=UnityEngine.Object.Instantiate(urp);hdr.name="Rescue HDR Pipeline";hdr.supportsHDR=true;hdr.shadowDistance=Mathf.Max(6f,hdr.shadowDistance);AssetDatabase.CreateAsset(hdr,hdrPath);}
        const string rendererPath="Assets/_Project/Art/RescueEnding/RescueRenderer.asset";
        var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if(rendererData==null)
        {
            var original=(UniversalRendererData)new SerializedObject(urp).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue;
            rendererData=UnityEngine.Object.Instantiate(original);rendererData.name="Rescue HDR and Bloom Renderer";
            var standalone=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Project Configuration/Standalone Preset.asset");
            var post=new SerializedObject(standalone).FindProperty("postProcessData").objectReferenceValue;
            if(post==null)throw new Exception("Existing post-process resources missing.");
            var rd=new SerializedObject(rendererData);rd.FindProperty("postProcessData").objectReferenceValue=post;rd.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(rendererData,rendererPath);
        }
        var pipelineData=new SerializedObject(hdr);pipelineData.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=rendererData;pipelineData.FindProperty("m_MainLightShadowmapResolution").intValue=2048;pipelineData.ApplyModifiedPropertiesWithoutUndo();
        data.FindProperty("endingPipeline").objectReferenceValue=hdr;
        var starts=data.FindProperty("removalStarts");var targets=data.FindProperty("rubble");
        float[] primary={3,14,14,3,26,14,37,44,47,50};
        for(int i=0;i<starts.arraySize;i++)starts.GetArrayElementAtIndex(i).floatValue=i<primary.Length?primary[i]:7f+(i-primary.Length)*4.5f;
        var env=GameObject.Find("Prototype Environment");
        var blockers=env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.gameObject.activeInHierarchy&&r.bounds.max.y>1.03f&&r.bounds.min.y<2f&&r.shadowCastingMode!=ShadowCastingMode.Off).ToArray();
        var array=data.FindProperty("lightBlockers");array.arraySize=blockers.Length;for(int i=0;i<blockers.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=blockers[i];
        var sun=Ref<Light>(ending,"openingLight");sun.type=LightType.Directional;sun.transform.rotation=Quaternion.LookRotation(new Vector3(-.22f,-1f,-.28f));sun.color=new Color(1,.96f,.86f);sun.intensity=4.5f;sun.shadows=LightShadows.Soft;sun.shadowBias=.025f;sun.shadowNormalBias=.12f;sun.enabled=false;
        var sky=Ref<MeshRenderer>(ending,"outsideSky");const string skyPath="Assets/_Project/Art/RescueEnding/SunlitExterior.mat";
        var skyMaterial=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if(skyMaterial==null){skyMaterial=new Material(sky.sharedMaterial){name="Strong Sunlit Exterior"};skyMaterial.SetColor("_BaseColor",new Color(22,20.5f,18,1));AssetDatabase.CreateAsset(skyMaterial,skyPath);}sky.sharedMaterial=skyMaterial;
        const string shaftPath="Assets/_Project/Art/RescueEnding/SunShafts.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(shaftPath);
        if(material==null){material=new Material(shader){name="Shadowed Rescue Sun Shafts"};AssetDatabase.CreateAsset(material,shaftPath);}
        var renderer=Ref<MeshRenderer>(ending,"sunShafts");
        if(renderer==null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Sunlight Scattering Through Open Rubble";go.transform.SetParent(Camera.main.transform,false);float distance=Mathf.Max(.04f,Camera.main.nearClipPlane+.025f);go.transform.localPosition=Vector3.forward*distance;go.transform.localScale=Vector3.one*distance*12f;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        renderer.sharedMaterial=material;renderer.enabled=false;data.FindProperty("sunShafts").objectReferenceValue=renderer;data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new{backup,duration=60,individualLift=9.5f,mainSlabStarts=primary.Take(5),sun="Strong directional daylight, actual moving slab shadows",blockerSamples=blockers.Length,scattering="12 shadow samples along each visible ray; ending only",whiteout="Driven by aperture/glare, no independent delayed fade"};
    }
    public static async Task<object> Review()
    {
        if(!Application.isPlaying||DesktopPhoneTestDriver.HasPhysicalHmd)throw new Exception("PC Play Mode required.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();
        var previousPipeline=QualitySettings.renderPipeline;bool enabled=sequence.enabled;sequence.enabled=false;
        var go=new GameObject("Temporary sunlit ending review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.transform.SetPositionAndRotation(Camera.main.transform.position,Quaternion.Euler(-22,0,0));
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.requiresDepthTexture=true;data.volumeLayerMask=1;
        var frames=new System.Collections.Generic.List<object>();Directory.CreateDirectory("Temp/SunlitRescueReview");
        try
        {
            sequence.RestoreAftershock();sequence.BeginAftershockNow();sequence.Advance(18.1f);UnityEngine.Object.FindFirstObjectByType<HeadImpactFeedback>().Clear();UnityEngine.Object.FindFirstObjectByType<AftershockDustEffect>().Clear();
            foreach(var p in sequence.GetComponentsInChildren<ParticleSystem>(true))p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            sequence.PreviewNextStage();
            foreach(float seconds in new[]{0f,8f,18f,30f,42f,54f,60f})
            {
                ending.Advance(seconds-ending.ElapsedSeconds);await Task.Delay(120);
                string path="Temp/SunlitRescueReview/At_"+seconds.ToString("00")+"s.png";var brightness=Save(camera,path);
                frames.Add(new{seconds,ending.OpeningAmount,ending.SunlightAdmission,ending.GlareAmount,ending.WhiteoutAmount,brightness,path});
            }
            if(!ending.IsComplete||ending.WhiteoutAmount<.999f)throw new Exception("Opening did not complete in white.");
            sequence.RestoreAftershock();if(QualitySettings.renderPipeline!=previousPipeline)throw new Exception("Pipeline was not restored.");
            return new{result="PASS",frames,pipelineRestored=true};
        }
        finally{sequence.RestoreAftershock();sequence.enabled=enabled;UnityEngine.Object.DestroyImmediate(go);}
    }
    static object Save(Camera camera,string path)
    {
        var prior=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(1000,850,24,RenderTextureFormat.ARGBHalf);var image=new Texture2D(1000,850,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1000,850),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            var colors=image.GetPixels32();double total=0;int bright=0;foreach(var c in colors){float value=(c.r*.2126f+c.g*.7152f+c.b*.0722f)/255f;total+=value;if(value>.95f)bright++;}
            return new{mean=total/colors.Length,over95percent=bright/(float)colors.Length};
        }
        finally{camera.targetTexture=prior;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
