using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;

public static class BuildLayeredBurial
{
    const string Art="Assets/_Project/Art/LayeredBurial";
    struct Shaft{public Vector2 center,radius;public Shaft(float x,float z,float rx,float rz){center=new Vector2(x,z);radius=new Vector2(rx,rz);}}
    static Shaft[] shafts={new Shaft(-.62f,.92f,.35f,.34f),new Shaft(.62f,.94f,.35f,.34f),new Shaft(-.90f,.10f,.30f,.35f),new Shaft(.90f,.10f,.30f,.35f),new Shaft(.25f,-.82f,.35f,.29f),new Shaft(0,.88f,.29f,.30f)};
    static System.Random random=new System.Random(19365);
    static float R(float a,float b)=>(float)(a+random.NextDouble()*(b-a));
    static void Ref(UnityEngine.Object o,string f,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(f).objectReferenceValue=value;s.ApplyModifiedProperties();}
    static void Refs(UnityEngine.Object o,string f,IEnumerable<UnityEngine.Object> values){var a=values.ToArray();var s=new SerializedObject(o);var p=s.FindProperty(f);p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];s.ApplyModifiedProperties();}
    static void Number(UnityEngine.Object o,string f,float value){var s=new SerializedObject(o);s.FindProperty(f).floatValue=value;s.ApplyModifiedProperties();}
    static void Vector(UnityEngine.Object o,string f,Vector3 value){var s=new SerializedObject(o);s.FindProperty(f).vector3Value=value;s.ApplyModifiedProperties();}
    static void Save(){var s=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();}
    struct Vertex{public Vector3 p,n;public Vector2 uv;public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
    static List<Vertex> Clip(List<Vertex> input,Vector3 normal,float distance,bool inside)
    {
        var output=new List<Vertex>();if(input.Count==0)return output;var prev=input[input.Count-1];float dp=Vector3.Dot(prev.p,normal)+distance;bool pin=inside?dp>=0:dp<=0;
        foreach(var cur in input){float dc=Vector3.Dot(cur.p,normal)+distance;bool cin=inside?dc>=0:dc<=0;if(cin!=pin)output.Add(Vertex.Lerp(prev,cur,dp/(dp-dc)));if(cin)output.Add(cur);prev=cur;dp=dc;pin=cin;}return output;
    }
    static IEnumerable<List<Vertex>> Subtract(List<Vertex> polygon,Shaft shaft)
    {
        var rim=Enumerable.Range(0,8).Select(i=>{float a=i*Mathf.PI/4;return shaft.center+new Vector2(Mathf.Cos(a)*shaft.radius.x,Mathf.Sin(a)*shaft.radius.y);}).ToArray();
        var normals=new List<Vector3>{Vector3.up,Vector3.down};var distances=new List<float>{-1.12f,4.3f};
        for(int i=0;i<rim.Length;i++){var a=rim[i];var b=rim[(i+1)%rim.Length];var n=new Vector3(-(b.y-a.y),0,b.x-a.x);normals.Add(n);distances.Add(-n.x*a.x-n.z*a.y);}
        var remaining=polygon;
        for(int i=0;i<normals.Count&&remaining.Count>=3;i++){var outside=Clip(remaining,normals[i],distances[i],false);if(outside.Count>=3)yield return outside;remaining=Clip(remaining,normals[i],distances[i],true);}
    }
    static int Carve(MeshRenderer renderer,int index)
    {
        var filter=renderer.GetComponent<MeshFilter>();var original=filter.sharedMesh;var bounds=renderer.bounds;
        var relevant=shafts.Where(s=>bounds.max.y>1.12f&&bounds.min.y<4.3f&&bounds.max.x>s.center.x-s.radius.x&&bounds.min.x<s.center.x+s.radius.x&&bounds.max.z>s.center.y-s.radius.y&&bounds.min.z<s.center.y+s.radius.y).ToArray();if(relevant.Length==0)return 0;
        var vs=original.vertices;var ns=original.normals;var uv=original.uv;var positions=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var subs=new List<int[]>();
        for(int sub=0;sub<original.subMeshCount;sub++)
        {
            var indices=new List<int>();var tris=original.GetTriangles(sub);
            for(int t=0;t<tris.Length;t+=3)
            {
                var polygons=new List<List<Vertex>>{Enumerable.Range(0,3).Select(k=>{int id=tris[t+k];return new Vertex{p=filter.transform.TransformPoint(vs[id]),n=ns.Length>id?ns[id]:Vector3.up,uv=uv.Length>id?uv[id]:Vector2.zero};}).ToList()};
                foreach(var shaft in relevant)polygons=polygons.SelectMany(p=>Subtract(p,shaft)).ToList();
                foreach(var poly in polygons){int start=positions.Count;foreach(var v in poly){positions.Add(filter.transform.InverseTransformPoint(v.p));normals.Add(v.n);uvs.Add(v.uv);}for(int k=1;k<poly.Count-1;k++)indices.AddRange(new[]{start,start+k,start+k+1});}
            }
            subs.Add(indices.ToArray());
        }
        if(positions.Count==0){renderer.enabled=false;foreach(var c in renderer.GetComponents<Collider>())c.enabled=false;return 1;}
        var mesh=new Mesh{name="Rough deep fracture opening "+index,indexFormat=IndexFormat.UInt32};mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.subMeshCount=subs.Count;for(int i=0;i<subs.Count;i++)mesh.SetTriangles(subs[i],i);mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Art+"/CutCanopy_"+index.ToString("000")+".asset");filter.sharedMesh=mesh;
        foreach(var c in renderer.GetComponents<Collider>())c.enabled=false;
        var shape=renderer.GetComponent<MeshCollider>();if(shape==null)shape=renderer.gameObject.AddComponent<MeshCollider>();shape.sharedMesh=mesh;shape.convex=false;shape.contactOffset=.001f;shape.enabled=true;
        return 1;
    }
    public static object Prepare()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");if(AssetDatabase.IsValidFolder(Art))throw new Exception("Already prepared");
        Directory.CreateDirectory("Temp/LayeredBurialReview");EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Temp/LayeredBurialReview/Before.unity",true);AssetDatabase.CreateFolder("Assets/_Project/Art","LayeredBurial");
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var p=new SerializedObject(director).FindProperty("fractureSections");var sections=Enumerable.Range(0,p.arraySize).Select(i=>(AftershockFractureSection)p.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();foreach(var section in sections)if(section!=null)section.gameObject.SetActive(false);Refs(director,"fractureSections",Array.Empty<UnityEngine.Object>());
        var debris=UnityEngine.Object.FindFirstObjectByType<AftershockDebris>();p=new SerializedObject(debris).FindProperty("fragments");for(int i=0;i<p.arraySize;i++)if(p.GetArrayElementAtIndex(i).objectReferenceValue is Transform t)t.gameObject.SetActive(false);debris.enabled=false;Ref(director,"debris",null);
        foreach(var rain in UnityEngine.Object.FindObjectsByType<AftershockRubbleRain>(FindObjectsSortMode.None)){p=new SerializedObject(rain).FindProperty("joints");for(int i=0;i<p.arraySize;i++)if(p.GetArrayElementAtIndex(i).objectReferenceValue is ParticleSystem ps){ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.gameObject.SetActive(false);}rain.enabled=false;}
        var notch=GameObject.Find("Pipe underside bearing notch");if(notch!=null)notch.SetActive(false);
        var env=GameObject.Find("Prototype Environment");var renderers=env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponent<MeshFilter>()?.sharedMesh!=null).ToArray();int cut=0;foreach(var r in renderers)cut+=Carve(r,cut);
        Save();return new{archivedSections=sections.Length,carvedSurfaces=cut,deepFractureChannels=shafts.Length};
    }
    static GameObject Make(Transform parent,string asset,string name,Vector3 center,Vector3 size,Vector3 angles,string meshId,bool collider)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RubbleAndDebris/Prefabs/"+asset+".prefab");var source=prefab.GetComponent<LODGroup>().GetLODs()[0].renderers[0];var original=source.GetComponent<MeshFilter>().sharedMesh;var path=Art+"/"+meshId+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=UnityEngine.Object.Instantiate(original);var b=original.bounds;var matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(angles),new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z))*Matrix4x4.Translate(-b.center);mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();mesh.normals=mesh.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();mesh.RecalculateBounds();var offset=mesh.bounds.center;mesh.vertices=mesh.vertices.Select(v=>v-offset).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name=meshId;AssetDatabase.CreateAsset(mesh,path);}
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=center;go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ImportedRubble/Materials/"+m.name+"_URP.mat")).ToArray();renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
        if(collider){var shape=go.AddComponent<MeshCollider>();shape.sharedMesh=mesh;shape.contactOffset=.001f;}
        return go;
    }
    static AftershockFallingChunk Dynamic(GameObject go,Transform settled,Collider[] protectedShapes,Vector3 velocity,bool chip)
    {
        var shape=go.GetComponent<MeshCollider>();if(shape==null)shape=go.AddComponent<MeshCollider>();shape.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;shape.convex=true;shape.contactOffset=.001f;shape.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Art/BuriedThreat/ForegroundConcreteContact.physicMaterial");
        var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.mass=chip?.035f:R(2,9);body.linearDamping=.04f;body.angularDamping=.65f;body.maxDepenetrationVelocity=.35f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.sleepThreshold=.015f;
        var chunk=go.AddComponent<AftershockFallingChunk>();Ref(chunk,"detachedRoot",settled);Ref(chunk,"head",Camera.main.transform);var tracked=Camera.main.transform.root.GetComponentsInChildren<Transform>(true);Ref(chunk,"leftHand",tracked.First(t=>t.name=="Left Controller"));Ref(chunk,"rightHand",tracked.First(t=>t.name=="Right Controller"));Refs(chunk,"ignoreColliders",protectedShapes);Vector(chunk,"releaseVelocity",velocity);Vector(chunk,"releaseAngularVelocity",new Vector3(R(-1.1f,1.1f),R(-.8f,.8f),R(-1.2f,1.2f)));Number(chunk,"attachmentSeconds",.05f);
        var data=new SerializedObject(chunk);data.FindProperty("colliderWhileAttached").boolValue=!chip;data.ApplyModifiedProperties();if(chip)shape.enabled=false;
        if(!chip){var audio=go.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.30f;audio.minDistance=.4f;audio.maxDistance=5;audio.dopplerLevel=0;Ref(go.AddComponent<RubbleImpactAudio>(),"impactClip",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Art/BuriedThreat/HeavyConcreteCrash.wav"));}
        return chunk;
    }
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");if(GameObject.Find("Deep Layered Collapse")!=null)throw new Exception("Already built");
        var env=GameObject.Find("Prototype Environment");var root=new GameObject("Deep Layered Collapse");root.transform.SetParent(env.transform,false);var settled=new GameObject("Permanent fallen rubble");settled.transform.SetParent(root.transform,false);
        var director=UnityEngine.Object.FindFirstObjectByType<AftershockDirector>();var protectedShapes=Camera.main.transform.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).ToArray();
        float[][] releases={new[]{3.05f,6.28f,11.74f},new[]{3.89f,7.48f,13.15f},new[]{4.67f,8.66f,14.28f},new[]{5.41f,9.53f,15.34f},new[]{6.12f,10.76f,14.82f}};
        string[] stone={"Props/Wall_Gray_Medium_02","Props/Wall_Gray_Medium_03","Props/Wall_DarkGrey_Medium_02","Props/Blocks_Big_01","Props/Bricks_Big_02"};
        int large=0,chips=0;
        for(int c=0;c<5;c++)
        {
            var shaft=shafts[c];var pocketGo=new GameObject("Deep failure pocket "+(c+1));pocketGo.transform.SetParent(root.transform,false);pocketGo.transform.position=new Vector3(shaft.center.x,0,shaft.center.y);var pocket=pocketGo.AddComponent<RubbleFailurePocket>();Ref(pocket,"aftershock",director);
            var bearingList=new List<RubbleFailurePocket.Bearing>();
            for(int layer=0;layer<4;layer++)
            {
                var group=new GameObject("Bearing stratum "+(layer+1));group.transform.SetParent(pocketGo.transform,false);float y=new[]{1.70f,2.18f,2.70f,3.15f}[layer];group.transform.localPosition=Vector3.up*y;
                var b=new RubbleFailurePocket.Bearing{root=group.transform,releaseAt=layer<3?releases[c][layer]:0,settlesAt=layer==0?100:layer==3?releases[c][2]+1.05f:releases[c][layer-1]+.2f,settleSeconds=layer==3?1.25f:1.05f,settleOffset=Vector3.down*new[]{0,.43f,.81f,1.59f}[layer]};
                var falling=new List<AftershockFallingChunk>();
                for(int piece=0;piece<3;piece++)
                {
                    Vector3 center=piece==0?new Vector3(0,0,.025f):new Vector3(piece==1?-.23f:.23f,.025f,piece==1?-.08f:.07f);
                    Vector3 size=piece==0?new Vector3(R(.38f,.48f),R(.12f,.19f),R(.30f,.39f)):new Vector3(R(.13f,.18f),R(.09f,.16f),R(.23f,.32f));
                    if(c>=2&&piece==0)size.x*=.85f;
                    var go=Make(group.transform,stone[(c+layer+piece)%stone.Length],"Stratum concrete "+c+" "+layer+" "+piece,center,size,new Vector3(R(-5,5),R(-17,17),R(-5,5)),"Bearing_"+c+"_"+layer+"_"+piece,true);
                    if(layer<3){var inward=new Vector3(-shaft.center.x,0,-shaft.center.y).normalized*R(.04f,.14f);falling.Add(Dynamic(go,settled.transform,protectedShapes,inward,false));large++;}
                }
                b.pieces=falling.ToArray();bearingList.Add(b);
            }
            var chipList=new List<AftershockFallingChunk>();var times=new List<float>();
            string[] chipAssets={"Props/Brick_Small_03","Props/Brick_Small_07","Props/Brick_Small_11","Props/Wall_Grey_Small_02","Props/Block_Small_03","Props/Wall_Grey_Small_03"};
            for(int i=0;i<36;i++)
            {
                int wave=i/12;float length=R(.045f,.14f),width=length*R(.35f,.8f),thin=R(.012f,.038f);
                // Every chip is already in the deep joint, below the next bearing stratum.
                var go=Make(pocketGo.transform,chipAssets[i%chipAssets.Length],"Persistent thin chip "+c+" "+i,new Vector3(R(-.085f,.085f),new[]{1.94f,1.99f,2.11f}[wave]+R(-.025f,.025f),R(-.07f,.07f)),new Vector3(length,thin,width),new Vector3(R(0,360),R(0,360),R(0,360)),"ThinChip_"+c+"_"+i,true);
                var inward=new Vector3(-shaft.center.x,0,-shaft.center.y).normalized*R(.02f,.12f);chipList.Add(Dynamic(go,settled.transform,protectedShapes,inward,true));times.Add(releases[c][wave]+R(-.12f,.92f));chips++;
            }
            var audio=pocketGo.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.4f;audio.minDistance=.4f;audio.maxDistance=5;audio.dopplerLevel=0;Ref(pocket,"strainSource",audio);Ref(pocket,"strainClip",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Art/BuriedThreat/LoadedConcreteStrain.wav"));
            var so=new SerializedObject(pocket);var array=so.FindProperty("bearings");array.arraySize=bearingList.Count;
            for(int j=0;j<bearingList.Count;j++){var element=array.GetArrayElementAtIndex(j);var b=bearingList[j];element.FindPropertyRelative("root").objectReferenceValue=b.root;element.FindPropertyRelative("releaseAt").floatValue=b.releaseAt;element.FindPropertyRelative("settlesAt").floatValue=b.settlesAt;element.FindPropertyRelative("settleSeconds").floatValue=b.settleSeconds;element.FindPropertyRelative("settleOffset").vector3Value=b.settleOffset;var ps=element.FindPropertyRelative("pieces");ps.arraySize=b.pieces.Length;for(int i=0;i<b.pieces.Length;i++)ps.GetArrayElementAtIndex(i).objectReferenceValue=b.pieces[i];}
            so.ApplyModifiedProperties();Refs(pocket,"chips",chipList);so=new SerializedObject(pocket);array=so.FindProperty("chipReleaseTimes");array.arraySize=times.Count;for(int i=0;i<times.Count;i++)array.GetArrayElementAtIndex(i).floatValue=times[i];so.ApplyModifiedProperties();
            // A closed top and rough vertical shoulders keep every failure backed by actual rubble.
            Make(pocketGo.transform,"Props/Wall_Grey_Big_01","Deep buried top mass",new Vector3(0,3.6f,0),new Vector3(.95f,.35f,.90f),new Vector3(2,13,2),"PocketTop_"+c,true);
            for(int side=-1;side<=1;side+=2)Make(pocketGo.transform,"Props/Wall_DarkGrey_Medium_01","Deep broken shoulder "+side,new Vector3(side*(shaft.radius.x+.09f),2.22f,0),new Vector3(.20f,1.9f,.82f),new Vector3(1,side*7,side*3),"PocketSide_"+c+"_"+side,true);
        }
        BuildPipe(root.transform,director);
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();Refs(ending,"lightBlockers",env.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy));Save();return new{large,permanentThinChips=chips,independentPockets=5,bearingStrata=4,hiddenPipeStart=2.45f};
    }
    static void BuildPipe(Transform root,AftershockDirector director)
    {
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var data=new SerializedObject(pipe);
        var names=new[]{"stowedPose","lipPose","releasePose"};var positions=new[]{new Vector3(0,2.45f,.88f),new Vector3(0,1.34f,.88f),new Vector3(-.025f,1.04f,.49f)};
        for(int i=0;i<3;i++){var t=(Transform)data.FindProperty(names[i]).objectReferenceValue;t.SetParent(root,true);t.SetPositionAndRotation(positions[i],Quaternion.Euler(0,12,90));}
        data.FindProperty("slideSeconds").floatValue=.48f;data.FindProperty("tipSeconds").floatValue=.20f;data.FindProperty("fallSeconds").floatValue=.39f;data.FindProperty("settleSeconds").floatValue=.26f;data.ApplyModifiedProperties();pipe.transform.SetPositionAndRotation(positions[0],Quaternion.Euler(0,12,90));pipe.GetComponent<Rigidbody>().position=positions[0];pipe.GetComponent<Rigidbody>().rotation=pipe.transform.rotation;
        var go=new GameObject("Buried utility fissure closure");go.transform.SetParent(root,false);go.transform.position=new Vector3(0,0,.88f);
        var group=new GameObject("Utility backfill bearing");group.transform.SetParent(go.transform,false);group.transform.localPosition=Vector3.up*3.0f;
        Make(group.transform,"Props/Wall_Grey_Big_01","Utility upper rubble",Vector3.zero,new Vector3(.75f,.28f,.72f),new Vector3(0,9,2),"UtilityBackfill",true);
        var pocket=go.AddComponent<RubbleFailurePocket>();Ref(pocket,"aftershock",director);var s=new SerializedObject(pocket);var p=s.FindProperty("bearings");p.arraySize=1;var b=p.GetArrayElementAtIndex(0);b.FindPropertyRelative("root").objectReferenceValue=group.transform;b.FindPropertyRelative("settlesAt").floatValue=9;b.FindPropertyRelative("settleSeconds").floatValue=1.8f;b.FindPropertyRelative("settleOffset").vector3Value=Vector3.down*1.40f;b.FindPropertyRelative("releaseAt").floatValue=0;s.ApplyModifiedProperties();
        for(int side=-1;side<=1;side+=2)Make(go.transform,"Props/Wall_Gray_Medium_02","Utility broken shoulder "+side,new Vector3(side*.38f,2.15f,0),new Vector3(.16f,1.70f,.65f),Vector3.zero,"UtilitySide_"+side,true);
    }
    public static object DressRoughChannels()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");var root=GameObject.Find("Deep Layered Collapse");
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("Deep broken shoulder")||r.name.StartsWith("Utility broken shoulder"))){r.enabled=false;foreach(var c in r.GetComponents<Collider>())c.enabled=false;}
        for(int c=0;c<6;c++)
        {
            var parent=GameObject.Find(c<5?"Deep failure pocket "+(c+1):"Buried utility fissure closure").transform;var shaft=shafts[c];
            for(int side=-1;side<=1;side+=2)for(int layer=0;layer<4;layer++)
                Make(parent,(layer+c)%2==0?"Props/Wall_Gray_Medium_02":"Props/Wall_DarkGrey_Medium_01","Interlocked shaft shoulder "+c+" "+side+" "+layer,new Vector3(side*(shaft.radius.x+.10f),1.57f+layer*.48f,0),new Vector3(.57f,.17f,.74f),new Vector3(R(-4,4),R(-7,7),90+R(-3,3)),"ShoulderStack_"+c+"_"+side+"_"+layer,true);
            for(int edge=0;edge<8;edge++)
            {
                float a=edge*Mathf.PI/4;var center=new Vector3(Mathf.Cos(a)*(shaft.radius.x+.10f),1.47f+R(-.025f,.025f),Mathf.Sin(a)*(shaft.radius.y+.10f));
                Make(parent,edge%2==0?"Props/Wall_Grey_Small_02":"Props/Bricks_Medium_01","Broken embedded fracture rim "+c+" "+edge,center,new Vector3(.19f,.10f,.21f),new Vector3(R(-12,12),R(0,360),R(-10,10)),"EmbeddedRim_"+c+"_"+edge,true);
            }
        }
        foreach(var pocket in root.GetComponentsInChildren<RubbleFailurePocket>())
        {
            var so=new SerializedObject(pocket);var p=so.FindProperty("chips");
            for(int i=0;i<p.arraySize;i++)if(p.GetArrayElementAtIndex(i).objectReferenceValue is AftershockFallingChunk chip){var pos=chip.transform.localPosition;pos.y+=i<12?.09f:.045f;chip.transform.localPosition=pos;}
        }
        var ending=UnityEngine.Object.FindFirstObjectByType<RescueEndingDirector>();Refs(ending,"lightBlockers",GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy));Save();return "Native proportion interlocked shoulders and embedded broken rims replace stretched surfaces";
    }
    public static object AddFractureCues()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");Physics.SyncTransforms();int lines=0;
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/StructuralAftershock/ConcreteStressCrack.mat");
        foreach(var pocket in GameObject.Find("Deep Layered Collapse").GetComponentsInChildren<RubbleFailurePocket>())
        {
            var data=new SerializedObject(pocket);var bearings=data.FindProperty("bearings");
            for(int layer=0;layer<bearings.arraySize;layer++)
            {
                var b=bearings.GetArrayElementAtIndex(layer);if(b.FindPropertyRelative("releaseAt").floatValue<=0)continue;
                var root=(Transform)b.FindPropertyRelative("root").objectReferenceValue;var shapes=root.GetComponentsInChildren<MeshCollider>();var points=new List<Vector3>();
                for(int k=0;k<25;k++)
                {
                    var origin=root.TransformPoint(new Vector3(-.26f+k*.52f/24f,-.4f,.025f+Mathf.Sin(k*2.3f)*.009f));var ray=new Ray(origin,Vector3.up);float distance=2;RaycastHit nearest=default;bool found=false;
                    foreach(var shape in shapes)if(shape.Raycast(ray,out var hit,distance)){distance=hit.distance;nearest=hit;found=true;}
                    if(found)points.Add(root.InverseTransformPoint(nearest.point+nearest.normal*.001f));
                }
                if(points.Count>=2)
                {
                    var go=new GameObject("Branching load fissure");go.transform.SetParent(root,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=material;line.positionCount=points.Count;line.SetPositions(points.ToArray());line.startWidth=line.endWidth=1;line.widthMultiplier=.001f;line.numCornerVertices=1;line.shadowCastingMode=ShadowCastingMode.Off;line.enabled=false;
                    var p=b.FindPropertyRelative("cracks");p.arraySize=1;p.GetArrayElementAtIndex(0).objectReferenceValue=line;lines++;
                }
            }
            data.ApplyModifiedProperties();
            var old=pocket.GetComponent<AudioSource>();if(old!=null){var go=new GameObject("Strain above the buried joint");go.transform.SetParent(pocket.transform,false);go.transform.localPosition=Vector3.up*1.9f;var audio=go.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.4f;audio.minDistance=.4f;audio.maxDistance=5;audio.dopplerLevel=0;Ref(pocket,"strainSource",audio);old.enabled=false;}
        }
        Save();return new{surfaceCracks=lines,spatialStrain="inside the upper joint"};
    }
    public static object Foundation()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var go=new GameObject("Continuous buried foundation concrete");go.transform.SetParent(GameObject.Find("Deep Layered Collapse").transform,false);
        Vector2[] ring={new Vector2(-2.5f,-2.2f),new Vector2(-1.8f,-2.5f),new Vector2(2.1f,-2.45f),new Vector2(2.5f,-1.9f),new Vector2(2.45f,2.1f),new Vector2(1.9f,2.5f),new Vector2(-2.1f,2.45f),new Vector2(-2.5f,1.9f)};
        var vs=new List<Vector3>();var tris=new List<int>();var uv=new List<Vector2>();
        void Triangle(Vector3 a,Vector3 b,Vector3 c){int n=vs.Count;vs.AddRange(new[]{a,b,c});uv.AddRange(new[]{new Vector2(a.x,a.z)*.25f+Vector2.one*.5f,new Vector2(b.x,b.z)*.25f+Vector2.one*.5f,new Vector2(c.x,c.z)*.25f+Vector2.one*.5f});tris.AddRange(new[]{n,n+1,n+2});}
        for(int i=0;i<ring.Length;i++)
        {
            var a=new Vector3(ring[i].x,-.035f,ring[i].y);var b=new Vector3(ring[(i+1)%ring.Length].x,-.035f,ring[(i+1)%ring.Length].y);var lowA=a-Vector3.up*.32f;var lowB=b-Vector3.up*.32f;
            Triangle(new Vector3(0,-.035f,0),b,a);Triangle(new Vector3(0,-.355f,0),lowA,lowB);Triangle(a,b,lowB);Triangle(a,lowB,lowA);
        }
        var mesh=new Mesh{name="Continuous native concrete foundation"};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Art+"/ContinuousFoundation.asset");go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=GameObject.Find("Pipe landing bearing slab").GetComponentInChildren<MeshRenderer>().sharedMaterial;
        var shape=go.AddComponent<MeshCollider>();shape.sharedMesh=mesh;shape.contactOffset=.001f;
        // Stabilize contacts between accumulated thin pieces.
        foreach(var chip in GameObject.Find("Deep Layered Collapse").GetComponentsInChildren<RubbleFailurePocket>().SelectMany(p=>p.Chips))
        {var body=chip.GetComponent<Rigidbody>();body.solverIterations=8;body.solverVelocityIterations=2;}
        Save();return "Thick, visible continuous foundation beneath the rubble seams";
    }
    public static object ClearSharedFissures()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");int count=0,index=1000;
        foreach(var r in GameObject.Find("Deep Layered Collapse").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&(r.name.StartsWith("Interlocked shaft shoulder")||r.name.StartsWith("Broken embedded fracture rim"))).ToArray())count+=Carve(r,index++);
        Save();return new{trimmedSharedSeams=count,pipeCorridor="continuous through adjoining native rubble"};
    }
    public static object FinishConnections()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        GameObject.Find("Continuous buried foundation concrete").transform.localScale=new Vector3(2,1,2);
        var pipe=UnityEngine.Object.FindFirstObjectByType<AftershockPipeDrop>();var data=new SerializedObject(pipe);var pose=(Transform)data.FindProperty("landingPose").objectReferenceValue;
        var slab=GameObject.Find("Pipe landing bearing slab");var shape=slab.GetComponentsInChildren<MeshCollider>().First(c=>c.enabled);var point=pose.position;var axis=pose.up;float height=float.NegativeInfinity;int supported=0;
        for(int i=0;i<=12;i++){var p=point+axis*Mathf.Lerp(-.219f,.219f,i/12f);if(shape.Raycast(new Ray(new Vector3(p.x,1.2f,p.z),Vector3.down),out var hit,2)){height=Mathf.Max(height,hit.point.y+.013f-(p.y-point.y));supported++;}}
        if(supported==0)throw new Exception("Pipe bearing surface not found");point.y=height;pose.SetParent(shape.transform,true);pose.position=point;
        Save();return new{continuousFloorFootprint="approximately 10 metres, beneath existing rubble",pipeContactSamples=supported,landing=point.ToString("F3"),landingParent=shape.name};
    }
    public static object ThinChipCollision()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");int count=0;
        foreach(var chip in GameObject.Find("Deep Layered Collapse").GetComponentsInChildren<RubbleFailurePocket>().SelectMany(p=>p.Chips))
        {
            var mesh=chip.GetComponent<MeshCollider>();var bounds=chip.GetComponent<MeshFilter>().sharedMesh.bounds;var box=chip.GetComponent<BoxCollider>();if(box==null)box=chip.gameObject.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;box.contactOffset=.001f;box.sharedMaterial=mesh.sharedMaterial;box.enabled=false;mesh.enabled=false;count++;
        }
        Save();return new{continuousCollisionChipShapes=count,geometry="fitted to each rendered chip"};
    }
}
