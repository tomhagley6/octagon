using UnityEngine;

[System.Serializable]
public class WallSpawnPair
{
    public Transform player1Spawn; // Closer to wall
    public Transform player2Spawn; // Closer to centre
}

public class WallSpawnPoints : MonoBehaviour
{
    public WallSpawnPair[] wallSpawns; // Assign in Inspector, one per wall
}
