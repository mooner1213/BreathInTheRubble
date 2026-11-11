using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class InspectAftershockSetup
{
 public static object Run()=>new{scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,head=Camera.main.transform.position.ToString(),near=Camera.main.nearClipPlane,
  controllers=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Left Controller"||t.name=="Right Controller").Select(t=>new{t.name,pos=t.position.ToString(),components=t.GetComponentsInChildren<Component>(true).Where(c=>c!=null&&c.GetType().Name.Contains("Haptic")).Select(c=>c.GetType().FullName).ToArray()}).ToArray(),
  ceiling=GameObject.Find("Prototype Environment").GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.Contains("Ceiling")||r.name.Contains("Overhead")).Select(r=>new{r.name,pos=r.transform.position.ToString(),bounds=r.bounds.ToString(),isStatic=r.gameObject.isStatic}).ToArray()};
}
