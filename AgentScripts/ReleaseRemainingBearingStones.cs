using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BreathInTheRubble.Prototype;
public static class ReleaseRemainingBearingStones
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");var manager=UnityEngine.Object.FindFirstObjectByType<RubbleStructuralCollapse>();var data=new SerializedObject(manager);var array=data.FindProperty("elements");var persistent=GameObject.Find("Persistent structural rubble").transform;var head=Camera.main.transform;var hands=head.root.GetComponentsInChildren<Transform>(true);
        var ignored=head.root.GetComponentsInChildren<Collider>(true).Concat(UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None).SelectMany(g=>g.GetComponentsInChildren<Collider>(true))).Distinct().ToArray();var rows=new List<object>();int index=0;
        for(int i=0;i<manager.Elements.Length;i++)
        {
            var e=manager.Elements[i];if(e.loose!=null||e.target?.parent==null||!e.target.parent.name.StartsWith("Compressed bearing fragment"))continue;
            var filter=e.target.GetComponent<MeshFilter>();var r=e.target.GetComponent<Renderer>();if(filter?.sharedMesh==null||filter.sharedMesh.vertexCount<4||r==null||!r.enabled)continue;
            var contact=UnityEngine.Object.Instantiate(filter.sharedMesh);var b=contact.bounds;contact.name="Bearing stone contact "+index;contact.vertices=contact.vertices.Select(v=>b.center+(v-b.center)*.86f).ToArray();contact.RecalculateBounds();AssetDatabase.CreateAsset(contact,"Assets/_Project/Art/BurialBacking/ExistingBearing_"+index.ToString("D2")+".asset");
            var meshCollider=e.target.GetComponent<MeshCollider>();meshCollider.sharedMesh=contact;meshCollider.convex=true;meshCollider.contactOffset=.001f;
            foreach(var box in e.target.GetComponents<BoxCollider>()){box.center=b.center;box.size=b.size*.78f;box.contactOffset=.001f;box.enabled=true;meshCollider.enabled=false;}
            var body=e.target.GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.mass=Mathf.Clamp(r.bounds.size.magnitude*3,.15f,3);body.maxDepenetrationVelocity=.18f;body.angularDamping=.6f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var chunk=e.target.gameObject.AddComponent<AftershockFallingChunk>();var s=new SerializedObject(chunk);s.FindProperty("detachedRoot").objectReferenceValue=persistent;s.FindProperty("head").objectReferenceValue=head;s.FindProperty("leftHand").objectReferenceValue=hands.First(t=>t.name=="Left Controller");s.FindProperty("rightHand").objectReferenceValue=hands.First(t=>t.name=="Right Controller");s.FindProperty("headClearanceRadius").floatValue=.20f;s.FindProperty("handClearanceRadius").floatValue=0;var refs=s.FindProperty("ignoreColliders");refs.arraySize=ignored.Length;for(int j=0;j<ignored.Length;j++)refs.GetArrayElementAtIndex(j).objectReferenceValue=ignored[j];s.ApplyModifiedPropertiesWithoutUndo();
            var element=array.GetArrayElementAtIndex(i);element.FindPropertyRelative("loose").objectReferenceValue=chunk;element.FindPropertyRelative("failureResistance").floatValue=Mathf.Lerp(.60f,1.42f,Mathf.Repeat(index*.6180339f,1));element.FindPropertyRelative("compression").vector3Value=Vector3.zero;element.FindPropertyRelative("openingOffset").vector3Value=Vector3.zero;rows.Add(new{parent=e.target.parent.name,mesh=filter.sharedMesh.name});index++;
        }
        data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);EditorSceneManager.SaveScene(manager.gameObject.scene);File.WriteAllText("AgentScripts/ReviewImages/BurialGaps/RemainingStones.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return new{additionalPhysicalStones=index};
    }
}
