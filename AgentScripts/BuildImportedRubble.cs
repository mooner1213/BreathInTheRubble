using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>Reuses imported geometry and maps without modifying vendor assets or gameplay transforms.</summary>
public static class BuildImportedRubble
{
    const string Folder="Assets/_Project/Art/ImportedRubble";
    static Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static List<string> changes=new List<string>();
    static bool refining;
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    static void Ensure(string path){if(AssetDatabase.IsValidFolder(path))return;var split=path.LastIndexOf('/');Ensure(path.Substring(0,split));AssetDatabase.CreateFolder(path.Substring(0,split),path.Substring(split+1));}

    static Material Convert(Material source)
    {
        if(materials.TryGetValue(source.name,out var result))return result;
        string path=Folder+"/Materials/"+source.name+"_URP.mat";
        if(refining){result=AssetDatabase.LoadAssetAtPath<Material>(path);if(result!=null){materials.Add(source.name,result);return result;}}
        if(AssetDatabase.LoadAssetAtPath<Material>(path)!=null)throw new Exception("Refusing to overwrite "+path);
        result=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=source.name+"_URP",enableInstancing=true};
        foreach(var pair in new[]{new[]{"_MainTex","_BaseMap"},new[]{"_BumpMap","_BumpMap"},new[]{"_MetallicGlossMap","_MetallicGlossMap"},new[]{"_OcclusionMap","_OcclusionMap"}}){
            if(!source.HasProperty(pair[0]))continue;
            result.SetTexture(pair[1],source.GetTexture(pair[0]));result.SetTextureScale(pair[1],source.GetTextureScale(pair[0]));result.SetTextureOffset(pair[1],source.GetTextureOffset(pair[0]));
        }
        result.SetColor("_BaseColor",source.GetColor("_Color"));result.SetFloat("_BumpScale",.8f);result.SetFloat("_Smoothness",.16f);result.SetFloat("_Metallic",0);result.SetFloat("_OcclusionStrength",.85f);
        if(result.GetTexture("_BumpMap")!=null)result.EnableKeyword("_NORMALMAP");
        if(result.GetTexture("_MetallicGlossMap")!=null)result.EnableKeyword("_METALLICSPECGLOSSMAP");
        if(result.GetTexture("_OcclusionMap")!=null)result.EnableKeyword("_OCCLUSIONMAP");
        AssetDatabase.CreateAsset(result,path);materials.Add(source.name,result);return result;
    }

    static void Replace(MeshFilter target,string relative)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/"+relative+".prefab");
        var source=prefab.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name.EndsWith("LOD0"));
        var src=source.sharedMesh;var old=target.sharedMesh;
        // Put the imported fragment's thin axis along the old surface's thin physical axis.
        var physical=Vector3.Scale(old.bounds.size,target.transform.lossyScale);
        int[] order={0,1,2};Array.Sort(order,(a,b)=>Mathf.Abs(physical[a]).CompareTo(Mathf.Abs(physical[b])));
        int[] permutation=new int[3];permutation[order[0]]=1;permutation[order[1]]=src.bounds.size.x<src.bounds.size.z?0:2;permutation[order[2]]=permutation[order[1]]==0?2:0;
        int inversions=0;for(int a=0;a<3;a++)for(int b=a+1;b<3;b++)if(permutation[a]>permutation[b])inversions++;
        string key=prefab.name+"_"+old.name+"_"+string.Join("",permutation)+"_"+string.Join("_",Enumerable.Range(0,3).Select(a=>Mathf.RoundToInt(Mathf.Abs(target.transform.lossyScale[a])*10000).ToString()));
        if(!meshes.TryGetValue(key,out var mesh)){
            mesh=UnityEngine.Object.Instantiate(src);mesh.name=key;
            var vs=src.vertices;var ns=src.normals;var from=src.bounds;var to=old.bounds;
            float fit=float.MaxValue;
            for(int axis=0;axis<3;axis++)fit=Mathf.Min(fit,Mathf.Abs(physical[axis])/from.size[permutation[axis]]);
            // Fit uniformly in world metres: preserve concrete thickness, bricks and rebar proportions.
            for(int i=0;i<vs.Length;i++){
                var v=vs[i]-from.center;var n=ns[i];Vector3 p=Vector3.zero,normal=Vector3.zero;
                for(int axis=0;axis<3;axis++){
                    int s=permutation[axis];float sign=axis==0 && inversions%2==1?-1:1;
                    float scale=fit/Mathf.Abs(target.transform.lossyScale[axis]);
                    p[axis]=v[s]*scale*sign+to.center[axis];
                    normal[axis]=n[s]/scale*sign;
                }
                vs[i]=p;ns[i]=normal.normalized;
            }
            mesh.vertices=vs;mesh.normals=ns;mesh.RecalculateBounds();mesh.RecalculateTangents();
            string path=Folder+"/Meshes/"+key+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing!=null){if(!refining)throw new Exception("Refusing to overwrite "+path);EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
            else AssetDatabase.CreateAsset(mesh,path);meshes.Add(key,mesh);
        }
        var renderer=target.GetComponent<MeshRenderer>();
        Undo.RecordObject(target,"Use imported rubble geometry");Undo.RecordObject(renderer,"Use imported rubble maps");
        target.sharedMesh=mesh;renderer.sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials.Select(Convert).ToArray();
        // Existing colliders and the animation roots keep their exact shape and placement.
        changes.Add(PathOf(target.transform)+" <- "+relative);
        EditorUtility.SetDirty(target);EditorUtility.SetDirty(renderer);
    }
    public static object Refine()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");refining=true;
        var scene=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/Backups/BeforeImportedRubble_20261106_094847.unity");
        var originals=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).GroupBy(f=>PathOf(f.transform)).ToDictionary(g=>g.Key,g=>g.First());
        var mappings=File.ReadAllLines("Temp/ImportedRubbleReview/Replacements.txt").Select(l=>l.Split(new[]{" <- "},StringSplitOptions.None)).ToDictionary(p=>p[0],p=>p[1]);
        int count=0;
        try{
            foreach(var f in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).ToArray()){
                var path=PathOf(f.transform);if(!mappings.TryGetValue(path,out var relative)||!originals.TryGetValue(path,out var original))continue;
                f.sharedMesh=original.sharedMesh;
                Replace(f,relative);count++;
                if(path.Contains("/Concrete Shell/")&&f.transform.Find("Structural Backing - Original")==null){
                    // A dark original backing closes gaps without changing physics or moving roots.
                    var backing=new GameObject("Structural Backing - Original");backing.transform.SetParent(f.transform,false);
                    backing.AddComponent<MeshFilter>().sharedMesh=original.sharedMesh;
                    backing.AddComponent<MeshRenderer>().sharedMaterials=original.GetComponent<MeshRenderer>().sharedMaterials;
                }
            }
        }finally{EditorSceneManager.ClosePreviewScene(source);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return new{proportionsPreserved=count};
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/_Project/Scenes/ConfinedPrototype.unity")throw new Exception("Wrong scene");
        if(AssetDatabase.IsValidFolder(Folder))throw new Exception("Imported pass already exists; refusing to duplicate");
        string backup="Assets/_Project/Scenes/Backups/BeforeImportedRubble_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
        Ensure(Folder+"/Materials");Ensure(Folder+"/Meshes");
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Replace procedural environment with imported rubble");
        var env=GameObject.Find("Prototype Environment");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.sharedMesh!=null).ToArray();
        int replaced=0,hidden=0,index=0;
        foreach(var f in all){
            string p=PathOf(f.transform);string asset=AssetDatabase.GetAssetPath(f.sharedMesh);
            if(p.Contains("Archived Floating"))continue;
            if(!p.StartsWith("Prototype Environment/")&&!p.StartsWith("Prototype Game Flow/"))continue;
            bool angular=asset.Contains("/ProceduralRubble/");
            bool embedded=asset.Contains("/EmbeddedWallFractures/")&&!f.name.Contains("Steel");
            bool shell=p.Contains("/Concrete Shell/")&&f.name!="Floor Slab";
            bool ledge=f.name=="Phone Ledge"||f.name=="Bottle Resting Slab";
            if(!angular&&!embedded&&!shell&&!ledge)continue;
            string source;
            var size=Vector3.Scale(f.sharedMesh.bounds.size,f.transform.lossyScale);float max=Mathf.Max(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));
            if(shell||p.Contains("Collapsed Slabs")||p.Contains("Collapsed Wall Layers")||p.Contains("Inner Fracture")||f.name.Contains("Ceiling Panel"))
                source="WithRebar/Rebar_Wall_"+new[]{"01","04","06","03","05"}[index++%5];
            else if(max>.32f && (p.Contains("Lower Body Rubble")||p.Contains("Close Waist")||f.name.Contains("Rubble Bank")))
                source="Piles/Rubble_Small_0"+(index++%3+1);
            else if(max>.3f)source="Props/Wall_Gray_Medium_0"+(index++%3+1);
            else if(max>.1f)source="Props/Wall_Grey_Small_0"+(index++%3+1);
            else source="Props/Brick_Small_"+(index++%13+1).ToString("00");
            if(ledge)source="Props/Wall_Grey_Big_01";
            Replace(f,source);replaced++;
        }
        // Retire the thin black crack stickers and primitive aggregate/rebar decorations; originals remain in the scene.
        foreach(var r in env.GetComponentsInChildren<MeshRenderer>(true)){
            string p=PathOf(r.transform);
            bool decoration=p.Contains("/Surface Cracks/")||p.Contains("/Exposed Aggregate/")||p.Contains("/Exposed Rebar/")||p.Contains("/Edge Crack")||p.Contains("/Hairline")||p.Contains("/Dust Scratch");
            if(!decoration||!r.enabled)continue;
            Undo.RecordObject(r,"Retire procedural surface decoration");r.enabled=false;EditorUtility.SetDirty(r);hidden++;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);
        Directory.CreateDirectory("Temp/ImportedRubbleReview");File.WriteAllLines("Temp/ImportedRubbleReview/Replacements.txt",changes);
        return new{backup,replaced,hidden,meshes=meshes.Count,materials=materials.Count,preserved="Transforms, physics shapes, XR, UI, aftershock and rescue references",folder=Folder};
    }
}
