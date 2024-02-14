using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTrigger : MonoBehaviour
{

    public GameManager gameManager;

    int triggerScoreValue = 50;
    void OnTriggerEnter()
    {
        // Triggers initiate full logic progression for the end of a trial 
        // and beginning of the next
        // gameManager.EndTrial(triggerScoreValue);
        
        // gameManager.AdjustScore(triggerScoreValue);
        
    }
}
