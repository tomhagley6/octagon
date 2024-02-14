using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections.Specialized;


// Keep the dictionary that associates custom IDs of walls to wall GameObjects
// Allow indexing this dictionary with the wall custom ID, and ordering this dictionary at Start
public class IdentityManager : MonoBehaviour
{
    private Dictionary<int, GameObject> wallDictionary = new Dictionary<int, GameObject>();

    // Called to instantiate a wall in the dictionary with its custom ID
    public void AssignIdentifier(GameObject obj, int customID)
    {
        if (!wallDictionary.ContainsKey(customID))
        {
            wallDictionary.Add(customID, obj);
        }
        else
        {
            Debug.LogWarning("Custom ID already exists:" + customID);
        }
    }

    // Assuming a populated Dict, get the wall number from the custom ID
    public GameObject GetObjectByIdentifier(int customID)
    {
        if (wallDictionary.ContainsKey(customID))
        {
            return wallDictionary[customID];
        }
        else
        {
            Debug.LogWarning("Custom ID not found: " + customID);
            return null;
        }
    }

    public List<int> ListCustomIDs()
    {
        Debug.Log($"Keys are: {wallDictionary.Keys}");
        return new List<int>(wallDictionary.Keys);
    }

    // From GPT - Using Linq to order my dictionary
    // Call this before using the dictionary
    public void OrderDictionary()
    {
        // Order by keys
        var orderedDictionary = wallDictionary.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
        wallDictionary = orderedDictionary;
    }

}
