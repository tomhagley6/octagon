using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapezoidCreator : MonoBehaviour
{
    public GameObject trapezoidPrefab;
    public int numberOfCubes = 5;

    void Start()
    {
        CreateTrapezoid();
    }

    void CreateTrapezoid()
    {
        for (int i = 0; i < numberOfCubes; i++)
        {
            float scale = 1f + i * 0.2f; // Adjust the scale to create a trapezoid
            Vector3 position = new Vector3(i, 0, 0);

            GameObject cube = Instantiate(trapezoidPrefab, position, Quaternion.identity);
            cube.transform.localScale = new Vector3(scale, 1f, 1f);
        }
    }
}
