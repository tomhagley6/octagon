using System.Collections.Generic;
using UnityEngine;
using System.Linq;



// Keep a dictionary that associates custom IDs of walls to wall GameObjects
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
            Debug.LogWarning("CustomID already assigned to a GameObject: " + customID);
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
            Debug.LogWarning("No record of custom ID: " + customID);
            return null;
        }
    }

    public List<int> ListCustomIDs()
    {
        return new List<int>(wallDictionary.Keys);
    }

    // From GPT - Using Linq to order my dictionary
    // Call this before using the dictionary
    public void OrderDictionary()
    {
        // Order by keys
        /* Both OrderBy and ToDictionary expect a lambda expression as their input
        and the compiler implicitly declares the variable 'pair' to represent each 
        collection element during the iteration.
        Therefore, we just need to specify either Key or Value in the lambda function
        output to order or create the dictionary
        OrderBy will just return an IEnumerable of keys, so we need to call ToDictionary
        to create a new ordered dictionary  */
        var orderedDictionary = wallDictionary.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
        wallDictionary = orderedDictionary;
    }

}
