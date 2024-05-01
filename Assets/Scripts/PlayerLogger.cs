using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Logging;
using System;

// Class to implement a DiskLogger, attached to each client's FirstPersonPlayer
public class PlayerLogger : NetworkBehaviour
{


public DiskLogger diskLogger;
public GameObject player;
public Action<bool> playerSpawned;
public override void OnNetworkSpawn() {
        // Get this player
        player = transform.parent.gameObject;

        
        // Start logging data only for the player in the current client
        // This could be an event that triggers the logger initiation method in GameManager
        // does not even need a player reference passed
        // First line of OnNetworkSpawn can be to call a method in GameManager that subscribes to 
        if (IsLocalPlayer) {
            Debug.Log("Local player; beginning logging");
            diskLogger = FindObjectOfType<DiskLogger>();
            diskLogger.StartLogger();
            StartCoroutine("LogPos");
        }
        else {
            Debug.Log("NOT local player");
        }

    }

    IEnumerator LogPos()
    {
        while (true)
        {
            // diskLogger.Log(String.Format(Globals.posFormat, Globals.posTag,
            //                                                 Globals.player, 
            //                                                 player.transform.position.x,
            //                                                 player.transform.position.z));
            
            yield return new WaitForSeconds(0.2f);

        }
    }

    void LogEvent(int increment, int triggerID, string rewardType) {

        // Was event triggered by this player instance
        bool thisPlayer;
        thisPlayer = IsLocalPlayer;

        // diskLogger.Log(String.Format(Globals.wallTriggerFormat, Globals.wallTriggerTag,
        //                                                         Globals.trialNum,
        //                                                         Globals.trialType,
        //                                                         triggerID,
        //                                                         rewardType,
        //                                                         increment,
        //                                                         thisPlayer)); 
    }

}




