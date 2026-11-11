using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using BreathInTheRubble.Prototype;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class InspectPipeSetup
{
    public static object Probe()
    {
        Physics.SyncTransforms();
        var points = new[]{-.23f,-.12f,0f,.12f,.23f};
        var all = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        return new {
            landing = points.Select(x => {var hits=Physics.RaycastAll(new Vector3(x,.8f,.25f),Vector3.down,1f).Where(h=>!h.collider.isTrigger).OrderBy(h=>h.distance).Take(3);return new{x,hits=hits.Select(h=>new{h.collider.name,p=h.point.ToString("F3")})};}),
            steel = all.Where(r=>r.name.Contains("EmbeddedSteel") || r.name.Contains("Rebar") || r.name.Contains("Steel") || r.name.Contains("Pipe")).Select(r=>new{r.name,p=r.transform.position.ToString("F3"),b=r.bounds.ToString("F3"),collider=r.GetComponent<Collider>()?.GetType().Name,mat=AssetDatabase.GetAssetPath(r.sharedMaterial)}),
            ceiling = new[]{"Low Ceiling Front","Low Ceiling Rear","Overhead Broken Slab Left","Overhead Broken Slab Right"}.Select(n=>GameObject.Find(n)).Select(g=>new{g.name,scale=g.transform.lossyScale.ToString("F3"),collider=g.GetComponent<Collider>()?.bounds.ToString("F3")})
        };
    }
    public static object Run()
    {
        var scene = SceneManager.GetActiveScene();
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var motion = UnityEngine.Object.FindFirstObjectByType<AftershockEnvironmentMotion>();
        var ceilings = new SerializedObject(motion).FindProperty("ceilingTargets");
        return new {
            scene = scene.path, dirty = scene.isDirty,
            head = Camera.main.transform.position.ToString("F3"),
            devices = all.Where(t => t.name == "Left Controller" || t.name == "Right Controller").Select(t=>new {t.name, p=t.position.ToString("F3")}),
            ceilings = Enumerable.Range(0, ceilings.arraySize).Select(i => (Transform)ceilings.GetArrayElementAtIndex(i).objectReferenceValue)
                .Select(t => new {t.name, p=t.position.ToString("F3"), rotation=t.eulerAngles.ToString("F2"), bounds=t.GetComponent<Renderer>()?.bounds.ToString("F3"), mesh=t.GetComponent<MeshFilter>()?.sharedMesh.name}),
            grabs = all.Select(t=>t.GetComponent<XRGrabInteractable>()).Where(g=>g!=null).Select(g=>new {g.name,p=g.transform.position.ToString("F3"),g.movementType,g.useDynamicAttach,g.matchAttachPosition,g.matchAttachRotation,g.throwOnDetach,g.retainTransformParent,mask=g.interactionLayers.value,colliders=g.colliders.Select(c=>c.name),body=g.GetComponent<Rigidbody>()?.isKinematic,components=g.GetComponents<Component>().Select(c=>c.GetType().Name)}),
            surfaces = all.Where(t=>t.GetComponent<Renderer>()!=null && (t.name.Contains("Steel") || t.name.Contains("Rebar") || t.name.Contains("Wall") || t.name.Contains("Inner") || t.name.Contains("Floor") || t.name.Contains("Pipe") || t.name.Contains("Rubble")))
                .Select(t=>new {t.name,active=t.gameObject.activeInHierarchy,p=t.position.ToString("F3"),bounds=t.GetComponent<Renderer>().bounds.ToString("F3"),collider=t.GetComponent<Collider>()?.GetType().Name,mat=t.GetComponent<Renderer>().sharedMaterial?.name}).Take(85),
            nearColliders = all.Select(t=>t.GetComponent<Collider>()).Where(c=>c!=null && c.gameObject.activeInHierarchy && c.bounds.center.magnitude<1.5f && !c.isTrigger).Select(c=>new {c.name,type=c.GetType().Name,bounds=c.bounds.ToString("F3"),body=c.attachedRigidbody?.name}).Take(70)
        };
    }
}
