using UnityEngine;
using Globals;

public class MaterialLoader : MonoBehaviour
{
    // This method runs when the object is initialized
    void Start()
    {
        LoadMaterial(ref General.wallHighMaterial, "VerticalGrating");
        LoadMaterial(ref General.wallLowMaterial, "HorizontalGrating");
        // LoadMaterial(ref General.wallRiskyMaterial, "Checkers");
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