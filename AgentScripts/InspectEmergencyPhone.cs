using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class InspectEmergencyPhone
{
 public static object Run(){var phone=Object.FindFirstObjectByType<BreathInTheRubble.Prototype.PhoneInterfaceController>();var canvas=phone.GetComponentInChildren<Canvas>(true);return new {scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,children=canvas.GetComponentsInChildren<Transform>(true).Select(t=>new{t.name,active=t.gameObject.activeSelf,rect=t is RectTransform r?r.rect.ToString():"",font=t.GetComponent<TMPro.TMP_Text>()?.font?.name}).ToArray(),prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(phone)};}
}
