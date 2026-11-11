using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
public static class BuildBuriedCell
{
    const string Art="Assets/_Project/Art/BuriedCell";
    static int id;
    public static object ToneCenter()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");var r=GameObject.Find("Center crushed bearing stone").GetComponent<MeshRenderer>();var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++){var path=Art+"/NearStainedConcrete_"+i+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(path)!=null)throw new Exception("Already toned");var m=new Material(materials[i]);m.name="Near stained concrete";m.SetColor("_BaseColor",new Color(.12f,.12f,.115f,1));m.SetFloat("_Smoothness",.03f);AssetDatabase.CreateAsset(m,path);materials[i]=m;}r.sharedMaterials=materials;EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return "Near concrete preserves texture in close phone light";
    }
    public static object CloseCenter()
    {
        if(Application.isPlaying||GameObject.Find("Center crushed bearing stone")!=null)throw new Exception("Edit mode and unapplied center required");id=63;Solid(GameObject.Find("Close buried cell").transform,"Wall_Grey_Small_02","Center crushed bearing stone",new Vector3(0,.875f,.53f),new Vector3(.26f,.12f,.23f),new Vector3(88,4,6));EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return "Center breathing slot packed with a close bearing stone";
    }
    static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedProperties();}
    public static object Finish()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");var root=GameObject.Find("Close buried cell").transform;if(GameObject.Find("Concealed close seam chips")!=null)throw new Exception("Already finished");
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var so=new SerializedObject(ending);var rubble=so.FindProperty("rubble");var starts=so.FindProperty("removalStarts");var offsets=so.FindProperty("removalOffsets");var blockers=so.FindProperty("apertureBlockers");
        string[] names={"Low continuous overburden","Overlapping front ceiling lip","Low overlapping fracture veil"};
        for(int i=0;i<names.Length;i++){var target=GameObject.Find(names[i]).transform;int index=rubble.arraySize;rubble.arraySize++;starts.arraySize++;offsets.arraySize++;rubble.GetArrayElementAtIndex(index).objectReferenceValue=target;starts.GetArrayElementAtIndex(index).floatValue=31+i*7;offsets.GetArrayElementAtIndex(index).vector3Value=new Vector3(i%2==0?-1.5f:1.5f,.35f,.75f);int b=blockers.arraySize;blockers.arraySize++;blockers.GetArrayElementAtIndex(b).objectReferenceValue=target.GetComponent<Renderer>();}so.ApplyModifiedProperties();
        id=9;var host=new GameObject("Concealed close seam chips");host.transform.SetParent(root,false);var settled=new GameObject("Permanent close seam fragments");settled.transform.SetParent(root,false);var pocket=host.AddComponent<RubbleFailurePocket>();Assign(pocket,"aftershock",UnityEngine.Object.FindFirstObjectByType<AftershockDirector>());
        var ignored=Camera.main.transform.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).ToArray();
        var random=new System.Random(8103);Func<float,float,float> R=(a,b)=>a+(float)random.NextDouble()*(b-a);var pieces=new System.Collections.Generic.List<AftershockFallingChunk>();var releaseTimes=new System.Collections.Generic.List<float>();
        for(int i=0;i<54;i++)
        {
            bool plate=i%9==0;float x=(i%2==0?-1:1)*R(.06f,.20f);var center=new Vector3(x,R(1.31f,1.41f),R(.78f,.85f));var size=plate?new Vector3(R(.11f,.16f),R(.018f,.033f),R(.08f,.12f)):new Vector3(R(.025f,.075f),R(.005f,.014f),R(.02f,.05f));
            var go=Solid(host.transform,i%3==0?"Bricks_Medium_01":"Wall_Grey_Small_02","Close seam "+(plate?"plate ":"chip ")+i,center,size,new Vector3(R(-12,12),R(0,360),R(-12,12)));go.GetComponent<MeshCollider>().enabled=false;var box=go.AddComponent<BoxCollider>();box.center=go.GetComponent<MeshFilter>().sharedMesh.bounds.center;box.size=go.GetComponent<MeshFilter>().sharedMesh.bounds.size;box.contactOffset=.001f;
            var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.mass=plate?.4f:.02f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.angularDamping=.8f;body.maxDepenetrationVelocity=.25f;body.sleepThreshold=.015f;
            var piece=go.AddComponent<AftershockFallingChunk>();so=new SerializedObject(piece);so.FindProperty("detachedRoot").objectReferenceValue=settled.transform;so.FindProperty("head").objectReferenceValue=Camera.main.transform;var tracked=Camera.main.transform.root.GetComponentsInChildren<Transform>(true);so.FindProperty("leftHand").objectReferenceValue=tracked.First(t=>t.name=="Left Controller");so.FindProperty("rightHand").objectReferenceValue=tracked.First(t=>t.name=="Right Controller");var p=so.FindProperty("ignoreColliders");p.arraySize=ignored.Length;for(int j=0;j<ignored.Length;j++)p.GetArrayElementAtIndex(j).objectReferenceValue=ignored[j];so.FindProperty("releaseVelocity").vector3Value=new Vector3(R(-.10f,.10f),-.04f,R(-.85f,-.55f));so.FindProperty("releaseAngularVelocity").vector3Value=new Vector3(R(-2,2),R(-1,1),R(-2,2));so.FindProperty("attachmentSeconds").floatValue=.025f;so.FindProperty("colliderWhileAttached").boolValue=false;so.ApplyModifiedProperties();box.enabled=false;pieces.Add(piece);releaseTimes.Add(new[]{2.84f,4.13f,6.72f,9.11f,12.3f,15.02f}[i/9]+R(0,.95f));
        }
        so=new SerializedObject(pocket);var chips=so.FindProperty("chips");var times=so.FindProperty("chipReleaseTimes");chips.arraySize=times.arraySize=pieces.Count;for(int i=0;i<pieces.Count;i++){chips.GetArrayElementAtIndex(i).objectReferenceValue=pieces[i];times.GetArrayElementAtIndex(i).floatValue=releaseTimes[i];}so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{foregroundFragments=54,closeCeilingRemovedDuringRescue=3};
    }
    static GameObject Solid(Transform parent,string source,string name,Vector3 center,Vector3 size,Vector3 angles)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/Props/"+source+".prefab");var original=prefab.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var raw=original.GetComponent<MeshFilter>().sharedMesh;var mesh=UnityEngine.Object.Instantiate(raw);var b=mesh.bounds;
        var matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(angles),new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z))*Matrix4x4.Translate(-b.center);mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();mesh.normals=mesh.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();var offset=mesh.bounds.center;mesh.vertices=mesh.vertices.Select(v=>v-offset).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name=name;AssetDatabase.CreateAsset(mesh,Art+"/CloseRubble_"+(id++)+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=center;go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=original.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();r.shadowCastingMode=ShadowCastingMode.TwoSided;var shape=go.AddComponent<MeshCollider>();shape.sharedMesh=mesh;shape.contactOffset=.001f;go.AddComponent<PipeImpactSurface>();return go;
    }
    public static object Tune()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");var root=GameObject.Find("Close buried cell").transform;if(GameObject.Find("Low overlapping fracture veil")!=null)throw new Exception("Already tuned");id=8;
        GameObject.Find("Front face left pressed rubble").transform.position+=Vector3.down*.12f;
        GameObject.Find("Front face right interlocked masonry").transform.position+=Vector3.down*.10f;
        Solid(root,"Wall_DarkGrey_Medium_01","Low overlapping fracture veil",new Vector3(.01f,1.185f,.53f),new Vector3(.96f,.16f,.27f),new Vector3(-2,5,-3));
        var drop=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var so=new SerializedObject(drop);var lip=(Transform)so.FindProperty("lipPose").objectReferenceValue;var release=(Transform)so.FindProperty("releasePose").objectReferenceValue;lip.position=new Vector3(0,1.055f,.83f);release.position=new Vector3(-.025f,1.045f,.34f);
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();so=new SerializedObject(ending);var p=so.FindProperty("lightBlockers");var rs=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();p.arraySize=rs.Length;for(int i=0;i<rs.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=rs[i];so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{lip=lip.position.ToString("F3"),release=release.position.ToString("F3")};
    }
    public static object Build()
    {
        if(Application.isPlaying||GameObject.Find("Close buried cell")!=null||AssetDatabase.IsValidFolder(Art))throw new Exception("Edit mode and unapplied scene required");
        AssetDatabase.CreateFolder("Assets/_Project/Art","BuriedCell");var root=new GameObject("Close buried cell");root.transform.SetParent(GameObject.Find("Interlocked Rubble Structure").transform,false);
        Solid(root.transform,"Wall_Grey_Big_01","Low continuous overburden",new Vector3(-.02f,1.28f,-.015f),new Vector3(1.22f,.20f,.90f),new Vector3(1,4,-3));
        Solid(root.transform,"Wall_Gray_Medium_02","Overlapping front ceiling lip",new Vector3(.04f,1.265f,.38f),new Vector3(.91f,.17f,.29f),new Vector3(-4,-7,2));
        Solid(root.transform,"Wall_DarkGrey_Medium_01","Left close shoulder canopy",new Vector3(-.41f,1.16f,.38f),new Vector3(.40f,.17f,.69f),new Vector3(0,8,70));
        Solid(root.transform,"Wall_Gray_Medium_03","Right close shoulder canopy",new Vector3(.42f,1.15f,.39f),new Vector3(.42f,.17f,.69f),new Vector3(0,-10,-73));
        Solid(root.transform,"Wall_Gray_Medium_02","Front face left pressed rubble",new Vector3(-.18f,.88f,.565f),new Vector3(.47f,.17f,.48f),new Vector3(86,8,-9));
        Solid(root.transform,"Bricks_Big_02","Front face right interlocked masonry",new Vector3(.22f,.85f,.59f),new Vector3(.40f,.18f,.45f),new Vector3(83,-12,7));
        Solid(root.transform,"Wall_Grey_Small_02","Left slit buried cheek",new Vector3(-.39f,1.07f,.64f),new Vector3(.24f,.17f,.38f),new Vector3(66,19,12));
        Solid(root.transform,"Bricks_Medium_01","Right slit buried cheek",new Vector3(.40f,1.06f,.67f),new Vector3(.24f,.18f,.35f),new Vector3(77,-16,-9));
        // Match the previous embedded steel to the newly nearer front faces.
        GameObject.Find("Reachable ribbed rebar striker").transform.position+=new Vector3(0,0,-.12f);
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();var so=new SerializedObject(ending);var p=so.FindProperty("lightBlockers");var rs=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();p.arraySize=rs.Length;for(int i=0;i<rs.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=rs[i];so.ApplyModifiedProperties();
        Undo.RegisterCreatedObjectUndo(root,"Fill the close trapped air pocket");EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return new{closedNativePieces=id,head=Camera.main.transform.position.ToString("F3"),cameraUnchanged=true};
    }
}
