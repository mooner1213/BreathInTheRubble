using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class InspectDesktopPhone
{
    public static object Run()=>new {scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,
        desktop=Object.FindFirstObjectByType<BreathInTheRubble.Prototype.DesktopPhoneTestDriver>()!=null,
        physicalHmd=BreathInTheRubble.Prototype.DesktopPhoneTestDriver.HasPhysicalHmd,
        devices=UnityEngine.InputSystem.InputSystem.devices.Select(d=>new {d.name,d.layout,type=d.GetType().FullName,description=d.description.ToString(),tracked=(d as UnityEngine.InputSystem.XR.XRHMD)?.isTracked.ReadValue()}).ToArray(),
        cameras=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>new {c.name,pos=c.transform.position.ToString(),components=c.GetComponents<Behaviour>().Select(b=>b.GetType().FullName+"="+b.enabled).ToArray()}).ToArray(),
        simulators=Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(b=>b.GetType().Name.Contains("Simulator")).Select(b=>new{b.name,type=b.GetType().FullName,b.enabled}).ToArray()};
}
