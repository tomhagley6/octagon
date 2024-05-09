using System;
using System.Collections;
using System.Collections.Generic;
using QFSW.QC;
using Unity.Netcode;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.UI;

public class ScoreSubmission : NetworkBehaviour
{
   private string leaderboardId = "Octagon"; // Currently hard-coded
   [SerializeField] private Button submitScoreButton;
   [SerializeField] private Button loadScoresButton;
   [SerializeField] private Transform scoresContent;
   private TrialHandler trialHandler;
   [SerializeField] private LeaderboardScoreView scoreViewPrefab;
   private int score;



    public override void OnNetworkSpawn()
    {
        trialHandler = FindObjectOfType<TrialHandler>();
         
         // subscribe to the score submission button
         submitScoreButton.onClick.AddListener(SubmitScoreAsync);
         loadScoresButton.onClick.AddListener(LoadScoresASync);
         

    }

   // Submit player score to leaderboard on SubmitScore UI button press
   private async void SubmitScoreAsync()
   {
      try
      {
         score = trialHandler.GetScore();
         Debug.Log($"Score at time of submission reads: {score}");
         await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
         Debug.Log("Score submitted!");
      }
      catch (Exception e) {
         Debug.Log($"Failed to submit score: {e}");
         throw;
      }
   }

   private async void LoadScoresASync()
   {
      try
      {
         var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId);
         var childCount = scoresContent.childCount;
         for (int i = 0; i < childCount; i++)
         {
            Destroy(scoresContent.GetChild(i).gameObject);
         }

         foreach (var leaderboardEntry in scoresResponse.Results)
         {
            var scoreView = Instantiate(scoreViewPrefab, scoresContent);
            int leaderboardEntryRank = leaderboardEntry.Rank + 1; // remove rank 0
            scoreView.Initialize(leaderboardEntryRank.ToString(), 
                                 leaderboardEntry.PlayerName,
                                 leaderboardEntry.Score.ToString());
         }

         Debug.Log("Scores fetched!");
      }
      catch (Exception e) {
         Debug.Log($"Failed to fetch scores: {e}");
         throw;
      }

   }


}
