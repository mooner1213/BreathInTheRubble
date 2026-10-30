using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyBuriedSurroundings
{
    public static object Run()
    {
        Physics.SyncTransforms();var environment=GameObject.Find("Prototype Environment").transform;var added=environment.Find("Buried Surroundings");var head=Camera.main.transform.position;
        var headOverlaps=Physics.OverlapSphere(head,0.12f).Where(c=>c.transform.IsChildOf(environment)).Select(c=>c.name).ToArray();
        var phone=UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneInterfaceController>();
        var bottle=UnityEngine.Object.FindFirstObjectByType<BreathInTheRubble.Prototype.BottleLiquidVisual>().transform.parent;
        string[] Intersections(Transform item)
        {
            var source=item.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
            return added.GetComponentsInChildren<Collider>().Where(c=>source.Any(s=>Physics.ComputePenetration(s,s.transform.position,s.transform.rotation,c,c.transform.position,c.transform.rotation,out var direction,out var distance)&&distance>0.001f)).Select(c=>c.name).ToArray();
        }
        var phoneClips=Intersections(phone.transform);var bottleClips=Intersections(bottle);
        var hits=new System.Collections.Generic.List<string>();
        foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.back,Vector3.down})
        {
            if(Physics.Raycast(head,direction,out var hit,2f))hits.Add(direction+"="+hit.collider.name+"@"+hit.distance.ToString("F2"));
        }
        var components=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();
        return new{scene=SceneManager.GetActiveScene().path,head=head.ToString(),headOverlaps,phoneClips,bottleClips,hits=hits.ToArray(),
            origins=components.Count(c=>c.GetType().Name=="XROrigin"),inputs=components.Count(c=>c.GetType().Name=="InputActionManager"),
            newRenderers=added.GetComponentsInChildren<MeshRenderer>().Length,newColliders=added.GetComponentsInChildren<Collider>().Length,
            bodyMeshes=added.Find("Seated Buried Lower Body").GetComponentsInChildren<MeshFilter>().Select(f=>new{f.name,bounds=f.sharedMesh.bounds.ToString()}).ToArray()};
    }
}
