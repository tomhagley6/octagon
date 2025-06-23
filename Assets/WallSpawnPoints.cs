using UnityEngine;

[System.Serializable]
public class WallSpawnPair
{
    public Transform playerSpawnOuter; // Closer to wall
    public Transform playerSpawnInner; // Closer to centre
}

public class WallSpawnPoints : MonoBehaviour
{
    public WallSpawnPair[] wallSpawns; // Assign in Inspector, one per wall
}
