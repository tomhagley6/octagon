using TMPro;
using UnityEngine;

// Attached to the scoreboard Canvas, will update the score for the game HUD
// as recorded in GameManager
public class Score : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TrialHandler trialHandler;
    public int score = 0;


    void Start() 
    {
        trialHandler = FindObjectOfType<TrialHandler>();
    }
    
    // Keep canvas text updated with the current value of score
    void Update()
    {
        scoreText.text = $"Score: {score}";
    }


      // change the score variable
        public void AdjustScore(int increment = 0)
    {
        score += increment;
    }
}
