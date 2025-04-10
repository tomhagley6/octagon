using UnityEngine;
using Globals;

public class MaterialLoader : MonoBehaviour
{
    // This method runs when the object is initialized
    void Start()
    {
        LoadMaterial(ref General.wallHighColour, "VerticalGrating");
        LoadMaterial(ref General.wallLowColour, "HorizontalGrating");
        LoadMaterial(ref General.wallRiskyColour, "Checkers");
    }

    // Helper method to load materials and log status
    private void LoadMaterial(ref Material material, string materialName)
    {
        material = (Material)Resources.Load(materialName);
        if (material == null)
            Debug.LogError($"Failed to load material: {materialName}");
        else
            Debug.Log($"{materialName} loaded successfully: {material}");
    }
}