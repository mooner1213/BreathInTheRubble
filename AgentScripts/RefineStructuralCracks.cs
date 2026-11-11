using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using BreathInTheRubble.Prototype;
public static class RefineStructuralCracks
{
    public static object UseLitSurface()
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/StructuralAftershock/ConcreteStressCrack.mat");material.shader=Shader.Find("Universal Render Pipeline/Lit");material.SetFloat("_Smoothness",0);material.SetFloat("_Metallic",0);EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();return "Cracks receive scene light";
    }
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        int total=0;
        foreach(var effect in UnityEngine.Object.FindObjectsByType<RubbleStressCrack>(FindObjectsSortMode.None))
        {
            var data=new SerializedObject(effect);var refs=data.FindProperty("lines");
            for(int i=0;i<refs.arraySize;i++){var old=(LineRenderer)refs.GetArrayElementAtIndex(i).objectReferenceValue;old.enabled=false;}
            var host=effect.transform.parent;var shape=host.GetComponent<MeshCollider>();
            Vector3 a,b;int seed;
            if(effect.name.StartsWith("Ceiling")){a=new Vector3(-.27f,0,.27f);b=new Vector3(.25f,0,.29f);seed=901;}
            else if(effect.name.StartsWith("Front")){a=new Vector3(-.22f,0,.65f);b=new Vector3(.26f,0,.68f);seed=148;}
            else{a=new Vector3(-.57f,0,.02f);b=new Vector3(-.57f,0,.49f);seed=483;}
            var random=new System.Random(seed);float Next(float low,float high)=>low+(float)random.NextDouble()*(high-low);
            var direction=(b-a).normalized;var side=Vector3.Cross(direction,Vector3.up);var paths=new List<Vector3[]>();var main=new List<Vector3>();float u=0,offset=0;
            while(u<1){offset=Mathf.Clamp(offset+Next(-.010f,.010f),-.026f,.026f);main.Add(Vector3.Lerp(a,b,u)+side*offset);u+=Next(.018f,.065f);}main.Add(b);paths.Add(main.ToArray());
            foreach(float fraction in new[]{.28f,.57f,.78f})
            {
                var start=main[Mathf.RoundToInt((main.Count-1)*fraction)];float sign=Next(0,1)>.5f?1:-1;var branch=new List<Vector3>{start};var p=start;
                for(int i=0;i<6;i++){p+=direction*Next(.003f,.012f)+side*sign*Next(.004f,.014f);branch.Add(p);}paths.Add(branch.ToArray());
            }
            var lines=new List<LineRenderer>();int pathIndex=0;
            foreach(var path in paths)
            {
                var points=new List<Vector3>();
                void Flush()
                {
                    if(points.Count>1)
                    {
                        var child=new GameObject("Irregular surface fissure "+lines.Count);child.transform.SetParent(effect.transform,false);var line=child.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Count;line.SetPositions(points.ToArray());line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/StructuralAftershock/ConcreteStressCrack.mat");line.numCornerVertices=1;line.numCapVertices=1;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
                        var keys=new List<Keyframe>();float scale=pathIndex==0?1:.38f;for(int i=0;i<points.Count;i++)keys.Add(new Keyframe(i/(float)(points.Count-1),i==0||i==points.Count-1?.06f:Next(.3f,1.1f)*scale));line.widthCurve=new AnimationCurve(keys.ToArray());line.widthMultiplier=0;line.enabled=false;lines.Add(line);
                    }
                    points.Clear();
                }
                foreach(var p in path){if(shape.Raycast(new Ray(new Vector3(p.x,.85f,p.z),Vector3.up),out var hit,1.5f))points.Add(effect.transform.InverseTransformPoint(hit.point+hit.normal*.0005f));else Flush();}
                Flush();pathIndex++;
            }
            refs.arraySize=lines.Count;for(int i=0;i<lines.Count;i++)refs.GetArrayElementAtIndex(i).objectReferenceValue=lines[i];data.FindProperty("maximumWidth").floatValue=.0022f;data.ApplyModifiedProperties();total+=lines.Count;
        }
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return new{irregularSurfaceLines=total};
    }
}
