using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class InspectPhoneInteraction
{
    public static string Run()
    {
        var scene = SceneManager.GetActiveScene();
        var sb = new StringBuilder();
        sb.AppendLine($"Scene={scene.path}; dirty={scene.isDirty}");

        var phone = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name.Contains("Smartphone", StringComparison.OrdinalIgnoreCase));
        if (phone == null)
            return sb.AppendLine("Phone=missing").ToString();

        sb.AppendLine($"Phone={GetPath(phone)}; active={phone.gameObject.activeInHierarchy}; world={phone.position:F3}; rot={phone.eulerAngles:F1}");
        foreach (var component in phone.GetComponents<Component>())
            sb.AppendLine($"PhoneComponent={component.GetType().FullName}");

        var attach = phone.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == "Grab Attach");
        if (attach != null)
            sb.AppendLine($"Attach localPos={attach.localPosition:F4}; localRot={attach.localEulerAngles:F1}");

        foreach (var light in phone.GetComponentsInChildren<Light>(true))
            sb.AppendLine($"Light={light.name}; type={light.type}; enabled={light.enabled}; intensity={light.intensity}; range={light.range}; localPos={light.transform.localPosition:F4}; forward={light.transform.forward:F3}");

        foreach (var canvas in phone.GetComponentsInChildren<Canvas>(true))
            sb.AppendLine($"PhoneCanvas={GetPath(canvas.transform)}; render={canvas.renderMode}; scale={canvas.transform.localScale:F5}");

        var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"EventSystems={eventSystems.Length}");
        foreach (var eventSystem in eventSystems)
            sb.AppendLine($"EventSystem={GetPath(eventSystem.transform)}; modules={string.Join(",", eventSystem.GetComponents<BaseInputModule>().Select(item => item.GetType().FullName))}");

        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var behaviour in behaviours.Where(IsRelevantInteractor))
        {
            var type = behaviour.GetType();
            sb.Append($"Interactor={GetPath(behaviour.transform)}; type={type.FullName}");
            foreach (var propertyName in new[] { "selectActionTrigger", "handedness" })
            {
                var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.GetIndexParameters().Length == 0)
                    sb.Append($"; {propertyName}={property.GetValue(behaviour)}");
            }
            sb.AppendLine();
        }

        var camera = Camera.main;
        if (camera != null)
            sb.AppendLine($"MainCamera={GetPath(camera.transform)}; world={camera.transform.position:F3}; forward={camera.transform.forward:F3}");

        Debug.Log(sb.ToString());
        return sb.ToString();
    }

    static bool IsRelevantInteractor(MonoBehaviour behaviour)
    {
        var name = behaviour.GetType().Name;
        return name.Contains("Interactor", StringComparison.OrdinalIgnoreCase) &&
               (name.Contains("NearFar", StringComparison.OrdinalIgnoreCase) || name.Contains("Ray", StringComparison.OrdinalIgnoreCase) || name.Contains("Direct", StringComparison.OrdinalIgnoreCase));
    }

    static string GetPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
