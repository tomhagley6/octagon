using UnityEngine;
using UnityEditor;

public class CheckPrefabStatus : MonoBehaviour
{

    [SerializeField] GameObject obj; 
    void Start()
    {
        // Check if the GameObject is a prefab instance
        bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(obj);

        if (isPrefabInstance)
        {
            // Get the prefab parent
            GameObject prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(obj);

            if (prefabParent != null)
            {
                Debug.Log(obj.name + " is a prefab instance. Prefab Name: " + prefabParent.name);
            }
            else
            {
                Debug.LogWarning(obj.name + " is an orphan prefab instance.");
            }
        }
        else
        {
            Debug.Log(obj.name + " is not a prefab instance.");
        }
    }
}