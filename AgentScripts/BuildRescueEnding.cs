using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;

public static class BuildRescueEnding
{
    const string Art="Assets/_Project/Art/RescueEnding";
    static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value)
    {var d=new SerializedObject(target);d.FindProperty(field).objectReferenceValue=value;d.ApplyModifiedPropertiesWithoutUndo();}
    static AudioSource Source(string name,Transform parent,Vector3 position,float volume)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;
        var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;source.volume=volume;source.minDistance=.65f;source.maxDistance=7f;source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Logarithmic;
        return source;
    }
    static AudioClip Cue(string name,float seconds,int kind)
    {
        const int rate=22050;int count=Mathf.CeilToInt(rate*seconds);var random=new System.Random(19021+kind);float low=0f,slow=0f;
        var path=Art+"/"+name+".wav";
        using(var stream=File.Create(path))using(var w=new BinaryWriter(stream))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate,n=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,n,.23f);slow=Mathf.Lerp(slow,n,.012f);float sample;
                if(kind<2)
                {
                    float syllable=.25f+.75f*Mathf.Pow(Mathf.Abs(Mathf.Sin(t*8.9f)*Mathf.Sin(t*4.7f)),.6f);
                    float burst=.18f+.82f*Mathf.SmoothStep(0f,1f,(Mathf.Sin(t*3.4f)+.3f)*1.5f);
                    float staticBed=(low-slow)*.24f;
                    float chatter=(Mathf.Sin(t*(137f+24f*Mathf.Sin(t*7f))*2*Mathf.PI)*.06f+(low-slow)*.15f)*syllable*burst;
                    float beep=t<.14f || (t>.29f&&t<.41f) || (t>seconds-.30f&&t<seconds-.14f)?Mathf.Sin(t*(kind==0?1280:980)*2*Mathf.PI)*.15f:0;
                    sample=staticBed+chatter+beep;
                }
                else
                {
                    float scrape=Mathf.Pow(Mathf.Max(0f,Mathf.Sin(t*1.45f)),3f);
                    float gravel=Mathf.Pow(Mathf.Max(0f,Mathf.Sin(t*13.3f)*Mathf.Sin(t*6.4f)),7f);
                    float liftPulse=Mathf.Pow(Mathf.Max(0f,Mathf.Cos((t-3)*Mathf.PI*.61f)),18f);
                    sample=low*(.035f+.17f*scrape)+slow*.15f+(n-low)*gravel*.18f+Mathf.Sin(t*165f*2*Mathf.PI)*liftPulse*.10f;
                }
                sample*=Mathf.Clamp01(t/.035f)*Mathf.Clamp01((seconds-t)/.15f);
                w.Write((short)(Mathf.Clamp(sample,-.9f,.9f)*32767));
            }
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("ConfinedPrototype required.");
        if(UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>()!=null||AssetDatabase.IsValidFolder(Art))throw new Exception("Rescue ending already exists; refusing to overwrite or duplicate.");
        var sequence=UnityEngine.Object.FindFirstObjectByType<GameSequenceController>();var pipe=UnityEngine.Object.FindFirstObjectByType<PipeImpactAudio>();var camera=Camera.main;
        if(sequence==null||pipe==null||camera==null||Shader.Find("BreathInTheRubble/Rescue Whiteout")==null)throw new Exception("Existing setup or whiteout shader missing.");
        string backup="Assets/_Project/Scenes/Backups/BeforeRescueEnding_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Scene backup failed.");
        AssetDatabase.CreateFolder("Assets/_Project/Art","RescueEnding");
        var radio=Cue("OutsideSearchRadio_Prototype",8f,0);var response=Cue("RescuerAcknowledgementRadio_Prototype",2.8f,1);var work=Cue("RubbleRemovalWork_Prototype",34f,2);
        var root=new GameObject("Rescue Ending");root.transform.SetParent(sequence.transform,false);Undo.RegisterCreatedObjectUndo(root,"Add conditional rescue ending");
        var search=Source("Outside Rescuer Search Radio",root.transform,new Vector3(-.24f,1.9f,.90f),.8f);search.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency=2200;
        var reply=Source("Outside Rescuer Response",root.transform,new Vector3(.15f,1.8f,.65f),.7f);reply.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency=2800;
        var working=Source("Overhead Rescue Work",root.transform,new Vector3(.12f,1.65f,.25f),.48f);
        var relay=pipe.gameObject.AddComponent<PipeRescueSignalRelay>();
        var gate=root.AddComponent<RescueSignalController>();Ref(gate,"sequence",sequence);Ref(gate,"pipeSignals",relay);Ref(gate,"searchSource",search);Ref(gate,"searchClip",radio);
        var ending=root.AddComponent<RescueEndingDirector>();Ref(ending,"sequence",sequence);Ref(ending,"workSource",working);Ref(ending,"workClip",work);Ref(ending,"responseSource",reply);Ref(ending,"responseClip",response);
        var all=GameObject.Find("Prototype Environment").GetComponentsInChildren<Transform>(true);
        var primary=new[]{"Overhead Broken Slab Left","Overhead Broken Slab Right","Low Ceiling Front","Low Ceiling Rear","Broken Ceiling Lip","Ceiling Crevice","Rear Ceiling Broken Corner","Rear Breakaway Ceiling Panel 1","Rear Breakaway Ceiling Panel 2","Rear Breakaway Ceiling Panel 3"};
        var selected=primary.Select(n=>all.First(t=>t.name==n)).Concat(all.Where(t=>t.name=="Upper Rear Wedged Chunk"||t.name=="Rubble 07"||t.name=="Rubble 08"||t.name=="Rubble 09"||t.name=="Rubble 10"||t.name.StartsWith("Front Seal Chunk 08")||t.name.StartsWith("Front Seal Chunk 09")||t.name.StartsWith("Front Seal Chunk 10"))).Distinct().ToArray();
        var d=new SerializedObject(ending);var targets=d.FindProperty("rubble");var offsets=d.FindProperty("removalOffsets");var times=d.FindProperty("removalStarts");targets.arraySize=offsets.arraySize=times.arraySize=selected.Length;
        for(int i=0;i<selected.Length;i++)
        {
            var t=selected[i];t.gameObject.isStatic=false;targets.GetArrayElementAtIndex(i).objectReferenceValue=t;
            bool wide=t.name=="Low Ceiling Front"||t.name=="Low Ceiling Rear";
            float side=(t.position.x<0?-1f:1f)*(wide?1.9f:1.05f);
            offsets.GetArrayElementAtIndex(i).vector3Value=new Vector3(side,1.05f+(i%3)*.12f,t.position.z<0?-.45f:.60f);
            times.GetArrayElementAtIndex(i).floatValue=i<primary.Length?3f+i*1.0f:8f+(i-primary.Length)*.65f;
        }
        d.ApplyModifiedPropertiesWithoutUndo();
        var lightGo=new GameObject("Daylight Through Rescue Opening");lightGo.transform.SetParent(root.transform,false);lightGo.transform.position=new Vector3(-.02f,2.65f,.30f);lightGo.transform.rotation=Quaternion.LookRotation(new Vector3(0,.40f,.20f)-lightGo.transform.position);
        var beam=lightGo.AddComponent<Light>();beam.type=LightType.Spot;beam.color=new Color(1,.98f,.92f);beam.range=5f;beam.spotAngle=18;beam.innerSpotAngle=12;beam.intensity=0;beam.shadows=LightShadows.Soft;beam.shadowCustomResolution=512;beam.shadowNearPlane=.08f;beam.enabled=false;Ref(ending,"openingLight",beam);
        var fillGo=new GameObject("Rescue Opening Soft Bounce");fillGo.transform.SetParent(root.transform,false);fillGo.transform.position=new Vector3(0,1.12f,.1f);var fill=fillGo.AddComponent<Light>();fill.type=LightType.Point;fill.color=new Color(1,.98f,.94f);fill.range=2f;fill.intensity=0;fill.shadows=LightShadows.None;fill.enabled=false;Ref(ending,"softFill",fill);
        var sky=GameObject.CreatePrimitive(PrimitiveType.Quad);sky.name="Daylight Beyond Removed Rubble";sky.transform.SetParent(root.transform,false);sky.transform.SetPositionAndRotation(new Vector3(0,2.9f,.2f),Quaternion.Euler(90,0,0));sky.transform.localScale=new Vector3(4,4,1);UnityEngine.Object.DestroyImmediate(sky.GetComponent<Collider>());
        var skyMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Soft Exterior Daylight"};skyMaterial.SetColor("_BaseColor",new Color(3.5f,3.5f,3.3f,1));skyMaterial.SetFloat("_Cull",0);AssetDatabase.CreateAsset(skyMaterial,Art+"/ExteriorDaylight.mat");
        var skyRenderer=sky.GetComponent<MeshRenderer>();skyRenderer.sharedMaterial=skyMaterial;skyRenderer.shadowCastingMode=ShadowCastingMode.Off;skyRenderer.enabled=false;Ref(ending,"outsideSky",skyRenderer);
        var overlay=GameObject.CreatePrimitive(PrimitiveType.Quad);overlay.name="Rescue Gradual Whiteout";overlay.transform.SetParent(camera.transform,false);float distance=Mathf.Max(.025f,camera.nearClipPlane+.015f);overlay.transform.localPosition=Vector3.forward*distance;overlay.transform.localScale=Vector3.one*distance*12f;UnityEngine.Object.DestroyImmediate(overlay.GetComponent<Collider>());
        var whiteMaterial=new Material(Shader.Find("BreathInTheRubble/Rescue Whiteout")){name="Rescue Whiteout"};AssetDatabase.CreateAsset(whiteMaterial,Art+"/RescueWhiteout.mat");
        var overlayRenderer=overlay.GetComponent<MeshRenderer>();overlayRenderer.sharedMaterial=whiteMaterial;overlayRenderer.shadowCastingMode=ShadowCastingMode.Off;overlayRenderer.receiveShadows=false;overlayRenderer.enabled=false;Ref(ending,"whiteout",overlayRenderer);
        var volumeGo=new GameObject("Rescue Exposure and Bloom");volumeGo.transform.SetParent(root.transform,false);var volume=volumeGo.AddComponent<Volume>();volume.isGlobal=true;volume.priority=20;volume.weight=0;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();var color=profile.Add<ColorAdjustments>(true);color.postExposure.value=0;
        var bloom=profile.Add<Bloom>(true);bloom.threshold.value=1.05f;bloom.intensity.value=.15f;bloom.scatter.value=.65f;bloom.downscale.value=BloomDownscaleMode.Quarter;bloom.maxIterations.value=3;bloom.highQualityFiltering.value=false;
        AssetDatabase.CreateAsset(profile,Art+"/RescueEndingVolume.asset");foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);volume.sharedProfile=profile;Ref(ending,"endingVolume",volume);
        Ref(ending,"view",camera.GetComponent<UniversalAdditionalCameraData>());
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed.");
        return new{backup,conditions="119 request sent AND held-pipe impact during audible outside radio",firstSearch=sequence.FirstRescueSearchSeconds,radioSeconds=radio.length,quietInterval=18,endingSeconds=ending.DurationSeconds,removedObjects=selected.Select(t=>t.name),newCharacters=0,uiAdded=false,volume=Art+"/RescueEndingVolume.asset",cameraPostProcessing="Enabled during ending only"};
    }
    public static object ScopePostProcessing()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var data=Camera.main.GetComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=false;
        Ref(UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>(),"view",data);
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{postProcessing="Restored original false; enabled/restored by ending only"};
    }
    public static object WidenOpening()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var d=new SerializedObject(ending);var targets=d.FindProperty("rubble");var offsets=d.FindProperty("removalOffsets");
        for(int i=0;i<targets.arraySize;i++)
        {
            var t=(Transform)targets.GetArrayElementAtIndex(i).objectReferenceValue;bool wide=t.name=="Low Ceiling Front"||t.name=="Low Ceiling Rear";
            offsets.GetArrayElementAtIndex(i).vector3Value=new Vector3((t.position.x<0?-1f:1f)*(wide?1.9f:1.05f),1.05f+(i%3)*.12f,t.position.z<0?-.45f:.60f);
        }
        d.ApplyModifiedPropertiesWithoutUndo();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{adjusted=targets.arraySize,wideSlabLateralRemoval=1.9f};
    }
}
