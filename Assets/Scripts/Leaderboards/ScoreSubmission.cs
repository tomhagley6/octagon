using System;
using Globals;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using UnityEngine;
using Button = UnityEngine.UI.Button;

/* Handle score submission to Unity Leaderboards, display Leaderboard scores,
   and listen for Leaderboard UI toggle */
// This script can be updated to the new Input System (listening for UI toggle)
public class ScoreSubmission : NetworkBehaviour
{
   [SerializeField] private Button submitScoreButton;
   [SerializeField] private Button loadScoresButton;
   [SerializeField] private Transform scoresContent;
   [SerializeField] private TMP_InputField playerName;
   private Score scoreInstance;
   [SerializeField] private LeaderboardScoreView scoreViewPrefab;
   [SerializeField] private GameObject leaderboardsUI;


   public override void OnNetworkSpawn()
   {
      scoreInstance = FindObjectOfType<Score>();

      // subscribe to the score submission button
      submitScoreButton.onClick.AddListener(SubmitScoreAsync);
      loadScoresButton.onClick.AddListener(LoadScoresASync);

      Debug.Log($"playername is {playerName.text}, type: {playerName.text.GetType()}, isnull: {playerName.text == null}, isemptystring: {playerName.text == ""}");

   }

   public void Start()
   {
      // Initially hide the leaderboards UI
      leaderboardsUI.transform.localScale = new Vector3(0, 0, 0);

   }


   // Submit player score to leaderboard on SubmitScore UI button press
   private async void SubmitScoreAsync()
   {
      try
      {
         // Update player name
         string name = playerName.text != "" ? playerName.text : "Anonymous";
         await AuthenticationService.Instance.UpdatePlayerNameAsync(name);

         // Upload score
         int score = scoreInstance.score;
         // int normalisedScore = NormaliseScore(gameManager.trialNum.Value, score); // normalise to trialNum
         await LeaderboardsService.Instance.AddPlayerScoreAsync(General.leaderboardId, score);
         Debug.Log($"{playerName.text}, score submitted!");
      }
      catch (Exception e)
      {
         Debug.Log($"Failed to submit score: {e}");
         throw;
      }
   }


   // Normalise score to number of trials
   // Currently unused
   private int NormaliseScore(ushort trialNum, int score)
   {
      // Setting max score equal to 10000
      // if score values change often, this could potentially be automated
      return (int)Mathf.Round(score / trialNum * 100 * 4);
   }


   // load scores from Unity services into the leaderboard UI 
   // or clear the leaderboard UI if it is currently filled
   private async void LoadScoresASync()
   {
      try
      {
         // get scores from Unity Leaderboard services
         var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(General.leaderboardId);
         var childCount = scoresContent.childCount;

         // clear UI if it is occupied
         if (childCount > 0)
         {
            for (int i = 0; i < childCount; i++)
            {
               Destroy(scoresContent.GetChild(i).gameObject);
            }
         }
         else  // otherwise populate UI 
         {
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
      }
      catch (Exception e)
      {
         Debug.Log($"Failed to fetch scores: {e}");
         throw;
      }
   }

   public void Update()
   {
      if (Input.GetKeyDown(General.toggleLeaderboards))
      {
         ToggleButtons();
      }
   }

   // Toggle Leaderboards UI visibility
   private void ToggleButtons()
   {
      // var leaderboardsUI = GameObject.Find("LeaderboardsUI");
      if (leaderboardsUI.transform.localScale == new Vector3(0, 0, 0))
      {
         leaderboardsUI.transform.localScale = new Vector3(1, 1, 1);
      }
      else
      {
         leaderboardsUI.transform.localScale = new Vector3(0, 0, 0);

      }

   }


}
