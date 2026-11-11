using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class InspectAwakeningLighting
{
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var scene=SceneManager.GetActiveScene();var path="Assets/_Project/Scenes/Backups/BeforeAwakeningLighting_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";EditorSceneManager.SaveScene(scene,path,true);
        return new{backup=path,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,
            ambient=new{mode=RenderSettings.ambientMode.ToString(),color=RenderSettings.ambientLight.ToString("F4"),intensity=RenderSettings.ambientIntensity,sky=RenderSettings.ambientSkyColor.ToString("F4"),equator=RenderSettings.ambientEquatorColor.ToString("F4"),ground=RenderSettings.ambientGroundColor.ToString("F4"),fog=RenderSettings.fog,fogColor=RenderSettings.fogColor.ToString("F4"),fogDensity=RenderSettings.fogDensity},
            camera=new{position=Camera.main.transform.position.ToString("F3"),near=Camera.main.nearClipPlane,fov=Camera.main.fieldOfView,background=Camera.main.backgroundColor.ToString()},
            lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(l=>new{name=l.name,type=l.type.ToString(),enabled=l.enabled,intensity=l.intensity,range=l.range,angle=l.spotAngle,inner=l.innerSpotAngle,color=l.color.ToString("F3"),position=l.transform.position.ToString("F3"),forward=l.transform.forward.ToString("F3"),shadow=l.shadows.ToString(),parent=l.transform.parent?.name}).ToArray(),
            particles=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Select(p=>new{name=p.name,parent=p.transform.parent?.name,position=p.transform.position.ToString("F3"),duration=p.main.duration,lifetime=p.main.startLifetime.constantMax,size=p.main.startSize.constantMax,color=p.main.startColor.colorMax.ToString("F3"),capacity=p.main.maxParticles,shape=p.shape.shapeType.ToString(),scale=p.shape.scale.ToString("F3"),material=AssetDatabase.GetAssetPath(p.GetComponent<Renderer>().sharedMaterial),shader=p.GetComponent<Renderer>().sharedMaterial.shader.name}).ToArray(),
            overlay=UnityEngine.Object.FindObjectsByType<HeadImpactFeedback>(FindObjectsSortMode.None).Select(h=>new SerializedObject(h).FindProperty("edgeRenderer").objectReferenceValue.name).ToArray()};
    }
}
