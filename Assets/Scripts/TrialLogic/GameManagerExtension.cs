using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using JetBrains.Annotations;
using Globals;
using LoggingClasses;
using Unity.VisualScripting;
using UnityEngine;
using Random=UnityEngine.Random;
using KaimiraGames;
using Mono.CSharp;
using Unity.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors; // needed for .Select and .Where
using System.Linq;

public class GameManagerExtension : MonoBehaviour
{
    public IdentityManager identityManager;
    public WallTriggerExtension wallTriggerExtension;
    List<int> walls;
    public List<Collider> wallColliders;

    public Vector3 arenaCenter = new Vector3(0, 0, 0);
    public float spawnRadius = 2f;

    void Start()
    {
        if (identityManager == null) identityManager = FindObjectOfType<IdentityManager>();

        if (wallTriggerExtension == null)
        {
            wallTriggerExtension = FindObjectOfType<WallTriggerExtension>();
        }

        wallColliders = GameObject.FindGameObjectsWithTag("Wall")
            .Select(go => go.GetComponent<Collider>())
            .Where(c => c != null)
            .ToList();

        var colliders = wallTriggerExtension.wallColliders;

        foreach (var col in colliders)
        {
            Debug.Log($"Found wall collider: {col.name}");
        }
    }
    public string SelectTrial()
    {
        // Create weighted list of trial types to draw from 
        WeightedList<string> trialTypeDist = new();
        for (int i = 0; i < General.trialTypes.Count; i++)
        {
            trialTypeDist.Add(General.trialTypes[i], General.trialTypeProbabilities[i]);
        }
        
        // Return trial type for this trial
        return trialTypeDist.Next();
    }

    public List<int> SelectNewWalls() 
    {
        Debug.Log("NEW TRIAL");

        // Generate wall trigger IDs for a new trial
        walls = identityManager.ListCustomIDs();

        // Choose a random anchor wall to reference the trial to 
        int anchorWallIndex = Random.Range(0, walls.Count);
        // Debug.Log($"anchor walls is {anchorWallIndex}");

        // Replaced the below with a weighted list to bias towards wallSep=1 trials
        // // Randomly choose a wall separation value for this trial
        // int i = General.wallSeparations[Random.Range(0, General.wallSeparations.Count)];


        // Create weighted list of wall separation values to draw from 
        WeightedList<int> wallSeparationsWeighted = new();
        for (int i = 0; i < General.wallSeparations.Count; i++)
        {
            wallSeparationsWeighted.Add(General.wallSeparations[i], General.wallSeparationsProbabilities[i]);
        }
        // Query the weighted list for this trial's wall separation
        int wallSeparation = wallSeparationsWeighted.Next();
        

        // choose a random second wall that is consistent with anchor wall for this trial type
        int wallIndexDiff = new List<int>{-wallSeparation, wallSeparation}[Random.Range(0, 2)];
        // Debug.Log($"wallIndexDiff = {wallIndexDiff}");
        int dependentWallIndex = anchorWallIndex + wallIndexDiff;
        // Debug.Log($"naive dependent wall is walls is {dependentWallIndex}");
        
        // Account for circular octagon structure
        if (dependentWallIndex < 0)
        {
            dependentWallIndex += walls.Count;
            // Debug.Log($" dependent wall < 0, so corrected to {dependentWallIndex}");

        }
        else if (dependentWallIndex >= walls.Count)
        {
            dependentWallIndex -= walls.Count;
            // Debug.Log($" dependent wall >= walls.Count - 1, so corrected to {dependentWallIndex}");
        }
        
        // assign high and low walls with the generated indexes
        // Debug.Log($"chosen walls are {anchorWallIndex}, {dependentWallIndex}");
        int highWallTriggerID = walls[anchorWallIndex];
        int lowWallTriggerID = walls[dependentWallIndex];   

        return new List<int>(new int[] {highWallTriggerID, lowWallTriggerID});
    }

    public Vector3 GetValidSpawnPosition()
    {
        Vector3 spawnPos;
        int attempts = 0; // limit attempts to find valid spawn position
        const int maxAttempts = 20;

        do
        {
            // sample a random point in a circle
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            spawnPos = new Vector3(randomCircle.x, arenaCenter.y + 0.5f, randomCircle.y) + arenaCenter;

            attempts++;
        }
        while (IsInsideWall(spawnPos) && attempts < maxAttempts);
        
        return spawnPos;
    }

    public bool IsInsideWall(Vector3 position)
    {
        return wallColliders.Any(walls => walls.bounds.Contains(position));
    }

}